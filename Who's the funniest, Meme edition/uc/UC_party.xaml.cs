using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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

namespace Who_s_the_funniest__Meme_edition.uc
{
    /// <summary>
    /// Logique d'interaction pour uc_party_header.xaml
    /// </summary>
    public partial class UC_party : UserControl
    {
        public UC_party(Party party, Action<Party> gameJoined)
        {
            InitializeComponent();

            Party = party;
            GameJoined = gameJoined;
            this.DataContext = Party;
            Run_NbreJoueurConnecté.Text = Party.Players.Count().ToString();
        }

        public Party Party { get; set; }
        public Action<Party> GameJoined { get; }

        private void UserControl_MouseEnter(object sender, MouseEventArgs e)
        {
            this.Background = Brushes.LightSkyBlue;
        }

        private void UserControl_MouseLeave(object sender, MouseEventArgs e)
        {
            this.Background = Brushes.Transparent;

        }

        internal void AddNewPlayer(string username, string id)
        {
            Party.Players.Add(new Player(username, id));
            Run_NbreJoueurConnecté.Text = Party.Players.Count().ToString();

        }

        /// <summary>
        /// Rentre dans la game
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UserControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // s'ajoute
            Party.Players.Add(new Player(MainWindow.Username, MainWindow.ZoneckClient.MyId));

            MainWindow.ZoneckClient.Send(JsonConvert.SerializeObject(new WhosTheFunniestMessage(MessageType.JOIN_PARTY,
                JsonConvert.SerializeObject(new Join(MainWindow.Username, MainWindow.ZoneckClient.MyId, Party.Id)))
            ));

            GameJoined(Party);
        }

        /// <summary>
        /// Quelqu'un a quitté cette game
        /// </summary>
        /// <param name="player"></param>
        internal void SomeoneLeft(Player player)
        {
            Party.Players.RemoveAll(x => x.Id == player.Id);
            Run_NbreJoueurConnecté.Text = Party.Players.Count().ToString();

            // si plus personne dans la game on la supp
            if (Party.Players.Count == 0)
                this.Visibility = Visibility.Collapsed;
        }
    }
}
