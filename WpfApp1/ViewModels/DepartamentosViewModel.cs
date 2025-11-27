// ViewModels/DepartamentosViewModel.cs
using Microsoft.EntityFrameworkCore; // ¡Para usar .Include()!
using System.Collections.ObjectModel; // ¡Muy importante!
using System.Linq;
using System.Windows.Input;
using OrySiPOS.Data;
using OrySiPOS.Models;
using OrySiPOS.Helpers;      // Para nuestro RelayCommand
using OrySiPOS.Views.Dialogs; // Para nuestros diálogos
using System.Windows;       // Para MessageBox
using System.Threading.Tasks; // Para Task.Run
using OrySiPOS.Services;       // Para SupabaseService

namespace OrySiPOS.ViewModels
{
    public class DepartamentosViewModel : ViewModelBase
    {
        // --- Campos Privados ---
        private InventarioDbContext _context;
        private Categoria _categoriaSeleccionada;

        // --- Propiedades Públicas (a esto se "atará" la Vista) ---

        // Usamos ObservableCollection en lugar de List<T>
        // ¿Por qué? Porque "avisa" automáticamente a la lista (al ListBox)
        // cuando le añades o quitas un item. ¡Es magia!
        public ObservableCollection<Categoria> Categorias { get; set; }
        public ObservableCollection<Subcategoria> Subcategorias { get; set; }
        public ICommand CrearCategoriaCommand { get; private set; }
        public ICommand CrearSubcategoriaCommand { get; private set; }

        // --- 1. Propiedades para los NUEVOS Comandos ---
        public ICommand EditarCategoriaCommand { get; private set; }
        public ICommand EliminarCategoriaCommand { get; private set; }
        public ICommand EditarSubcategoriaCommand { get; private set; }
        public ICommand EliminarSubcategoriaCommand { get; private set; }
        // --- 1. Propiedad para el NUEVO Comando ---
        public ICommand VerDetallesCategoriaCommand { get; private set; }
        public ICommand VerDetallesSubcategoriaCommand { get; private set; }
        public ICommand MoverSubcategoriasCommand { get; private set; }

        public Categoria CategoriaSeleccionada
        {
            get { return _categoriaSeleccionada; }
            set
            {
                // Si el valor no es nuevo, no hacemos nada
                if (_categoriaSeleccionada == value) return;

                _categoriaSeleccionada = value;

                // ¡El "boletín" de la radio!
                // Avisa a la vista: "¡Oye, CategoriaSeleccionada ha cambiado!"
                OnPropertyChanged();

                // Cuando la selección cambia, ¡cargamos las subcategorías!
                CargarSubcategorias();
            }
        }

        // --- 2. Actualiza el Constructor ---
        public DepartamentosViewModel()
        {
            _context = new InventarioDbContext();

            Categorias = new ObservableCollection<Categoria>();
            Subcategorias = new ObservableCollection<Subcategoria>();

            // "Cableamos" los comandos de CREAR (los que ya tenías)
            CrearCategoriaCommand = new RelayCommand(OnCrearCategoria);
            CrearSubcategoriaCommand = new RelayCommand(OnCrearSubcategoria);

            // "Cableamos" los NUEVOS comandos de EDITAR y ELIMINAR
            EditarCategoriaCommand = new RelayCommand(OnEditarCategoria);
            EliminarCategoriaCommand = new RelayCommand(OnEliminarCategoria);
            EditarSubcategoriaCommand = new RelayCommand(OnEditarSubcategoria);
            EliminarSubcategoriaCommand = new RelayCommand(OnEliminarSubcategoria);
            // "Cableamos" el nuevo comando
            VerDetallesCategoriaCommand = new RelayCommand(OnVerDetallesCategoria);
            MoverSubcategoriasCommand = new RelayCommand(OnMoverSubcategorias);
            VerDetallesSubcategoriaCommand = new RelayCommand(OnVerDetallesSubcategoria);
            CargarCategorias();
        }

        // --- Métodos (La Lógica) ---

