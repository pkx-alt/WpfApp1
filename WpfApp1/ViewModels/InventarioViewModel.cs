using System.Collections.ObjectModel;
using System.Linq;
using OrySiPOS.Data;
using OrySiPOS.Models;
using OrySiPOS.Helpers; // Para RelayCommand
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using OrySiPOS.Views.Dialogs;
using System.Windows;

namespace OrySiPOS.ViewModels
{
    public class InventarioViewModel : ViewModelBase // Asegúrate que herede de tu Base
    {
        // --- Campos Privados ---
        private InventarioDbContext _context;
        private Categoria _categoriaSeleccionada;
        private Subcategoria _subcategoriaSeleccionada;
        private string _textoBusqueda;
        private bool _verActivos;
        private bool _verInactivos;
        private bool _verBajoStock;
        public const int NivelBajoStock = 5;
        // 1. AGREGA ESTA CONSTANTE AL PRINCIPIO DE LA CLASE
        public const string TextoPredeterminado = "Buscar por nombre o descripción...";

        // --- Propiedades Públicas (para el Binding) ---
        public ObservableCollection<Producto> Productos { get; set; }
        public ObservableCollection<Categoria> CategoriasDisponibles { get; set; }
        public ObservableCollection<Subcategoria> SubcategoriasDisponibles { get; set; }

        public ICommand LimpiarFiltrosCommand { get; }
        public ICommand NuevoProductoCommand { get; }
        // Comandos para el DataGrid (los crearemos pero no los usaremos aún)
        // public ICommand AddStockCommand { get; } 
        // ...etc...

        // --- Propiedades para los Filtros ---

        public Categoria CategoriaSeleccionada
        {
            get => _categoriaSeleccionada;
            set
            {
                if (_categoriaSeleccionada == value) return;
                _categoriaSeleccionada = value;
                OnPropertyChanged(); // Avisa a la UI
                ActualizarSubcategorias(); // ¡LA MAGIA!
                CargarProductos(); // Refresca el DataGrid
            }
        }

        public Subcategoria SubcategoriaSeleccionada
        {
            get => _subcategoriaSeleccionada;
            set
            {
                if (_subcategoriaSeleccionada == value) return;
                _subcategoriaSeleccionada = value;
                OnPropertyChanged();
                CargarProductos(); // Refresca el DataGrid
            }
        }

        public string TextoBusqueda
        {
            get => _textoBusqueda;
            set
            {
                if (_textoBusqueda == value) return;
                _textoBusqueda = value;
                OnPropertyChanged();
                CargarProductos(); // Refresca el DataGrid
            }
        }

        public bool VerActivos
        {
            get => _verActivos;
            set
            {
                if (_verActivos == value) return;
                _verActivos = value;
                OnPropertyChanged();
                CargarProductos();
            }
        }

        public bool VerInactivos
        {
            get => _verInactivos;
            set
            {
                if (_verInactivos == value) return;
                _verInactivos = value;
                OnPropertyChanged();
                CargarProductos();
            }
        }

        public bool VerBajoStock
        {
            get => _verBajoStock;
            set
            {
                if (_verBajoStock == value) return;
                _verBajoStock = value;
                OnPropertyChanged();
                CargarProductos();
            }
        }


        // --- Constructor ---
        public InventarioViewModel()
        {
            _context = new InventarioDbContext();

            Productos = new ObservableCollection<Producto>();
            CategoriasDisponibles = new ObservableCollection<Categoria>();
            SubcategoriasDisponibles = new ObservableCollection<Subcategoria>();

            // Comandos
            LimpiarFiltrosCommand = new RelayCommand(OnLimpiarFiltros);
            NuevoProductoCommand = new RelayCommand(OnNuevoProducto);

            // Carga los filtros y pon los valores por defecto
            CargarFiltros();
            OnLimpiarFiltros(null); // Esto pone los filtros por defecto Y carga productos
        }

        // --- Métodos de Lógica ---

        private void CargarFiltros()
        {
            CategoriasDisponibles.Clear();
            var categorias = _context.Categorias
                                     .Include(c => c.Subcategorias)
                                     .ToList();

            CategoriasDisponibles.Add(new Categoria { Id = 0, Nombre = "Todas las categorías" });
            foreach (var cat in categorias)
            {
                CategoriasDisponibles.Add(cat);
            }
        }

