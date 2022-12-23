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
using Who_s_the_funniest.classe.msg;

namespace Who_s_the_funniest.uc
{
    /// <summary>
    /// Logique d'interaction pour UC_PlayerMeme.xaml
    /// </summary>
    public partial class UC_PlayerMeme : UserControl
    {
        public UC_PlayerMeme(classe.Player player, List<classe.msg.Mème?> memeOfPlayer, Action<Panel, classe.msg.Mème, double> showMeme)
        {
            InitializeComponent();

            TextBlock_nomJoueur.Text = player.Username + " " + Math.Round(player.Moyenne, 2) + "/5";

            foreach(Mème meme in memeOfPlayer)
            {
                if (meme != null)
                {
                    StackPanel_meme.Children.Add(new Grid()
                    {
                        Width = 290,
                        Height = 290,
                        Margin = new Thickness(0, 5, 0, 5)
                    });

                    showMeme(((Grid)StackPanel_meme.Children[StackPanel_meme.Children.Count - 1]), meme, 0);
                }
            }
        }
    }
}
