using System.Windows;
using System.Windows.Controls;

namespace Match3
{
    /// <summary>
    /// Interaction logic for MainMenuControl.xaml
    /// </summary>
    public partial class MainMenuControl
    {
        public MainMenuControl()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ((ContentControl)Parent).Content = new GameControl();
        }
    }
}
