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
        Random Rdn = new Random();
        const int MIN_PLAYER_TO_START = 1;

        // Trouver une partie
        Party Party;

        // Game
        Timer GameTimer;
        List<Mème> OtherPeopleMème = new List<Mème>();

        // Créer mème
        BrushConverter Converter = new System.Windows.Media.BrushConverter();

        public MainWindow()
        {
            InitializeComponent();

            StackPanel_party.Children.Add(new UC_party_header());

            Label_SearchPlayer.Visibility = Visibility.Visible;
            Grid_LogIn.Visibility = Visibility.Visible;
            Grid_CreateNewGame.Visibility = Visibility.Hidden;
            Grid_GameJoined.Visibility = Visibility.Hidden;
            Grid_GameSearcher.Visibility = Visibility.Visible;
            Grid_Game.Visibility = Visibility.Hidden;
            Grid_Vote.Visibility = Visibility.Hidden;

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

                Label_SearchPlayer.Visibility = Visibility.Hidden;
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
            foreach (var party in StackPanel_party.Children)
            {
                if (party is UC_party)
                {
                    if (((UC_party)party).Party.Players.Any(x => x.Id == playerIdToRemove))
                    {
                        ((UC_party)party).SomeoneLeft(((UC_party)party).Party.Players.FirstOrDefault(x => x.Id == playerIdToRemove)!);

                        // Si il s'est enlevé de la game où on est actuellement
                        if (Party != null)
                        {
                            if (Party.Id == ((UC_party)party).Party.Id)
                            {
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
        /// Créé une partie et l'envois aux autres
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_CreateGame_Click(object sender, RoutedEventArgs e)
        {
            if(!String.IsNullOrEmpty(TextBox_GameName.Text))
            {
                // créé la partie
                Party party = new Party(TextBox_GameName.Text, (byte)NbreJoueur, ComboBox_Lang.Text, new List<Player>() { new Player(Username, ZoneckClient.MyId) }, Rdn.Next(int.MinValue, int.MaxValue));
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
            foreach (UIElement ui in StackPanel_party.Children)
            {
                if(ui is UC_party)
                {
                    if (((UC_party)ui).Party.Id == Party.Id)
                        ((UC_party)ui).SomeoneLeft(new Player(Username, ZoneckClient.MyId));
                }
            }

            Party = null;
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
            
            GameStart();
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
                                    if(((UC_party)uc_Party).Party.Players.Count == ((UC_party)uc_Party).Party.NbreJoueurMax)
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

                                if(Party.Players.Count >= MIN_PLAYER_TO_START)
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
                        if (StackPanel_party.Children.Count == 1)
                        {

                            JsonConvert.DeserializeObject<List<Party>>(wtfM.Content)!.ForEach(x =>
                            {
                                StackPanel_party.Children.Add(new UC_party(x, GameJoined));
                            });
                        }
                    });

                }
                else if (wtfM.MsgType == MessageType.GAME_LEFT)
                {
                    // La personne d'id wtfM.Content a quitté la game dans laquelle elle est
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        RemoveSomeoneFromTheGameWhereHeIs(wtfM.Content);
                    });
                }
                else if (wtfM.MsgType == MessageType.GAME_START)
                {
                    // la game qui a start son Id est dans .Content
                    // l'enlève de la liste des games
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        StartGameVisual(wtfM.Content);
                    });
                }
                else if(wtfM.MsgType == MessageType.MY_MEME)
                {
                    // Quelqu'un a envoyé son mème
                    OtherPeopleMème.Add(JsonConvert.DeserializeObject<Mème>(wtfM.Content));

                    // Si on a reçu tous les mèmes ont peut commencer le vote
                    if(OtherPeopleMème.Count == Party.Players.Count)
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
                            Run_MemeCounter.Text = OtherPeopleMème.Count + "/" + Party.Players.Count;
                        });
                    }
                }
                
                #endregion
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
                GameStart();
            }
        }

        /// <summary>
        /// Jeu qui commence
        /// </summary>
        private void GameStart()
        {
            Grid_GameJoined.Visibility = Visibility.Hidden;
            Grid_GameSearcher.Visibility = Visibility.Hidden;
            Grid_Game.Visibility = Visibility.Visible;
            Grid_Vote.Visibility = Visibility.Hidden;
            Grid_Vote.Visibility = Visibility.Hidden;

            Button_ChangeMeme.Content = "Changer de mème (3)";
            Button_ChangeMeme.Tag = 3;
            Button_ChangeMeme.IsEnabled = true;

            OtherPeopleMème = new List<Mème>();

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
                        video.LoadedBehavior = MediaState.Manual;
                        bool firstLoop = true;

                        video.MediaEnded += (sender, e) =>
                        {
                            // boucle infini, la deuxieme fois sans le son
                            video.Position = new TimeSpan(0, 0, 0, 0, 1);
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
            if (ZoneDeTexte.Any() || sender == null) // ||sender == null : le mème a été forcé à être envoyé (countdown à 0)
            {

                Mème m = new Mème(nom_et_url.Split('|')[0], nom_et_url.Split('|')[1], ZoneDeTexte, ZoneckClient.MyId, Username);
                OtherPeopleMème.Add(m);

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
                if (OtherPeopleMème.Count == Party.Players.Count)
                {

                    StartVoting();
                   
                }
                else
                {
                    // On attend que tout le monde envois leur mème
                    Grid_Vote.Visibility = Visibility.Visible;                    
                    ShowMeme(m);
                    StackPanel_note.Visibility = Visibility.Hidden;
                    Label_WaitMemeOfOtherCounter.Visibility = Visibility.Visible;
                    Run_MemeCounter.Text = OtherPeopleMème.Count + "/" + Party.Players.Count;
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
        private void ShowMeme(Mème m)
        {
            Border_MemeVoting.Child = null;

            Canvas canvas = new Canvas()
            {
                Background = Brushes.White,
                Width = Canvas_meme.Width,
                Height = Canvas_meme.Height,
                Margin = new Thickness(20),
            };

            Border_MemeVoting.Child = canvas;
            
            // Image/Vidéo du mème
            if (m.Url.Contains(".mp4"))
            {
                MediaElement video = new MediaElement();
                video.LoadedBehavior = MediaState.Manual;

                video.Volume = 0.3;
                int loopNumber = 0;
                video.MediaEnded += (sender, e) =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        // boucle infini
                        video.Position = new TimeSpan(0, 0, 0, 0, 1);
                        video.Play();
                        loopNumber++;

                        // plus de son après 3 répétitions
                        if (loopNumber >= 3)
                        {

                                video.Volume = 0;
                        }
                    });

                };

                video.Source = new Uri(m.Url, UriKind.RelativeOrAbsolute);
                video.Width = ((Canvas)Border_MemeVoting.Child).Width;
                video.Height = ((Canvas)Border_MemeVoting.Child).Height;
                ((Canvas)Border_MemeVoting.Child).Children.Add(video);
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
                img.Width = ((Canvas)Border_MemeVoting.Child).Width;
                img.Height = ((Canvas)Border_MemeVoting.Child).Height;
                ((Canvas)Border_MemeVoting.Child).Children.Add(img);
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
                    Text = zt.Texte
                };

                vb.Child = zone_de_texte;

                ((Canvas)Border_MemeVoting.Child).Children.Add(vb);
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

            GameTimer.Stop();

            // 10 seconds of voting per meme
            DispatcherTimer timer_vote = new DispatcherTimer();
            timer_vote.Interval = TimeSpan.FromMilliseconds(10000);

            int actual_mème_showing = 0;

            timer_vote.Tick += (sender, e) =>
            {
                if (actual_mème_showing < OtherPeopleMème.Count)
                {
                    Border_MemeVoting.Tag = OtherPeopleMème.First(x => x.FromId == Party.Players[actual_mème_showing].Id);
                    // Display the meme
            ShowMeme(OtherPeopleMème.First(x => x.FromId == Party.Players[actual_mème_showing].Id));

                    actual_mème_showing++;
                }
            };

            timer_vote.Start();


            // Affiche déjà le premier mème (pour ne pas attendre 10 sec)
            Border_MemeVoting.Tag = OtherPeopleMème.First(x => x.FromId == Party.Players[actual_mème_showing].Id);
            // L'affiche
            ShowMeme(OtherPeopleMème.First(x => x.FromId == Party.Players[actual_mème_showing].Id));

            actual_mème_showing++;
            timer_vote.Start();
        }
    }
}
