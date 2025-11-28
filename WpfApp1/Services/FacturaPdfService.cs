using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using OrySiPOS.Models;
using System.IO;
using System.Linq;

namespace OrySiPOS.Services
{
    public class FacturaPdfService
    {
        public string GenerarPdf(Venta venta, Factura factura, string rutaDestino)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            // --- 1. RECUPERAR DATOS DE AJUSTES ---
            // Usamos variables por si algún dato está vacío, poner un texto por defecto
            var settings = Properties.Settings.Default;

            string nombreEmpresa = !string.IsNullOrEmpty(settings.NombreTienda) ? settings.NombreTienda : "MI EMPRESA (Configurar en Ajustes)";
            string rfcEmpresa = !string.IsNullOrEmpty(settings.RFCTienda) ? settings.RFCTienda: "XAXX010101000";
            string direccionEmpresa = !string.IsNullOrEmpty(settings.DireccionTienda) ? settings.DireccionTienda: "Sin dirección configurada";
            // Puedes agregar más, como Teléfono o Correo si los tienes en settings

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.Letter);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    // --- 2. CABECERA DINÁMICA ---
                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            // AQUI USAMOS LAS VARIABLES
                            col.Item().Text(nombreEmpresa).Bold().FontSize(20).FontColor(Colors.Blue.Medium);
                            col.Item().Text($"RFC: {rfcEmpresa}");
                            col.Item().Text($"Dirección: {direccionEmpresa}");
                        });

                        row.ConstantItem(150).Column(col =>
                        {
                            col.Item().Text($"Folio: {factura.SerieFolio}").Bold();
                            col.Item().Text($"UUID: {factura.UUID.Substring(0, 8)}...");
                            col.Item().Text($"Fecha: {factura.FechaEmision:dd/MM/yyyy}");
                            col.Item().Text("TIPO: INGRESO");
                        });
                    });

                    // ... (El resto del código del Content y Footer se queda IGUAL) ...
                    page.Content().PaddingVertical(1, Unit.Centimetre).Column(col =>
                    {
                        col.Item().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingBottom(5).Row(row =>
                        {
                            row.RelativeItem().Text($"Cliente: {factura.ReceptorNombre}").Bold();
                            row.RelativeItem().Text($"RFC: {factura.ReceptorRFC}");
                        });

                        col.Item().PaddingTop(10).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(50);
                                columns.RelativeColumn();
                                columns.ConstantColumn(80);
                                columns.ConstantColumn(80);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Text("Cant").Bold();
                                header.Cell().Text("Descripción").Bold();
                                header.Cell().Text("Precio").Bold();
                                header.Cell().Text("Importe").Bold();
                            });

                            foreach (var item in venta.Detalles)
                            {
                                table.Cell().Text(item.Cantidad.ToString());
                                table.Cell().Text(item.Descripcion ?? "Producto");
                                table.Cell().Text($"${item.PrecioUnitario:F2}");
                                table.Cell().Text($"${(item.Cantidad * item.PrecioUnitario):F2}");
                            }
                        });

                        col.Item().PaddingTop(10).AlignRight().Column(c =>
                        {
                            c.Item().Text($"Subtotal: ${venta.Subtotal:F2}");
                            c.Item().Text($"IVA: ${venta.IVA:F2}");
                            c.Item().Text($"TOTAL: ${venta.Total:F2}").Bold().FontSize(14);
                        });
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Página ");
                        x.CurrentPageNumber();
                        x.Span(" de ");
                        x.TotalPages();
                    });
                });
            })
            .GeneratePdf(rutaDestino);

            return rutaDestino;
        }
    }
}