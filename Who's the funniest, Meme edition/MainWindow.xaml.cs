using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Reflection.Emit;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Who_s_the_funniest.classe;
using Who_s_the_funniest.classe.msg;
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
        const int MIN_PLAYER_TO_START = 2;

        // Trouver une partie
        Party Party;

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

            TextBox_Username.Text = Who_s_the_funniest.Properties.Settings.Default.Username;
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
                StartGame(Party.Id.ToString());
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

                    Dispatcher.Invoke(() =>
                    {
                        StackPanel_party.Children.Add(new uc.UC_party(p, GameJoined));
                    });
                }
                else if (wtfM.MsgType == MessageType.JOIN_PARTY)
                {
                    Join j = JsonConvert.DeserializeObject<Join>(wtfM.Content)!;

                    // Quelqu'un s'est ajouté à une game
                    Dispatcher.Invoke(() =>
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
                                        StartGame(((UC_party)uc_Party).Party.Id.ToString());
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

                    Dispatcher.Invoke(() =>
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
                    Dispatcher.Invoke(() =>
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
                    Dispatcher.Invoke(() =>
                    {
                        RemoveSomeoneFromTheGameWhereHeIs(wtfM.Content);
                    });
                }
                else if (wtfM.MsgType == MessageType.GAME_START)
                {
                    // la game qui a start son Id est dans .Content
                    // l'enlève de la liste des games
                    Dispatcher.Invoke(() =>
                    {
                        StartGame(wtfM.Content);
                    });
                }
                
                #endregion
            }

            // Une personne s'est déconnecté, on la supp de toutes les games
            else if (obj.MessageType == ClassLibrary.MESSAGE_TYPE.DISCONNECTION)
            {
                Dispatcher.Invoke(() =>
                {
                    RemoveSomeoneFromTheGameWhereHeIs(obj.Id);
                });
            }
        }

        /// <summary>
        /// Start la game qui a pour id gameId. Si on est dans la game, on commence à jouer.
        /// </summary>
        /// <param name="gameId"></param>
        private void StartGame(string gameId)
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


        }
    }
}
