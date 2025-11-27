using System.Linq; // ¡Asegúrate de tener este using!
using System.Windows;
using System.Collections.Generic; // Para la clase de ayuda

namespace OrySiPOS.Views
{
    public partial class ResumirVentaDialog : Window
    {
        // --- 1. Esta es la "respuesta" que la ventana dará
        public int IndiceSeleccionado { get; private set; } = -1;

        // --- 2. Clase de ayuda para mostrar en el ListBox
        private class SesionDisplayItem
        {
            public string Resumen { get; set; }
            public int IndiceReal { get; set; }

            // No necesitamos un DataTemplate si usamos esto:
            // public override string ToString() => Resumen; 
        }

        public ResumirVentaDialog()
        {
            InitializeComponent();

            // 3. Llenar la lista al iniciar
            CargarSesionesEnEspera();
        }

        private void CargarSesionesEnEspera()
        {
            var todasLasSesiones = VentaSessionManager.GetTodasSesiones();
            int indiceActivo = VentaSessionManager.IndiceSesionActiva;

            var itemsParaMostrar = new List<SesionDisplayItem>();

            for (int i = 0; i < todasLasSesiones.Count; i++)
            {
                // ¡No mostramos la que ya está activa!
                if (i == indiceActivo) continue;

                var sesion = todasLasSesiones[i];

                // Creamos un resumen: "Venta 1 (3 productos, $150.00)"
                itemsParaMostrar.Add(new SesionDisplayItem
                {
                    Resumen = $"Venta {i + 1} ({sesion.Count} productos, {sesion.Sum(p => p.Subtotal):C})",
                    IndiceReal = i // Guardamos el índice real
                });
            }

            // Asignamos la lista de resúmenes al ListBox
            SesionesListBox.ItemsSource = itemsParaMostrar;
        }

        private void AbrirButton_Click(object sender, RoutedEventArgs e)
        {
            // 1. Ver qué item se seleccionó
            var itemSeleccionado = SesionesListBox.SelectedItem as SesionDisplayItem;

            if (itemSeleccionado == null)
            {
                MessageBox.Show("Por favor, selecciona una venta para abrir.");
                return;
            }

            // 2. Guardar el índice real de esa venta
            this.IndiceSeleccionado = itemSeleccionado.IndiceReal;

            // 3. Cerrar la ventana con "OK" (éxito)
            // Esto le dice a VentaPage que la operación fue exitosa.
            this.DialogResult = true;
        }
    }
}