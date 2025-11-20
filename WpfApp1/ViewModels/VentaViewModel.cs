// ViewModels/VentaViewModel.cs
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using WpfApp1.Data;
using WpfApp1.Models; // Asumo que aquí están Producto, Venta, etc.
using WpfApp1.Views;
using WpfApp1.Views.Dialogs; // Para tu FormaPagoModal

// ¡Importante! Usamos la clase base que ya tenías
namespace WpfApp1.ViewModels
{
    public class VentaViewModel : ViewModelBase
    {
        // --- 1. Estado y Propiedades (Lo que la Vista va a "ver") ---

        // La lista del carrito. La Vista se bindea a esto.
        public ObservableCollection<CartItem> CarritoItems { get; set; }

        // La lista de búsqueda.
        public ObservableCollection<Producto> ResultadosBusqueda { get; set; }

        private const decimal TASA_IVA = 0.16m;

        // Propiedades para los TOTALES
        // Fíjate cómo usamos el patrón de "propiedad completa"
        // para poder notificar a la Vista cuando el valor cambie.

        // --- CONTROL DE MODO ---
        private bool _esModoCotizacion;
        public bool EsModoCotizacion
        {
            get { return _esModoCotizacion; }
            set
            {
                if (_esModoCotizacion != value)
                {
                    _esModoCotizacion = value;
                    OnPropertyChanged();

                    // Actualizamos textos y colores visuales
                    OnPropertyChanged(nameof(TituloPagina));
                    OnPropertyChanged(nameof(TextoBotonFinalizar));
                    OnPropertyChanged(nameof(ColorBotonFinalizar));
                }
            }
        }

        // Propiedades visuales (Bindings)
        public string TituloPagina => EsModoCotizacion ? "Nueva Cotización 📝" : "Realizar Venta 🛒";
        public string TextoBotonFinalizar => EsModoCotizacion ? "Guardar Cotización" : "Finalizar Venta (F12)";
        public string ColorBotonFinalizar => EsModoCotizacion ? "#FF8C00" : "#28A745"; // Naranja vs Verde

        private decimal _subtotal;
        public decimal Subtotal
        {
            get { return _subtotal; }
            set
            {
                _subtotal = value;
                OnPropertyChanged(); // ¡Avisa a la Vista!
            }
        }

        // ... después de public decimal Total ...

        private decimal _montoDescuento;
        public decimal MontoDescuento
        {
            get { return _montoDescuento; }
            set
            {
                _montoDescuento = value;
                OnPropertyChanged();
                // ¡IMPORTANTE! Si el descuento cambia,
                // forzamos un re-cálculo de todo.
                ActualizarTotales();
            }
        }

        private string _tipoDescuento; // Para mostrar "10%" o "$50"
        public string TipoDescuento
        {
            get { return _tipoDescuento; }
            set
            {
                _tipoDescuento = value;
                OnPropertyChanged();
            }
        }

        // ... El resto de tus propiedades (como _searchText) ...

        private decimal _iva;
        public decimal Iva
        {
            get { return _iva; }
            set
            {
                _iva = value;
                OnPropertyChanged(); // ¡Avisa a la Vista!
            }
        }

        private decimal _total;
        public decimal Total
        {
            get { return _total; }
            set
            {
                _total = value;
                OnPropertyChanged(); // ¡Avisa a la Vista!
            }
        }

        // Propiedad para el Texto del Buscador
        private string _searchText;
        public string SearchText
        {
            get { return _searchText; }
            set
            {
                _searchText = value;
                OnPropertyChanged();
                // ¡Cada vez que el texto cambia, ejecutamos la búsqueda!
                EjecutarBusquedaLive(_searchText);
            }
        }

        // --- 2. Comandos (Las "Acciones" que la Vista puede ejecutar) ---

