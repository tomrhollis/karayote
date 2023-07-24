using Karayote.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.ComponentModel;
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
            Closing += ConfirmClose;
        }

        /// <summary>
        /// Be sure about closing, since it doesn't have database restoration yet
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ConfirmClose(object? sender, CancelEventArgs e)
        {
            MessageBoxResult closeConf = MessageBox.Show("Are you sure you want to close?", "Confirm", MessageBoxButton.OKCancel, MessageBoxImage.Question, MessageBoxResult.Cancel);
            if(closeConf == MessageBoxResult.Cancel)
            {
                e.Cancel = true;
            }
        }
    }
}
