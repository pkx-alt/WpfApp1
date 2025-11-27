using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using OrySiPOS.Data;

namespace OrySiPOS.Services
{
    public class ReportesVentasService
    {
        // REPORTE 1: Top Productos Más Vendidos
        public List<dynamic> ObtenerProductosMasVendidos(DateTime inicio, DateTime fin)
        {
            using (var db = new InventarioDbContext())
            {
                // 1. Hacemos la consulta SIN ORDENAR
                var datosCrudos = db.VentasDetalle
                    .Include(d => d.Producto)
                    .Include(d => d.Venta)
                    .Where(d => d.Venta.Fecha >= inicio && d.Venta.Fecha <= fin)
                    .GroupBy(d => new { d.Producto.ID, d.Producto.Descripcion })
                    .Select(grupo => new
                    {
                        Producto = grupo.Key.Descripcion,
                        Unidades = grupo.Sum(x => x.Cantidad),
                        TotalDinero = grupo.Sum(x => x.Cantidad * x.PrecioUnitario)
                    })
                    .ToList(); // <--- ¡AQUÍ ESTÁ EL TRUCO! Traemos los datos a memoria primero.

                // 2. Ahora que están en memoria (C#), ya podemos ordenar sin que SQLite se queje
                var datosOrdenados = datosCrudos
                    .OrderByDescending(x => x.Unidades)
                    .Take(10)
                    .ToList<dynamic>();

                return datosOrdenados;
            }
        }

        // REPORTE 2: Ventas por Departamento (Categoría)
        public List<dynamic> ObtenerVentasPorCategoria(DateTime inicio, DateTime fin)
        {
            using (var db = new InventarioDbContext())
            {
                // 1. Hacemos la consulta SIN ORDENAR
                var datosCrudos = db.VentasDetalle
                    .Include(d => d.Venta)
                    .Include(d => d.Producto)
                        .ThenInclude(p => p.Subcategoria)
                            .ThenInclude(s => s.Categoria)
                    .Where(d => d.Venta.Fecha >= inicio && d.Venta.Fecha <= fin)
                    .GroupBy(d => d.Producto.Subcategoria.Categoria.Nombre)
                    .Select(grupo => new
                    {
                        Departamento = grupo.Key,
                        Tickets = grupo.Select(x => x.VentaId).Distinct().Count(),
                        TotalVendido = grupo.Sum(x => x.Cantidad * x.PrecioUnitario)
                    })
                    .ToList(); // <--- ¡EL MISMO TRUCO AQUÍ! Ejecutamos la consulta primero.

                // 2. Ordenamos en memoria
                var datosOrdenados = datosCrudos
                    .OrderByDescending(x => x.TotalVendido)
                    .ToList<dynamic>();

                return datosOrdenados;
            }
        }
        // En Services/ReportesVentasService.cs

        public List<dynamic> ObtenerComparativoMensual(DateTime inicio, DateTime fin)
        {
            using (var db = new InventarioDbContext())
            {
                // 1. Traer datos crudos (Solo lo necesario: Fecha y Total)
                var datosCrudos = db.Ventas
                    .Where(v => v.Fecha >= inicio && v.Fecha <= fin)
                    .Select(v => new { v.Fecha, v.Total })
                    .ToList(); // Traemos a memoria para que SQLite no falle con las fechas

                // 2. Agrupar y Calcular en C#
                var reporte = datosCrudos
                    .GroupBy(v => new { v.Fecha.Year, v.Fecha.Month })
                    .Select(grupo => new
                    {
                        // Creamos una fecha "base" (día 1) para formatearla bonito
                        FechaOrden = new DateTime(grupo.Key.Year, grupo.Key.Month, 1),

                        Periodo = new DateTime(grupo.Key.Year, grupo.Key.Month, 1).ToString("MMMM yyyy").ToUpper(),
                        Tickets = grupo.Count(),
                        // Usamos "TotalVendido" para que el formateador automático le ponga signo de pesos
                        TotalVendido = grupo.Sum(v => v.Total),
                        PromedioTicket = grupo.Average(v => v.Total)
                    })
                    .OrderByDescending(x => x.FechaOrden) // Mes más reciente primero
                    .ToList<dynamic>();

                return reporte;
            }
        }
    }
}