using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using OrySiPOS.Data;

namespace OrySiPOS.Services
{
    public class ReportesCotizacionesService
    {
        // 1. Cotizaciones que están vivas y no se han cerrado
        // En Services/ReportesCotizacionesService.cs

        public List<dynamic> ObtenerPendientes(DateTime inicio, DateTime fin)
        {
            using (var db = new InventarioDbContext())
            {
                // PASO 1: Traemos los datos "crudos" de la base de datos.
                // Solo filtramos por fecha de emisión para no traer todo el historial.
                var datosCrudos = db.Cotizaciones
                    .Include(c => c.Cliente)
                    .Where(c => c.FechaEmision >= inicio && c.FechaEmision <= fin)
                    .ToList(); // <--- ¡AQUÍ ESTÁ EL ARREGLO! Traemos a memoria.

                // PASO 2: Ahora sí, en memoria (C#), hacemos los cálculos y el filtro final.
                var reporteFinal = datosCrudos
                    .Select(c => new
                    {
                        Folio = c.ID,
                        Fecha = c.FechaEmision,
                        Cliente = c.Cliente != null ? c.Cliente.RazonSocial : "Público General",
                        Vence = c.FechaVencimiento,
                        Total = c.Total,
                        // C# calcula esto sin problemas:
                        DiasRestantes = (c.FechaVencimiento - DateTime.Now).Days
                    })
                    .Where(x => x.DiasRestantes >= 0) // Filtramos las vigentes
                    .OrderBy(x => x.DiasRestantes)    // Ordenamos por urgencia
                    .ToList<dynamic>();

                return reporteFinal;
            }
        }

        // 2. KPI: ¿Cuántas cotizaciones realmente vendemos?
        // (Nota: Para esto necesitaríamos cruzar datos exactos, aquí haremos una aproximación estadística)
        public List<dynamic> ObtenerEfectividad(DateTime inicio, DateTime fin)
        {
            using (var db = new InventarioDbContext())
            {
                int totalCotizaciones = db.Cotizaciones.Count(c => c.FechaEmision >= inicio && c.FechaEmision <= fin);
                int totalVentas = db.Ventas.Count(v => v.Fecha >= inicio && v.Fecha <= fin);

                // Este es un reporte simple de resumen
                var datos = new List<dynamic>
                {
                    new { Metrica = "Total Cotizaciones Emitidas", Valor = totalCotizaciones },
                    new { Metrica = "Total Ventas Cerradas", Valor = totalVentas },
                    // Aquí podrías calcular un % si tuvieras un campo "CotizacionId" en la tabla Ventas
                    new { Metrica = "Relación Ventas/Cotizaciones", Valor = totalCotizaciones > 0 ? (double)totalVentas / totalCotizaciones : 0 }
                };

                return datos;
            }
        }
    }
}