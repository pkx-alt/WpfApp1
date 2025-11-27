using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using OrySiPOS.Data;
using OrySiPOS.Dialogs;
using OrySiPOS.Models;
using OrySiPOS.ViewModels;
using OrySiPOS.Views.Dialogs;

namespace OrySiPOS.Views
{
    public partial class CajaPage : Page
    {
        private DispatcherTimer timerReloj;

        // 1. CORRECCIÓN DE MONEDA: Usamos 'es-MX' para Pesos Mexicanos
        private CultureInfo culturaEspanol = new CultureInfo("es-MX"); // <--- ANTES DECÍA "es-ES"

        private Brush colorVerdeAbierto => (Brush)Application.Current.Resources["SuccessColor"];
        private Brush colorRojoCerrado => (Brush)Application.Current.Resources["DangerColor"];
        private Brush colorGrisDesactivado => (Brush)Application.Current.Resources["BorderBrush"];
        private Brush colorAzulIngreso => (Brush)Application.Current.Resources["PrimaryColor"];
        private Brush colorNaranjaEgreso => (Brush)Application.Current.Resources["WarningColor"];

        public CajaPage()
        {
            InitializeComponent();

            timerReloj = new DispatcherTimer();
            timerReloj.Tick += TimerReloj_Tick;
            timerReloj.Interval = new TimeSpan(0, 0, 1);
            timerReloj.Start();
            ActualizarFechaHora();

            CargarEstadoCaja();
        }

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

        private void CargarEstadoCaja()
        {
            bool estaAbierta = Properties.Settings.Default.IsBoxOpen;
            string mensajeGuardado = Properties.Settings.Default.LastActionMessage;
            ActualizarVisualCaja(estaAbierta, mensajeGuardado);
        }

        private void BtnAbrirCaja_Click(object sender, RoutedEventArgs e)
        {
            bool estadoActual = Properties.Settings.Default.IsBoxOpen;

            if (estadoActual == true)
            {
                // --- LÓGICA DE CIERRE ---
                decimal montoInicial = Properties.Settings.Default.MontoInicialCaja;

                // (Aquí podríamos calcular el 'esperado' real antes de abrir la ventana, 
                // pero por ahora pasamos el inicial. El cálculo final lo harás visualmente o 
                // podrías mover la lógica de cálculo aquí mismo).
                decimal efectivoEsperado = montoInicial;

                CierreCajaWindow dialog = new CierreCajaWindow(efectivoEsperado);
                dialog.Owner = Window.GetWindow(this);
                bool? dialogResult = dialog.ShowDialog();

                if (dialogResult == true)
                {
                    decimal totalContado = dialog.TotalContado;
                    string notas = dialog.Notas;
                    decimal diferencia = totalContado - efectivoEsperado;

                    using (var db = new InventarioDbContext())
                    {
                        var corteActivo = db.CortesCaja
                                            .OrderByDescending(c => c.Id)
                                            .FirstOrDefault(c => c.Abierta);

                        if (corteActivo != null)
                        {
                            corteActivo.FechaCierre = DateTime.Now;
                            corteActivo.MontoFinal = totalContado;
                            corteActivo.Diferencia = diferencia;
                            corteActivo.Notas = notas;
                            corteActivo.Abierta = false;
                            db.SaveChanges();
                        }
                    }

                    Properties.Settings.Default.IsBoxOpen = false;
                    Properties.Settings.Default.LastActionMessage = $"Cierre realizado el {DateTime.Now:g}";
                    Properties.Settings.Default.MontoInicialCaja = 0;
                    Properties.Settings.Default.Save();

                    ActualizarVisualCaja(false, Properties.Settings.Default.LastActionMessage);
                }
            }
            else
            {
                // --- LÓGICA DE APERTURA ---
                AperturaCajaWindow dialog = new AperturaCajaWindow();
                dialog.Owner = Window.GetWindow(this);
                bool? dialogResult = dialog.ShowDialog();

                if (dialogResult == true)
                {
                    decimal montoInicial = dialog.MontoInicial;

                    using (var db = new InventarioDbContext())
                    {
                        var nuevoCorte = new CorteCaja
                        {
                            FechaApertura = DateTime.Now,
                            MontoInicial = montoInicial,
                            Abierta = true,
                            MontoFinal = 0,
                            Diferencia = 0,
                            Notas = ""
                        };
                        db.CortesCaja.Add(nuevoCorte);
                        db.SaveChanges();
                    }

                    Properties.Settings.Default.IsBoxOpen = true;
                    Properties.Settings.Default.MontoInicialCaja = montoInicial;
                    Properties.Settings.Default.LastActionMessage = $"Apertura realizada el {DateTime.Now:g}";
                    Properties.Settings.Default.Save();

                    ActualizarVisualCaja(true, Properties.Settings.Default.LastActionMessage);
                }
            }
        }

