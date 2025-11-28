using Microsoft.Win32;
using OrySiPOS.Data;
using OrySiPOS.Models;
using OrySiPOS.Helpers;
using System;
using System.Collections.Generic; // Necesario para List<>
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;              // Necesario para quitar acentos
using System.Globalization;     // Necesario para quitar acentos
using System.Windows;
using System.Windows.Input;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore; // Para AsNoTracking()

namespace OrySiPOS.ViewModels
{
    public class SatCatalogViewModel : ViewModelBase
    {
        public ObservableCollection<SatProducto> ProductosSat { get; set; }

        // --- CACHÉ EN MEMORIA ---
        // Guardaremos aquí todo el catálogo procesado para buscar a la velocidad de la luz
        private List<SatItemCache> _memoriaSat;

        private string _busqueda;
        public string Busqueda
        {
            get { return _busqueda; }
            set
            {
                _busqueda = value;
                OnPropertyChanged();
                RealizarBusqueda();
            }
        }

        private bool _estaCargando;
        public bool EstaCargando
        {
            get { return _estaCargando; }
            set { _estaCargando = value; OnPropertyChanged(); }
        }

        public ICommand ImportarCommand { get; }

        public SatCatalogViewModel()
        {
            ProductosSat = new ObservableCollection<SatProducto>();
            ImportarCommand = new RelayCommand(ImportarCatalogo);

            // Iniciamos la carga en segundo plano para no trabar la pantalla al abrir
            Task.Run(() => CargarMemoriaInicial());
        }

        private void CargarMemoriaInicial()
        {
            try
            {
                EstaCargando = true;
                using (var db = new InventarioDbContext())
                {
                    // --- 1. AUTO-IMPORTACIÓN (SEEDING) ---
                    // Si la tabla está vacía, buscamos el archivo local
                    if (!db.SatProductos.Any())
                    {
                        // Buscamos el archivo junto al .exe
                        string rutaDefault = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "catalogo_sat.csv");

                        if (System.IO.File.Exists(rutaDefault))
                        {
                            // Avisamos (opcional, o dejamos que la barra de carga hable)
                            // Application.Current.Dispatcher.Invoke(() => MessageBox.Show("Realizando carga inicial del catálogo SAT... esto tomará un momento."));

                            db.ImportarCatalogoSAT(rutaDefault);
                        }
                    }
                    // -------------------------------------

                    // 2. Carga Normal a Memoria (Igual que antes)
                    // Traemos todo usando AsNoTracking
                    var todos = db.SatProductos.AsNoTracking().ToList();

                    _memoriaSat = todos.Select(p => new SatItemCache
                    {
                        ProductoOriginal = p,
                        TextoBusqueda = QuitarAcentos(p.Clave + " " + p.Descripcion).ToLower()
                    }).ToList();
                }

                // Si ya había búsqueda, refrescar
                if (!string.IsNullOrWhiteSpace(Busqueda))
                {
                    Application.Current.Dispatcher.Invoke(RealizarBusqueda);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error cargando SAT: " + ex.Message);
            }
            finally
            {
                EstaCargando = false;
            }
        }

        private void RealizarBusqueda()
        {
            // Si la memoria aún no carga, no hacemos nada (el usuario verá el spinner de carga)
            if (_memoriaSat == null || string.IsNullOrWhiteSpace(Busqueda))
            {
                ProductosSat.Clear();
                return;
            }

            // 1. Normalizamos lo que el usuario escribió (quitamos acentos y mayúsculas)
            string termino = QuitarAcentos(Busqueda).ToLower();

            // 2. Buscamos en la memoria (es instantáneo)
            var resultados = _memoriaSat
                .Where(x => x.TextoBusqueda.Contains(termino))
                .Take(50) // Solo mostramos 50 para no saturar la vista
                .Select(x => x.ProductoOriginal)
                .ToList();

            // 3. Actualizamos la UI
            ProductosSat.Clear();
            foreach (var item in resultados)
            {
                ProductosSat.Add(item);
            }
        }

        private async void ImportarCatalogo(object obj)
        {
            var openDialog = new OpenFileDialog
            {
                Filter = "Archivos CSV (*.csv)|*.csv",
                Title = "Selecciona el archivo del SAT"
            };

            if (openDialog.ShowDialog() == true)
            {
                EstaCargando = true;

                await Task.Run(() =>
                {
                    try
                    {
                        using (var db = new InventarioDbContext())
                        {
                            db.ImportarCatalogoSAT(openDialog.FileName);
                        }

                        // Recargamos la memoria con los nuevos datos
                        CargarMemoriaInicial();

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show("¡Catálogo importado y procesado exitosamente!", "Éxito");
                        });
                    }
                    catch (Exception ex)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show($"Error: {ex.Message}");
                        });
                    }
                });

                EstaCargando = false;
            }
        }

        // --- MÉTODOS AUXILIARES (HELPERS) ---

        // Función mágica para quitar acentos (Lápiz -> Lapiz)
        private string QuitarAcentos(string texto)
        {
            if (string.IsNullOrEmpty(texto)) return "";

            var normalizedString = texto.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }

        // Clase ligera para el caché
        private class SatItemCache
        {
            public SatProducto ProductoOriginal { get; set; }
            public string TextoBusqueda { get; set; } // Versión limpia: "01010101 lapices de madera"
        }
    }
}