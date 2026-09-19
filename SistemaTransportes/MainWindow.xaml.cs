using System.Windows;

namespace SistemaTransportes
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void BtnAccesoRapido_Click(object sender, RoutedEventArgs e)
        {
            txtUsuario.Text = "OpControl";
            txtPassword.Password = "1598753";
        }
    }
}