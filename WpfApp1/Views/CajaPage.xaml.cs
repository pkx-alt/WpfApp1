using System;
using System.Globalization;
using System.Linq; // Necesario para Sum()
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using WpfApp1.Data;       // Acceso a la BD
using WpfApp1.Models;     // Acceso al modelo Gasto
using WpfApp1.ViewModels; // Acceso al VM del modal
using WpfApp1.Dialogs;    // Acceso a la ventana
using WpfApp1.Views.Dialogs; // (A veces el namespace varía, revisa dónde está tu RegistroGastoWindow)
namespace WpfApp1.Views
{
    public partial class CajaPage : Page
    {
        // Variables del Reloj (ya las tenías)
        private DispatcherTimer timerReloj;
        private CultureInfo culturaEspanol = new CultureInfo("es-ES");

        // <-- NUEVO: Variables para los colores (así es más fácil cambiarlos)
        private SolidColorBrush colorVerdeAbierto = (SolidColorBrush)new BrushConverter().ConvertFrom("#28A745");
        private SolidColorBrush colorRojoCerrado = (SolidColorBrush)new BrushConverter().ConvertFrom("#D90429");
        private SolidColorBrush colorGrisDesactivado = (SolidColorBrush)new BrushConverter().ConvertFrom("#E0E0E0");
        private SolidColorBrush colorTextoGris = (SolidColorBrush)new BrushConverter().ConvertFrom("#A0A0A0");
        private SolidColorBrush colorAzulIngreso = (SolidColorBrush)new BrushConverter().ConvertFrom("#007BFF"); // Un azul para el botón
        private SolidColorBrush colorNaranjaEgreso = (SolidColorBrush)new BrushConverter().ConvertFrom("#FF8C00"); // Una naranja para el otro


        public CajaPage()
        {
            InitializeComponent();

            // --- Lógica del Reloj (ya la tenías) ---
            timerReloj = new DispatcherTimer();
            timerReloj.Tick += TimerReloj_Tick;
            timerReloj.Interval = new TimeSpan(0, 0, 1);
            timerReloj.Start();
            ActualizarFechaHora();

            // --- ¡NUEVO! Lógica de la Caja ---
            // 1. Cargamos el estado guardado al iniciar la página
            CargarEstadoCaja();
        }

        // --- MÉTODOS DEL RELOJ (ya los tenías) ---
        private void TimerReloj_Tick(object sender, EventArgs e)
        {
            ActualizarFechaHora();
        }

        private void ActualizarFechaHora()
        {
            string formato = "dddd, dd 'de' MMMM 'de' yyyy, hh:mm:ss tt";
            string fechaHoraActual = DateTime.Now.ToString(formato, culturaEspanol);
            FechaActualTextBlock.Text = char.ToUpper(fechaHoraActual[0]) + fechaHoraActual.Substring(1);
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            timerReloj?.Stop();
        }


        // --- ¡NUEVOS MÉTODOS PARA LA CAJA! ---

        /// <summary>
        /// Lee los Settings guardados y actualiza la UI.
        /// Se llama 1 sola vez al cargar la página.
        /// </summary>
        private void CargarEstadoCaja()
        {
            // Leemos la "memoria"
            bool estaAbierta = Properties.Settings.Default.IsBoxOpen;
            string mensajeGuardado = Properties.Settings.Default.LastActionMessage;

            // "Pintamos" la pantalla según esos valores
            ActualizarVisualCaja(estaAbierta, mensajeGuardado);
        }


        /// <summary>
        /// Este es el método que conectamos al botón en el XAML.
        /// Se ejecuta CADA VEZ que el usuario hace clic.
        /// </summary>
        // En CajaPage.xaml.cs
        // (Asegúrate de tener tus 'usings' al principio, incluyendo WpfApp1.Properties)

        // En CajaPage.xaml.cs
        // (Asegúrate de tener tus 'usings' al principio: System, WpfApp1.Properties, etc.)

