using System.Windows;
using System.Windows.Controls;

namespace Match3
{
    /// <summary>
    /// Interaction logic for GameOverControl.xaml
    /// </summary>
    public partial class GameOverControl
    {
        public GameOverControl()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ((ContentControl)Parent).Content = new MainMenuControl();
        }
    }
}
