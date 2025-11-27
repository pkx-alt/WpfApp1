using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using OrySiPOS.Data;

namespace OrySiPOS.Services
{
    public class ReportesAdministrativosService
    {
        public List<dynamic> ObtenerMejoresClientes(DateTime inicio, DateTime fin)
        {
            using (var db = new InventarioDbContext())
            {
                // Buscamos ventas en el periodo y agrupamos por cliente
                var datos = db.Ventas
                    .Include(v => v.Cliente)
                    .Where(v => v.Fecha >= inicio && v.Fecha <= fin)
                    .AsEnumerable() // Traemos a memoria para manejar el null del cliente
                    .GroupBy(v => v.Cliente != null ? v.Cliente.RazonSocial : "Público en General")
                    .Select(g => new
                    {
                        Cliente = g.Key,
                        Compras = g.Count(), // Cantidad de tickets
                        TotalGastado = g.Sum(v => v.Total),
                        TicketPromedio = g.Average(v => v.Total)
                    })
                    .OrderByDescending(x => x.TotalGastado) // Los que más gastan arriba
                    .Take(20) // Top 20
                    .ToList<dynamic>();

                return datos;
            }
        }
    }
}