using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Text.RegularExpressions; // Para Regex
using OrySiPOS.ViewModels;

namespace OrySiPOS.Views
{
    public partial class AjustesPage : Page
    {
        // Bandera para evitar bucles infinitos al sincronizar las cajas de contraseña
        private bool _isSyncing = false;

        public AjustesPage()
        {
            InitializeComponent();
            this.DataContext = new AjustesViewModel();

            // --- CARGAR CONTRASEÑA AL INICIO ---
            // Como el PasswordBox no tiene Binding directo, le metemos el valor manualmente al arrancar
            if (this.DataContext is AjustesViewModel vm)
            {
                PassBoxSecret.Password = vm.PassEmailInventario;
                TxtPassVisible.Text = vm.PassEmailInventario;
            }
        }

        // --- 1. VALIDACIÓN DE SOLO NÚMEROS (Para el Stock) ---
        private void SoloNumeros_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        // --- 2. LÓGICA DE SEGURIDAD DE CONTRASEÑA ---

        // A. Cuando escriben en los asteriscos, actualizamos el ViewModel
        private void PassBoxSecret_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_isSyncing) return;

            if (this.DataContext is AjustesViewModel vm)
            {
                vm.PassEmailInventario = PassBoxSecret.Password;

                // Sincronizamos la caja visible por si acaso cambian de modo
                _isSyncing = true;
                TxtPassVisible.Text = PassBoxSecret.Password;
                _isSyncing = false;
            }
        }

        // B. Cuando escriben en texto plano (si está visible), actualizamos el ViewModel
        private void TxtPassVisible_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isSyncing) return;

            if (this.DataContext is AjustesViewModel vm)
            {
                vm.PassEmailInventario = TxtPassVisible.Text;

                _isSyncing = true;
                PassBoxSecret.Password = TxtPassVisible.Text;
                _isSyncing = false;
            }
        }

        // C. BOTÓN MOSTRAR/OCULTAR (CON PERMISO)
        private void BtnTogglePass_Click(object sender, RoutedEventArgs e)
        {
            // CASO A: Si está visible, lo ocultamos (no pide permiso para ocultar)
            if (TxtPassVisible.Visibility == Visibility.Visible)
            {
                TxtPassVisible.Visibility = Visibility.Collapsed;
                PassBoxSecret.Visibility = Visibility.Visible;

                // Devolvemos el foco a la caja secreta
                PassBoxSecret.Focus();
            }
            // CASO B: Quiere ver la contraseña (¡PEDIR PERMISO!)
            else
            {
                if (SolicitarPermisoAdministrador())
                {
                    PassBoxSecret.Visibility = Visibility.Collapsed;
                    TxtPassVisible.Visibility = Visibility.Visible;
                    TxtPassVisible.Focus();
                }
            }
        }

        // D. SIMULACIÓN DE SEGURIDAD
        private bool SolicitarPermisoAdministrador()
        {
            var resultado = MessageBox.Show(
                "Esta información es sensible.\n¿Eres el administrador del sistema?",
                "Confirmación de Seguridad",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            return resultado == MessageBoxResult.Yes;
        }

        // (Opcional) Si todavía tienes el botón de guardar con evento Click en el XAML
        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            // Si el botón usa Command="{Binding...}" esto no se ejecuta, pero lo dejamos por si acaso.
        }
    }
}