        private void BtnAbrirCaja_Click(object sender, RoutedEventArgs e)
        {
            bool estadoActual = Properties.Settings.Default.IsBoxOpen;

            if (estadoActual == true)
            {
                // -------------------------------------------------------
                // LÓGICA DE CIERRE (CAJA YA ESTABA ABIERTA)
                // -------------------------------------------------------

                decimal montoInicial = Properties.Settings.Default.MontoInicialCaja;
                // Aquí podrías sumar ventas reales para calcular el esperado exacto
                decimal efectivoEsperado = montoInicial;

                // Abrimos el diálogo de conteo
                CierreCajaWindow dialog = new CierreCajaWindow(efectivoEsperado);
                dialog.Owner = Window.GetWindow(this);
                bool? dialogResult = dialog.ShowDialog();

                if (dialogResult == true)
                {
                    decimal totalContado = dialog.TotalContado;
                    string notas = dialog.Notas;
                    decimal diferencia = totalContado - efectivoEsperado;

                    // 1. GUARDAR EN BASE DE DATOS (NUEVO)
                    using (var db = new InventarioDbContext())
                    {
                        // Buscamos el corte que estaba abierto (el último creado)
                        var corteActivo = db.CortesCaja
                                            .OrderByDescending(c => c.Id)
                                            .FirstOrDefault(c => c.Abierta);

                        if (corteActivo != null)
                        {
                            corteActivo.FechaCierre = DateTime.Now;
                            corteActivo.MontoFinal = totalContado;
                            corteActivo.Diferencia = diferencia;
                            corteActivo.Notas = notas;
                            corteActivo.Abierta = false; // ¡Cerramos el ciclo!

                            db.SaveChanges();
                        }
                    }

                    // 2. Guardar en Settings (Para mantener la UI sincronizada)
                    Properties.Settings.Default.IsBoxOpen = false;
                    Properties.Settings.Default.LastActionMessage = $"Cierre realizado el {DateTime.Now:g}";
                    Properties.Settings.Default.MontoInicialCaja = 0;
                    Properties.Settings.Default.Save();

                    // 3. Refrescar Pantalla
                    ActualizarVisualCaja(false, Properties.Settings.Default.LastActionMessage);
                }
            }
            else
            {
                // -------------------------------------------------------
                // LÓGICA DE APERTURA (CAJA ESTABA CERRADA)
                // -------------------------------------------------------

                AperturaCajaWindow dialog = new AperturaCajaWindow();
                dialog.Owner = Window.GetWindow(this);
                bool? dialogResult = dialog.ShowDialog();

                if (dialogResult == true)
                {
                    decimal montoInicial = dialog.MontoInicial;

                    // 1. GUARDAR EN BASE DE DATOS (NUEVO)
                    using (var db = new InventarioDbContext())
                    {
                        var nuevoCorte = new CorteCaja
                        {
                            FechaApertura = DateTime.Now,
                            MontoInicial = montoInicial,
                            Abierta = true, // ¡Abrimos ciclo!
                            MontoFinal = 0,
                            Diferencia = 0,
                            Notas = ""
                        };
                        db.CortesCaja.Add(nuevoCorte);
                        db.SaveChanges();
                    }

                    // 2. Guardar en Settings
                    Properties.Settings.Default.IsBoxOpen = true;
                    Properties.Settings.Default.MontoInicialCaja = montoInicial;
                    Properties.Settings.Default.LastActionMessage = $"Apertura realizada el {DateTime.Now:g}";
                    Properties.Settings.Default.Save();

                    // 3. Refrescar Pantalla
                    ActualizarVisualCaja(true, Properties.Settings.Default.LastActionMessage);
                }
            }
        }

