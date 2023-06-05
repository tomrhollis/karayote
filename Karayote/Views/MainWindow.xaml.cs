using Karayote.Models;
using System.Windows;

namespace Karayote.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow(IKarayoteBot karayote)
        {
            InitializeComponent();
        }
    }
}