        private void CargarCategorias()
        {
            // Limpiamos la lista por si acaso
            Categorias.Clear();

            // Cargamos TODAS las categorías Y (con Include)
            // traemos "pegadas" sus subcategorías hijas de una vez.
            var categoriasDesdeDb = _context.Categorias
                                            .Include(c => c.Subcategorias)
                                            .ToList();

            // Llenamos la ObservableCollection
            foreach (var cat in categoriasDesdeDb)
            {
                Categorias.Add(cat);
            }
        }

        private void CargarSubcategorias()
        {
            // Limpiamos la lista (de las subcategorías anteriores)
            Subcategorias.Clear();

            // Si la categoría seleccionada no es nula y tiene subcategorías...
            if (CategoriaSeleccionada != null && CategoriaSeleccionada.Subcategorias != null)
            {
                // ... las añadimos a la lista observable
                foreach (var sub in CategoriaSeleccionada.Subcategorias)
                {
                    Subcategorias.Add(sub);
                }
            }
        }

        // --- 3. Método para el NUEVO Comando ---

        private void OnVerDetallesCategoria(object parametro)
        {
            if (parametro is Categoria categoria)
            {
                // --- Aquí calculamos los datos para el diálogo ---

                // 1. Conteo de Subcategorías (¡Fácil! ya lo tenemos)
                int subCount = categoria.Subcategorias.Count;

                // 2. Conteo de Productos (¡Asumimos que Producto tiene SubcategoriaId!)
                // Esto es una consulta LINQ poderosa.
                // "Cuenta en la tabla Productos todos los productos
                //  cuya SubcategoriaId pertenezca a la lista de Ids de
                //  las subcategorías de mi categoría seleccionada."
                var subcategoriaIds = categoria.Subcategorias.Select(s => s.Id).ToList();

                // OJO: Necesitas tener Productos en tu DbContext
                // Si no tienes Productos, pon 0 por ahora.
                int prodCount = _context.Productos
                                    .Count(p => subcategoriaIds.Contains(p.SubcategoriaId));
                // (Si tu modelo es diferente, esta consulta cambiará)

                // 3. Descripción (Asumimos que tu modelo Categoria
                //    no tiene descripción. Si la tuviera, sería: categoria.Descripcion)
                string descripcion = "Artículos de papelería, material de oficina y útiles escolares."; // (Texto de ejemplo de tu imagen)


                // --- Abrimos el diálogo pasándole los datos ---
                var dialog = new DetallesCategoriaDialog(categoria.Nombre, descripcion, subCount, prodCount);

                // Lo mostramos. Si devuelve 'true', es porque el usuario
                // hizo clic en el botón "Editar" de ADENTRO del diálogo.
                if (dialog.ShowDialog() == true)
                {
                    // ¡Reutilizamos la lógica que ya teníamos!
                    // Llamamos al método que edita la categoría
                    OnEditarCategoria(categoria);
                }
            }
        }

        private void OnVerDetallesSubcategoria(object parametro)
        {
            if (parametro is Subcategoria sub)
            {
                // ¡Aquí es donde mostraríamos el diálogo!
                // Por ahora, solo mostremos la info en un MessageBox
                // para probar que funciona.

                // 1. Obtenemos el nombre del padre (¡ya lo tenemos!)
                string parentCat = CategoriaSeleccionada.Nombre;

                // 2. Contamos los productos asociados
                int prodCount = 0;
                try
                {
                    // Hacemos una consulta rápida a la BD
                    prodCount = _context.Productos
                                        .Count(p => p.SubcategoriaId == sub.Id);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al contar productos: {ex.Message}");
                }

                // 3. Mostramos el resumen
                MessageBox.Show(
                    $"Detalles de la Subcategoría:\n\n" +
                    $"Nombre:       {sub.Nombre}\n" +
                    $"Pertenece a:  {parentCat}\n" +
                    $"Productos asociados: {prodCount}",
                    "Detalles de Subcategoría"
                );

                // (Si quisieras, aquí crearías un 'new DetallesSubcategoriaDialog(...)'
                //  igual que hicimos con el de Categorías)
            }
        }

