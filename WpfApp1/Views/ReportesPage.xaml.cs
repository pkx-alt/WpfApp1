using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic; // Necesario para List<>
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media; // Para los colores (Brushes)
using WpfApp1.Services;

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

            // Deshabilitado al inicio
            DtInicio.IsEnabled = false;
            DtFin.IsEnabled = false;
        }

        // --- LÓGICA DE MENÚS ---
        private void Categoria_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string categoria)
            {
                MenuCategorias.Visibility = Visibility.Collapsed;
                SubMenuContainer.Visibility = Visibility.Visible;

                PanelOpcionesVentas.Visibility = Visibility.Collapsed;
                PanelOpcionesInventario.Visibility = Visibility.Collapsed;
                PanelOpcionesFinancieros.Visibility = Visibility.Collapsed;
                PanelOpcionesAdmin.Visibility = Visibility.Collapsed;
                PanelOpcionesCotizaciones.Visibility = Visibility.Collapsed;

                switch (categoria)
                {
                    case "Ventas": PanelOpcionesVentas.Visibility = Visibility.Visible; TxtSubtitulo.Text = "Categoría: VENTAS"; break;
                    case "Inventario": PanelOpcionesInventario.Visibility = Visibility.Visible; TxtSubtitulo.Text = "Categoría: INVENTARIO"; break;
                    case "Financieros": PanelOpcionesFinancieros.Visibility = Visibility.Visible; TxtSubtitulo.Text = "Categoría: FINANZAS"; break;
                    case "Administrativos": PanelOpcionesAdmin.Visibility = Visibility.Visible; TxtSubtitulo.Text = "Categoría: ADMINISTRATIVOS"; break;
                    case "Cotizaciones": PanelOpcionesCotizaciones.Visibility = Visibility.Visible; TxtSubtitulo.Text = "Categoría: COTIZACIONES"; break;
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

        private void ReporteEspecifico_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                _reporteSeleccionadoTag = btn.Tag.ToString();

                bool usaFechas = !_reporteSeleccionadoTag.StartsWith("Inv_");
                DtInicio.IsEnabled = usaFechas;
                DtFin.IsEnabled = usaFechas;
                TxtInfoReporte.Text = usaFechas ? "Mostrando periodo seleccionado." : "Mostrando estado actual.";

                BtnExportar.IsEnabled = true;

                // Resetear vista a Tabla por defecto
                RadioTabla.IsChecked = true;

                EjecutarReporte();
            }
        }

        private void OnFechaFiltroChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_reporteSeleccionadoTag))
            {
                if (DtInicio.SelectedDate.HasValue && DtFin.SelectedDate.HasValue &&
                    DtInicio.SelectedDate.Value > DtFin.SelectedDate.Value) return;

                EjecutarReporte();
            }
        }

        // --- MOTOR DE REPORTES ---
        private void EjecutarReporte()
        {
            SubMenuContainer.Visibility = Visibility.Collapsed;
            VistaResultados.Visibility = Visibility.Visible;

            DateTime inicio = DtInicio.SelectedDate ?? DateTime.Today;
            DateTime fin = DtFin.SelectedDate ?? DateTime.Today;
            DateTime finAjustado = fin.Date.AddDays(1).AddTicks(-1);

            var servicioVentas = new ReportesVentasService();
            var servicioInventario = new ReportesInventarioService();
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
                        datos = servicioVentas.ObtenerComparativoMensual(inicio, finAjustado);
                        break;

                    // --- INVENTARIO ---
                    case "Inv_Valorizado":
                        titulo = "Inventario Valorizado (Al día de hoy)";
                        datos = servicioInventario.ObtenerInventarioValorizado();
                        break;
                    case "Inv_BajoStock":
                        titulo = "Productos con Bajo Stock";
                        datos = servicioInventario.ObtenerProductosBajoStock(5);
                        break;
                    case "Inv_Rotacion":
                        titulo = $"Movimientos de Inventario";
                        datos = servicioInventario.ObtenerMovimientosInventario(inicio, finAjustado);
                        break;

                    // --- FINANCIEROS ---
                    case "Fin_Utilidad":
                        titulo = $"Estado de Resultados";
                        datos = servicioFinanzas.ObtenerUtilidadNeta(inicio, finAjustado);
                        break;
                    case "Fin_Balance":
                        titulo = $"Balance Ingresos vs Egresos";
                        datos = servicioFinanzas.ObtenerBalanceIngresosEgresos(inicio, finAjustado);
                        break;

                    // --- ADMINISTRATIVOS ---
                    case "Admin_TopClientes":
                        titulo = $"Mejores Clientes";
                        datos = servicioAdmin.ObtenerMejoresClientes(inicio, finAjustado);
                        break;

                    // --- COTIZACIONES ---
                    case "Cot_Pendientes":
                        titulo = $"Cotizaciones Vigentes";
                        datos = servicioCot.ObtenerPendientes(inicio, finAjustado);
                        break;
                    case "Cot_Efectividad":
                        titulo = "Indicadores de Efectividad";
                        datos = servicioCot.ObtenerEfectividad(inicio, finAjustado);
                        break;
                }

                TxtTituloResultado.Text = titulo;
                GridResultados.ItemsSource = datos;

                // GENERAR GRÁFICA
                PrepararDatosGrafica(datos);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        // --- LÓGICA DE GRÁFICAS MANUALES ---

        public class DatoGrafica
        {
            public string Etiqueta { get; set; }
            public double Valor { get; set; }
            public string ValorFormateado { get; set; }
            public double AlturaCalculada { get; set; } // Altura en pixeles
            public string TooltipTexto { get; set; }
            public Brush ColorBarra { get; set; }
        }

        private void PrepararDatosGrafica(IEnumerable datosOrigen)
        {
            if (datosOrigen == null) return;

            List<DatoGrafica> listaGrafica = new List<DatoGrafica>();
            bool graficaDisponible = false;

            var listaDinamica = datosOrigen.Cast<dynamic>().ToList();
            double alturaMaxPixel = 250;

            // --- VENTAS ---
            if (_reporteSeleccionadoTag == "Ventas_MasVendidos")
            {
                graficaDisponible = true;
                foreach (var item in listaDinamica)
                {
                    double val = (double)item.Unidades;
                    listaGrafica.Add(new DatoGrafica
                    {
                        Etiqueta = item.Producto,
                        Valor = val,
                        ValorFormateado = val.ToString() + " u.",
                        TooltipTexto = $"{item.Producto}: {val} unidades",
                        ColorBarra = Brushes.CornflowerBlue
                    });
                }
            }
            else if (_reporteSeleccionadoTag == "Ventas_PorDepto")
            {
                graficaDisponible = true;
                foreach (var item in listaDinamica)
                {
                    double val = (double)item.TotalVendido;
                    listaGrafica.Add(new DatoGrafica
                    {
                        Etiqueta = item.Departamento,
                        Valor = val,
                        ValorFormateado = val.ToString("C0"),
                        TooltipTexto = $"{item.Departamento}: {val:C}",
                        ColorBarra = Brushes.MediumSeaGreen
                    });
                }
            }
            else if (_reporteSeleccionadoTag == "Ventas_Comparativo")
            {
                graficaDisponible = true;
                foreach (var item in listaDinamica)
                {
                    double val = (double)item.TotalVendido;
                    listaGrafica.Add(new DatoGrafica
                    {
                        Etiqueta = item.Periodo,
                        Valor = val,
                        ValorFormateado = val.ToString("C0"),
                        TooltipTexto = $"{item.Periodo}: {val:C}",
                        ColorBarra = Brushes.Orange
                    });
                }
            }
            // --- INVENTARIO ---
            else if (_reporteSeleccionadoTag == "Inv_Valorizado")
            {
                graficaDisponible = true;
                var topValor = listaDinamica.Take(15).ToList();
                foreach (var item in topValor)
                {
                    double val = (double)item.ValorTotal;
                    listaGrafica.Add(new DatoGrafica
                    {
                        Etiqueta = item.Producto,
                        Valor = val,
                        ValorFormateado = val.ToString("C0"),
                        TooltipTexto = $"{item.Producto}: {val:C} invertidos",
                        ColorBarra = Brushes.Teal
                    });
                }
            }
            else if (_reporteSeleccionadoTag == "Inv_BajoStock")
            {
                graficaDisponible = true;
                foreach (var item in listaDinamica)
                {
                    double val = (double)item.StockActual;
                    listaGrafica.Add(new DatoGrafica
                    {
                        Etiqueta = item.Producto,
                        Valor = val,
                        ValorFormateado = val.ToString(),
                        TooltipTexto = $"{item.Producto}: Quedan {val}",
                        ColorBarra = Brushes.Crimson
                    });
                }
            }
            // --- FINANCIEROS ---
            else if (_reporteSeleccionadoTag == "Fin_Utilidad")
            {
                graficaDisponible = true;
                var filasReales = listaDinamica.Where(x => !string.IsNullOrWhiteSpace(x.Concepto) && x.Monto != 0).ToList();

                foreach (var item in filasReales)
                {
                    decimal monto = (decimal)item.Monto;
                    string concepto = item.Concepto.Trim().Replace("(+)", "").Replace("(-)", "").Replace("(=)", "").Trim();

                    Brush color;
                    if (concepto.ToUpper().Contains("UTILIDAD") || concepto.ToUpper().Contains("NETA"))
                        color = Brushes.Goldenrod;
                    else if (monto < 0)
                        color = Brushes.IndianRed;
                    else
                        color = Brushes.SeaGreen;

                    listaGrafica.Add(new DatoGrafica
                    {
                        Etiqueta = concepto,
                        Valor = (double)Math.Abs(monto),
                        ValorFormateado = monto.ToString("C0"),
                        TooltipTexto = $"{concepto}: {monto:C}",
                        ColorBarra = color
                    });
                }
            }
            else if (_reporteSeleccionadoTag == "Fin_Balance")
            {
                graficaDisponible = true;
                foreach (var item in listaDinamica)
                {
                    decimal total = (decimal)item.Total;
                    string tipo = item.Tipo;

                    Brush color = Brushes.Gray;
                    if (tipo.Contains("Gastos") || item.Categoria == "SALIDAS") color = Brushes.IndianRed;
                    else if (tipo.Contains("Ventas") || tipo.Contains("Ingresos") || item.Categoria == "ENTRADAS") color = Brushes.MediumSeaGreen;

                    if (item.Categoria == "RESULTADO")
                    {
                        color = total >= 0 ? Brushes.Goldenrod : Brushes.DarkRed;
                    }

                    listaGrafica.Add(new DatoGrafica
                    {
                        Etiqueta = tipo,
                        Valor = (double)Math.Abs(total),
                        ValorFormateado = total.ToString("C0"),
                        TooltipTexto = $"{tipo}: {total:C}",
                        ColorBarra = color
                    });
                }
            }
            // --- ADMINISTRATIVOS (¡AQUÍ ESTÁ LO NUEVO!) ---
            else if (_reporteSeleccionadoTag == "Admin_TopClientes")
            {
                graficaDisponible = true;
                // Graficamos el total gastado por cliente
                foreach (var item in listaDinamica)
                {
                    double val = (double)item.TotalGastado;
                    listaGrafica.Add(new DatoGrafica
                    {
                        Etiqueta = item.Cliente,
                        Valor = val,
                        ValorFormateado = val.ToString("C0"),
                        TooltipTexto = $"{item.Cliente}: Ha comprado {val:C} en {item.Compras} tickets",
                        ColorBarra = Brushes.SlateBlue // Morado para clientes
                    });
                }
            }
            // --- COTIZACIONES (¡Y ESTO TAMBIÉN!) ---
            else if (_reporteSeleccionadoTag == "Cot_Pendientes")
            {
                graficaDisponible = true;
                foreach (var item in listaDinamica)
                {
                    double val = (double)item.Total;
                    listaGrafica.Add(new DatoGrafica
                    {
                        Etiqueta = $"Folio {item.Folio}\n{item.Cliente}", // Doble línea en etiqueta
                        Valor = val,
                        ValorFormateado = val.ToString("C0"),
                        TooltipTexto = $"Vence el {item.Vence:dd/MM} ({item.DiasRestantes} días restantes)",
                        ColorBarra = Brushes.DarkOrange // Naranja de "Pendiente"
                    });
                }
            }
            else if (_reporteSeleccionadoTag == "Cot_Efectividad")
            {
                graficaDisponible = true;
                foreach (var item in listaDinamica)
                {
                    // Aquí mezclamos conteos y porcentajes, así que solo graficamos
                    // si el valor es mayor a 1 (para evitar que el % se vea como una línea invisible)
                    // O podríamos multiplicar el % por 100, pero para simplificar lo mostramos tal cual.
                    double val = Convert.ToDouble(item.Valor);

                    // Truco: Si es la tasa (menor a 1), la mostramos como porcentaje en texto pero la barra será pequeña
                    string textoValor = val <= 1 ? val.ToString("P0") : val.ToString("N0");

                    listaGrafica.Add(new DatoGrafica
                    {
                        Etiqueta = item.Metrica,
                        Valor = val,
                        ValorFormateado = textoValor,
                        TooltipTexto = $"{item.Metrica}: {textoValor}",
                        ColorBarra = Brushes.SteelBlue
                    });
                }
            }


            // --- CÁLCULOS DE RENDERIZADO ---
            if (graficaDisponible && listaGrafica.Count > 0)
            {
                double maxValor = listaGrafica.Max(x => x.Valor);
                if (maxValor == 0) maxValor = 1;

                foreach (var punto in listaGrafica)
                {
                    punto.AlturaCalculada = (punto.Valor / maxValor) * alturaMaxPixel;
                    if (punto.AlturaCalculada < 2) punto.AlturaCalculada = 2;
                }

                GraficaItemsControl.ItemsSource = listaGrafica;
                TxtSinGrafica.Visibility = Visibility.Collapsed;
                GraficaItemsControl.Visibility = Visibility.Visible;
                RadioGrafica.IsEnabled = true;
            }
            else
            {
                GraficaItemsControl.ItemsSource = null;
                TxtSinGrafica.Visibility = Visibility.Visible;
                GraficaItemsControl.Visibility = Visibility.Collapsed;

                RadioTabla.IsChecked = true;
                RadioGrafica.IsEnabled = false;
                GridResultados.Visibility = Visibility.Visible;
                ContenedorGrafica.Visibility = Visibility.Collapsed;
            }
        }

        // --- SWITCH VISTA TABLA / GRÁFICA ---
        private void ToggleVista_Checked(object sender, RoutedEventArgs e)
        {
            if (GridResultados == null || ContenedorGrafica == null) return;

            if (RadioTabla.IsChecked == true)
            {
                GridResultados.Visibility = Visibility.Visible;
                ContenedorGrafica.Visibility = Visibility.Collapsed;
            }
            else
            {
                GridResultados.Visibility = Visibility.Collapsed;
                ContenedorGrafica.Visibility = Visibility.Visible;
            }
        }

        // --- UTILIDADES ---
        private void BtnCerrarResultados_Click(object sender, RoutedEventArgs e)
        {
            VistaResultados.Visibility = Visibility.Collapsed;
            GridResultados.ItemsSource = null;
            SubMenuContainer.Visibility = Visibility.Visible;
            ResetearControles();
        }

        private void ResetearControles()
        {
            _reporteSeleccionadoTag = "";
            BtnExportar.IsEnabled = false;
            DtInicio.IsEnabled = false;
            DtFin.IsEnabled = false;
            TxtInfoReporte.Text = "(Selecciona un reporte arriba)";
        }

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
                    var enumerator = datos.GetEnumerator();
                    if (!enumerator.MoveNext()) return;

                    object primerItem = enumerator.Current;
                    PropertyInfo[] propiedades = primerItem.GetType().GetProperties();

                    sb.AppendLine(string.Join(",", propiedades.Select(p => p.Name)));

                    foreach (var item in datos)
                    {
                        var valores = propiedades.Select(p =>
                        {
                            var valor = p.GetValue(item, null)?.ToString() ?? "";
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

        private void GridResultados_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyName == "TotalDinero" || e.PropertyName == "TotalVendido" ||
                e.PropertyName == "Monto" || e.PropertyName == "Precio" ||
                e.PropertyName == "CostoUnitario" || e.PropertyName == "ValorTotal" ||
                e.PropertyName == "TotalGastado" || e.PropertyName == "TicketPromedio" ||
                e.PropertyName == "PromedioTicket" || e.PropertyName == "Total")
            {
                if (e.Column is DataGridTextColumn col)
                {
                    if (col.Binding is System.Windows.Data.Binding binding)
                    {
                        binding.StringFormat = "C2";
                    }
                    var style = new Style(typeof(TextBlock));
                    style.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Right));
                    col.ElementStyle = style;
                }
            }

            if (e.PropertyName == "FechaOrden") e.Cancel = true;

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