        private void ActualizarSubcategorias()
        {
            SubcategoriasDisponibles.Clear();

            if (CategoriaSeleccionada == null || CategoriaSeleccionada.Id == 0)
            {
                SubcategoriasDisponibles.Add(new Subcategoria { Id = 0, Nombre = "Todas las subcategorías" });
            }
            else
            {
                SubcategoriasDisponibles.Add(new Subcategoria { Id = 0, Nombre = $"Todas en {CategoriaSeleccionada.Nombre}" });
                foreach (var sub in CategoriaSeleccionada.Subcategorias)
                {
                    SubcategoriasDisponibles.Add(sub);
                }
            }

            SubcategoriaSeleccionada = SubcategoriasDisponibles.FirstOrDefault();
        }

        private void OnLimpiarFiltros(object obj)
        {
            CategoriaSeleccionada = CategoriasDisponibles.FirstOrDefault(c => c.Id == 0);

            // 2. CAMBIA ESTO: En lugar de "", usamos el texto
            TextoBusqueda = TextoPredeterminado;

            VerActivos = true;
            VerInactivos = false;
            VerBajoStock = false;

            CargarProductos();
        }

        private void OnNuevoProducto(object obj)
        {
            var ventanaNuevo = new NuevoProductoModal();
            bool? resultado = ventanaNuevo.ShowDialog();
            if (resultado == true)
            {
                CargarProductos(); // Recarga la lista
            }
        }

        // ¡Este es tu método de Carga/Filtro, ahora en el VM!
        // Lo hacemos público para que el code-behind (los clics del DataGrid) pueda llamarlo
        public void CargarProductos()
        {
            // 1. Empezamos con la consulta base
            IQueryable<Producto> query = _context.Productos
                // AÑADIDO: Incluye la Subcategoría
                .Include(p => p.Subcategoria)
                    // AÑADIDO: Y también la Categoría que está dentro de la Subcategoría
                    .ThenInclude(s => s.Categoria);

            // 2. Leemos el estado de NUESTRAS PROPIEDADES

            // FILTRO 1: Por Texto
            // Si el texto NO es vacío Y NO es el texto predeterminado, entonces filtramos.
            if (!string.IsNullOrWhiteSpace(TextoBusqueda) && TextoBusqueda != TextoPredeterminado)
            {
                string filtroLower = TextoBusqueda.ToLower();
                query = query.Where(p => p.Descripcion.ToLower().Contains(filtroLower) ||
                                         p.ID.ToString().Contains(filtroLower));
            }

            // FILTRO 2: Por Categoría/Subcategoría
            if (CategoriaSeleccionada != null && CategoriaSeleccionada.Id != 0)
            {
                if (SubcategoriaSeleccionada != null && SubcategoriaSeleccionada.Id != 0)
                {
                    // Filtro específico por Subcategoría
                    query = query.Where(p => p.SubcategoriaId == SubcategoriaSeleccionada.Id);
                }
                else
                {
                    // Filtro por todas las subcategorías de la Categoría padre
                    var idsSub = CategoriaSeleccionada.Subcategorias.Select(s => s.Id).ToList();
                    query = query.Where(p => idsSub.Contains(p.SubcategoriaId));
                }
            }

            // FILTRO 3: Por Estado (Activo / Inactivo)
            if (VerActivos != VerInactivos)
            {
                query = query.Where(p =>
                    (VerActivos && p.Activo) ||
                    (VerInactivos && !p.Activo)
                );
            }
            else if (!VerActivos && !VerInactivos)
            {
                query = query.Where(p => false); // No mostrar nada
            }
            // Si ambos son true, no se aplica filtro de estado

            // FILTRO 4: Por Bajo Stock
            if (VerBajoStock)
            {
                query = query.Where(p => p.Stock <= NivelBajoStock);
            }

            // 3. Ejecutamos la consulta
            var productosDeDb = query.ToList();

            // 4. Actualizamos la lista de la UI
            Productos.Clear();
            foreach (var producto in productosDeDb)
            {
                Productos.Add(producto);
            }
        }
    }
}