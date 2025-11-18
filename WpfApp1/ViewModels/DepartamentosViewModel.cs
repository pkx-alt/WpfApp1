// ViewModels/DepartamentosViewModel.cs
using Microsoft.EntityFrameworkCore; // ¡Para usar .Include()!
using System.Collections.ObjectModel; // ¡Muy importante!
using System.Linq;
using System.Windows.Input;
using WpfApp1.Data;
using WpfApp1.Models;
using WpfApp1.Helpers;      // Para nuestro RelayCommand
using WpfApp1.Views.Dialogs; // Para nuestros diálogos
using System.Windows;       // Para MessageBox

namespace WpfApp1.ViewModels
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
            // 1. Abre el diálogo
            var dialog = new CrearCategoriaDialog();
            if (dialog.ShowDialog() == true)
            {
                // 2. Si el usuario dio "OK", lee el nombre
                var nuevoNombre = dialog.NombreCategoria;

                // 3. Crea el objeto
                var nuevaCategoria = new Categoria { Nombre = nuevoNombre };

                // 4. Guárdalo en la Base de Datos
                _context.Categorias.Add(nuevaCategoria);
                _context.SaveChanges();

                // 5. ¡Añádelo a la lista de la pantalla!
                //    (Gracias a ObservableCollection, la UI se actualiza sola)
                Categorias.Add(nuevaCategoria);
            }
        }

        private void OnCrearSubcategoria(object obj)
        {
            // ¡Validación! No podemos crear una subcategoría sin padre
            if (CategoriaSeleccionada == null)
            {
                MessageBox.Show("Debes seleccionar una categoría primero.", "Error");
                return;
            }

            // 1. Abre el diálogo (el de Subcategoría)
            var dialog = new CrearSubcategoriaDialog();
            if (dialog.ShowDialog() == true)
            {
                // 2. Lee el nombre
                var nuevoNombre = dialog.NombreSubcategoria; // Usa la propiedad correcta

                // 3. Crea el objeto (¡con la FK!)
                var nuevaSubcategoria = new Subcategoria
                {
                    Nombre = nuevoNombre,
                    CategoriaId = CategoriaSeleccionada.Id // La clave foránea
                };

                // 4. Guárdalo en la BD
                _context.Subcategorias.Add(nuevaSubcategoria);
                _context.SaveChanges();

                // 5. ¡Añádelo a la lista de la pantalla!
                Subcategorias.Add(nuevaSubcategoria);
            }
        }

        // --- 3. Métodos para EDITAR ---

        private void OnEditarCategoria(object parametro)
        {
            // El 'parametro' es la Categoría que el XAML nos envió
            if (parametro is Categoria categoriaParaEditar)
            {
                // Reutilizamos el mismo diálogo de "Crear"
                var dialog = new CrearCategoriaDialog();

                // ¡Pero lo pre-llenamos con el nombre actual!
                dialog.NombreTextBox.Text = categoriaParaEditar.Nombre;

                if (dialog.ShowDialog() == true)
                {
                    // 1. Actualiza el objeto en memoria
                    categoriaParaEditar.Nombre = dialog.NombreCategoria;

                    // 2. Le decimos a EF Core que este objeto fue modificado
                    _context.Categorias.Update(categoriaParaEditar);
                    _context.SaveChanges();

                    // 3. Forzamos un refresco de la lista en la UI.
                    //    (Esta es la forma más simple, aunque recarga todo)
                    CargarCategorias();
                }
            }
        }

        private void OnEditarSubcategoria(object parametro)
        {
            if (parametro is Subcategoria subcategoriaParaEditar)
            {
                // Reutilizamos el diálogo de "Crear" Subcategoría
                var dialog = new CrearSubcategoriaDialog();
                dialog.NombreTextBox.Text = subcategoriaParaEditar.Nombre;

                if (dialog.ShowDialog() == true)
                {
                    // 1. Actualiza objeto en memoria
                    subcategoriaParaEditar.Nombre = dialog.NombreSubcategoria;

                    // 2. Guarda en BD
                    _context.Subcategorias.Update(subcategoriaParaEditar);
                    _context.SaveChanges();

                    // 3. Refresca la lista de subcategorías (solo las de esa categoría)
                    CargarSubcategorias();
                }
            }
        }

        // --- 4. Métodos para ELIMINAR (con aviso) ---

        private void OnEliminarCategoria(object parametro)
        {
            if (parametro is Categoria categoriaParaEliminar)
            {
                // ¡La advertencia!
                // Verificamos si tiene subcategorías (ya las cargamos con .Include())
                string aviso = $"¿Estás seguro de que quieres eliminar '{categoriaParaEliminar.Nombre}'?";
                if (categoriaParaEliminar.Subcategorias.Any())
                {
                    aviso += $"\n\n¡ATENCIÓN! Esto borrará también sus {categoriaParaEliminar.Subcategorias.Count} subcategorías asociadas.";
                }

                // Mostramos el MessageBox de confirmación
                if (MessageBox.Show(aviso, "Confirmar eliminación", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    // 1. (Opcional) Borramos los hijos de la BD si EF no lo hace en cascada
                    // En este caso, EF Core es listo. Si borramos el padre,
                    // las claves foráneas de los hijos harán que se borren.

                    // 2. Borra el padre de la BD
                    _context.Categorias.Remove(categoriaParaEliminar);
                    _context.SaveChanges();

                    // 3. Borra de la lista de la pantalla
                    Categorias.Remove(categoriaParaEliminar);

                    // 4. Limpiamos la lista de subcategorías (porque su padre ya no existe)
                    Subcategorias.Clear();
                }
            }
        }

        private void OnEliminarSubcategoria(object parametro)
        {
            if (parametro is Subcategoria subcategoriaParaEliminar)
            {
                // ¡La advertencia!
                string aviso = $"¿Estás seguro de que quieres eliminar la subcategoría '{subcategoriaParaEliminar.Nombre}'?";

                if (MessageBox.Show(aviso, "Confirmar eliminación", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    // 1. Borra de la BD
                    _context.Subcategorias.Remove(subcategoriaParaEliminar);
                    _context.SaveChanges();

                    // 2. Borra de la lista de la pantalla
                    Subcategorias.Remove(subcategoriaParaEliminar);
                }
            }
        }

        // --- 3. Método para el NUEVO Comando ---
        private void OnMoverSubcategorias(object obj)
        {
            // 1. Validar que tengamos una categoría "padre" seleccionada
            if (CategoriaSeleccionada == null)
            {
                MessageBox.Show("Selecciona la categoría de la que quieres mover subcategorías.", "Error");
                return;
            }

            // 2. Encontrar todas las subcategorías marcadas con IsSelected
            var subcategoriasParaMover = Subcategorias
                .Where(s => s.IsSelected)
                .ToList(); // ¡Nuestra propiedad [NotMapped] en acción!

            // 3. Validar que se haya seleccionado al menos una
            if (subcategoriasParaMover.Count == 0)
            {
                MessageBox.Show("No has seleccionado ninguna subcategoría para mover (marca las casillas).", "Aviso");
                return;
            }

            // 4. Preparar la lista de "a dónde" se pueden mover
            var categoriasDestino = Categorias
                .Where(c => c.Id != CategoriaSeleccionada.Id) // Todas, excepto la actual
                .ToList();

            // 5. Abrir el diálogo
            var dialog = new MoverSubcategoriasDialog(subcategoriasParaMover.Count, categoriasDestino);
            if (dialog.ShowDialog() == true)
            {
                // 6. El usuario hizo clic en "Mover"
                Categoria categoriaDestino = dialog.CategoriaDestino;

                // 7. Actualizar el CategoriaId de cada subcategoría
                foreach (var sub in subcategoriasParaMover)
                {
                    sub.CategoriaId = categoriaDestino.Id;
                    _context.Subcategorias.Update(sub);
                }

                // 8. Guardar TODOS los cambios a la BD en una sola transacción
                _context.SaveChanges();

                // 9. ¡Refrescar la lista!
                //    Llamamos a CargarSubcategorias. Como los IDs cambiaron,
                //    las subcategorías movidas ya no aparecerán.
                CargarSubcategorias();
            }
        }
    }
}