        private void OnCrearCategoria(object obj)
        {
            var dialog = new CrearCategoriaDialog();
            if (dialog.ShowDialog() == true)
            {
                var nuevoNombre = dialog.NombreCategoria;
                var nuevaCategoria = new Categoria { Nombre = nuevoNombre };

                _context.Categorias.Add(nuevaCategoria);
                _context.SaveChanges(); // Genera ID local

                Categorias.Add(nuevaCategoria);

                // --- SYNC NUBE ---
                int idParaSync = nuevaCategoria.Id;
                Task.Run(async () =>
                {
                    var srv = new SupabaseService();
                    // Creamos un objeto temporal seguro para enviar
                    var catSegura = new Categoria { Id = idParaSync, Nombre = nuevoNombre };
                    await srv.SincronizarCategoria(catSegura);
                });
                // -----------------
            }
        }

        private void OnCrearSubcategoria(object obj)
        {
            if (CategoriaSeleccionada == null) { MessageBox.Show("Selecciona una categoría."); return; }

            var dialog = new CrearSubcategoriaDialog();
            if (dialog.ShowDialog() == true)
            {
                var nuevoNombre = dialog.NombreSubcategoria;
                var nuevaSubcategoria = new Subcategoria
                {
                    Nombre = nuevoNombre,
                    CategoriaId = CategoriaSeleccionada.Id
                };

                _context.Subcategorias.Add(nuevaSubcategoria);
                _context.SaveChanges();

                Subcategorias.Add(nuevaSubcategoria);

                // --- SYNC NUBE ---
                int id = nuevaSubcategoria.Id;
                int catId = nuevaSubcategoria.CategoriaId;
                Task.Run(async () =>
                {
                    var srv = new SupabaseService();
                    await srv.SincronizarSubcategoria(new Subcategoria { Id = id, Nombre = nuevoNombre, CategoriaId = catId });
                });
                // -----------------
            }
        }

        // --- 3. Métodos para EDITAR ---

        private void OnEditarCategoria(object parametro)
        {
            if (parametro is Categoria categoriaParaEditar)
            {
                var dialog = new CrearCategoriaDialog();
                dialog.NombreTextBox.Text = categoriaParaEditar.Nombre;

                if (dialog.ShowDialog() == true)
                {
                    categoriaParaEditar.Nombre = dialog.NombreCategoria;
                    _context.Categorias.Update(categoriaParaEditar);
                    _context.SaveChanges();
                    CargarCategorias();

                    // --- SYNC NUBE ---
                    int id = categoriaParaEditar.Id;
                    string nombre = categoriaParaEditar.Nombre;
                    Task.Run(async () =>
                    {
                        var srv = new SupabaseService();
                        await srv.SincronizarCategoria(new Categoria { Id = id, Nombre = nombre });
                    });
                    // -----------------
                }
            }
        }

        private void OnEditarSubcategoria(object parametro)
        {
            if (parametro is Subcategoria subcategoriaParaEditar)
            {
                var dialog = new CrearSubcategoriaDialog();
                dialog.NombreTextBox.Text = subcategoriaParaEditar.Nombre;

                if (dialog.ShowDialog() == true)
                {
                    subcategoriaParaEditar.Nombre = dialog.NombreSubcategoria;
                    _context.Subcategorias.Update(subcategoriaParaEditar);
                    _context.SaveChanges();
                    CargarSubcategorias();

                    // --- SYNC NUBE ---
                    int id = subcategoriaParaEditar.Id;
                    int catId = subcategoriaParaEditar.CategoriaId;
                    string nombre = subcategoriaParaEditar.Nombre;
                    Task.Run(async () =>
                    {
                        var srv = new SupabaseService();
                        await srv.SincronizarSubcategoria(new Subcategoria { Id = id, Nombre = nombre, CategoriaId = catId });
                    });
                    // -----------------
                }
            }
        }

        // --- 4. Métodos para ELIMINAR (con aviso) ---