        // ¡Este es el que tú mencionaste!
        public ICommand FinalizarVentaCommand { get; }
        public ICommand CancelarVentaCommand { get; }
        public ICommand IncreaseQuantityCommand { get; }
        public ICommand DecreaseQuantityCommand { get; }
        public ICommand AddItemFromSearchCommand { get; }
        public ICommand SearchOnEnterCommand { get; }
        public ICommand CambiarClienteCommand { get; }
        public ICommand ResumirVentaCommand { get; }
        public ICommand HistorialVentasCommand { get; }
        public ICommand RealizarDescuentoCommand { get; }
        public ICommand CancelarDescuentoCommand { get; }
        public ICommand CrearProductoRapidoCommand { get; }

        // --- 3. Constructor (Donde "conectamos" todo) ---
        public VentaViewModel()
        {
            // Inicializar colecciones
            CarritoItems = VentaSessionManager.GetSesionActiva();
            ResultadosBusqueda = new ObservableCollection<Producto>();

            // Suscribirnos a los eventos del carrito
            CarritoItems.CollectionChanged += CarritoItems_CollectionChanged;
            // Re-suscribir items existentes (si los hay al cargar)
            foreach (var item in CarritoItems)
            {
                item.PropertyChanged += CartItem_PropertyChanged;
            }

            // "Enchufar" los Comandos a sus métodos
            // Usamos la clase "traductora" RelayCommand
            FinalizarVentaCommand = new RelayCommand(EjecutarFinalizarVenta, PuedeFinalizarVenta);
            CancelarVentaCommand = new RelayCommand(EjecutarCancelarVenta);

            // Comandos que reciben un parámetro (el CartItem)
            IncreaseQuantityCommand = new RelayCommand(EjecutarIncreaseQuantity);
            DecreaseQuantityCommand = new RelayCommand(EjecutarDecreaseQuantity);

            // Comandos de Búsqueda
            AddItemFromSearchCommand = new RelayCommand(EjecutarAddItemFromSearch);
            SearchOnEnterCommand = new RelayCommand(EjecutarSearchOnEnter);

            // Comandos del panel derecho
            CambiarClienteCommand = new RelayCommand(EjecutarCambiarCliente);
            ResumirVentaCommand = new RelayCommand(EjecutarResumirVenta);
            HistorialVentasCommand = new RelayCommand(EjecutarHistorialVentas);
            RealizarDescuentoCommand = new RelayCommand(EjecutarRealizarDescuento, PuedeRealizarDescuento);
            CancelarDescuentoCommand = new RelayCommand(EjecutarCancelarDescuento, PuedeCancelarDescuento);
            // ... otros comandos ...
            CrearProductoRapidoCommand = new RelayCommand(EjecutarCrearProductoRapido);
            // Calcular totales al iniciar
            ActualizarTotales();
        }


        // --- 4. Lógica de Métodos (Las "Acciones" en sí) ---

        #region Lógica de Totales