        /// <summary>
        /// Método "ayudante" que se encarga de "pintar" la pantalla.
        /// Actualiza todos los textos, colores y botones según el estado.
        /// </summary>
        private void ActualizarVisualCaja(bool estaAbierta, string mensaje)
        {
            // Actualiza el mensaje
            UltimoMovimientoTextBlock.Text = mensaje;

            if (estaAbierta)
            {
                // -- Estado ABIERTO --
                EstadoEllipse.Fill = colorVerdeAbierto;
                EstadoTextBlock.Text = "CAJA ABIERTA";

                // Botón principal (Ahora dice "Cerrar")
                BtnAbrirCaja.Content = "Cerrar caja";
                // Asignamos un color rojo más oscuro, como el de tu imagen
                BtnAbrirCaja.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#A12A45");

                // Botones de acciones
                BtnRegistrarIngreso.IsEnabled = true;
                // Asignamos los colores de tu nueva imagen
                BtnRegistrarIngreso.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#E59F71");
                BtnRegistrarIngreso.Foreground = Brushes.Black;

                BtnRegistrarEgreso.IsEnabled = true;
                // Asignamos los colores de tu nueva imagen
                BtnRegistrarEgreso.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#00A99D");
                BtnRegistrarEgreso.Foreground = Brushes.White;

                // --- ¡NUEVO! Mostramos y poblamos el panel de resúmenes ---
                ResumenesPanel.Visibility = Visibility.Visible;
                // 2. CÁLCULO REAL DE MOVIMIENTOS
                decimal egresosReales = 0;
                decimal ingresosReales = 0; // <--- NUEVA VARIABLE


                // 1. Leemos el único dato "real" que tenemos
                decimal montoInicial = Properties.Settings.Default.MontoInicialCaja;

                using (var db = new InventarioDbContext())
                {
                    // Definimos "Hoy" como desde las 00:00 horas
                    DateTime inicioDia = DateTime.Today;

                    // Sumamos todos los gastos registrados hoy
                    egresosReales = db.Gastos
                                      .Where(g => g.Fecha >= inicioDia)
                                      .Sum(g => g.Monto);
                    // SUMAMOS TODOS LOS INGRESOS (¡NUEVA CONSULTA!)
                    ingresosReales = db.Ingresos
                                       .Where(i => i.Fecha >= inicioDia)
                                       .Sum(i => i.Monto); // <--- AHORA LEEMOS LA TABLA Ingresos
                }

                // 2. Valores "hardcoded" (quemados) para simular el resto
                decimal ventasEfectivo = 850.50m;
                decimal ventasTarjeta = 500.00m;
                decimal ventasTransferencia = 100.00m;
                decimal totalGeneralVentas = 4500.00m; // Valor de la imagen

                // 3. Calculamos el total de efectivo esperado
                decimal totalEfectivoEsperado = montoInicial + ventasEfectivo + ingresosReales - egresosReales;

                // 4. Actualizamos los TextBlocks
                // (Usamos 'culturaEspanol' que ya definimos en la clase para el formato de moneda)

                // 5. Actualizamos los TextBlocks
                ResumenEstadoInicial.Text = $"Estado inicial: {montoInicial.ToString("C", culturaEspanol)}";
                ResumenVentasEfectivo.Text = $"Ventas en efectivo: {ventasEfectivo.ToString("C", culturaEspanol)}";
                ResumenOtrosIngresos.Text = $"Otros ingresos: {ingresosReales.ToString("C", culturaEspanol)}";
                // ¡Aquí se verá la suma real de lo que acabas de registrar!
                ResumenEgresos.Text = $"Egresos: {egresosReales.ToString("C", culturaEspanol)}";

                ResumenTotalEsperado.Text = $"Total esperado: {totalEfectivoEsperado.ToString("C", culturaEspanol)}";

                // Ventas Totales
                VentasEfectivo.Text = $"Ventas en efectivo: {ventasEfectivo.ToString("C", culturaEspanol)}";
                VentasTarjeta.Text = $"Ventas con tarjeta: {ventasTarjeta.ToString("C", culturaEspanol)}";
                VentasTransferencia.Text = $"Ventas por transferencia: {ventasTransferencia.ToString("C", culturaEspanol)}";
                VentasTotalGeneral.Text = $"Total esperado: {totalGeneralVentas.ToString("C", culturaEspanol)}";
            }
            else
            {
                // -- Estado CERRADO --
                EstadoEllipse.Fill = colorRojoCerrado;
                EstadoTextBlock.Text = "CAJA CERRADA";

                // Botón principal (Vuelve a "Abrir")
                BtnAbrirCaja.Content = "Abrir caja";
                BtnAbrirCaja.Background = colorVerdeAbierto; // Verde para la acción de "abrir"

                // Botones de acciones
                BtnRegistrarIngreso.IsEnabled = false;
                BtnRegistrarIngreso.Background = colorGrisDesactivado;
                BtnRegistrarIngreso.Foreground = colorTextoGris;

                BtnRegistrarEgreso.IsEnabled = false;
                BtnRegistrarEgreso.Background = colorGrisDesactivado;
                BtnRegistrarEgreso.Foreground = colorTextoGris;

                // --- ¡NUEVO! Ocultamos el panel de resúmenes ---
                ResumenesPanel.Visibility = Visibility.Collapsed;
            }
        }
        // En CajaPage.xaml.cs

