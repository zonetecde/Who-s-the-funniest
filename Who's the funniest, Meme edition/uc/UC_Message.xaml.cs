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

namespace Who_s_the_funniest.uc
{
    /// <summary>
    /// Logique d'interaction pour UC_Message.xaml
    /// </summary>
    public partial class UC_Message : UserControl
    {
        public UC_Message(string message, string username)
        {
            InitializeComponent();

            Run_Message.Text = message;
            Run_username.Text = username;
        }
    }
}