        // --- AQUÍ ESTÁ LA MAGIA DE LOS DATOS REALES ---
        private void ActualizarVisualCaja(bool estaAbierta, string mensaje)
        {
            UltimoMovimientoTextBlock.Text = mensaje;

            if (estaAbierta)
            {
                EstadoEllipse.Fill = colorVerdeAbierto;
                EstadoTextBlock.Text = "CAJA ABIERTA";
                BtnAbrirCaja.Content = "Cerrar caja";
                BtnAbrirCaja.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#A12A45");

                BtnRegistrarIngreso.IsEnabled = true;
                BtnRegistrarIngreso.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#E59F71");
                BtnRegistrarIngreso.Foreground = Brushes.Black;

                BtnRegistrarEgreso.IsEnabled = true;
                BtnRegistrarEgreso.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#00A99D");
                BtnRegistrarEgreso.Foreground = Brushes.White;

                ResumenesPanel.Visibility = Visibility.Visible;

                // 2. CÁLCULO REAL DE MOVIMIENTOS (ADIÓS DATOS HARDCODEADOS)
                decimal montoInicial = Properties.Settings.Default.MontoInicialCaja;
                decimal egresosReales = 0;
                decimal ingresosReales = 0;

                // Variables para ventas
                decimal ventasEfectivo = 0;
                decimal ventasTarjeta = 0;
                decimal ventasTransferencia = 0;
                decimal totalGeneralVentas = 0;

                using (var db = new InventarioDbContext())
                {
                    DateTime inicioDia = DateTime.Today; // Desde las 00:00:00 de hoy

                    // A. Sumar Gastos (Egresos)
                    egresosReales = db.Gastos
                                      .Where(g => g.Fecha >= inicioDia)
                                      .Sum(g => (decimal?)g.Monto) ?? 0m;

                    // B. Sumar Ingresos (Entradas Manuales)
                    ingresosReales = db.Ingresos
                                       .Where(i => i.Fecha >= inicioDia)
                                       .Sum(i => (decimal?)i.Monto) ?? 0m;

                    // C. Sumar VENTAS (Clasificadas por método de pago SAT)
                    // Nota: Usamos ToList() primero para facilitar la suma en memoria y evitar errores de traducción de LINQ complejos
                    var ventasHoy = db.Ventas
                                      .Where(v => v.Fecha >= inicioDia)
                                      .ToList();

                    // "01" = Efectivo
                    ventasEfectivo = ventasHoy.Where(v => v.FormaPagoSAT == "01").Sum(v => v.Total);

                    // "04" = Tarjeta Crédito, "28" = Débito (Asumimos ambos como Tarjeta)
                    ventasTarjeta = ventasHoy.Where(v => v.FormaPagoSAT == "04" || v.FormaPagoSAT == "28").Sum(v => v.Total);

                    // "03" = Transferencia
                    ventasTransferencia = ventasHoy.Where(v => v.FormaPagoSAT == "03").Sum(v => v.Total);

                    // Total de todo lo vendido hoy
                    totalGeneralVentas = ventasHoy.Sum(v => v.Total);
                }

                // 3. Calculamos el total de EFECTIVO ESPERADO en la caja física
                //    (Base + VentasEfectivo + IngresosExtras - Gastos)
                //    Nota: Las ventas con tarjeta NO suman al efectivo físico.
                decimal totalEfectivoEsperado = montoInicial + ventasEfectivo + ingresosReales - egresosReales;

                // 4. Actualizamos la interfaz (UI)
                ResumenEstadoInicial.Text = $"Estado inicial: {montoInicial.ToString("C", culturaEspanol)}";

                ResumenVentasEfectivo.Text = $"Ventas en efectivo: {ventasEfectivo.ToString("C", culturaEspanol)}";
                ResumenOtrosIngresos.Text = $"Otros ingresos: {ingresosReales.ToString("C", culturaEspanol)}";
                ResumenEgresos.Text = $"Egresos: {egresosReales.ToString("C", culturaEspanol)}";

                ResumenTotalEsperado.Text = $"Total esperado (Efec): {totalEfectivoEsperado.ToString("C", culturaEspanol)}";

                // Panel derecho (Totales informativos)
                VentasEfectivo.Text = $"Efectivo: {ventasEfectivo.ToString("C", culturaEspanol)}";
                VentasTarjeta.Text = $"Tarjeta: {ventasTarjeta.ToString("C", culturaEspanol)}";
                VentasTransferencia.Text = $"Transferencia: {ventasTransferencia.ToString("C", culturaEspanol)}";

                VentasTotalGeneral.Text = $"Vendido Hoy: {totalGeneralVentas.ToString("C", culturaEspanol)}";
            }
            else
            {
                EstadoEllipse.Fill = colorRojoCerrado;
                EstadoTextBlock.Text = "CAJA CERRADA";
                BtnAbrirCaja.Content = "Abrir caja";
                BtnAbrirCaja.Background = colorVerdeAbierto;

                BtnRegistrarIngreso.IsEnabled = false;
                BtnRegistrarIngreso.Background = colorGrisDesactivado;
                BtnRegistrarIngreso.Foreground = colorGrisDesactivado;

                BtnRegistrarEgreso.IsEnabled = false;
                BtnRegistrarEgreso.Background = colorGrisDesactivado;
                BtnRegistrarEgreso.Foreground = colorGrisDesactivado;

                ResumenesPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnRegistrarIngreso_Click(object sender, RoutedEventArgs e)
        {
            var modalVM = new ViewModels.RegistroIngresoViewModel();
            var modalWindow = new RegistroIngresoWindow();

            modalVM.CerrarVentana = () =>
            {
                modalWindow.DialogResult = true;
                modalWindow.Close();
            };

            modalWindow.DataContext = modalVM;
            modalWindow.Owner = Window.GetWindow(this);

            bool? result = modalWindow.ShowDialog();

            if (result == true)
            {
                using (var db = new InventarioDbContext())
                {
                    db.Ingresos.Add(modalVM.NuevoIngreso);
                    db.SaveChanges();
                }

                MessageBox.Show("Ingreso registrado correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);

                string mensaje = Properties.Settings.Default.LastActionMessage;
                ActualizarVisualCaja(true, mensaje);
            }
        }

        private void BtnRegistrarEgreso_Click(object sender, RoutedEventArgs e)
        {
            var modalVM = new RegistroGastoViewModel();
            var modalWindow = new RegistroGastoWindow();

            modalVM.CerrarVentana = () =>
            {
                modalWindow.DialogResult = true;
                modalWindow.Close();
            };

            modalWindow.DataContext = modalVM;
            modalWindow.Owner = Window.GetWindow(this);

            bool? result = modalWindow.ShowDialog();

            if (result == true)
            {
                using (var db = new InventarioDbContext())
                {
                    db.Gastos.Add(modalVM.NuevoGasto);
                    db.SaveChanges();
                }

                MessageBox.Show("Egreso registrado correctamente en caja.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);

                string mensaje = Properties.Settings.Default.LastActionMessage;
                ActualizarVisualCaja(true, mensaje);
            }
        }

        private void BtnHistorialCaja_Click(object sender, RoutedEventArgs e)
        {
            var ventanaHistorial = new Views.Dialogs.HistorialCajaWindow();
            ventanaHistorial.Owner = Window.GetWindow(this);
            ventanaHistorial.ShowDialog();
        }
    }
}