        private void OnEliminarCategoria(object parametro)
        {
            if (parametro is Categoria categoriaParaEliminar)
            {
                string aviso = $"¿Estás seguro de que quieres eliminar '{categoriaParaEliminar.Nombre}'?";
                if (categoriaParaEliminar.Subcategorias.Any())
                {
                    aviso += $"\n\n¡ATENCIÓN! Esto borrará también sus {categoriaParaEliminar.Subcategorias.Count} subcategorías asociadas.";
                }

                if (MessageBox.Show(aviso, "Confirmar eliminación", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    // CAPTURAR IDs: Guardamos los IDs de lo que vamos a borrar
                    long idCategoria = categoriaParaEliminar.Id;
                    // Hacemos una lista de los IDs de sus hijos para borrarlos en la nube también
                    var idsSubcategorias = categoriaParaEliminar.Subcategorias.Select(s => s.Id).ToList();

                    // 1. Borrado Local (EF Core se encarga de la cascada aquí)
                    _context.Categorias.Remove(categoriaParaEliminar);
                    _context.SaveChanges();

                    // 2. Actualizar UI
                    Categorias.Remove(categoriaParaEliminar);
                    Subcategorias.Clear();

                    // 3. --- SYNC NUBE EN ORDEN ---
                    Task.Run(async () =>
                    {
                        var srv = new SupabaseService();

                        // A. Primero borramos los hijos en la nube para no romper la integridad
                        foreach (var idSub in idsSubcategorias)
                        {
                            await srv.EliminarSubcategoria(idSub);
                        }

                        // B. Ahora sí, borramos al padre tranquilamente
                        await srv.EliminarCategoria(idCategoria);
                    });
                    // -----------------------------
                }
            }
        }

        private void OnEliminarSubcategoria(object parametro)
        {
            if (parametro is Subcategoria subcategoriaParaEliminar)
            {
                string aviso = $"¿Estás seguro de que quieres eliminar la subcategoría '{subcategoriaParaEliminar.Nombre}'?";

                if (MessageBox.Show(aviso, "Confirmar eliminación", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    // Guardamos el ID antes de que el objeto desaparezca del contexto
                    long idParaBorrar = subcategoriaParaEliminar.Id;

                    // 1. Borra de la BD Local
                    _context.Subcategorias.Remove(subcategoriaParaEliminar);
                    _context.SaveChanges();

                    // 2. Borra de la lista visual
                    Subcategorias.Remove(subcategoriaParaEliminar);

                    // 3. --- SYNC NUBE (Background) ---
                    Task.Run(async () =>
                    {
                        var srv = new SupabaseService();
                        await srv.EliminarSubcategoria(idParaBorrar);
                    });
                    // --------------------------------
                }
            }
        }

        // --- 3. Método para el NUEVO Comando ---
        private void OnMoverSubcategorias(object obj)
        {
            // ... (tus validaciones iniciales siguen igual) ...
            if (CategoriaSeleccionada == null) return;

            var subcategoriasParaMover = Subcategorias.Where(s => s.IsSelected).ToList();
            if (subcategoriasParaMover.Count == 0) return;

            var categoriasDestino = Categorias.Where(c => c.Id != CategoriaSeleccionada.Id).ToList();

            var dialog = new MoverSubcategoriasDialog(subcategoriasParaMover.Count, categoriasDestino);
            if (dialog.ShowDialog() == true)
            {
                Categoria categoriaDestino = dialog.CategoriaDestino;

                // Lista para guardar los datos necesarios para sync
                var listaParaSync = new System.Collections.Generic.List<Subcategoria>();

                foreach (var sub in subcategoriasParaMover)
                {
                    sub.CategoriaId = categoriaDestino.Id;
                    _context.Subcategorias.Update(sub);

                    // Guardamos copia ligera para el hilo secundario
                    listaParaSync.Add(new Subcategoria { Id = sub.Id, Nombre = sub.Nombre, CategoriaId = categoriaDestino.Id });
                }

                _context.SaveChanges();
                CargarSubcategorias();

                // --- SYNC NUBE MASIVO ---
                Task.Run(async () =>
                {
                    var srv = new SupabaseService();
                    foreach (var item in listaParaSync)
                    {
                        await srv.SincronizarSubcategoria(item);
                    }
                });
                // ------------------------
            }
        }
    }
}