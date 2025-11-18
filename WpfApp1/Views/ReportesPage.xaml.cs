using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WpfApp1.Views
{
    public partial class ReportesPage : Page
    {
        public ReportesPage()
        {
            InitializeComponent();
        }

        // Evento genérico para cuando hacen clic en cualquier tarjeta de reporte
        private void ReportButton_Click(object sender, RoutedEventArgs e)
        {
            // En el futuro, esto detectará QUÉ botón se presionó para configurar los filtros específicos
            // Por ahora, solo un efecto visual o mensaje.

            if (sender is Button btn)
            {
                // Buscamos el título dentro del botón para mostrarlo
                // (Esto es solo para demo, en prod usaríamos Binding o Tag)
                // Navegamos por el árbol visual rápido (sabemos la estructura: StackPanel -> TextBlock[1])

                var stack = btn.Content as StackPanel;
                if (stack != null && stack.Children.Count > 1 && stack.Children[1] is TextBlock titleBlock)
                {
                    MessageBox.Show($"Has seleccionado: {titleBlock.Text}\n\nConfigura las fechas abajo y pulsa 'Generar'.", "Reporte Seleccionado");
                }
            }
        }

        private void BtnGenerar_Click(object sender, RoutedEventArgs e)
        {
            // Simulación de generación
            MessageBox.Show("Generando reporte... (Funcionalidad pendiente)", "Procesando", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}