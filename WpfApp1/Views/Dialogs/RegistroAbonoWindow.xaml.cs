using System.Windows;
using System.Windows.Input; // <--- NECESARIO

namespace WpfApp1.Views.Dialogs
{
    public partial class RegistroAbonoWindow : Window
    {
        public RegistroAbonoWindow()
        {
            InitializeComponent();
        }

        // --- AGREGA ESTO ---
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }
        // -------------------
    }
}