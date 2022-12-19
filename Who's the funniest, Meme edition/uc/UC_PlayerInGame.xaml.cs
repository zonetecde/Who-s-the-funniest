using System;
using System.Collections.Generic;
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

namespace Who_s_the_funniest.uc
{
    /// <summary>
    /// Logique d'interaction pour UC_PlayerInGame.xaml
    /// </summary>
    public partial class UC_PlayerInGame : UserControl
    {
        public UC_PlayerInGame(Player player, bool isOwner)
        {
            InitializeComponent();
            Player = player;
            IsOwner = isOwner;

            Label_nom_personne.Content = player.Username;
            Image_Owner.Visibility = IsOwner ? Visibility.Visible : Visibility.Collapsed;
        }

        public Player Player { get; }
        public bool IsOwner { get; set; }
    }
}
