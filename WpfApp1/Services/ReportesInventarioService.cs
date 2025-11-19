using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using WpfApp1.Data;

namespace WpfApp1.Services
{
    public class ReportesInventarioService
    {
        // 1. REPORTE: Inventario Valorizado (¿Cuánto dinero tengo invertido?)
        public List<dynamic> ObtenerInventarioValorizado()
        {
            using (var db = new InventarioDbContext())
            {
                // Consultamos el estado ACTUAL (sin fechas)
                var datos = db.Productos
                    .Include(p => p.Subcategoria)
                        .ThenInclude(s => s.Categoria)
                    .Where(p => p.Activo && p.Stock > 0) // Solo lo que existe y está activo
                    .Select(p => new
                    {
                        Codigo = p.ID,
                        Producto = p.Descripcion,
                        Categoria = p.Subcategoria.Categoria.Nombre,
                        Existencia = p.Stock,
                        CostoUnitario = p.Costo,
                        ValorTotal = p.Stock * p.Costo // Cálculo: Cantidad * Costo
                    })
                    .ToList(); // Traemos a memoria

                // Ordenamos en memoria (por valor, de mayor a menor)
                return datos.OrderByDescending(x => x.ValorTotal).ToList<dynamic>();
            }
        }

        // 2. REPORTE: Bajo Stock (¿Qué necesito comprar?)
        public List<dynamic> ObtenerProductosBajoStock(int limite = 5)
        {
            using (var db = new InventarioDbContext())
            {
                var datos = db.Productos
                    .Include(p => p.Subcategoria)
                    .Where(p => p.Activo && p.Stock <= limite) // Filtro crítico
                    .Select(p => new
                    {
                        ID = p.ID,
                        Producto = p.Descripcion,
                        Subcategoria = p.Subcategoria.Nombre,
                        StockActual = p.Stock,
                        Estatus = "URGENTE" // Etiqueta visual
                    })
                    .ToList();

                return datos.OrderBy(x => x.StockActual).ToList<dynamic>();
            }
        }

        // 3. REPORTE: Rotación / Kardex (Historial de movimientos)
        public List<dynamic> ObtenerMovimientosInventario(DateTime inicio, DateTime fin)
        {
            using (var db = new InventarioDbContext())
            {
                var datos = db.Movimientos
                    .Include(m => m.Producto)
                    .Where(m => m.Fecha >= inicio && m.Fecha <= fin)
                    .Select(m => new
                    {
                        Fecha = m.Fecha,
                        Producto = m.Producto.Descripcion,
                        Tipo = m.TipoMovimiento, // "Entrada", "Venta", etc.
                        Cantidad = m.Cantidad,
                        StockFinal = m.StockNuevo,
                        Usuario = m.Usuario
                    })
                    .ToList();

                return datos.OrderByDescending(m => m.Fecha).ToList<dynamic>();
            }
        }
    }
}