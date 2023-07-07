using Karayote.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Windows;

namespace Karayote.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow(IHost host)
        {
            InitializeComponent();
            DataContext = host.Services.GetRequiredService<MainWindowViewModel>();
        }
    }
}
