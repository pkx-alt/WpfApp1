using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OrySiPOS.Models; // <--- ¡AÑADE ESTA LÍNEA!

namespace OrySiPOS.Views
{
    public static class VentaSessionManager
    {
        // Una lista "privada" que guarda todos los carritos.
        private static List<ObservableCollection<CartItem>> _sesiones = new List<ObservableCollection<CartItem>>();

        // El índice (posición) de la venta que estamos viendo AHORA.
        public static int IndiceSesionActiva { get; private set; }

        // Esto se llama "Constructor Estático". Se ejecuta 1 sola vez
        // cuando la app usa esta clase por primera vez.
        static VentaSessionManager()
        {
            // Empezamos siempre con una venta vacía.
            CrearNuevaSesion();
        }

        // Devuelve el carrito que está activo
        public static ObservableCollection<CartItem> GetSesionActiva()
        {
            return _sesiones[IndiceSesionActiva];
        }

        // Crea un nuevo carrito, lo añade a la lista y lo marca como activo
        public static ObservableCollection<CartItem> CrearNuevaSesion()
        {
            var nuevoCarrito = new ObservableCollection<CartItem>();
            _sesiones.Add(nuevoCarrito);
            IndiceSesionActiva = _sesiones.Count - 1; // El nuevo es el último
            return nuevoCarrito;
        }

        // Devuelve cuántas ventas tenemos en total (activas + en espera)
        public static int GetTotalSesiones()
        {
            return _sesiones.Count;
        }

        // --- Métodos que usaremos después para cambiar ENTRE ventas ---

        // Cambia la sesión activa a un índice diferente
        public static ObservableCollection<CartItem> CambiarSesionActiva(int indice)
        {
            if (indice >= 0 && indice < _sesiones.Count)
            {
                IndiceSesionActiva = indice;
                return GetSesionActiva();
            }
            return null; // O manejar error
        }

        // Devuelve todas las sesiones (para una futura lista)
        public static List<ObservableCollection<CartItem>> GetTodasSesiones()
        {
            return _sesiones;
        }

        // Elimina una sesión (cuando se completa o cancela)
        public static void EliminarSesion(int indice)
        {
            if (indice >= 0 && indice < _sesiones.Count)
            {
                _sesiones.RemoveAt(indice);

                // ¡Importante! Si borramos la que estábamos viendo,
                // o una anterior, hay que ajustar el índice activo.
                if (IndiceSesionActiva >= indice)
                {
                    IndiceSesionActiva--;
                }
                // Si nos quedamos sin sesiones, crear una nueva
                if (!_sesiones.Any())
                {
                    CrearNuevaSesion();
                }
                // Asegurarnos de que el índice nunca sea inválido
                if (IndiceSesionActiva < 0)
                {
                    IndiceSesionActiva = 0;
                }
            }
        }
    }
}