        private void BtnRegistrarIngreso_Click(object sender, RoutedEventArgs e)
        {
            // 1. Instancia del Nuevo ViewModel y Nueva Ventana
            var modalVM = new ViewModels.RegistroIngresoViewModel();
            var modalWindow = new RegistroIngresoWindow(); // Ventana Verde

            // 2. Conectar el cable de cierre
            modalVM.CerrarVentana = () =>
            {
                modalWindow.DialogResult = true;
                modalWindow.Close();
            };

            // 3. Asignar Contexto y Dueño
            modalWindow.DataContext = modalVM;
            modalWindow.Owner = Window.GetWindow(this);

            // 4. Mostrar
            bool? result = modalWindow.ShowDialog();

            // 5. Guardar si fue exitoso
            if (result == true)
            {
                using (var db = new InventarioDbContext())
                {
                    // Guardamos el objeto NuevoIngreso en la tabla Ingresos
                    db.Ingresos.Add(modalVM.NuevoIngreso);
                    db.SaveChanges();
                }

                MessageBox.Show("Ingreso registrado correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);

                // 6. Actualizar visualmente la caja
                string mensaje = Properties.Settings.Default.LastActionMessage;
                ActualizarVisualCaja(true, mensaje);
            }
        }
        private void BtnRegistrarEgreso_Click(object sender, RoutedEventArgs e)
        {
            // 1. Preparamos el Modal y su VM (¡Igual que antes!)
            var modalVM = new RegistroGastoViewModel();
            var modalWindow = new RegistroGastoWindow();

            // 2. Conectamos el cierre
            modalVM.CerrarVentana = () =>
            {
                modalWindow.DialogResult = true;
                modalWindow.Close();
            };

            modalWindow.DataContext = modalVM;

            // Opcional: Hacer que el modal pertenezca a la ventana actual para que salga centrado
            modalWindow.Owner = Window.GetWindow(this);

            // 3. Mostrar Modal
            bool? result = modalWindow.ShowDialog();

            // 4. Si guardó...
            if (result == true)
            {
                // A) Guardar en Base de Datos
                using (var db = new InventarioDbContext())
                {
                    // El objeto ya viene lleno del modal
                    db.Gastos.Add(modalVM.NuevoGasto);
                    db.SaveChanges();
                }

                // B) Feedback visual
                MessageBox.Show("Egreso registrado correctamente en caja.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);

                // C) Recargar la vista para que se actualicen los números
                // Pasamos 'true' porque sabemos que la caja sigue abierta
                string mensaje = Properties.Settings.Default.LastActionMessage;
                ActualizarVisualCaja(true, mensaje);
            }
        }

        private void BtnHistorialCaja_Click(object sender, RoutedEventArgs e)
        {
            // Abrimos la nueva ventana
            var ventanaHistorial = new Views.Dialogs.HistorialCajaWindow();
            ventanaHistorial.Owner = Window.GetWindow(this); // Para que salga centrada
            ventanaHistorial.ShowDialog();
        }

    }
}