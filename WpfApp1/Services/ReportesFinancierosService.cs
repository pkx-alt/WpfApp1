using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using OrySiPOS.Data;

namespace OrySiPOS.Services
{
    public class ReportesFinancierosService
    {
        // 1. REPORTE: UTILIDAD NETA (Ganancia Real)
        // Fórmula: (Ventas Totales - Costo de lo vendido) - Gastos Operativos + Otros Ingresos
        public List<dynamic> ObtenerUtilidadNeta(DateTime inicio, DateTime fin)
        {
            using (var db = new InventarioDbContext())
            {
                // A. Calcular Ventas y Costo de lo Vendido
                // (Usamos VentasDetalle para tener exactitud producto por producto)
                var metricasVentas = db.VentasDetalle
                    .Include(d => d.Venta)
                    .Include(d => d.Producto)
                    .Where(d => d.Venta.Fecha >= inicio && d.Venta.Fecha <= fin)
                    .ToList() // Traemos a memoria para sumar
                    .GroupBy(d => 1) // Grupo dummy para tener un solo total
                    .Select(g => new
                    {
                        IngresoVentas = g.Sum(x => x.Cantidad * x.PrecioUnitario),
                        CostoVendido = g.Sum(x => x.Cantidad * x.Producto.Costo)
                    })
                    .FirstOrDefault();

                decimal ingresoVentas = metricasVentas?.IngresoVentas ?? 0;
                decimal costoVendido = metricasVentas?.CostoVendido ?? 0;

                // Utilidad Bruta = Ventas - Costos de Mercancía
                decimal utilidadBruta = ingresoVentas - costoVendido;

                // B. Calcular Gastos Operativos (Luz, Renta, Nómina, etc.)
                decimal gastosOperativos = db.Gastos
                    .Where(g => g.Fecha >= inicio && g.Fecha <= fin)
                    .Sum(g => g.Monto);

                // C. Calcular Otros Ingresos (No derivados de ventas de mostrador)
                decimal otrosIngresos = db.Ingresos
                    .Where(i => i.Fecha >= inicio && i.Fecha <= fin)
                    .Sum(i => i.Monto);

                // D. Construir el reporte renglón por renglón
                // Usamos valores negativos visuales para los egresos
                var reporte = new List<dynamic>
                {
                    new { Concepto = " (+) Ventas Totales", Monto = ingresoVentas },
                    new { Concepto = " (-) Costo de lo Vendido", Monto = costoVendido * -1 },
                    new { Concepto = " (=) UTILIDAD BRUTA", Monto = utilidadBruta },
                    new { Concepto = " ", Monto = 0m }, // Espacio en blanco
                    new { Concepto = " (-) Gastos Operativos", Monto = gastosOperativos * -1 },
                    new { Concepto = " (+) Otros Ingresos", Monto = otrosIngresos },
                    new { Concepto = " ", Monto = 0m }, // Espacio en blanco
                    new { Concepto = " (=) UTILIDAD NETA REAL", Monto = utilidadBruta - gastosOperativos + otrosIngresos }
                };

                return reporte;
            }
        }

        // 2. REPORTE: BALANCE GENERAL (Flujo de Efectivo Simplificado)
        public List<dynamic> ObtenerBalanceIngresosEgresos(DateTime inicio, DateTime fin)
        {
            using (var db = new InventarioDbContext())
            {
                // 1. Sumar Entradas
                decimal ventas = db.Ventas
                    .Where(v => v.Fecha >= inicio && v.Fecha <= fin)
                    .Sum(v => v.Total);

                decimal otrosIngresos = db.Ingresos
                    .Where(i => i.Fecha >= inicio && i.Fecha <= fin)
                    .Sum(i => i.Monto);

                // 2. Sumar Salidas
                decimal gastos = db.Gastos
                    .Where(g => g.Fecha >= inicio && g.Fecha <= fin)
                    .Sum(g => g.Monto);

                // 3. Construir lista
                var lista = new List<dynamic>
                {
                    new { Categoria = "ENTRADAS", Tipo = "Ventas Mostrador", Total = ventas },
                    new { Categoria = "ENTRADAS", Tipo = "Otros Ingresos", Total = otrosIngresos },
                    new { Categoria = "SALIDAS", Tipo = "Gastos Operativos", Total = gastos * -1 }, // Negativo para restar visualmente
                    new { Categoria = "RESULTADO", Tipo = "Flujo Neto del Periodo", Total = (ventas + otrosIngresos) - gastos }
                };

                return lista;
            }
        }
    }
}