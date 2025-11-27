using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using OrySiPOS.Data;
using OrySiPOS.Models;

namespace OrySiPOS.ViewModels
{
    public class CuentasPorCobrarViewModel : ViewModelBase
    {
        // --- 1. Colecciones ---
        // Lista izquierda (Clientes con deuda)
        public ObservableCollection<CuentaPorCobrar> ListaDeudores { get; set; }

        // Lista derecha (Tickets del cliente seleccionado)
        public ObservableCollection<DetalleDeuda> MovimientosCliente { get; set; }

        // --- 2. Propiedades de Selección ---

        // 1. Necesitamos saber qué ticket de la tabla derecha seleccionó el usuario
        private DetalleDeuda _movimientoSeleccionado;
        public DetalleDeuda MovimientoSeleccionado
        {
            get { return _movimientoSeleccionado; }
            set
            {
                _movimientoSeleccionado = value;
                OnPropertyChanged();
                // Avisamos al botón que verifique si puede activarse
                (RealizarAbonoCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        private CuentaPorCobrar _deudorSeleccionado;
        public CuentaPorCobrar DeudorSeleccionado
        {
            get { return _deudorSeleccionado; }
            set
            {
                _deudorSeleccionado = value;
                OnPropertyChanged();
                // ¡Truco de Senior! 
                // Cuando seleccionamos un cliente, cargamos sus movimientos automáticamente.
                if (_deudorSeleccionado != null)
                {
                    CargarMovimientosDelCliente(_deudorSeleccionado.ClienteId);
                }
                else
                {
                    MovimientosCliente.Clear();
                    TotalSeleccionado = 0;
                }
            }
        }

        private decimal _totalSeleccionado;
        public decimal TotalSeleccionado
        {
            get { return _totalSeleccionado; }
            set { _totalSeleccionado = value; OnPropertyChanged(); }
        }

        // --- 3. Comandos ---
        public ICommand RefrescarCommand { get; set; }
        public ICommand RealizarAbonoCommand { get; set; } // Lo dejaremos listo para el futuro

        // --- 4. Constructor ---
        public CuentasPorCobrarViewModel()
        {
            ListaDeudores = new ObservableCollection<CuentaPorCobrar>();
            MovimientosCliente = new ObservableCollection<DetalleDeuda>();

            RefrescarCommand = new RelayCommand(param => CargarDeudores());
            RealizarAbonoCommand = new RelayCommand(EjecutarAbono, PuedeAbonar);

            // Cargamos datos al iniciar
            CargarDeudores();
        }

        // --- 5. Lógica de Base de Datos (La "Carnita") ---

        public void CargarDeudores()
        {
            using (var db = new InventarioDbContext())
            {
                ListaDeudores.Clear();

                // PASO A: Buscar todas las ventas que NO se han pagado completo.
                // Nota: Asumimos que hay deuda si (Total - PagoRecibido) > 0.01 (por redondeo)
                var ventasConDeuda = db.Ventas
                    .Include(v => v.Cliente)
                    .Where(v => v.ClienteId != null && (v.Total - v.PagoRecibido) > 0.01m)
                    .ToList();

                // PASO B: Agrupar esas ventas por Cliente para hacer la lista resumen
                var deudoresAgrupados = ventasConDeuda
                    .GroupBy(v => v.Cliente)
                    .Select(grupo => new CuentaPorCobrar
                    {
                        ClienteId = grupo.Key.ID,
                        NombreCompleto = grupo.Key.RazonSocial, // O Nombre si tienes esa prop
                        NumeroDeVentasPendientes = grupo.Count(),
                        TotalDeuda = grupo.Sum(v => v.Total - v.PagoRecibido),
                        FechaMasAntigua = grupo.Min(v => v.Fecha)
                    })
                    .ToList();

                // PASO C: Llenar la lista observable
                foreach (var deudor in deudoresAgrupados)
                {
                    ListaDeudores.Add(deudor);
                }
            }
        }

        private void CargarMovimientosDelCliente(int clienteId)
        {
            using (var db = new InventarioDbContext())
            {
                MovimientosCliente.Clear();

                // Buscamos solo las ventas de ESTE cliente que deban dinero
                var ventasDelCliente = db.Ventas
                    .Where(v => v.ClienteId == clienteId && (v.Total - v.PagoRecibido) > 0.01m)
                    .OrderBy(v => v.Fecha)
                    .ToList();

                foreach (var venta in ventasDelCliente)
                {
                    MovimientosCliente.Add(new DetalleDeuda
                    {
                        VentaId = venta.VentaId,
                        Folio = $"Ticket #{venta.VentaId}",
                        Fecha = venta.Fecha,
                        Concepto = "Venta de productos (Ver detalles...)", // Podrías mejorar esto
                        MontoOriginal = venta.Total,
                        PagadoHastaAhora = venta.PagoRecibido
                        // SaldoPendiente se calcula solo en la clase
                    });
                }

                // Actualizamos el total grande rojo de abajo
                TotalSeleccionado = MovimientosCliente.Sum(m => m.SaldoPendiente);
            }
        }

        // --- 6. Lógica de Botones (Placeholder) ---

        private bool PuedeAbonar(object param)
        {
            // Solo se puede abonar si hay un cliente seleccionado
            return DeudorSeleccionado != null;
        }

        // En ViewModels/CuentasPorCobrarViewModel.cs

        private void EjecutarAbono(object param)
        {
            // 1. Validación de seguridad
            if (MovimientoSeleccionado == null || DeudorSeleccionado == null) return;

            // 2. Preparamos el Modal
            var vm = new RegistroAbonoViewModel(MovimientoSeleccionado.SaldoPendiente);
            var ventana = new OrySiPOS.Views.Dialogs.RegistroAbonoWindow
            {
                DataContext = vm,
                Owner = Application.Current.MainWindow
            };

            // Conectamos el cierre
            vm.CloseAction = (resultado) =>
            {
                ventana.DialogResult = resultado;
                ventana.Close();
            };

            // 3. Mostramos la ventana
            if (ventana.ShowDialog() == true)
            {
                // ¡EL USUARIO DIJO SÍ! -> A PAGAR
                using (var db = new InventarioDbContext())
                {
                    // A) Actualizamos la Venta
                    var ventaEnBD = db.Ventas.Find(MovimientoSeleccionado.VentaId);
                    if (ventaEnBD != null)
                    {
                        ventaEnBD.PagoRecibido += vm.MontoAbono;
                        // (No tocamos 'Cambio' ni 'Total', solo aumentamos lo pagado)
                    }

                    // B) Registramos el Ingreso en Caja (IMPORTANTE)
                    var nuevoIngreso = new Ingreso
                    {
                        Fecha = DateTime.Now,
                        Categoria = "Abono de Cliente",
                        // Guardamos detalle en el concepto para saber de quién fue
                        Concepto = $"Abono a Ticket #{ventaEnBD.VentaId} - {DeudorSeleccionado.NombreCompleto} ({vm.MetodoPago})",
                        Usuario = "Admin", // O el usuario actual si tuvieras login
                        Monto = vm.MontoAbono
                    };
                    db.Ingresos.Add(nuevoIngreso);

                    // C) Guardamos todo
                    db.SaveChanges();

                    MessageBox.Show("Abono registrado correctamente.", "Éxito");
                }

                // 4. Refrescar la pantalla para ver los nuevos saldos
                // Guardamos el ID del cliente actual para volver a seleccionarlo
                int idClienteActual = DeudorSeleccionado.ClienteId;

                CargarDeudores(); // Recarga la lista izquierda

                // Volvemos a seleccionar al cliente para ver su tabla derecha actualizada
                DeudorSeleccionado = ListaDeudores.FirstOrDefault(c => c.ClienteId == idClienteActual);
            }
        }
    }
}