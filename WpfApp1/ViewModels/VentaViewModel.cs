// ViewModels/VentaViewModel.cs
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using OrySiPOS.Data;
using OrySiPOS.Models;
using OrySiPOS.Views;
using OrySiPOS.Views.Dialogs;

namespace OrySiPOS.ViewModels
{
    public class VentaViewModel : ViewModelBase
    {
        // --- 1. Estado y Propiedades (Lo que la Vista va a "ver") ---

        // La lista del carrito. La Vista se bindea a esto.
        public ObservableCollection<CartItem> CarritoItems { get; set; }

        // La lista de búsqueda.
        public ObservableCollection<Producto> ResultadosBusqueda { get; set; }

        //private const decimal TASA_IVA = 0.16m;

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

        // --- PROPIEDAD NUEVA ---
        private bool _imprimirCotizacion = true; // Iniciamos en true para que por defecto imprima
        public bool ImprimirCotizacion
        {
            get { return _imprimirCotizacion; }
            set { _imprimirCotizacion = value; OnPropertyChanged(); }
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
        // --- PROPIEDAD FALTANTE ---
        private Cliente _clienteSeleccionado;
        public Cliente ClienteSeleccionado
        {
            get { return _clienteSeleccionado; }
            set
            {
                _clienteSeleccionado = value;
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
        // En la sección de Comandos de VentaViewModel.cs
        public ICommand ReimprimirUltimoTicketCommand { get; }

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
            // En el constructor VentaViewModel()
            // ... tus otros comandos ...
            ReimprimirUltimoTicketCommand = new RelayCommand(EjecutarReimpresion);
            // Calcular totales al iniciar
            ActualizarTotales();
        }


        // --- 4. Lógica de Métodos (Las "Acciones" en sí) ---

        #region Lógica de Totales

        private void ActualizarTotales()
        {
            decimal acumuladoSubtotal = 0;
            decimal acumuladoIVA = 0;

            // Recorremos cada producto en el carrito
            foreach (var item in CarritoItems)
            {
                // El 'Price' es precio público (con impuestos).
                // El 'Subtotal' del item es (Price * Quantity).
                decimal totalLineaConImpuestos = item.Subtotal;

                // Desglosamos ESTA línea con SU propia tasa
                // Fórmula: Base = Total / (1 + Tasa)
                decimal baseLinea = totalLineaConImpuestos / (1 + item.TasaIVA);

                // Impuesto = Total - Base
                decimal ivaLinea = totalLineaConImpuestos - baseLinea;

                acumuladoSubtotal += baseLinea;
                acumuladoIVA += ivaLinea;
            }

            // Asignamos los resultados a las propiedades visuales
            Subtotal = acumuladoSubtotal;
            Iva = acumuladoIVA;

            // El total sigue siendo Subtotal + IVA (menos descuentos si hubiera)
            Total = (Subtotal + Iva) - MontoDescuento;

            // Notificamos comandos...
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
                    Quantity = 1,
                    TasaIVA = producto.PorcentajeIVA
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
                    // AGREGA: && p.Activo
                    productosEncontrados = db.Productos
                        .Where(p => p.ID == idBusqueda && p.Activo)
                        .ToList();
                }
                else
                {
                    string busquedaLower = terminoBusqueda.ToLower();
                    productosEncontrados = db.Productos
                                           .Where(p => p.Activo && p.Descripcion.ToLower().Contains(busquedaLower)) // AGREGA: p.Activo &&
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
                    productoParaAgregar = db.Productos.FirstOrDefault(p => p.ID == idBusqueda && p.Activo);
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
            // 1. Validaciones previas
            if (!CarritoItems.Any())
            {
                MessageBox.Show("El carrito está vacío.");
                return;
            }

            // Validación de Stock (Solo si es Venta normal)
            if (!this.EsModoCotizacion)
            {
                string productosSinStock = "";
                using (var db = new InventarioDbContext())
                {
                    foreach (var item in CarritoItems)
                    {
                        int id = int.Parse(item.ID);
                        var prod = db.Productos.AsNoTracking().FirstOrDefault(p => p.ID == id);
                        if (prod != null && prod.Stock < item.Quantity)
                        {
                            productosSinStock += $"- {prod.Descripcion} (Tienes: {prod.Stock}, Vendes: {item.Quantity})\n";
                        }
                    }
                }

                if (!string.IsNullOrEmpty(productosSinStock))
                {
                    var respuesta = MessageBox.Show(
                        $"⚠️ ADVERTENCIA DE INVENTARIO ⚠️\n\n" +
                        $"Los siguientes productos no tienen stock suficiente:\n{productosSinStock}\n" +
                        $"¿Deseas continuar y dejar el inventario en NEGATIVO?",
                        "Confirmar venta sin stock",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (respuesta == MessageBoxResult.No) return;
                }
            }

            // 2. Preparar y mostrar el Modal de Pago
            var modalPago = new FormaPagoModal();
            modalPago.Owner = Application.Current.MainWindow;
            modalPago.TotalAPagar = this.Total;

            // Configuramos el modal según el modo
            if (modalPago.DataContext is FormaPagoViewModel vmDelModal)
            {
                vmDelModal.EsModoCotizacion = this.EsModoCotizacion;
                // Opcional: Podrías pre-cargar aquí si el usuario ya tiene un cliente seleccionado
                if (this.ClienteSeleccionado != null)
                    vmDelModal.ClienteSeleccionado = this.ClienteSeleccionado;
            }

            bool? resultado = modalPago.ShowDialog();

            // 3. Procesar Resultado
            if (resultado == true)
            {
                try
                {
                    // Recuperamos datos del modal
                    var cliente = modalPago.ClienteSeleccionadoEnModal;
                    decimal pago = modalPago.PagoRecibidoEnModal;
                    string formaPagoSat = modalPago.FormaPagoSATEnModal;
                    string metodoPagoSat = modalPago.MetodoPagoSATEnModal;
                    decimal ajuste = modalPago.AjusteRedondeoEnModal;

                    // --- NUEVO: Recuperar el estado del CheckBox "Imprimir Ticket" ---
                    bool deseaImprimir = false;
                    if (modalPago.DataContext is FormaPagoViewModel vmFinal)
                    {
                        deseaImprimir = vmFinal.ImprimirTicket;
                    }

                    // --- Lógica de Ajuste por Redondeo (Solo afecta ventas en efectivo) ---
                    if (ajuste < 0)
                    {
                        this.MontoDescuento += Math.Abs(ajuste);
                        if (string.IsNullOrEmpty(this.TipoDescuento)) this.TipoDescuento = "Ajuste Redondeo";
                        ActualizarTotales(); // Esto iguala el Total con el Pago
                    }

                    if (this.EsModoCotizacion)
                    {
                        // ==========================================
                        // CAMINO A: COTIZACIÓN
                        // ==========================================

                        // 1. Guardar y obtener el objeto (con su nuevo ID)
                        var cotizacionNueva = GuardarCotizacionEnBD(cliente);

                        MessageBox.Show("Cotización guardada correctamente.", "Éxito");

                        // 2. Imprimir si el usuario lo pidió
                        if (deseaImprimir)
                        {
                            var itemsParaImprimir = new List<OrySiPOS.Services.ItemTicket>();
                            foreach (var item in this.CarritoItems)
                            {
                                itemsParaImprimir.Add(new OrySiPOS.Services.ItemTicket
                                {
                                    Nombre = item.Description,
                                    Cantidad = item.Quantity,
                                    Precio = item.Price
                                });
                            }

                            string nombreCliente = (cliente != null) ? cliente.RazonSocial : "Público General";

                            // Usamos el formato especial de COTIZACIÓN
                            OrySiPOS.Services.TicketPrintingService.ImprimirCotizacion(
                                productos: itemsParaImprimir,
                                subtotal: this.Subtotal,
                                iva: this.Iva,
                                total: this.Total,
                                cliente: nombreCliente,
                                folio: cotizacionNueva.ID.ToString(),
                                vigencia: cotizacionNueva.FechaVencimiento
                            );
                        }
                    }
                    else
                    {
                        // ==========================================
                        // CAMINO B: VENTA REAL
                        // ==========================================

                        decimal cambio = pago - this.Total;

                        // 1. Guardar Venta
                        var ventaGuardada = GuardarVentaEnBD(this.Subtotal, this.Iva, this.Total, cliente, pago, cambio, formaPagoSat, metodoPagoSat);

                        if (cambio < -0.01m)
                            MessageBox.Show($"Venta a CRÉDITO registrada. Adeudo: {Math.Abs(cambio):C}", "Crédito Autorizado");
                        else
                            MessageBox.Show("Venta realizada con éxito.", "Venta Finalizada");

                        // 2. Imprimir si el usuario lo pidió
                        if (deseaImprimir)
                        {
                            var listaParaImprimir = new List<OrySiPOS.Services.ItemTicket>();
                            foreach (var item in this.CarritoItems)
                            {
                                listaParaImprimir.Add(new OrySiPOS.Services.ItemTicket
                                {
                                    Nombre = item.Description,
                                    Cantidad = item.Quantity,
                                    Precio = item.Price
                                });
                            }

                            string nombreCliente = (cliente != null) ? cliente.RazonSocial : "Público General";

                            // Usamos el formato normal de TICKET DE VENTA
                            OrySiPOS.Services.TicketPrintingService.ImprimirTicket(
                                productos: listaParaImprimir,
                                subtotal: this.Subtotal,
                                iva: this.Iva,
                                descuento: this.MontoDescuento,
                                total: this.Total,
                                pago: pago,
                                cambio: cambio,
                                cliente: nombreCliente,
                                folio: ventaGuardada.VentaId.ToString()
                            );
                        }
                    }

                    // 4. Limpieza final
                    LimpiarSesion();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ocurrió un error al procesar: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

        private Venta GuardarVentaEnBD(decimal subtotal, decimal iva, decimal total, Cliente cliente, decimal pago, decimal cambio, string formaPagoSat, string metodoPagoSat)
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

                    // --- ¡ASIGNACIÓN DE CAMPOS CFDI! ---
                    FormaPagoSAT = formaPagoSat,
                    MetodoPagoSAT = metodoPagoSat,
                    ClienteId = (cliente != null && cliente.ID != 0) ? (int?)cliente.ID : null
                };

                foreach (var item in CarritoItems)
                {
                    var productoId = int.Parse(item.ID);
                    var productoEnDb = db.Productos.FirstOrDefault(p => p.ID == productoId);

                    if (productoEnDb == null) throw new Exception($"El producto '{item.Description}' ya no existe.");

                    if (!productoEnDb.Activo) throw new Exception($"El producto '{item.Description}' está desactivado.");

                    // --- CAMBIO IMPORTANTE AQUÍ ---
                    // Eliminamos el 'throw Exception' de stock insuficiente.
                    // Permitimos que la resta ocurra, incluso si da negativo.
                    productoEnDb.Stock -= item.Quantity;
                    // ------------------------------
                    var detalle = new VentaDetalle
                    {
                        ProductoId = productoId,
                        Cantidad = item.Quantity,
                        PrecioUnitario = item.Price,
                        Venta = nuevaVenta,
                        // --- ¡AQUÍ GUARDAMOS LA FOTO! ---
                        Descripcion = item.Description, // Guardamos el nombre actual
                        Costo = productoEnDb.Costo      // Guardamos el costo actual
                                                        // --------------------------------
                    };
                    nuevaVenta.Detalles.Add(detalle);
                }
                db.Ventas.Add(nuevaVenta);
                db.SaveChanges();

                // --- CÓDIGO NUEVO ---
                // Recolectamos los IDs de los productos que cambiaron
                var idsModificados = CarritoItems.Select(i => int.Parse(i.ID)).ToList();

                // Lanzamos la tarea en segundo plano para no hacer esperar al cliente en caja
                Task.Run(async () =>
                {
                    try
                    {
                        using (var dbSync = new InventarioDbContext())
                        {
                            var productosActualizados = dbSync.Productos
                                .Include(p => p.Subcategoria).ThenInclude(s => s.Categoria)
                                .Where(p => idsModificados.Contains(p.ID))
                                .ToList();

                            var srv = new OrySiPOS.Services.SupabaseService();
                            foreach (var prod in productosActualizados)
                            {
                                await srv.SincronizarProducto(prod);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log silencioso
                        System.Diagnostics.Debug.WriteLine("Error sync post-venta: " + ex.Message);
                    }
                });
                // --------------------

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
        // Cambiamos 'void' por 'Cotizacion'
        private Cotizacion GuardarCotizacionEnBD(Cliente clienteSeleccionado)
        {
            using (var db = new InventarioDbContext())
            {
                var nuevaCot = new Cotizacion
                {
                    FechaEmision = DateTime.Now,
                    FechaVencimiento = DateTime.Now.AddDays(15),
                    ClienteId = (clienteSeleccionado != null && clienteSeleccionado.ID != 0) ? (int?)clienteSeleccionado.ID : null,
                    Subtotal = this.Subtotal,
                    IVA = this.Iva,
                    Total = this.Total,
                    Origen = "Local"
                };

                foreach (var item in CarritoItems)
                {
                    var detalle = new CotizacionDetalle
                    {
                        ProductoId = int.Parse(item.ID),
                        Descripcion = item.Description,
                        Cantidad = item.Quantity,
                        PrecioUnitario = item.Price,
                        Cotizacion = nuevaCot
                    };
                    nuevaCot.Detalles.Add(detalle);
                }

                db.Cotizaciones.Add(nuevaCot);
                db.SaveChanges(); // Aquí se genera el ID

                return nuevaCot; // <--- ¡ESTO ES LO IMPORTANTE! Devolvemos el objeto con el ID
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

        private void EjecutarReimpresion(object obj)
        {
            try
            {
                using (var db = new InventarioDbContext())
                {
                    // 1. Buscamos la última venta
                    var ultimaVenta = db.Ventas
                                        .Include(v => v.Detalles)
                                        .Include(v => v.Cliente)
                                        .OrderByDescending(v => v.VentaId)
                                        .FirstOrDefault();

                    if (ultimaVenta == null)
                    {
                        MessageBox.Show("No hay ventas registradas en el historial.", "Aviso");
                        return;
                    }

                    // --- NUEVO: PREGUNTAR ANTES DE IMPRIMIR ---
                    var respuesta = MessageBox.Show(
                        $"¿Deseas reimprimir el último ticket?\n\n" +
                        $"Folio: {ultimaVenta.VentaId}\n" +
                        $"Fecha: {ultimaVenta.Fecha:dd/MM/yyyy HH:mm}\n" +
                        $"Total: {ultimaVenta.Total:C}",
                        "Confirmar Reimpresión",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    // Si el usuario dice "No", nos salimos y no pasa nada.
                    if (respuesta != MessageBoxResult.Yes) return;
                    // ------------------------------------------

                    // 2. Si dijo que SÍ, preparamos los datos...
                    var productosParaTicket = new List<OrySiPOS.Services.ItemTicket>();

                    foreach (var detalle in ultimaVenta.Detalles)
                    {
                        string nombreProducto = detalle.Descripcion ?? "Producto sin nombre";

                        productosParaTicket.Add(new OrySiPOS.Services.ItemTicket
                        {
                            Nombre = nombreProducto,
                            Cantidad = detalle.Cantidad,
                            Precio = detalle.PrecioUnitario
                        });
                    }

                    string nombreCliente = ultimaVenta.Cliente != null
                                           ? ultimaVenta.Cliente.RazonSocial
                                           : "Público en General";

                    // 3. ... y mandamos a imprimir
                    OrySiPOS.Services.TicketPrintingService.ImprimirTicket(
                        productos: productosParaTicket,
                        subtotal: ultimaVenta.Subtotal,
                        iva: ultimaVenta.IVA,
                        descuento: 0,
                        total: ultimaVenta.Total,
                        pago: ultimaVenta.PagoRecibido,
                        cambio: ultimaVenta.Cambio,
                        cliente: nombreCliente,
                        folio: ultimaVenta.VentaId.ToString()
                    );
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al reimprimir: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


    }
}