using Microsoft.Win32;
using System;
using System.Collections;
using System.IO;
using System.Linq; // Necesario para Select en exportación
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using WpfApp1.Services; // Asegúrate de tener tus servicios aquí

namespace WpfApp1.Views
{
    public partial class ReportesPage : Page
    {
        private string _reporteSeleccionadoTag = "";

        public ReportesPage()
        {
            InitializeComponent();
            // Fechas por defecto: Mes actual
            DtInicio.SelectedDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            DtFin.SelectedDate = DateTime.Now;
        }

        // --- 1. NAVEGACIÓN PRINCIPAL ---
        // En Views/ReportesPage.xaml.cs

        private void Categoria_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string categoria)
            {
                // 1. Transición de Menús
                MenuCategorias.Visibility = Visibility.Collapsed;
                SubMenuContainer.Visibility = Visibility.Visible;

                // --- LIMPIEZA TOTAL (El paso clave) ---
                // Aseguramos que TODO esté oculto antes de mostrar el elegido.
                PanelOpcionesVentas.Visibility = Visibility.Collapsed;
                PanelOpcionesInventario.Visibility = Visibility.Collapsed;
                PanelOpcionesFinancieros.Visibility = Visibility.Collapsed;
                PanelOpcionesAdmin.Visibility = Visibility.Collapsed;       // <--- Faltaba limpiar este
                PanelOpcionesCotizaciones.Visibility = Visibility.Collapsed; // <--- Y este

                // 2. Mostramos SOLO el panel correspondiente
                switch (categoria)
                {
                    case "Ventas":
                        PanelOpcionesVentas.Visibility = Visibility.Visible;
                        TxtSubtitulo.Text = "Categoría: VENTAS";
                        break;

                    case "Inventario":
                        PanelOpcionesInventario.Visibility = Visibility.Visible;
                        TxtSubtitulo.Text = "Categoría: INVENTARIO";
                        break;

                    case "Financieros":
                        PanelOpcionesFinancieros.Visibility = Visibility.Visible;
                        TxtSubtitulo.Text = "Categoría: FINANZAS";
                        break;

                    case "Administrativos":
                        PanelOpcionesAdmin.Visibility = Visibility.Visible;
                        TxtSubtitulo.Text = "Categoría: ADMINISTRATIVOS";
                        break;

                    case "Cotizaciones":
                        PanelOpcionesCotizaciones.Visibility = Visibility.Visible;
                        TxtSubtitulo.Text = "Categoría: COTIZACIONES";
                        break;
                }
            }
        }

        private void BtnVolver_Click(object sender, RoutedEventArgs e)
        {
            SubMenuContainer.Visibility = Visibility.Collapsed;
            VistaResultados.Visibility = Visibility.Collapsed;
            MenuCategorias.Visibility = Visibility.Visible;

            TxtSubtitulo.Text = "Selecciona una categoría para comenzar el análisis";
            ResetearControles();
        }

        // --- 2. EJECUCIÓN RÁPIDA ---
        private void ReporteEspecifico_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                _reporteSeleccionadoTag = btn.Tag.ToString();

                // Habilitar controles
                PanelFiltros.IsEnabled = true;
                PanelFiltros.Opacity = 1;
                BtnGenerar.IsEnabled = true;

                // Configurar fechas
                bool usaFechas = !_reporteSeleccionadoTag.StartsWith("Inv_");
                DtInicio.IsEnabled = usaFechas;
                DtFin.IsEnabled = usaFechas;

                TxtInfoReporte.Text = usaFechas ? "Mostrando periodo seleccionado." : "Mostrando estado actual.";

                // ¡Ejecutar!
                EjecutarReporte();
            }
        }

        private void BtnGenerar_Click(object sender, RoutedEventArgs e)
        {
            EjecutarReporte();
        }

        // --- 3. MOTOR DE REPORTES ---
        // Reemplaza todo el método EjecutarReporte con este corregido:

        private void EjecutarReporte()
        {
            // Interfaz
            SubMenuContainer.Visibility = Visibility.Collapsed;
            VistaResultados.Visibility = Visibility.Visible;

            DateTime inicio = DtInicio.SelectedDate ?? DateTime.Today;
            DateTime fin = DtFin.SelectedDate ?? DateTime.Today;
            DateTime finAjustado = fin.Date.AddDays(1).AddTicks(-1);

            // Instanciamos los servicios
            var servicioVentas = new ReportesVentasService();
            var servicioInventario = new ReportesInventarioService();

            // Estos dos son nuevos, asegúrate de tener los usings arriba si te marca error
            // (Si no has creado los archivos de Finanzas/Admin/Cotizaciones, coméntalos por ahora)
            var servicioFinanzas = new ReportesFinancierosService();
            var servicioAdmin = new ReportesAdministrativosService();
            var servicioCot = new ReportesCotizacionesService();

            string titulo = "";
            IEnumerable datos = null;

            try
            {
                switch (_reporteSeleccionadoTag)
                {
                    // --- VENTAS ---
                    case "Ventas_MasVendidos":
                        titulo = $"Productos Más Vendidos ({inicio:dd/MM} - {fin:dd/MM})";
                        datos = servicioVentas.ObtenerProductosMasVendidos(inicio, finAjustado);
                        break;
                    case "Ventas_PorDepto":
                        titulo = $"Ventas por Departamento ({inicio:dd/MM} - {fin:dd/MM})";
                        datos = servicioVentas.ObtenerVentasPorCategoria(inicio, finAjustado);
                        break;
                    case "Ventas_Comparativo":
                        titulo = $"Comparativo Mensual de Ventas";
                        // CORRECCIÓN AQUÍ: Usamos 'servicioVentas'
                        datos = servicioVentas.ObtenerComparativoMensual(inicio, finAjustado);
                        break;

                    // --- INVENTARIO ---
                    case "Inv_Valorizado":
                        titulo = "Inventario Valorizado (Al día de hoy)";
                        datos = servicioInventario.ObtenerInventarioValorizado();
                        break;
                    case "Inv_BajoStock":
                        titulo = "Productos con Bajo Stock (<= 5 unidades)";
                        datos = servicioInventario.ObtenerProductosBajoStock(5);
                        break;
                    case "Inv_Rotacion":
                        titulo = $"Movimientos de Inventario ({inicio:dd/MM} - {fin:dd/MM})";
                        datos = servicioInventario.ObtenerMovimientosInventario(inicio, finAjustado);
                        break;

                    // --- FINANCIEROS ---
                    case "Fin_Utilidad":
                        titulo = $"Estado de Resultados ({inicio:dd/MM} - {fin:dd/MM})";
                        datos = servicioFinanzas.ObtenerUtilidadNeta(inicio, finAjustado);
                        break;
                    case "Fin_Balance":
                        titulo = $"Balance Ingresos vs Egresos ({inicio:dd/MM} - {fin:dd/MM})";
                        datos = servicioFinanzas.ObtenerBalanceIngresosEgresos(inicio, finAjustado);
                        break;

                    // --- ADMINISTRATIVOS ---
                    case "Admin_TopClientes":
                        titulo = $"Mejores Clientes ({inicio:dd/MM} - {fin:dd/MM})";
                        datos = servicioAdmin.ObtenerMejoresClientes(inicio, finAjustado);
                        break;

                    // --- COTIZACIONES ---
                    case "Cot_Pendientes":
                        titulo = $"Cotizaciones Vigentes ({inicio:dd/MM} - {fin:dd/MM})";
                        datos = servicioCot.ObtenerPendientes(inicio, finAjustado);
                        break;
                    case "Cot_Efectividad":
                        titulo = "Indicadores de Efectividad";
                        datos = servicioCot.ObtenerEfectividad(inicio, finAjustado);
                        break;

                    default:
                        titulo = "Reporte no implementado";
                        break;
                }

                TxtTituloResultado.Text = titulo;
                GridResultados.ItemsSource = datos;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private void BtnCerrarResultados_Click(object sender, RoutedEventArgs e)
        {
            VistaResultados.Visibility = Visibility.Collapsed;
            GridResultados.ItemsSource = null;
            SubMenuContainer.Visibility = Visibility.Visible;
        }

        private void ResetearControles()
        {
            _reporteSeleccionadoTag = "";
            PanelFiltros.IsEnabled = false;
            PanelFiltros.Opacity = 0.5;
            BtnGenerar.IsEnabled = false;
            TxtInfoReporte.Text = "(Selecciona un reporte arriba)";
        }

        // --- 4. EXPORTAR A EXCEL (CSV) ---
        private void BtnExportar_Click(object sender, RoutedEventArgs e)
        {
            var datos = GridResultados.ItemsSource as IEnumerable;
            if (datos == null) return;

            SaveFileDialog saveDialog = new SaveFileDialog
            {
                Filter = "Archivo CSV (*.csv)|*.csv",
                FileName = $"Reporte_{DateTime.Now:yyyyMMdd_HHmm}.csv"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    var sb = new StringBuilder();

                    // Obtener enumerador para leer la lista
                    var enumerator = datos.GetEnumerator();
                    if (!enumerator.MoveNext()) return; // Lista vacía

                    // Usamos Reflexión para leer las propiedades del primer objeto (encabezados)
                    object primerItem = enumerator.Current;
                    PropertyInfo[] propiedades = primerItem.GetType().GetProperties();

                    // Escribir encabezados
                    sb.AppendLine(string.Join(",", propiedades.Select(p => p.Name)));

                    // Escribir datos (reiniciamos el recorrido o iteramos de nuevo)
                    foreach (var item in datos)
                    {
                        var valores = propiedades.Select(p =>
                        {
                            var valor = p.GetValue(item, null)?.ToString() ?? "";
                            // Escapar comas
                            return valor.Contains(",") ? $"\"{valor}\"" : valor;
                        });
                        sb.AppendLine(string.Join(",", valores));
                    }

                    File.WriteAllText(saveDialog.FileName, sb.ToString(), Encoding.UTF8);
                    MessageBox.Show("Reporte exportado correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al exportar: {ex.Message}", "Error");
                }
            }
        }

        // --- 5. FORMATO AUTOMÁTICO (Moneda y Fecha) ---
        // En Views/ReportesPage.xaml.cs

        private void GridResultados_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            // 1. DETECTAR COLUMNAS DE DINERO
            if (e.PropertyName == "TotalDinero" ||
                e.PropertyName == "TotalVendido" ||
                e.PropertyName == "Monto" ||
                e.PropertyName == "Precio" ||
                e.PropertyName == "CostoUnitario" ||
                e.PropertyName == "ValorTotal" ||
                e.PropertyName == "TotalGastado" ||
                e.PropertyName == "TicketPromedio" ||
                e.PropertyName == "PromedioTicket" ||
                e.PropertyName == "Total")
            {
                if (e.Column is DataGridTextColumn col)
                {
                    // A. Aplicar formato de moneda de forma segura
                    if (col.Binding is System.Windows.Data.Binding binding)
                    {
                        binding.StringFormat = "C2"; // $1,234.56
                    }

                    // B. Alinear a la derecha (Esto crea el Setter programáticamente)
                    var style = new Style(typeof(TextBlock));
                    style.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Right));
                    col.ElementStyle = style;
                }
            }

            // 2. OCULTAR COLUMNAS AUXILIARES
            if (e.PropertyName == "FechaOrden")
            {
                e.Cancel = true;
            }

            // 3. DETECTAR COLUMNAS DE FECHA
            if (e.PropertyName == "Fecha" && e.Column is DataGridTextColumn colFecha)
            {
                if (colFecha.Binding is System.Windows.Data.Binding binding)
                {
                    binding.StringFormat = "dd/MM/yyyy HH:mm";
                }
            }
        }
    }
}