using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using OrySiPOS.Data;
using OrySiPOS.Models;
using OrySiPOS.ViewModels;
using OrySiPOS.Views.Dialogs;
using QRCoder; // <--- ¡Nuevo!
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
// --- ¡NUEVOS USINGS OBLIGATORIOS PARA ACCEDER A LA BD Y MODELOS! ---
using System;
using System.Diagnostics; // Para abrir el PDF automáticamente al final
using System.IO;       // Para guardar el archivo
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents; // Para crear el reporte (FlowDocument, Table, Paragraph)
using System.Windows.Input;
using System.Windows.Media;     // Para colores y pinceles (Brushes)
using Colors = QuestPDF.Helpers.Colors; // Para evitar conflicto con System.Windows.Media.Colors
// ------------------------------------------------------------------

namespace OrySiPOS.Views
{
    public partial class FacturacionPage : Page
    {
        public FacturacionPage()
        {
            InitializeComponent();
            // Conectamos el ViewModel
            this.DataContext = new FacturacionViewModel();
        }

        // En Views/FacturacionPage.xaml.cs

        private void BtnExportarHistorial_Click(object sender, RoutedEventArgs e)
        {
            // 1. Obtenemos el ViewModel para acceder a los datos
            // (Hacemos un "casting" para decirle a C# que el DataContext es de tipo FacturacionViewModel)
            var vm = this.DataContext as FacturacionViewModel;

            if (vm == null || vm.ListaHistorial == null || vm.ListaHistorial.Count == 0)
            {
                MessageBox.Show("No hay datos en la lista para exportar.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 2. Preparamos el cuadro de diálogo para guardar
            SaveFileDialog saveDialog = new SaveFileDialog
            {
                Filter = "Archivo Excel (CSV)|*.csv",
                FileName = $"Facturas_{DateTime.Now:yyyyMMdd_HHmm}.csv", // Nombre sugerido con fecha
                Title = "Guardar reporte de facturas"
            };

            // 3. Si el usuario elige dónde guardar...
            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    // 4. Construimos el contenido del archivo (El CSV)
                    var sb = new StringBuilder();

                    // --- A. Encabezados (Títulos de las columnas) ---
                    sb.AppendLine("Serie/Folio,UUID Fiscal,Receptor (Cliente),Total,Estado");

                    // --- B. Filas de datos ---
                    foreach (var item in vm.ListaHistorial)
                    {
                        // Truco: Ponemos comillas "" alrededor de los textos por si contienen comas intermedias
                        // Ejemplo: "Papelería, S.A. de C.V." -> Las comillas protegen esa coma interna.

                        string folio = $"\"{item.SerieFolio}\"";
                        string uuid = $"\"{item.UUID}\"";
                        string receptor = $"\"{item.Receptor}\"";
                        string total = item.Total.ToString(); // Excel entiende números simples
                        string estado = $"\"{item.Estado}\"";

                        // Unimos todo con comas
                        sb.AppendLine($"{folio},{uuid},{receptor},{total},{estado}");
                    }

                    // 5. Guardamos el archivo
                    // Usamos Encoding.UTF8 para que respete acentos y ñ
                    File.WriteAllText(saveDialog.FileName, sb.ToString(), Encoding.UTF8);

                    MessageBox.Show("¡Reporte exportado correctamente!", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ocurrió un error al guardar: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // --- MÉTODO NUEVO: EXPORTAR TICKETS PENDIENTES CON DETALLE CFDI ---
        private void BtnExportarPendientes_Click(object sender, RoutedEventArgs e)
        {
            var vm = this.DataContext as FacturacionViewModel;

            if (vm == null || vm.ListaPendientes.Count == 0)
            {
                MessageBox.Show("No hay tickets pendientes para exportar.", "Aviso");
                return;
            }

            // 1. Diálogo para guardar
            SaveFileDialog saveDialog = new SaveFileDialog
            {
                Filter = "Archivo Excel (CSV)|*.csv",
                FileName = $"TicketsPendientes_{DateTime.Now:yyyyMMdd_HHmm}.csv",
                Title = "Exportar Tickets Pendientes"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    var sb = new StringBuilder();

                    // --- ENCABEZADOS CFDI (LO CRÍTICO PARA EL CONTADOR) ---
                    sb.AppendLine("VentaId,Fecha,Total,Subtotal,IVA,MetodoPagoSAT,FormaPagoSAT,ClienteRFC,ClienteRazonSocial,ClienteUsoCFDI,ClienteRegimenFiscal,ClienteCP,ClaveSat,ClaveUnidad,Cantidad,Descripcion,PrecioUnitario");

                    using (var db = new InventarioDbContext())
                    {
                        // Solo exportamos las ventas visibles en la lista actual
                        var idsAExportar = vm.ListaPendientes.Select(p => p.VentaId).ToList();

                        // Traemos de la DB las ventas completas.
                        // Usamos .Include() y .ThenInclude() para traer la información de otras tablas.
                        var ventasCompletas = db.Ventas
                            .Include(v => v.Cliente)
                            .Include(v => v.Detalles)
                                .ThenInclude(d => d.Producto) // Accedemos al Producto desde el Detalle
                            .Where(v => idsAExportar.Contains(v.VentaId))
                            .ToList();

                        // 2. CONSTRUCCIÓN DE LAS FILAS
                        foreach (var venta in ventasCompletas)
                        {
                            // DATOS DEL RECEPTOR (Cliente). Usamos valores por defecto si es Público General.
                            string rfc = venta.Cliente?.RFC ?? "XAXX010101000";
                            string razonSocial = venta.Cliente?.RazonSocial ?? "Público en General";
                            string usoCfdi = venta.Cliente?.UsoCFDI ?? "P01";
                            string regimenFiscal = venta.Cliente?.RegimenFiscal ?? "616"; // 616: Sin Obligaciones Fiscales (para Púb. Gral.)
                            string cp = venta.Cliente?.CodigoPostal ?? "00000";

                            // Iteramos los detalles de cada venta
                            foreach (var detalle in venta.Detalles)
                            {
                                // DATOS DEL CONCEPTO (Producto)
                                // Usamos el operador ternario para asegurarnos de que no sea null
                                string claveSat = detalle.Producto?.ClaveSat ?? "01010101";
                                string claveUnidad = detalle.Producto?.ClaveUnidad ?? "H87";

                                // Limpiamos descripciones de comas y comillas para el CSV
                                string descLimpia = detalle.Producto?.Descripcion.Replace(",", " ").Replace("\"", "") ?? "Producto sin descripción";

                                // CONSTRUCCIÓN DE LA LÍNEA
                                sb.AppendLine(
                                    $"{venta.VentaId}," +
                                    $"{venta.Fecha:yyyy-MM-dd HH:mm:ss}," +
                                    $"{venta.Total}," +
                                    $"{venta.Subtotal}," +
                                    $"{venta.IVA}," +
                                    $"{venta.MetodoPagoSAT}," +
                                    $"{venta.FormaPagoSAT}," +
                                    $"{rfc}," +
                                    $"\"{razonSocial}\"," + // Ponemos comillas para proteger el nombre
                                    $"{usoCfdi}," +
                                    $"{regimenFiscal}," +
                                    $"{cp}," +
                                    $"{claveSat}," +
                                    $"{claveUnidad}," +
                                    $"{detalle.Cantidad}," +
                                    $"\"{descLimpia}\"," +
                                    $"{detalle.PrecioUnitario}"
                                );
                            }
                        }
                    }

                    // 3. Guardar el archivo
                    File.WriteAllText(saveDialog.FileName, sb.ToString(), Encoding.UTF8);

                    MessageBox.Show("¡Tickets pendientes exportados correctamente! El contador tiene todo lo que necesita.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);

                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ocurrió un error al guardar: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        // --- FIN MÉTODO NUEVO ---


        // En Views/FacturacionPage.xaml.cs
        private void Historial_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var grid = sender as DataGrid;

            // CORRECCIÓN: Quitamos ".FacturacionViewModel" porque la clase ya no está anidada
            if (grid?.SelectedItem is FacturaHistorialItem item)
            {
                var listaDetalles = new List<ReporteItem>
        {
            new ReporteItem { Propiedad = "Folio Interno", Valor = item.SerieFolio },
            new ReporteItem { Propiedad = "UUID Fiscal", Valor = item.UUID },
            new ReporteItem { Propiedad = "Receptor", Valor = item.Receptor },
            new ReporteItem { Propiedad = "Monto Total", Valor = item.Total.ToString("C") },
            new ReporteItem { Propiedad = "Estado SAT", Valor = item.Estado },
            new ReporteItem { Propiedad = "Nota", Valor = "Para ver el XML o PDF, use los botones de la fila." }
        };

                var visor = new VisorReporteWindow($"Detalle Factura: {item.SerieFolio}", listaDetalles);
                visor.Owner = Window.GetWindow(this);
                visor.ShowDialog();
            }
        }

        private void Pendientes_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var grid = sender as DataGrid;

            // CORRECCIÓN: Quitamos ".FacturacionViewModel"
            if (grid?.SelectedItem is TicketPendienteItem ticket)
            {
                // Preparamos la lista para el visor
                var listaDetalles = new List<ReporteItem>
        {
            new ReporteItem { Propiedad = "Ticket #", Valor = ticket.Folio },
            new ReporteItem { Propiedad = "Fecha Venta", Valor = ticket.Fecha.ToString("F") },
            new ReporteItem { Propiedad = "Cliente", Valor = ticket.ClienteNombre },
            new ReporteItem { Propiedad = "RFC Receptor", Valor = ticket.RFC },
            new ReporteItem { Propiedad = "Monto Total", Valor = ticket.Total.ToString("C") },
            new ReporteItem { Propiedad = "Estatus", Valor = "Pendiente de Facturar" },
            new ReporteItem { Propiedad = "Acción", Valor = "Doble clic para ver detalles, clic en 'Facturar' para procesar." }
        };

                // Abrimos la ventana modal reutilizable
                var visor = new VisorReporteWindow($"Pre-vista Ticket: {ticket.Folio}", listaDetalles);
                visor.Owner = Window.GetWindow(this);
                visor.ShowDialog();
            }
        }

        private void BtnImprimirHistorial_Click(object sender, RoutedEventArgs e)
        {
            var vm = this.DataContext as FacturacionViewModel;
            if (vm == null || vm.ListaHistorial == null || vm.ListaHistorial.Count == 0)
            {
                MessageBox.Show("No hay datos para exportar.", "Aviso");
                return;
            }

            // 1. Preguntar dónde guardar el PDF
            SaveFileDialog saveDialog = new SaveFileDialog
            {
                Filter = "Archivo PDF (*.pdf)|*.pdf",
                FileName = $"ReporteFacturas_{DateTime.Now:yyyyMMdd_HHmm}.pdf"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    // 2. ¡AQUÍ EMPIEZA LA MAGIA DE QUESTPDF!
                    Document.Create(container =>
                    {
                        container.Page(page =>
                        {
                            // Configuración de la página
                            page.Size(PageSizes.A4);
                            page.Margin(2, Unit.Centimetre);
                            page.PageColor(Colors.White);
                            page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Segoe UI"));

                            // --- ENCABEZADO ---
                            // --- ENCABEZADO (ACTUALIZADO) ---
                            page.Header()
                                .Row(row =>
                                {
                                    // 1. Columna Izquierda: Logo y Textos
                                    row.RelativeItem().Column(col =>
                                    {
                                        // Aquí iría tu logo si lo tienes: col.Item().Image("logo.png");
                                        col.Item().Text("Papelería Orysi").SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);
                                        col.Item().Text("Reporte General de Facturación").FontSize(14);
                                        col.Item().Text($"Fecha de emisión: {DateTime.Now:g}").FontColor(Colors.Grey.Medium);
                                    });

                                    // 2. Columna Derecha: ¡EL CÓDIGO QR!
                                    // Usamos ConstantItem para fijar el tamaño del QR
                                    row.ConstantItem(85).Column(col =>
                                    {
                                        // Generamos un QR con información útil. 
                                        // En la vida real, esto sería la URL de validación del SAT.
                                        // Aquí pondremos un resumen del reporte.
                                        string dataParaQR = $"REPORTE-ORYSI|FECHA:{DateTime.Now:yyyyMMdd}|TOTAL:{vm.ListaHistorial.Count}";

                                        byte[] imagenQR = GenerarCodigoQR(dataParaQR);

                                        col.Item().Image(imagenQR);

                                        // Un textito pequeño abajo del QR
                                        col.Item().AlignCenter().Text("Validación").FontSize(8).FontColor(Colors.Grey.Medium);
                                    });
                                });

                            // --- CONTENIDO (LA TABLA) ---
                            page.Content().PaddingVertical(1, Unit.Centimetre).Table(table =>
                            {
                                // Definir columnas (Relative se ajusta, Constant es fijo)
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(80); // Folio
                                    columns.RelativeColumn();   // Receptor (ocupa el espacio libre)
                                    columns.ConstantColumn(80); // Estado
                                    columns.ConstantColumn(80); // Total
                                });

                                // Encabezados de tabla
                                table.Header(header =>
                                {
                                    header.Cell().Element(EstiloEncabezado).Text("Folio");
                                    header.Cell().Element(EstiloEncabezado).Text("Cliente / Receptor");
                                    header.Cell().Element(EstiloEncabezado).Text("Estado");
                                    header.Cell().Element(EstiloEncabezado).AlignRight().Text("Total");

                                    // Método local para estilo repetitivo
                                    static IContainer EstiloEncabezado(IContainer container)
                                    {
                                        return container
                                            .DefaultTextStyle(x => x.SemiBold())
                                            .PaddingVertical(5)
                                            .BorderBottom(1)
                                            .BorderColor(Colors.Black);
                                    }
                                });

                                // Filas de datos
                                foreach (var item in vm.ListaHistorial)
                                {
                                    table.Cell().Element(EstiloCelda).Text(item.SerieFolio);
                                    table.Cell().Element(EstiloCelda).Text(item.Receptor);

                                    // Lógica condicional: Rojo si está cancelada
                                    if (item.Estado == "Cancelada")
                                        table.Cell().Element(EstiloCelda).Text(item.Estado).FontColor(Colors.Red.Medium).Bold();
                                    else
                                        table.Cell().Element(EstiloCelda).Text(item.Estado).FontColor(Colors.Green.Medium);

                                    table.Cell().Element(EstiloCelda).AlignRight().Text(item.Total.ToString("C"));

                                    static IContainer EstiloCelda(IContainer container)
                                    {
                                        return container
                                            .BorderBottom(1)
                                            .BorderColor(Colors.Grey.Lighten2)
                                            .PaddingVertical(5);
                                    }
                                }

                                // Pie de tabla (Totales)
                                table.Footer(footer =>
                                {
                                    footer.Cell().ColumnSpan(3).AlignRight().PaddingTop(10).Text("TOTAL DEL PERIODO:").Bold();
                                    footer.Cell().AlignRight().PaddingTop(10).Text(vm.ListaHistorial.Sum(x => x.Total).ToString("C")).Bold().FontSize(12);
                                });
                            });

                            // --- PIE DE PÁGINA ---
                            page.Footer()
                                .AlignCenter()
                                .Text(x =>
                                {
                                    x.Span("Página ");
                                    x.CurrentPageNumber();
                                    x.Span(" de ");
                                    x.TotalPages();
                                });
                        });
                    })
                    .GeneratePdf(saveDialog.FileName); // ¡Aquí se crea el archivo!

                    // 3. Abrir el PDF automáticamente para que el usuario lo vea
                    var p = new Process();
                    p.StartInfo = new ProcessStartInfo(saveDialog.FileName)
                    {
                        UseShellExecute = true
                    };
                    p.Start();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error al generar PDF: " + ex.Message);
                }
            }
        }
        // Método auxiliar para generar el QR como arreglo de bytes
        private byte[] GenerarCodigoQR(string texto)
        {
            // 1. Creamos el generador
            using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
            {
                // 2. Calculamos los datos del QR
                // ECCLevel.Q es un nivel de corrección de errores alto (se ve más denso y profesional)
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(texto, QRCodeGenerator.ECCLevel.Q);

                // 3. Generamos los bytes de la imagen en formato PNG
                using (PngByteQRCode qrCode = new PngByteQRCode(qrCodeData))
                {
                    // El '20' es la calidad/píxeles por módulo.
                    return qrCode.GetGraphic(20);
                }
            }
        }


        // Método ayudante para no repetir código al crear celdas
        private TableCell CrearCelda(string texto)
        {
            return new TableCell(new Paragraph(new Run(texto)) { Margin = new Thickness(5) })
            {
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(0, 0, 0, 1) // Solo línea abajo
            };
        }
    }


}