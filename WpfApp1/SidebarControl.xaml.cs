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
using System.Windows.Media; // <-- ¡NUEVO! Para los colores
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using OrySiPOS.Properties; // <-- ¡NUEVO! Para acceder a los Settings
using System.ComponentModel; // <-- ¡Este es el importante!

namespace OrySiPOS
{
    public partial class SidebarControl : UserControl
    {
        // <-- ¡NUEVO! Colores para el indicador
        private SolidColorBrush colorVerdeAbierto = (SolidColorBrush)new BrushConverter().ConvertFrom("#28A745");
        private SolidColorBrush colorRojoCerrado = (SolidColorBrush)new BrushConverter().ConvertFrom("#D90429");

        public SidebarControl()
        {
            InitializeComponent();
            // ¡NUEVO! Escuchamos CUALQUIER cambio en los Settings.
            Settings.Default.PropertyChanged += Settings_PropertyChanged;

            // <-- ¡NUEVO! Nos suscribimos a los eventos del XAML
            // (También podrías haberlo hecho en el XAML como hicimos)
            // this.Loaded += SidebarControl_Loaded; 
            // this.IsVisibleChanged += SidebarControl_IsVisibleChanged;
        }

        // 1. Declaración del Routed Event (ya lo tenías)
        public static readonly RoutedEvent NavigationRequestedEvent =
            EventManager.RegisterRoutedEvent("NavigationRequested", RoutingStrategy.Bubble,
                typeof(RoutedEventHandler), typeof(SidebarControl));

        // 2. "Wrapper" del evento (ya lo tenías)
        public event RoutedEventHandler NavigationRequested
        {
            add { AddHandler(NavigationRequestedEvent, value); }
            remove { RemoveHandler(NavigationRequestedEvent, value); }
        }

        // 1. Definición de la Dependency Property (ya lo tenías)
        public static readonly DependencyProperty ActivePageProperty =
            DependencyProperty.Register("ActivePage", typeof(string), typeof(SidebarControl), new PropertyMetadata(string.Empty));

        // 2. "Wrapper" de C# (ya lo tenías)
        public string ActivePage
        {
            get { return (string)GetValue(ActivePageProperty); }
            set { SetValue(ActivePageProperty, value); }
        }

        // Método para levantar el evento
        private void RadioButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton clickedButton)
            {
                // CAMBIO CLAVE: Usamos .Tag en lugar de .Content
                // .Content trae "🏪  Caja" (Error)
                // .Tag trae "Caja" (Correcto)

                if (clickedButton.Tag != null)
                {
                    RaiseEvent(new RoutedEventArgs(NavigationRequestedEvent, clickedButton.Tag.ToString()));
                }
            }
        }

        private void Title_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // ... (tu código de Title_MouseLeftButtonDown no cambia) ...
            string dashboardIdentifier = "Dashboard";
            ActivePage = dashboardIdentifier;
            RaiseEvent(new RoutedEventArgs(NavigationRequestedEvent, dashboardIdentifier));
        }


        // -----------------------------------------------------------------
        // --- ¡AQUÍ EMPIEZA LA NUEVA LÓGICA! ---
        // -----------------------------------------------------------------

        /// <summary>
        /// Este es el método que conectamos al botón "Nueva Venta".
        /// AHORA comprueba el estado de la caja ANTES de navegar.
        /// </summary>
        private void NuevaVenta_Click(object sender, RoutedEventArgs e)
        {
            // Primero, refrescamos el estado de la UI por si acaso
            ActualizarIndicadorCaja();
            ActualizarNombreTienda(); // <--- ¡AGREGA ESTO!

            // <-- ¡MODIFICADO!
            // 1. Leemos el estado de la caja DESDE LOS SETTINGS
            bool estaAbierta = Settings.Default.IsBoxOpen;

            if (estaAbierta)
            {
                // ---- CAJA ABIERTA: Procedemos normal ----
                string nuevaVentaIdentifier = "NuevaVenta";
                RaiseEvent(new RoutedEventArgs(NavigationRequestedEvent, nuevaVentaIdentifier));
            }
            else
            {
                // ---- CAJA CERRADA: Mostramos aviso ----
                MessageBox.Show(
                    "Debe abrir la caja primero para registrar una nueva venta.", // El mensaje
                    "Caja Cerrada", // El título de la ventana
                    MessageBoxButton.OK, // El botón
                    MessageBoxImage.Warning // El icono
                );
            }
        }

        /// <summary>
        /// Se dispara cuando el control se carga por primera vez.
        /// </summary>
        private void SidebarControl_Loaded(object sender, RoutedEventArgs e)
        {
            ActualizarIndicadorCaja();
            ActualizarNombreTienda(); // <--- ¡AGREGA ESTA LÍNEA!
        }

        private void SidebarControl_Unloaded(object sender, RoutedEventArgs e)
        {
            // ¡NUEVO! Dejamos de escuchar.
            Settings.Default.PropertyChanged -= Settings_PropertyChanged;
        }

        /// <summary>
        /// Se dispara CADA VEZ que el control (o la ventana) se vuelve visible.
        /// (Ej: al volver de minimizar, al cambiar de pestaña, etc.)
        /// Esto asegura que el estado esté FRESCO.
        /// </summary>
        private void SidebarControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // (bool)e.NewValue es 'true' si se está VOLVIENDO visible
            if ((bool)e.NewValue)
            {
                ActualizarIndicadorCaja();
            }
        }

        /// <summary>
        /// ¡NUEVO MÉTODO!
        /// Este es nuestro "pintor". Lee el Setting y actualiza la UI del sidebar.
        /// </summary>
        /// 
        private void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // 'e.PropertyName' nos dice QUÉ setting cambió.
            // A nosotros solo nos importa uno:
            if (e.PropertyName == "IsBoxOpen")
            {
                // ¡Es el nuestro! Actualizamos la UI.
                ActualizarIndicadorCaja();
            }
            else if (e.PropertyName == "NombreTienda")
            {
                ActualizarNombreTienda();
            }
        }
        public void ActualizarIndicadorCaja()
        {
            // Leemos el estado guardado
            bool estaAbierta = Settings.Default.IsBoxOpen;

            if (estaAbierta)
            {
                // Estado Abierta
                CajaStatusBorder.Background = colorVerdeAbierto;
                CajaStatusText.Text = "Abierta";
            }
            else
            {
                // Estado Cerrada
                CajaStatusBorder.Background = colorRojoCerrado;
                CajaStatusText.Text = "Cerrada";
            }
        }

        private void ActualizarNombreTienda()
        {
            // Leemos de la configuración
            string nombre = Settings.Default.NombreTienda;

            // Si por alguna razón está vacío, ponemos uno por defecto
            if (string.IsNullOrWhiteSpace(nombre))
            {
                TxtNombreTienda.Text = "Mi Punto de Venta";
            }
            else
            {
                TxtNombreTienda.Text = nombre;
            }
        }
    }
}