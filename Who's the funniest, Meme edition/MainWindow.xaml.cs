using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Reflection;
using System.Reflection.Emit;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Who_s_the_funniest.classe;
using Who_s_the_funniest.classe.msg;
using Who_s_the_funniest.extension;
using Who_s_the_funniest.uc;
using Who_s_the_funniest__Meme_edition.uc;
using zck_client;
using Label = System.Windows.Controls.Label;

namespace Who_s_the_funniest__Meme_edition
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Server
        internal static ZoneckClient ZoneckClient;

        // Joueur
        internal static string Username;

        // Créé une partie
        int NbreJoueur = 8;
        int NbreTour = 3;
        Random Rdn = new Random();
        const int MIN_PLAYER_TO_START = 2;

        // Trouver une partie
        Party Party;

        // Game
        Timer GameTimer;
        List<List<Mème>> MemeOfPeopleInGame = new List<List<Mème>>();
        private int Round = 1;

        // Créer mème
        BrushConverter Converter = new System.Windows.Media.BrushConverter();

        public MainWindow()
        {
            InitializeComponent();

            Label_ConnexionServer.Visibility = Visibility.Visible;
            Grid_LogIn.Visibility = Visibility.Visible;
            Grid_CreateNewGame.Visibility = Visibility.Hidden;
            Grid_GameJoined.Visibility = Visibility.Hidden;
            Grid_GameSearcher.Visibility = Visibility.Visible;
            Grid_Game.Visibility = Visibility.Hidden;
            Grid_Wait.Visibility = Visibility.Hidden;
            Grid_Vote.Visibility = Visibility.Hidden;
            Grid_Leaderboard.Visibility = Visibility.Hidden;
            Grid_ClassementFinaux.Visibility = Visibility.Hidden;

            TextBox_Username.Text = Who_s_the_funniest.Properties.Settings.Default.Username;

            //GameStart();
        }

        /// <summary>
        /// Fenêtre initialisée
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Window_ContentRendered(object sender, EventArgs e)
        {
            // Server
            try
            {
                this.DataContext = new AnimationExtension();

                ZoneckClient = new ZoneckClient("Who_s the funniest", "127.0.0.1", 30_000, MessageRecieved);
                while (ZoneckClient.MyId == null)
                {
                    await Task.Delay(250);
                }

                Label_ConnexionServer.Visibility = Visibility.Hidden;
                ((AnimationExtension)this.DataContext).StopAnimation();
            }
            catch
            {
                MessageBox.Show("Aucun serveur trouvé.\nVeuillez le télécharger sur mon GitHub : \ngithub.com/zonetecde/zoneck-server/releases");
                this.Close();
            }
        }

        #region Game manager 

        /// <summary>
        /// Enlève le joueur x des games
        /// </summary>
        /// <param name="playerIdToRemove"></param>
        private void RemoveSomeoneFromTheGameWhereHeIs(string playerIdToRemove)
        {
            for (int i1 = 0; i1 < StackPanel_party.Children.Count; i1++)
            {
                object? party = StackPanel_party.Children[i1];
                if (party is UC_party)
                {
                    if (((UC_party)party).Party.Players.Any(x => x.Id == playerIdToRemove))
                    {
                        string usernameOfTheOneWhoLeft = ((UC_party)party).Party.Players.FirstOrDefault(x => x.Id == playerIdToRemove)!.Username;

                        ((UC_party)party).SomeoneLeft(((UC_party)party).Party.Players.FirstOrDefault(x => x.Id == playerIdToRemove)!);
                        if (((UC_party)party).Visibility == Visibility.Collapsed)
                            StackPanel_party.Children.Remove(((UC_party)party));

                        // Si il s'est enlevé de la game où on est actuellement
                        if (Party != null)
                        {
                            if (Party.Id == ((UC_party)party).Party.Id)
                            {
                                StackPanel_Conversation.Children.Add(new UC_Message(serverMessage: usernameOfTheOneWhoLeft + " a quitté la partie"));

                                for (int i = 0; i < StackPanel_PlayerInGame.Children.Count; i++)
                                {
                                    UC_PlayerInGame uC_PlayerInGame = (UC_PlayerInGame)StackPanel_PlayerInGame.Children[i];
                                    bool wasOwner = uC_PlayerInGame.IsOwner;

                                    if (uC_PlayerInGame.Player.Id == playerIdToRemove)
                                        StackPanel_PlayerInGame.Children.Remove(uC_PlayerInGame);

                                    if (wasOwner)
                                    {
                                        // Si c'est l'owner, l'owner passe à la prmière personne après
                                        ((UC_PlayerInGame)StackPanel_PlayerInGame.Children[0]).IsOwner = true;
                                        ((UC_PlayerInGame)StackPanel_PlayerInGame.Children[0]).Image_Owner.Visibility = Visibility.Visible;
                                        // Si c'est nous qui devient owner
                                        if (((UC_PlayerInGame)StackPanel_PlayerInGame.Children[0]).Player.Id == ZoneckClient.MyId)
                                        {
                                            Button_StartGame.Visibility = Visibility.Visible;
                                        }
                                    }
                                }

                                if (Party.Players.Count >= MIN_PLAYER_TO_START)
                                    Button_StartGame.IsEnabled = true;
                                else
                                    Button_StartGame.IsEnabled = false;

                        
                            }
                        }
                    }
                }
            }

            if(Party != null)
                if (Party.Players.Exists(x => x.Id == playerIdToRemove))
                {
                    // Si on est en game, en train de faire les mèmes et que la personne se déco on vérifie que peut être il y a tous le monde qui a envoyé leur mème
                    if (Grid_MemeMaker.Visibility == Visibility.Visible || (Label_WaitMemeOfOtherCounter.Visibility == Visibility.Visible && Grid_Vote.Visibility == Visibility.Visible))
                    {
                        Party.Players.RemoveAll(x => x.Id == playerIdToRemove);
                        MemeOfPeopleInGame[Round - 1].RemoveAll(x => x.FromId == playerIdToRemove);

                        // Si on a reçu tous les mèmes ont peut commencer le vote
                        if (MemeOfPeopleInGame[Round - 1].Count == Party.Players.Count)
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                StartVoting();
                            });
                        }
                        else
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                Run_MemeRemainingBeforeStartingVote.Text = MemeOfPeopleInGame[Round - 1].Count + "/" + Party.Players.Count;
                            });
                        }
                    }
                }
        }

        /// <summary>
        /// Veut créé une game
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Image_CreateGame_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Grid_CreateNewGame.Visibility = Visibility.Visible;
            TextBox_GameName.Text = "Partie de " + Username;
        }

        /// <summary>
        /// Entre dans le jeu
        /// </summary>
        private void Button_LogIn_Click(object sender, RoutedEventArgs e)
        {
            Regex regex = new Regex(@"^\w+$");

            if (!String.IsNullOrEmpty(TextBox_Username.Text) && regex.IsMatch(TextBox_Username.Text))
            {
                Grid_LogIn.Visibility = Visibility.Hidden;
                Username = TextBox_Username.Text;
                // Demande les games existantes pour pouvoir en rejoindre une
                ZoneckClient.Send(JsonConvert.SerializeObject(new WhosTheFunniestMessage(MessageType.SEND_ME_GAMES, ZoneckClient.MyId)));

                Who_s_the_funniest.Properties.Settings.Default.Username = TextBox_Username.Text;
                Who_s_the_funniest.Properties.Settings.Default.Save();
            }
        }

        /// <summary>
        /// Valide le pseudo
        /// </summary>
        private void TextBox_UserName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                Button_LogIn_Click(this, null!);
        }

        /// <summary>
        /// Sélectionne un nbre de joueur
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Label_NbrePlayer_X_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                foreach (Label label in StackPanel_nbrePlayer.Children)
                {
                    label.BorderBrush = Brushes.Black;
                }

                ((Label)sender).BorderBrush = Brushes.Red;

                if (((Label)sender).Content.ToString() != "*")
                    NbreJoueur = Convert.ToInt32(((Label)sender).Content);
                else
                    NbreJoueur = 99;
            }
        }

        /// <summary>
        /// Sélectionne un nombre de tour
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Label_NbreTour_X_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                foreach (Label label in StackPanel_nbreTour.Children)
                {
                    label.BorderBrush = Brushes.Black;
                }

                ((Label)sender).BorderBrush = Brushes.Red;

                NbreTour = Convert.ToInt32(((Label)sender).Content);
            }
        }

        /// <summary>
        /// Créé une partie et l'envois aux autres
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_CreateGame_Click(object sender, RoutedEventArgs e)
        {
            if(!String.IsNullOrEmpty(TextBox_GameName.Text))
            {
                // créé la partie
                Party party = new Party(TextBox_GameName.Text, (byte)NbreJoueur, ComboBox_Lang.Text, new List<Player>() { new Player(Username, ZoneckClient.MyId) }, Rdn.Next(int.MinValue, int.MaxValue), NbreTour);
                Party = party;
                StackPanel_party.Children.Add(new uc.UC_party(party, GameJoined));
                GameJoined(Party); // On la rejoint
                // Puisqu'on est host, on laisse le choix de commencer la game
                Button_StartGame.Visibility = Visibility.Visible;

                if(MIN_PLAYER_TO_START > 1)
                    Button_StartGame.IsEnabled = false;

                // Envois la game aux autres
                ZoneckClient.Send(JsonConvert.SerializeObject(new WhosTheFunniestMessage(MessageType.NEW_PARTY, JsonConvert.SerializeObject(party))));

                Grid_CreateNewGame.Visibility = Visibility.Hidden;
                UC_NoGame.Visibility = StackPanel_party.Children.Count > 2 ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        /// <summary>
        /// Annule la création d'une game
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Image_Close_CreateNewGame_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Grid_CreateNewGame.Visibility = Visibility.Hidden;
        }

        /// <summary>
        /// On rejoint une partie
        /// </summary>
        /// <param name="party">La game rejoint</param>
        internal void GameJoined(Party party)
        {
            Party = party;
            Button_StartGame.Visibility = Visibility.Hidden;
            Grid_GameJoined.Visibility = Visibility.Visible;
            label_NomGame.Content = party.Nom;
            StackPanel_PlayerInGame.Children.Clear();

            // Ajoute les noms des gens de la game
            foreach(Player player in Party.Players)
            {
                StackPanel_PlayerInGame.Children.Add(new UC_PlayerInGame(player, player == Party.Players[0])); 
            }

            // si la game est pleine 
            if (Party.Players.Count == Party.NbreJoueurMax)
            {
                // Game start
                StartGameVisual(Party.Id.ToString());
            }

            image_conversation.Visibility = Visibility.Visible;
            Label_newMess.Visibility = Visibility.Visible;

            if(StackPanel_Conversation.Children.Count > 0)
                StackPanel_Conversation.Children.Add(new UC_Message(serverMessage:"\n"));
            StackPanel_Conversation.Children.Add(new UC_Message(serverMessage: Username + " a rejoint la partie"));
        }

        /// <summary>
        /// Quitte la game
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_QuitterGame_Click(object sender, RoutedEventArgs e)
        {
            ZoneckClient.Send(JsonConvert.SerializeObject(new WhosTheFunniestMessage(MessageType.GAME_LEFT, ZoneckClient.MyId)));
            Grid_GameJoined.Visibility = Visibility.Hidden;

            // s'enlève de la game
            // si la game est vide (0 player) alors on l'enlève carrément (dans .SomeoneLeft)
            for (int i = 0; i < StackPanel_party.Children.Count; i++)
            {
                UIElement ui = (UIElement)StackPanel_party.Children[i];
                if (ui is UC_party)
                {
                    if (((UC_party)ui).Party.Id == Party.Id)
                    {
                        ((UC_party)ui).SomeoneLeft(new Player(Username, ZoneckClient.MyId));

                        if (((UC_party)ui).Visibility == Visibility.Collapsed)
                            StackPanel_party.Children.Remove(((UC_party)ui));


                    }
                }
            }

            Party = null;
            image_conversation.Visibility = Visibility.Collapsed;
            Border_Conversation.Visibility = Visibility.Collapsed;
            Label_newMess.Visibility = Visibility.Collapsed;

            UC_NoGame.Visibility = StackPanel_party.Children.Count > 2 ? Visibility.Collapsed : Visibility.Visible;
        }

        #endregion


        /// <summary>
        /// Commence la game
        /// </summary>
        private void Button_StartGame_Click(object sender, RoutedEventArgs e)
        {
            // l'enlève de la liste des games
            for (int i = 0; i < StackPanel_party.Children.Count; i++)
            {
                UIElement ui = (UIElement)StackPanel_party.Children[i];

                if (ui is UC_party)
                {
                    if(((UC_party)ui).Party.Id == Party.Id)
                    {
                        // Enlève la game
                        StackPanel_party.Children.Remove(ui);
                        break;
                    }
                }
            }

            // Préviens les autres que la game à commencer
            ZoneckClient.Send(JsonConvert.SerializeObject(new WhosTheFunniestMessage(MessageType.GAME_START, Party.Id.ToString())));
            
            GameStart(true);
            Round = 1;
        }

        /// <summary>
        /// Message reçu du serveur
        /// </summary>
        /// <param name="obj"></param>
        private void MessageRecieved(ClassLibrary.Message obj)
        {
            if (obj.MessageType == ClassLibrary.MESSAGE_TYPE.MESSAGE)
            {
                WhosTheFunniestMessage wtfM = JsonConvert.DeserializeObject<WhosTheFunniestMessage>(obj.Content)!;

                #region Game finder

                if (wtfM.MsgType == MessageType.NEW_PARTY)
                {
                    // Nouvelle partie créé, l'ajoute au pannel de partie
                    Party p = JsonConvert.DeserializeObject<Party>(wtfM.Content)!;

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        StackPanel_party.Children.Add(new uc.UC_party(p, GameJoined));
                        UC_NoGame.Visibility = StackPanel_party.Children.Count > 2 ? Visibility.Collapsed : Visibility.Visible;
                    });
                }
                else if (wtfM.MsgType == MessageType.JOIN_PARTY)
                {
                    Join j = JsonConvert.DeserializeObject<Join>(wtfM.Content)!;

                    // Quelqu'un s'est ajouté à une game
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        foreach (var uc_Party in StackPanel_party.Children)
                        {
                            if (uc_Party is UC_party)
                            {
                                if (((UC_party)uc_Party).Party.Id == j.PartyId)
                                {
                                    ((UC_party)uc_Party).AddNewPlayer(j.Username, j.Id);

                                    // si la game est pleine 
                                    if (((UC_party)uc_Party).Party.Players.Count == ((UC_party)uc_Party).Party.NbreJoueurMax)
                                    {
                                        // Game start
                                        StartGameVisual(((UC_party)uc_Party).Party.Id.ToString());
                                    }
                                }
                            }
                        }

                        // Si il s'est ajouté à la game où on est actuellement
                        if (Party != null)
                        {
                            if (j.PartyId == Party.Id && Grid_GameJoined.Visibility == Visibility.Visible)
                            {
                                StackPanel_PlayerInGame.Children.Add(new UC_PlayerInGame(new Player(j.Username, j.Id), false));
                                StackPanel_Conversation.Children.Add(new UC_Message(serverMessage: j.Username + " a rejoint la partie"));

                                if (Party.Players.Count >= MIN_PLAYER_TO_START)
                                    Button_StartGame.IsEnabled = true;
                                else
                                    Button_StartGame.IsEnabled = false;
                            }
                        }
                    });
                }
                else if (wtfM.MsgType == MessageType.SEND_ME_GAMES)
                {
                    // Quelqu'un vient de se connecter et veut qu'on lui envois les games
                    List<Party> parties = new List<Party>();

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        foreach (var party in StackPanel_party.Children)
                        {
                            if (party is UC_party)
                            {
                                parties.Add(((UC_party)party).Party);
                            }
                        }
                    });

                    // Envois à la personne les games
                    ZoneckClient.Send(JsonConvert.SerializeObject(new WhosTheFunniestMessage(MessageType.HERE_IS_THE_GAMES, JsonConvert.SerializeObject(parties))), wtfM.Content);
                }
                else if (wtfM.MsgType == MessageType.HERE_IS_THE_GAMES)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        // On a reçu les games de quelqu'un après leur avoir demandé
                        if (StackPanel_party.Children.Count == 2)
                        {

                            JsonConvert.DeserializeObject<List<Party>>(wtfM.Content)!.ForEach(x =>
                            {
                                StackPanel_party.Children.Add(new UC_party(x, GameJoined));
                            });

                            UC_NoGame.Visibility = StackPanel_party.Children.Count > 2 ? Visibility.Collapsed : Visibility.Visible;
                        }
                    });

                }
                else if (wtfM.MsgType == MessageType.GAME_LEFT)
                {
                    // La personne d'id wtfM.Content a quitté la game dans laquelle elle est
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        RemoveSomeoneFromTheGameWhereHeIs(wtfM.Content);

                        UC_NoGame.Visibility = StackPanel_party.Children.Count > 2 ? Visibility.Collapsed : Visibility.Visible;

                    });
                }
                else if (wtfM.MsgType == MessageType.GAME_START)
                {
                    // la game qui a start son Id est dans .Content
                    // l'enlève de la liste des games
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        StartGameVisual(wtfM.Content);

                        UC_NoGame.Visibility = StackPanel_party.Children.Count > 2 ? Visibility.Collapsed : Visibility.Visible;

                    });
                }
                else if (wtfM.MsgType == MessageType.MESSAGE)
                {
                    // message
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        StackPanel_Conversation.Children.Add(new UC_Message(wtfM.Content, Party.Players.First(x => x.Id == obj.Id).Username));
                        ScrollViewer_message.ScrollToBottom();

                        if (Border_Conversation.Visibility != Visibility.Visible)
                            Label_newMess.Content = Convert.ToInt16(Label_newMess.Content) + 1;
                    });
                }
                #endregion

                else if (wtfM.MsgType == MessageType.MY_MEME)
                {
                    // Quelqu'un a envoyé son mème
                    MemeOfPeopleInGame[Round - 1].Add(JsonConvert.DeserializeObject<Mème>(wtfM.Content));

                    // Si on a reçu tous les mèmes ont peut commencer le vote
                    if (MemeOfPeopleInGame[Round - 1].Count == Party.Players.Count)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            StartVoting();
                        });
                    }
                    else
                    {
                        // On incrémente le nombre de personne qui ont donné leur mème
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            Label_WaitMemeOfOtherCounter.Visibility = Visibility.Visible;
                            Run_MemeRemainingBeforeStartingVote.Text = MemeOfPeopleInGame[Round - 1].Count + "/" + Party.Players.Count;
                        });
                    }
                }
                else if (wtfM.MsgType == MessageType.MY_VOTE)
                {
                    // Id de la personne qui a fait le mème,note
                    var meme = MemeOfPeopleInGame[Round - 1].FirstOrDefault(x => x.FromId == wtfM.Content.Split(',')[0]);
                    if(meme != default)
                    {
                        // Ajoute la note au mème
                        meme.Notes.Add(Convert.ToInt32(wtfM.Content.Split(',')[1]));
                    }
                }
            }

            // Une personne s'est déconnecté, on la supp de toutes les games
            else if (obj.MessageType == ClassLibrary.MESSAGE_TYPE.DISCONNECTION)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    RemoveSomeoneFromTheGameWhereHeIs(obj.Id);
                });
            }
        }

        /// <summary>
        /// Start la game qui a pour id gameId. Si on est dans la game, on commence à jouer.
        /// </summary>
        /// <param name="gameId"></param>
        private void StartGameVisual(string gameId)
        {
            for (int i = 0; i < StackPanel_party.Children.Count; i++)
            {
                UIElement ui = (UIElement)StackPanel_party.Children[i];
                if (ui is UC_party)
                {
                    if (((UC_party)ui).Party.Id.ToString() == gameId)
                    {
                        // Enlève la game
                        StackPanel_party.Children.Remove(ui);
                        break;
                    }
                }
            }

            // Si c'est notre game qui à commencé
            if (Party.Id.ToString() == gameId)
            {
                Round = 1;
                GameStart(true);
            }
        }

        /// <summary>
        /// Jeu qui commence
        /// </summary>
        private void GameStart(bool premierLancementGame)
        {
            Grid_GameJoined.Visibility = Visibility.Hidden;
            Grid_GameSearcher.Visibility = Visibility.Hidden;
            Grid_Game.Visibility = Visibility.Visible;
            Grid_Vote.Visibility = Visibility.Hidden;
            Grid_Vote.Visibility = Visibility.Hidden;

            Button_ChangeMeme.Content = "Changer de mème (3)";
            Button_ChangeMeme.Tag = 3;
            Button_ChangeMeme.IsEnabled = true;

            // si c'est le premier lancement de la game on initialise MemeOfPeopleInGame (sinon c'est que c'est une nouvelle round uniquement)
            if(premierLancementGame)
            {
                MemeOfPeopleInGame = new List<List<Mème>>();
                for (int i = 0; i < Party.NbreRound; i++)
                {
                    MemeOfPeopleInGame.Add(new List<Mème>());
                }
            }


            label_timer.Content = "90";
            int compteur = 90;
            GameTimer = new Timer(1000);
            GameTimer.Elapsed += (sender, e) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    compteur--;
                    label_timer.Content = compteur;

                    // round finit
                    if (compteur <= 0)
                    {
                        // Si pas encore validé son mème:
                        if(Grid_MemeMaker.Visibility == Visibility.Visible)
                        {
                            Button_ValideMeme_Click(this, null);
                        }

                        GameTimer.Stop();
                    }
                });
            };
            GameTimer.Start();

            ShowRandomMeme();
        }

        /// <summary>
        /// Affiche un meme random à custom
        /// </summary>
        private void ShowRandomMeme()
        {
            // Il commence tous à faire un meme aléatoire
            Grid_MemeMaker.Visibility = Visibility.Visible;
            Canvas_meme.Children.RemoveRange(2, Canvas_meme.Children.Count - 2); // 1 = l'image son donc on l'enlève jamais, 2 = label "chargement" on l'enleve jamais
            Image_Sound.Visibility = Visibility.Hidden;

            textBlock_textIndication.Text = "Pour ajouter du texte au mème, tracer une zone de texte en maintenant le clique gauche de la souris. Fait clique-droit pour annuler votre tracer, ou relâcher le clique gauche pour commencer à écrire dans votre zone de texte.";
            textBlock_textIndication.Visibility = Visibility.Visible;
            Grid_CustomTexte.Visibility = Visibility.Collapsed;

            // Prend une image/vidéo aléatoire
            bool isVideo = Rdn.Next(0, 100) > 50;

            if (!isVideo)
            {
                bool linkIsDead = false;

                do
                {
                    // Image random
                    string randomLine = File.ReadAllLines(@"meme\memes.txt")[Rdn.Next(File.ReadAllLines(@"meme\memes.txt").Length)];
                    string url = randomLine.Split('|')[1];

                    // Check si l'image existe
                    HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
                    request.Method = "HEAD";

                    try
                    {
                        request.GetResponse();
                        linkIsDead = false;
                    }
                    catch
                    {
                        linkIsDead = true;
                    }

                    if (!linkIsDead)
                    {
                        Canvas_meme.Tag = randomLine; // En tag le meme

                        Label_titreMeme.Content = randomLine.Split('|')[0].Replace(" ", "  ");

                        Image img = new Image();
                        var bitmapImage = new BitmapImage();
                        bitmapImage.BeginInit();
                        bitmapImage.UriSource = new Uri(url);
                        bitmapImage.EndInit();

                        img.Source = bitmapImage;
                        img.Width = Canvas_meme.Width;
                        img.Height = Canvas_meme.Height;
                        Canvas_meme.Children.Add(img);
                    }
                } while (linkIsDead);
            }
            else
            {
                bool linkIsDead = false;

                do
                {
                    // Image random
                    string randomLine = File.ReadAllLines(@"meme\memes_gif.txt")[Rdn.Next(File.ReadAllLines(@"meme\memes_gif.txt").Length)];
                    string url = randomLine.Split('|')[1];

                    // Check si l'image existe
                    HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
                    request.Method = "HEAD";

                    try
                    {
                        request.GetResponse();
                        linkIsDead = false;
                    }
                    catch
                    {
                        linkIsDead = true;
                    }

                    if (!linkIsDead)
                    {
                        Canvas_meme.Tag = randomLine; // En tag le meme

                        Image_Sound.Visibility = Visibility.Visible;
                        Label_titreMeme.Content = randomLine.Split('|')[0].Replace(" ", "  ");

                        MediaElement video = new MediaElement();
                        video.Volume = 0.25;
                        video.LoadedBehavior = MediaState.Manual;
                        bool firstLoop = true;

                        video.MediaEnded += (sender, e) =>
                        {
                            // boucle infini, la deuxieme fois sans le son
                            video.Position = new TimeSpan(0, 0, 1);
                            video.Play();

                            // enleve le son icone
                            if (firstLoop)
                            {
                                video.Volume = 0;
                                Image_Sound.Source = FindResource("assets.sound_off.png");
                                Image_Sound.Tag = "Off";
                                firstLoop = false;
                            }
                        };

                        video.Source = new Uri(url, UriKind.RelativeOrAbsolute); ;
                        video.Width = Canvas_meme.Width;
                        video.Height = Canvas_meme.Height;
                        Canvas_meme.Children.Add(video);
                        video.Play();

                        // met Image_Sound au premier plan
                        Canvas.SetZIndex(Image_Sound, 500);

                        Image_Sound.Source = FindResource("assets.sound_on.png");
                        Image_Sound.Tag = "On";
                    }
                } while (linkIsDead);

            }

        }

        private BitmapImage FindResource(string resName)
        {
            var img = new BitmapImage();
            img.BeginInit();
            img.StreamSource = Assembly.GetEntryAssembly()!.GetManifestResourceStream("Who_s_the_funniest." + resName);
            img.EndInit();

            return img;
        }

        /// <summary>
        /// Désactive/Active le son du mediaElement dans le Canvas
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Image_Sound_MouseDown(object sender, MouseButtonEventArgs e)
        {

            if (e.LeftButton == MouseButtonState.Pressed)
            {


                // Switch le son en on/off
                if (Image_Sound.Tag.ToString() == "On")
                {
                    Image_Sound.Tag = "Off";
                    Image_Sound.Source = FindResource("assets.sound_off.png");
                }
                else
                {
                    Image_Sound.Tag = "On";
                    Image_Sound.Source = FindResource("assets.sound_on.png");
                }

                foreach (UIElement ui in Canvas_meme.Children)
                {
                    if (ui is MediaElement)
                    {
                        ((MediaElement)ui).Volume = Image_Sound.Tag.ToString() == "On" ? 0.5 : 0;
                    }
                }
            }
        }

        /// <summary>
        /// Change de mème
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_ChangeMeme_Click(object sender, RoutedEventArgs e)
        {
            int essaie = (int)Button_ChangeMeme.Tag - 1;
            if (essaie == 0)
                Button_ChangeMeme.IsEnabled = false;
            Button_ChangeMeme.Content = "Changer de mème (" + essaie + ")";
            ShowRandomMeme();
            Button_ChangeMeme.Tag = (int)Button_ChangeMeme.Tag - 1;
        }

        bool isDrawingBorder = false;
        Border? drawingBorder;
        Point startingPoint;

        /// <summary>
        /// Customise le mème
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Canvas_meme_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.Source != Image_Sound && e.LeftButton == MouseButtonState.Pressed)
            {
                // Commence la traçage de bordure
                isDrawingBorder = true;

                drawingBorder = new Border()
                {
                    Width = 0,
                    Height = 0,
                    BorderBrush = Brushes.Black,
                    BorderThickness = new Thickness(2)
                };

                var pos = Mouse.GetPosition(Canvas_meme);
                startingPoint = pos;
                Canvas_meme.Children.Add(drawingBorder);
                Canvas.SetLeft(drawingBorder, pos.X);
                Canvas.SetTop(drawingBorder, pos.Y);
            }
        }

        /// <summary>
        /// Dessine
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Canvas_meme_MouseMove(object sender, MouseEventArgs e)
        {
            if(isDrawingBorder && e.LeftButton == MouseButtonState.Pressed)
            {
                var pos = Mouse.GetPosition(Canvas_meme);

                double x = Math.Min(pos.X, startingPoint.X);
                double y = Math.Min(pos.Y, startingPoint.Y);
                double width = Math.Abs(pos.X - startingPoint.X);
                double height = Math.Abs(pos.Y - startingPoint.Y);

                Canvas.SetLeft(drawingBorder, x);
                Canvas.SetTop(drawingBorder, y);
                drawingBorder.Width = width;
                drawingBorder.Height = height;               
            }
        }

        /// <summary>
        /// Annule le drawing en cours
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Canvas_meme_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Annule le rec
            if (isDrawingBorder)
            {
                isDrawingBorder = false;
                Canvas_meme.Children.Remove(drawingBorder);
            }
        }

        /// <summary>
        /// Trace la textBox dessiné
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Canvas_meme_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (isDrawingBorder)
            {
                if (drawingBorder.Width > 15 && drawingBorder.Height > 15)
                {

                    isDrawingBorder = false;

                    // trace la textbox

                    // Ajoute une zone de texte 
                    #region Ajout de la zone de texte

                    Viewbox vb = new Viewbox()
                    {
                        Width = drawingBorder.Width,
                        Height = drawingBorder.Height,
                    };

                    TextBox zone_de_texte = new TextBox()
                    {
                        VerticalContentAlignment = VerticalAlignment.Center,
                        HorizontalContentAlignment = HorizontalAlignment.Center,

                        Background = Label_impact_font.Background,
                        BorderBrush = Brushes.Transparent,
                        Foreground = Brushes.White,
                        TextWrapping = TextWrapping.Wrap,
                        AcceptsReturn = true,
                        FontFamily = Label_impact_font.FontFamily,
                        FontSize = 40,
                        SelectionBrush = Brushes.Transparent,
                        Cursor = Cursors.Arrow
                    };

                    vb.Child = zone_de_texte;

                    #endregion

                    // Créé un contexte menue pour pouvoir supprimer la textBox
                    #region Context Menu
                    ContextMenu cm = new ContextMenu();
                    MenuItem mI = new MenuItem()
                    {
                        Header = "Supprimer"
                    };
                    mI.Click += (sender, e) =>
                    {
                        Canvas_meme.Children.Remove(vb);
                        Grid_CustomTexte.Visibility = Visibility.Collapsed;
                        Grid_CustomTexte.Tag = null;
                        textBlock_textIndication.Text = "Cliquer sur une zone de texte pour la customiser.";
                        textBlock_textIndication.Visibility = Visibility.Visible;
                    };

                    cm.Items.Add(mI);

                    zone_de_texte.ContextMenu = cm;
                    #endregion

                    Canvas_meme.Children.Add(vb);
                    Canvas.SetLeft(vb, Canvas.GetLeft(drawingBorder));
                    Canvas.SetTop(vb, Canvas.GetTop(drawingBorder));

                    zone_de_texte.Focus();
                    Grid_CustomTexte.Visibility = Visibility.Visible;
                    Grid_CustomTexte.Tag = vb;
                    textBlock_textIndication.Visibility = Visibility.Collapsed;

                    // Enlève le dessin de la bordure
                    Canvas_meme.Children.Remove(drawingBorder);

                    // Lorsque la textBox à le focus, il y a des options qui s'affiche à droite
                    zone_de_texte.GotFocus += (sender, e) =>
                    {
                        Grid_CustomTexte.Visibility = Visibility.Visible;
                        Grid_CustomTexte.Tag = vb;
                        textBlock_textIndication.Visibility = Visibility.Collapsed;
                    };
                    zone_de_texte.TextChanged += (sender, e) =>
                    {
                        textBox_texte.Text = zone_de_texte.Text;
                    };
                }
                else
                {
                    isDrawingBorder = false;
                    Canvas_meme.Children.Remove(drawingBorder);
                }
            }
        }

        /// <summary>
        /// On relache la souris mais en dehors du canvas
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (isDrawingBorder)
            {
                Canvas_meme_MouseLeftButtonUp(this, null);
            }
        }

        /// <summary>
        /// Change la couleur du texte
        /// </summary>
        private void SquarePicker_couleurTexte_ColorChanged(object sender, RoutedEventArgs e)
        {
            if(Grid_CustomTexte.Tag != null)
            ((TextBox)((Viewbox)Grid_CustomTexte.Tag).Child).Foreground = new SolidColorBrush(SquarePicker_couleurTexte.SelectedColor);
        }

        /// <summary>
        /// Change la couleur de fond
        /// </summary>
        private void squarePicker_couleurFond_ColorChanged(object sender, RoutedEventArgs e)
        {
            if (Grid_CustomTexte.Tag != null)
            {
                var c = squarePicker_couleurFond.SelectedColor;
                var c_temp = AlphaSlider_background.SelectedColor;
                c.A = c_temp.A;
                ((TextBox)((Viewbox)Grid_CustomTexte.Tag).Child).Background = new SolidColorBrush(c);
            }
        }

        /// <summary>
        /// Supprime la zone de texte sélectionner du mème
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_DeleteZoneDeTexte_Click(object sender, RoutedEventArgs e)
        {
            if (Grid_CustomTexte.Tag != null)
            {
                Canvas_meme.Children.Remove(((Viewbox)Grid_CustomTexte.Tag));
                Grid_CustomTexte.Visibility = Visibility.Collapsed;
                Grid_CustomTexte.Tag = null;
                textBlock_textIndication.Text = "Cliquer sur une zone de texte pour la customiser.";
                textBlock_textIndication.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// Change le texte
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textBox_texte_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (Grid_CustomTexte.Tag != null)
            {
                ((TextBox)((Viewbox)Grid_CustomTexte.Tag).Child).Text = textBox_texte.Text;
            }
        }

        /// <summary>
        /// Change l'opacité du background
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AlphaSlider_ColorChanged(object sender, RoutedEventArgs e)
        {
            if (Grid_CustomTexte.Tag != null)
            {
                var c = squarePicker_couleurFond.SelectedColor;
                var c_temp = AlphaSlider_background.SelectedColor;
                c.A = c_temp.A;
                ((TextBox)((Viewbox)Grid_CustomTexte.Tag).Child).Background = new SolidColorBrush(c);
            }
        }

        /// <summary>
        /// Valide le mème, l'envois aux autres
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_ValideMeme_Click(object sender, RoutedEventArgs e)
        {
            // Convertis le mème en texte pour que les autres puisses le lire
            string nom_et_url = Canvas_meme.Tag.ToString(); // nom et url du meme séparé par un |
            List<ZoneTexte> ZoneDeTexte = new List<ZoneTexte>();
            foreach(UIElement ui in Canvas_meme.Children)
            {
                if(ui is Viewbox)
                {
                    var vb = (Viewbox)ui;
                    var txt = (TextBox)vb.Child;
                    // ajoute le texte dans le format
                    // pos L, pos T, w, h, texte, foreground, background
                    ZoneDeTexte.Add(new ZoneTexte(Canvas.GetLeft(vb), Canvas.GetTop(vb), vb.ActualWidth, vb.ActualHeight, txt.Text, txt.Foreground.ToString(), txt.Background.ToString()));
                }
            }

            // Il faut au minimum une zone de texte
            if (ZoneDeTexte.Any() || label_timer.Content.ToString() == "0") // ||sender == null : le mème a été forcé à être envoyé (countdown à 0)
            {
                Mème m = new Mème(nom_et_url.Split('|')[0], nom_et_url.Split('|')[1], ZoneDeTexte, ZoneckClient.MyId, Username);
                MemeOfPeopleInGame[Round - 1].Add(m);

                // supprime notre mème
                Canvas_meme.Children.RemoveRange(2, Canvas_meme.Children.Count - 2); // 1 = l'image son donc on l'enlève jamais, 2 = label "chargement" on l'enleve jamais

                // Envois le mème au joueur de notre game
                foreach (Player player in Party.Players)
                {
                    if (player.Id != ZoneckClient.MyId)
                    {
                        ZoneckClient.Send(JsonConvert.SerializeObject(new WhosTheFunniestMessage(MessageType.MY_MEME, JsonConvert.SerializeObject(m))), player.Id);
                    }
                }


                // Si on a reçu tous les mèmes ont peut commencer le vote
                if (MemeOfPeopleInGame[Round - 1].Count == Party.Players.Count)
                {
                    StartVoting();                 
                }
                else
                {
                    // On attend que tout le monde envois leur mème
                    Grid_Vote.Visibility = Visibility.Visible;                    
                    Grid_MemeMaker.Visibility = Visibility.Hidden;                    
                    ShowMeme(Grid_MemeVote, m);
                    StackPanel_note.Visibility = Visibility.Hidden;
                    Label_WaitMemeOfOtherCounter.Visibility = Visibility.Visible;
                    Run_MemeRemainingBeforeStartingVote.Text = MemeOfPeopleInGame[Round - 1].Count + "/" + Party.Players.Count;
                }
            }
            else
            {
                textBlock_textIndication.Text = "Il faut au minimum ajouter une zone de texte pour pouvoir envoyer votre mème!";
            }
        }

        /// <summary>
        /// Affiche le mème
        /// </summary>
        /// <param name="border_MyMeme"></param>
        /// <param name="m"></param>
        internal void ShowMeme(Panel panel, Mème m, double volume = 0.25)
        {
            panel.Children.Clear();

            Viewbox viewbox = new Viewbox();
            Border Border_Meme = new Border()
            {
                CornerRadius = new CornerRadius(50),
                Background = Brushes.White,
                BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFromString("#FF1C2638"),
                BorderThickness = new Thickness(3)
            };

            viewbox.Child = Border_Meme;

            Canvas canvas = new Canvas()
            {
                Background = Brushes.White,
                Width = Canvas_meme.Width,
                Height = Canvas_meme.Height,
                Margin = new Thickness(20),
            };

            Border_Meme.Child = canvas;
            panel.Children.Add(viewbox);

            // Image/Vidéo du mème
            if (m.Url.Contains(".mp4"))
            {
                MediaElement video = new MediaElement();
                video.LoadedBehavior = MediaState.Manual;

                video.Volume = volume;
                int loopNumber = 0;
                video.MediaEnded += (sender, e) =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        // boucle infini
                        video.Position = new TimeSpan(0, 0, 1);
                        video.Play();
                        loopNumber++;
                    });

                };

                video.Source = new Uri(m.Url, UriKind.RelativeOrAbsolute);
                video.Width = ((Canvas)Border_Meme.Child).Width;
                video.Height = ((Canvas)Border_Meme.Child).Height;
                ((Canvas)Border_Meme.Child).Children.Add(video);
                video.Play();
            }
            else
            {
                Image img = new Image();
                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.UriSource = new Uri(m.Url);
                bitmapImage.EndInit();

                img.Source = bitmapImage;
                img.Width = ((Canvas)Border_Meme.Child).Width;
                img.Height = ((Canvas)Border_Meme.Child).Height;
                ((Canvas)Border_Meme.Child).Children.Add(img);
            }

            foreach (ZoneTexte zt in m.Textes)
            {
                Viewbox vb = new Viewbox()
                {
                    Width = zt.Width,
                    Height = zt.Height,
                };

                TextBox zone_de_texte = new TextBox()
                {
                    VerticalContentAlignment = VerticalAlignment.Center,
                    HorizontalContentAlignment = HorizontalAlignment.Center,

                    Background = (SolidColorBrush)new BrushConverter().ConvertFromString(zt.Background), // Une erreur qui m'a pris 3h à résoudre =) on ne peut pas sérialize un Brush.
                    BorderBrush = Brushes.Transparent,
                    Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString(zt.Foreground),
                    TextWrapping = TextWrapping.Wrap,
                    AcceptsReturn = true,
                    FontFamily = Label_impact_font.FontFamily,
                    FontSize = 40,
                    SelectionBrush = Brushes.Transparent,
                    IsReadOnly = true,
                    Text = zt.Texte
                };

                vb.Child = zone_de_texte;

                ((Canvas)Border_Meme.Child).Children.Add(vb);
                Canvas.SetLeft(vb, zt.CanvasLeft);
                Canvas.SetTop(vb, zt.CanvasTop);
            }


            Label_MemeVoteName.Content = m.Nom;
        }

        /// <summary>
        /// Commence à voter le mème de tous les joueurs
        /// </summary>
        private void StartVoting()
        {
            Grid_MemeMaker.Visibility = Visibility.Hidden;
            Grid_Vote.Visibility = Visibility.Visible;
            StackPanel_note.Visibility = Visibility.Visible;
            Label_WaitMemeOfOtherCounter.Visibility = Visibility.Hidden;

            Canvas_meme.Children.RemoveRange(2, Canvas_meme.Children.Count - 2); // 1 = l'image son donc on l'enlève jamais, 2 = label "chargement" on l'enleve jamais

            GameTimer.Stop();

            // 10 seconds of voting per meme
            DispatcherTimer timer_vote_memeShower = new DispatcherTimer();
            timer_vote_memeShower.Interval = TimeSpan.FromMilliseconds(15000); // 15 secondes de vote par mème

            
            int actual_mème_showing = 0;

            timer_vote_memeShower.Tick += async (sender, e) =>
            {

                // Récupère le vote du mème précédemment affiché.
                int vote = -1;

                // Réinitialise les votes et le récupère
                foreach (Border b in StackPanel_note.Children)
                {
                    if (((Label)(b).Child).Opacity == 1)
                        vote = Convert.ToInt32(((Label)(b).Child).Content);

                    ((Label)(b).Child).Opacity = 0.5;
                    ((Label)(b).Child).FontWeight = FontWeights.Regular;
                }

                // Si le vote = -1 : l'utilisateur n'a pas voté
                if (vote != -1)
                {
                    // Envois le vote
                    foreach (Player player in Party.Players)
                    {
                        // ID_DU_PLAYER_QUI_A_FAIT_LE_MEME,note
                        ZoneckClient.Send(JsonConvert.SerializeObject(new WhosTheFunniestMessage(MessageType.MY_VOTE, ((Mème)Grid_MemeVote.Tag).FromId + "," + vote)));
                    }

                    // l'ajoute
                    MemeOfPeopleInGame[Round - 1].First(x => x.FromId == ((Mème)Grid_MemeVote.Tag).FromId).Notes.Add(vote);
                }

                if (actual_mème_showing >= MemeOfPeopleInGame[Round - 1].Count)
                {
                    // Tous les mèmes ont été montré
                    timer_vote_memeShower.Stop();

                    // On attend un peu avant quand même qu'on reçoit toutes les notes
                    Grid_Vote.Visibility = Visibility.Hidden;
                    Grid_Wait.Visibility = Visibility.Visible;
                    await Task.Delay(2000);

                    // On affiche maintenant le leaderBord avec les meilleurs mèmes
                    ShowLeaderBoard();
                    await Task.Delay(1000);

                    Grid_Wait.Visibility = Visibility.Hidden;
                    Grid_Leaderboard.Visibility = Visibility.Visible;
                    Run_Round.Text = Round.ToString();
                    Run_roundNumber.Text = Party.NbreRound.ToString();

                    if (Round >= Party.NbreRound)
                    {
                        Run_IndicationLeaderBoardRound.Text = "Classement final dans ";

                        // Timer 10 secondes avant le classement finale 
                        DispatcherTimer timer_vote_memeShower = new DispatcherTimer();
                        timer_vote_memeShower.Interval = TimeSpan.FromMilliseconds(1000);

                        int timer = 10;
                        timer_vote_memeShower.Tick += (sender, e) =>
                        {
                            if (timer <= 0)
                            {
                                Run_CompteurBeforeStartingRound.Text = "10".ToString();

                                timer_vote_memeShower.Stop();
                                Grid_Leaderboard.Visibility = Visibility.Hidden;
                                Grid_ClassementFinaux.Visibility = Visibility.Visible;

                                StackPanel_AllPlayerMeme.Children.Clear();
                                // Affichage des mèmes
                                foreach(Player player in Party.Players.OrderByDescending(x => x.Moyenne))
                                {
                                    // Récupère tous les mèmes de ce joueur
                                    List<Mème?> memeOfPlayer = MemeOfPeopleInGame.ConvertAll(x => {
                                        var t = x.FirstOrDefault(y =>                                
                                            y.FromId == player.Id
                                        );
                                        if (t == default)
                                            return null;
                                        else
                                            return t;
                                    });

                                    // Les affiches
                                    StackPanel_AllPlayerMeme.Children.Add(new UC_PlayerMeme(player, memeOfPlayer, ShowMeme));
                                }
                            }

                            Run_CompteurBeforeStartingRound.Text = timer.ToString();
                            timer--;
                        };

                        timer_vote_memeShower.Start();
                    }
                    else
                    {
                        Grid_ClassementFinaux.Visibility = Visibility.Hidden;
                        Label_Round.Visibility = Visibility.Visible;
                        Run_IndicationLeaderBoardRound.Text = "Prochain tour dans ";
                        Round++;

                        // Timer 10 secondes pour le prochain tour
                        DispatcherTimer timer_vote_memeShower = new DispatcherTimer();
                        timer_vote_memeShower.Interval = TimeSpan.FromMilliseconds(1000); 

                        int timer = 10;
                        timer_vote_memeShower.Tick += (sender, e) =>
                        {
                            if(timer <= 0)
                            {
                                Run_CompteurBeforeStartingRound.Text = "10".ToString();

                                timer_vote_memeShower.Stop();
                                Grid_Leaderboard.Visibility = Visibility.Hidden;
                                GameStart(false);
                            }

                            Run_CompteurBeforeStartingRound.Text = timer.ToString();
                            timer--;
                        };

                        timer_vote_memeShower.Start();
                    }
                }

                // Montre le prochain mème
                ShowMemeToVote(actual_mème_showing);
                actual_mème_showing++;
            };

            timer_vote_memeShower.Start();

            // Affiche déjà le premier mème (pour ne pas attendre 10 sec)
            ShowMemeToVote(actual_mème_showing);

            actual_mème_showing++;
            
        }

        /// <summary>
        /// Affiche le leaderBoard des meilleurs mèmes
        /// </summary>
        private void ShowLeaderBoard()
        {

            // trie les mèmes en fonction des meilleurs notes
            MemeOfPeopleInGame[Round - 1].ForEach(x =>
            {
                if (!x.Notes.Any())
                    x.Notes.Add(0);
            });

            MemeOfPeopleInGame[Round - 1] = MemeOfPeopleInGame[Round - 1].OrderByDescending(x => {

                // Transforme la moyenne des notes de ce joueur avec la nouvelle moyenne en prenant en compte ce round
                var temp = Party.Players.FirstOrDefault(y => y.Id == x.FromId);
                if (temp != default)
                {
                    if (temp.Moyenne != null)
                        temp.Moyenne = new List<double>() { temp.Moyenne,x.Notes.Average()}.Average();
                    else temp.Moyenne = x.Notes.Average();

                }

                return x.Notes.Average();

            }).ToList();

            // Affiche les 3 meilleurs
            try
            {
                Grid_MemeVote.Children.Clear();
                Grid_FirstPlace.Children.Clear();
                Grid_SecondPlace.Children.Clear();
                Grid_ThirdPlace.Children.Clear();
                Label_FirstPlace.Content = string.Empty;
                Label_SecondPlace.Content = string.Empty;
                Label_ThirdPlace.Content = string.Empty;
                ShowMeme(Grid_FirstPlace, MemeOfPeopleInGame[Round - 1][0], 0);
                Label_FirstPlace.Content = MemeOfPeopleInGame[Round - 1][0].FromUsername + " " +  Math.Round(MemeOfPeopleInGame[Round - 1][0].Notes.Average(),1) + "/5";
                ShowMeme(Grid_SecondPlace, MemeOfPeopleInGame[Round - 1][1], 0);
                Label_SecondPlace.Content = MemeOfPeopleInGame[Round - 1][1].FromUsername + " " + Math.Round(MemeOfPeopleInGame[Round - 1][1].Notes.Average(), 1) + "/5";
                ShowMeme(Grid_ThirdPlace, MemeOfPeopleInGame[Round - 1][2], 0);
                Label_ThirdPlace.Content = MemeOfPeopleInGame[Round - 1][2].FromUsername + " " + Math.Round(MemeOfPeopleInGame[Round - 1][2].Notes.Average(), 1) + "/5";
            }
            catch 
            { 
            }
        }

        /// <summary>
        /// Affiche un mème pour le voter
        /// </summary>
        /// <param name="actual_mème_showing"></param>
        private void ShowMemeToVote(int actual_mème_showing)
        {
            if (Grid_Leaderboard.Visibility == Visibility.Hidden)
            {
                Mème m = MemeOfPeopleInGame[Round - 1].First(x => x.FromId == Party.Players[actual_mème_showing].Id);
                Grid_MemeVote.Tag = m;
                // L'affiche
                ShowMeme(Grid_MemeVote, m);
                // Timer de vote
                var anim = new DoubleAnimation((double)0, (double)100, new TimeSpan(0, 0, 0, 15, 0));
                (ProgressBar_VoteTimer as ProgressBar).BeginAnimation(ProgressBar.ValueProperty, anim, HandoffBehavior.Compose);

                // Si c'est notre mème on peut pas voter pour lui
                if (m.FromId == ZoneckClient.MyId)
                    StackPanel_note.Cursor = Cursors.No;
                else
                    StackPanel_note.Cursor = Cursors.Arrow;
            }
        }

        /// <summary>
        /// Note animation
        /// </summary>
        private void Border_Note_MouseEnter(object sender, MouseEventArgs e)
        {
            // Label à l'intérieur se met en gras
            if(StackPanel_note.Cursor != Cursors.No)
                ((Label)((Border)sender).Child).FontWeight = FontWeights.Bold; 
        }

        /// <summary>
        /// Note animation
        /// </summary>
        private void Border_Note_MouseLeave(object sender, MouseEventArgs e)
        {
            // Label à l'intérieur n'est plus en gras
            if(((Label)((Border)sender).Child).Opacity != 1)
                ((Label)((Border)sender).Child).FontWeight = FontWeights.Regular;
        }

        /// <summary>
        /// Note cliqué
        /// </summary>
        private void Border_Note_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Sinon c'est que c'est notre mème et on peut pas voter pour nous-même
            if (StackPanel_note.Cursor != Cursors.No)
            {
                // Enlève les autres validation de note
                foreach (Border b in StackPanel_note.Children)
                {
                    ((Label)(b).Child).Opacity = 0.5;
                    ((Label)(b).Child).FontWeight = FontWeights.Regular;
                }

                ((Label)((Border)sender).Child).Opacity = 1;
                ((Label)((Border)sender).Child).FontWeight = FontWeights.Bold;
            }
        }

        /// <summary>
        /// Retourne au menu principal, partie terminé
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_GameFinish_Click(object sender, RoutedEventArgs e)
        {
            Grid_Leaderboard.Visibility = Visibility.Hidden;
            Grid_GameSearcher.Visibility = Visibility.Visible;
            Label_newMess.Visibility = Visibility.Hidden;
            image_conversation.Visibility = Visibility.Hidden;
            Border_Conversation.Visibility = Visibility.Hidden;
            Grid_ClassementFinaux.Visibility = Visibility.Hidden;
            StackPanel_AllPlayerMeme.Children.Clear();

            Grid_FirstPlace.Children.Clear();
            Grid_SecondPlace.Children.Clear();
            Grid_ThirdPlace.Children.Clear();

            Party = null;
        }

        /// <summary>
        /// Ouvre le tchat de la partie
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Image_Conversation_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if(e.LeftButton == MouseButtonState.Pressed)
            {
                Border_Conversation.Visibility = Visibility.Visible;
            }
        }

        private void Image_Close_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Border_Conversation.Visibility = Visibility.Collapsed;
                Label_newMess.Content = "";
            }
        }

        /// <summary>
        /// Enlève watermark
        /// </summary>
        private void TextBox_Message_GotFocus(object sender, RoutedEventArgs e)
        {
            if (Textbox_Message.Text == "Écriver un message...")
                Textbox_Message.Text = string.Empty;
        }

        /// <summary>
        /// Envois un message
        /// </summary>
        private void Button_Send_Click(object sender, RoutedEventArgs e)
        {
            if (Party != null && !String.IsNullOrEmpty(Textbox_Message.Text) && Textbox_Message.Text != "Écriver un message...")
            {
                foreach (Player player in Party.Players)
                {
                    if(player.Id != ZoneckClient.MyId)
                        ZoneckClient.Send(JsonConvert.SerializeObject(new WhosTheFunniestMessage(MessageType.MESSAGE, Textbox_Message.Text)), player.Id);
                }

                StackPanel_Conversation.Children.Add(new UC_Message(Textbox_Message.Text, Username));
                Textbox_Message.Text = string.Empty;
                ScrollViewer_message.ScrollToBottom();
            }
        }

        private void Textbox_Message_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                Button_Send_Click(this, null);
        }


    }
}