        private void ActualizarTotales()
        {
            // 1. Sumamos el precio de lista de todos los productos
            // (Asumimos que tus precios en 'Productos' YA incluyen IVA)
            decimal sumaTotalConImpuestos = CarritoItems.Sum(item => item.Subtotal);

            // 2. Desglosamos el Subtotal (Matemática: Total / 1.16)
            Subtotal = sumaTotalConImpuestos / (1 + TASA_IVA);

            // 3. Calculamos cuánto de eso es IVA
            Iva = sumaTotalConImpuestos - Subtotal;

            // 4. Calculamos el Total Final a pagar
            // Al total de los productos le restamos el descuento (si hay)
            Total = sumaTotalConImpuestos - MontoDescuento;

            // --- (El resto se queda igual para notificar a la vista) ---
            (FinalizarVentaCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (RealizarDescuentoCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (CancelarDescuentoCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        private void CarritoItems_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (INotifyPropertyChanged item in e.NewItems)
                {
                    item.PropertyChanged += CartItem_PropertyChanged;
                }
            }

            if (e.OldItems != null)
            {
                foreach (INotifyPropertyChanged item in e.OldItems)
                {
                    item.PropertyChanged -= CartItem_PropertyChanged;
                }
            }
            ActualizarTotales();
        }

        private void CartItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Subtotal")
            {
                ActualizarTotales();
            }
        }

        #endregion

        #region Lógica del Carrito (Añadir/Quitar)

        private void EjecutarIncreaseQuantity(object parameter)
        {
            if (parameter is CartItem currentItem)
            {
                // TODO: Validar contra el stock
                currentItem.Quantity++;
            }
        }

        private void EjecutarDecreaseQuantity(object parameter)
        {
            if (parameter is CartItem currentItem)
            {
                if (currentItem.Quantity > 1)
                {
                    currentItem.Quantity--;
                }
                else
                {
                    CarritoItems.Remove(currentItem);
                }
            }
        }

        private void AgregarProductoAlCarrito(Producto producto)
        {
            if (producto == null) return;
            var itemEnCarrito = CarritoItems.FirstOrDefault(item => item.ID == producto.ID.ToString());

            if (itemEnCarrito != null)
            {
                itemEnCarrito.Quantity++;
            }
            else
            {
                var nuevoItem = new CartItem
                {
                    ID = producto.ID.ToString(),
                    Description = producto.Descripcion,
                    Price = producto.Precio,
                    Quantity = 1
                };
                CarritoItems.Add(nuevoItem);
            }
        }

        #endregion

        #region Lógica de Búsqueda

        private void EjecutarBusquedaLive(string terminoBusqueda)
        {
            if (string.IsNullOrEmpty(terminoBusqueda))
            {
                ResultadosBusqueda.Clear();
                return;
            }

            ResultadosBusqueda.Clear();
            using (var db = new InventarioDbContext())
            {
                bool esId = int.TryParse(terminoBusqueda, out int idBusqueda);
                List<Producto> productosEncontrados;

                if (esId)
                {
                    productosEncontrados = db.Productos.Where(p => p.ID == idBusqueda).ToList();
                }
                else
                {
                    string busquedaLower = terminoBusqueda.ToLower();
                    productosEncontrados = db.Productos
                                           .Where(p => p.Descripcion.ToLower().Contains(busquedaLower))
                                           .Take(15).ToList();
                }

                foreach (var prod in productosEncontrados)
                {
                    ResultadosBusqueda.Add(prod);
                }
            }
        }

        private void EjecutarSearchOnEnter(object parameter)
        {
            Producto productoParaAgregar = null;
            using (var db = new InventarioDbContext())
            {
                if (int.TryParse(SearchText, out int idBusqueda))
                {
                    productoParaAgregar = db.Productos.FirstOrDefault(p => p.ID == idBusqueda);
                }

                if (productoParaAgregar == null && ResultadosBusqueda.Any())
                {
                    productoParaAgregar = ResultadosBusqueda.First();
                }
            }

            if (productoParaAgregar != null)
            {
                AgregarProductoAlCarrito(productoParaAgregar);
                ResultadosBusqueda.Clear();
                SearchText = ""; // Esto limpia el TextBox gracias al Binding
            }
            else
            {
                ResultadosBusqueda.Clear();
                SearchText = "";
            }
        }

        private void EjecutarAddItemFromSearch(object parameter)
        {
            if (parameter is Producto productoSeleccionado)
            {
                AgregarProductoAlCarrito(productoSeleccionado);
                ResultadosBusqueda.Clear();
                SearchText = ""; // Limpia el TextBox
            }
        }

        #endregion

        #region Lógica de Acciones Principales

        // Esta es la lógica de tu "Mostrar Mensaje" (Button_Click)
        private void EjecutarFinalizarVenta(object parameter)
        {
            if (!CarritoItems.Any())
            {
                MessageBox.Show("El carrito está vacío.");
                return;
            }

            // 1. Preparamos el modal
            var modalPago = new FormaPagoModal();
            modalPago.Owner = Application.Current.MainWindow;
            modalPago.TotalAPagar = this.Total;

            // Pasamos el modo al ViewModel del modal
            if (modalPago.DataContext is FormaPagoViewModel vmDelModal)
            {
                vmDelModal.EsModoCotizacion = this.EsModoCotizacion;
            }

            // 2. Mostramos el modal
            bool? resultado = modalPago.ShowDialog();

            if (resultado == true)
            {
                try
                {
                    var cliente = modalPago.ClienteSeleccionadoEnModal;

                    if (this.EsModoCotizacion)
                    {
                        // --- LÓGICA DE COTIZACIÓN ---
                        GuardarCotizacionEnBD(cliente);
                        MessageBox.Show("Cotización guardada correctamente.", "Éxito");
                    }
                    else
                    {
                        // --- LÓGICA DE VENTA NORMAL ---
                        decimal pago = modalPago.PagoRecibidoEnModal;
                        decimal cambio = pago - this.Total;

                        // A. Guardamos en BD y obtenemos la venta guardada (para el Folio)
                        var ventaGuardada = GuardarVentaEnBD(this.Subtotal, this.Iva, this.Total, cliente, pago, cambio);

                        // B. Validamos si pagó completo o es crédito
                        if (cambio < 0)
                        {
                            decimal deuda = Math.Abs(cambio);
                            MessageBox.Show($"Venta a CRÉDITO registrada. Adeudo: {deuda:C}", "Crédito");
                        }
                        else
                        {
                            MessageBox.Show("Venta realizada con éxito.", "Venta Finalizada");
                        }

                        // C. --- IMPRESIÓN DE TICKET ---
                        // Verificamos si el usuario marcó "Imprimir ticket" en el modal
                        if (modalPago.DataContext is FormaPagoViewModel vmFinal && vmFinal.ImprimirTicket)
                        {
                            // 1. Convertimos los items del carrito al formato del impresor
                            var listaParaImprimir = new List<WpfApp1.Services.ItemTicket>();
                            foreach (var item in this.CarritoItems)
                            {
                                listaParaImprimir.Add(new WpfApp1.Services.ItemTicket
                                {
                                    Nombre = item.Description,
                                    Cantidad = item.Quantity,
                                    Precio = item.Price
                                });
                            }

                            // 2. Obtenemos datos finales
                            string nombreCliente = (cliente != null) ? cliente.RazonSocial : "Público General";
                            string folioString = ventaGuardada.VentaId.ToString();

                          
                            // 3. Llamamos al servicio (AHORA CON MÁS DATOS)
                            WpfApp1.Services.TicketPrintingService.ImprimirTicket(
                                productos: listaParaImprimir,
                                subtotal: this.Subtotal,
                                iva: this.Iva,
                                descuento: this.MontoDescuento, // Agregamos el descuento también por si acaso
                                total: this.Total,
                                pago: pago,
                                cambio: cambio,
                                cliente: nombreCliente,
                                folio: folioString
                            );
                        }
                    }

                    // Limpieza final
                    LimpiarSesion();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ocurrió un error: " + ex.Message);
                }
            }
        }

        // Método auxiliar para limpiar
        private void LimpiarSesion()
        {
            int index = VentaSessionManager.IndiceSesionActiva;
            VentaSessionManager.EliminarSesion(index);
            RefrescarSesionEnVM(VentaSessionManager.GetSesionActiva());
            ResultadosBusqueda.Clear();
            SearchText = "";
            // Opcional: Regresar a modo venta por defecto
            // EsModoCotizacion = false; 
        }

        // Este es el método "guardián" del comando
        private bool PuedeFinalizarVenta(object parameter)
        {
            // El botón SÓLO estará habilitado si hay items en el carrito
            return CarritoItems.Any();
        }

        private void EjecutarCancelarVenta(object parameter)
        {
            var respuesta = MessageBox.Show(
                "¿Estás seguro de que deseas cancelar la venta actual?",
                "Confirmar Cancelación",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (respuesta == MessageBoxResult.Yes)
            {
                int indiceVentaCancelada = VentaSessionManager.IndiceSesionActiva;
                VentaSessionManager.EliminarSesion(indiceVentaCancelada);
                var proximaVenta = VentaSessionManager.GetSesionActiva();
                RefrescarSesionEnVM(proximaVenta);

                ResultadosBusqueda.Clear();
                SearchText = "";
            }
        }

        private Venta GuardarVentaEnBD(decimal subtotal, decimal iva, decimal total, Cliente cliente, decimal pago, decimal cambio)
        {
            // (Esta lógica estaba perfecta, solo la copié y pegué)
            using (var db = new InventarioDbContext())
            {
                var nuevaVenta = new Venta
                {
                    Fecha = DateTime.Now,
                    Subtotal = subtotal,
                    IVA = iva,
                    Total = total,
                    Detalles = new List<VentaDetalle>(),
                    PagoRecibido = pago,
                    Cambio = cambio,
                    // --- ¡AQUÍ ESTÁ LA MAGIA! ---
                    // Si el cliente no es nulo Y su ID no es 0 (Público General),
                    // guardamos su ID. Si no, guardamos null.
                    ClienteId = (cliente != null && cliente.ID != 0) ? (int?)cliente.ID : null
                };

                foreach (var item in CarritoItems)
                {
                    var productoId = int.Parse(item.ID);
                    var productoEnDb = db.Productos.FirstOrDefault(p => p.ID == productoId);

                    if (productoEnDb == null)
                    {
                        throw new Exception($"El producto '{item.Description}' (ID: {item.ID}) ya no existe.");
                    }
                    if (productoEnDb.Stock < item.Quantity)
                    {
                        throw new Exception($"Stock insuficiente para '{item.Description}'. Solo quedan {productoEnDb.Stock} en inventario.");
                    }
                    productoEnDb.Stock -= item.Quantity;

                    var detalle = new VentaDetalle
                    {
                        ProductoId = productoId,
                        Cantidad = item.Quantity,
                        PrecioUnitario = item.Price,
                        Venta = nuevaVenta
                    };
                    nuevaVenta.Detalles.Add(detalle);
                }
                db.Ventas.Add(nuevaVenta);
                db.SaveChanges();
                return nuevaVenta; // <--- Agrega esto
            }
        }

        private void EjecutarRealizarDescuento(object parameter)
        {
            // --- ¡NUEVA LÓGICA CON EL DIÁLOGO! ---

            // 1. Crear y mostrar el diálogo
            var dialog = new DescuentoDialog();
            dialog.Owner = Application.Current.MainWindow;

            // 2. Esperar a que el usuario presione Aceptar o Cancelar
            bool? resultado = dialog.ShowDialog();

            // 3. Procesar el resultado
            if (resultado == true)
            {
                // El usuario presionó Aceptar, leemos los resultados
                decimal valor = dialog.Valor;
                bool esPorcentaje = dialog.EsPorcentaje;

                // 4. Calcular y guardar el descuento
                if (esPorcentaje)
                {
                    // Antes calculabas: (Subtotal + Iva) * porcentaje
                    // Ahora es lo mismo, pero conceptualmente es la suma directa:
                    decimal totalBruto = CarritoItems.Sum(item => item.Subtotal);

                    MontoDescuento = totalBruto * (valor / 100);
                    TipoDescuento = $"{valor}%";
                }
                else
                {
                    // Es un monto fijo
                    MontoDescuento = valor;
                    TipoDescuento = $"${valor} Fijo";
                }

                // NOTA: No llamamos a ActualizarTotales() aquí,
                // porque el 'set' de 'MontoDescuento' YA lo hace por nosotros.
                // ¡La magia del MVVM!
            }
            // Si el resultado es 'false' (cancelar), no hacemos nada.
        }

        private bool PuedeRealizarDescuento(object parameter)
        {
            // El botón de descuento debe estar habilitado SÓLO si:
            // 1. Hay items en el carrito.
            // 2. NO se ha aplicado un descuento todavía (MontoDescuento es 0).
            return CarritoItems.Any() && MontoDescuento == 0;
        }

        // ... (pon esto cerca de EjecutarRealizarDescuento) ...

        private void EjecutarCancelarDescuento(object parameter)
        {
            // ¡Simplemente borramos los valores!
            MontoDescuento = 0;
            TipoDescuento = null;

            // El 'set' de MontoDescuento llamará a ActualizarTotales()
            // y todo se recalculará y refrescará solo. ¡Magia!
        }

        private bool PuedeCancelarDescuento(object parameter)
        {
            // Solo podemos cancelar un descuento... si existe uno.
            return MontoDescuento > 0;
        }
        private void EjecutarCrearProductoRapido(object parameter)
        {
            // 1. Instanciamos tu ventana modal
            var modal = new NuevoProductoModal();

            // Definimos quién es el papá de la ventana (para que salga centrada sobre la principal)
            modal.Owner = Application.Current.MainWindow;

            // 2. Mostramos la ventana y esperamos (ShowDialog detiene el código aquí hasta que se cierre)
            bool? resultado = modal.ShowDialog();

            // 3. Si el usuario guardó (resultado == true)
            if (resultado == true)
            {
                // ¡Aquí es donde recuperamos el producto desde la propiedad que creamos en el Paso 1!
                var nuevoProducto = modal.ProductoRegistrado;

                if (nuevoProducto != null)
                {
                    // 4. Lo agregamos directamente al carrito usando tu método existente
                    AgregarProductoAlCarrito(nuevoProducto);

                    // Opcional: Mostrar un mensajito o sonido de éxito
                    // MessageBox.Show($"Se agregó '{nuevoProducto.Descripcion}' al carrito.");
                }
            }
        }
        // ... (fin de la región y la clase) ...

        #endregion

        #region Lógica de Sesiones (Panel Derecho)

        private void RefrescarSesionEnVM(ObservableCollection<CartItem> nuevoCarrito)
        {
            // 1. Darnos de baja del carrito viejo
            if (this.CarritoItems != null)
            {
                this.CarritoItems.CollectionChanged -= CarritoItems_CollectionChanged;
                foreach (var item in this.CarritoItems)
                {
                    item.PropertyChanged -= CartItem_PropertyChanged;
                }
            }

            // 2. Apuntar al nuevo carrito
            this.CarritoItems = nuevoCarrito;
            OnPropertyChanged(nameof(CarritoItems)); // ¡Avisamos que TODA la colección cambió!
                                                     // --- ¡LÍNEAS NUEVAS AQUÍ! ---
                                                     // ¡REINICIAMOS EL DESCUENTO!
            MontoDescuento = 0;
            TipoDescuento = null; // O puedes poner = "";
                                  // --- FIN DE LÍNEAS NUEVAS ---
                                  // 3. Suscribirnos a los eventos del nuevo carrito
            this.CarritoItems.CollectionChanged += CarritoItems_CollectionChanged;
            foreach (var item in this.CarritoItems)
            {
                item.PropertyChanged += CartItem_PropertyChanged;
            }

            // 4. Recalcular totales
            ActualizarTotales();
        }

        private void EjecutarCambiarCliente(object parameter)
        {
            var nuevoCarrito = VentaSessionManager.CrearNuevaSesion();
            RefrescarSesionEnVM(nuevoCarrito);

            int ventasEnEspera = VentaSessionManager.GetTotalSesiones() - 1;
            MessageBox.Show($"Iniciando nueva venta. \nHay {ventasEnEspera} venta(s) en espera.", "Cambio de Cliente");
        }

        private void EjecutarResumirVenta(object parameter)
        {
            if (VentaSessionManager.GetTotalSesiones() <= 1)
            {
                MessageBox.Show("No hay ninguna venta en espera.", "Información");
                return;
            }

            var dialog = new ResumirVentaDialog();
            dialog.Owner = Application.Current.MainWindow;
            bool? resultado = dialog.ShowDialog();

            if (resultado == true)
            {
                int indiceAResumir = dialog.IndiceSeleccionado;
                var carritoSeleccionado = VentaSessionManager.CambiarSesionActiva(indiceAResumir);
                RefrescarSesionEnVM(carritoSeleccionado);
            }
        }

        private void EjecutarHistorialVentas(object parameter)
        {
            // ¡Esto es un problema! El ViewModel no debe navegar.
            // Por ahora, lo dejaremos en el Code-Behind,
            // pero lo ideal es usar un "Servicio de Navegación".
            MessageBox.Show("La navegación aún debe conectarse.");

            // (Si DE VERDAD quieres hacerlo desde aquí, puedes hacer esto,
            // pero no es NADA recomendable)
            // (Application.Current.MainWindow as MainWindow)?.MainFrame.Navigate(new VentasRealizadasPage());
        }

        #endregion

        //COTIZACIONES
        private void GuardarCotizacionEnBD(Cliente clienteSeleccionado)
        {
            using (var db = new InventarioDbContext())
            {
                var nuevaCot = new Cotizacion
                {
                    FechaEmision = DateTime.Now,
                    FechaVencimiento = DateTime.Now.AddDays(15), // Vence en 15 días por defecto
                    ClienteId = (clienteSeleccionado != null && clienteSeleccionado.ID != 0) ? (int?)clienteSeleccionado.ID : null,
                    Subtotal = this.Subtotal,
                    IVA = this.Iva,
                    Total = this.Total
                };

                foreach (var item in CarritoItems)
                {
                    var detalle = new CotizacionDetalle
                    {
                        ProductoId = int.Parse(item.ID), // Asumiendo que tu ID es int
                        Descripcion = item.Description,
                        Cantidad = item.Quantity,
                        PrecioUnitario = item.Price,
                        Cotizacion = nuevaCot
                    };
                    nuevaCot.Detalles.Add(detalle);
                }

                db.Cotizaciones.Add(nuevaCot);
                db.SaveChanges();
            }
        }

        // En VentaViewModel.cs

        public void CargarDatosDeCotizacion(int cotizacionId)
        {
            using (var db = new InventarioDbContext())
            {
                // 1. Buscamos la cotización con sus detalles
                var cot = db.Cotizaciones
                            .Include(c => c.Cliente) // Traemos al cliente
                            .Include(c => c.Detalles) // Traemos los items
                            .FirstOrDefault(c => c.ID == cotizacionId);

                if (cot == null) return;

                // 2. Limpiamos el carrito actual (por si había algo sucio)
                CarritoItems.Clear();
                MontoDescuento = 0; // Reseteamos descuentos previos

                // 3. Configuramos el modo
                // IMPORTANTE: Lo ponemos en FALSE porque ahora vamos a vender (cobrar)
                EsModoCotizacion = false;

                // 4. Llenamos el carrito con los productos de la cotización
                foreach (var detalle in cot.Detalles)
                {
                    var itemCarrito = new CartItem
                    {
                        ID = detalle.ProductoId.ToString(),
                        Description = detalle.Descripcion,
                        Price = detalle.PrecioUnitario,
                        Quantity = detalle.Cantidad
                        // El Subtotal se calcula solo en la clase CartItem
                    };
                    CarritoItems.Add(itemCarrito);
                }

                // 5. Recalculamos los totales
                ActualizarTotales();

                // 6. Aquí podrías asignar el cliente si tu VentaViewModel 
                // tiene una propiedad para "ClienteSeleccionado" inicial.
                // Si usas el modal de cobro para elegir cliente, este paso es opcional,
                // pero ayuda para que el usuario sepa de quién es.
                // (Opcional: Guardar referencia del cliente en una variable temporal si gustas)
            }
        }


    }
}