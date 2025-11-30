using System.ComponentModel; // Para INotifyPropertyChanged
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices; // Para CallerMemberName

namespace OrySiPOS.Models
{
    // 1. Agregamos la interfaz INotifyPropertyChanged
    public class Producto : INotifyPropertyChanged
    {
        [Key]
        public int ID { get; set; }

        // Para propiedades visuales simples, podemos dejarlas auto-implementadas si no afectan lógica visual crítica,
        // pero para ClaveSat y ClaveUnidad necesitamos avisar del cambio.

        private string _descripcion;
        public string Descripcion
        {
            get => _descripcion;
            set { _descripcion = value; OnPropertyChanged(); }
        }

        private decimal _precio;
        public decimal Precio
        {
            get => _precio;
            set { _precio = value; OnPropertyChanged(); OnPropertyChanged(nameof(Ganancia)); }
        }

        private decimal _costo;
        public decimal Costo
        {
            get => _costo;
            set { _costo = value; OnPropertyChanged(); OnPropertyChanged(nameof(Ganancia)); }
        }

        public decimal PorcentajeIVA { get; set; } = 0.16m;

        public decimal Ganancia => Precio - Costo;

        private int _stock;
        public int Stock
        {
            get => _stock;
            set { _stock = value; OnPropertyChanged(); }
        }

        public string ImagenUrl { get; set; }

        private bool _activo;
        public bool Activo
        {
            get => _activo;
            set { _activo = value; OnPropertyChanged(); }
        }

        private bool _esServicio;
        public bool EsServicio
        {
            get => _esServicio;
            set
            {
                _esServicio = value;
                OnPropertyChanged();
                // Si es servicio, podríamos querer avisar para cambiar validaciones visuales
            }
        }

        public int SubcategoriaId { get; set; }

        // --- AQUÍ ESTÁ LA MAGIA PARA EL SAT ---

        private string _claveSat;
        [MaxLength(8)]
        public string ClaveSat
        {
            get => _claveSat;
            set
            {
                if (_claveSat != value)
                {
                    _claveSat = value;
                    OnPropertyChanged();
                    // ¡AVISAR QUE EL ESTATUS DE DATOS FISCALES CAMBIÓ!
                    OnPropertyChanged(nameof(TieneDatosFiscales));
                }
            }
        }

        private string _claveUnidad;
        [MaxLength(3)]
        public string ClaveUnidad
        {
            get => _claveUnidad;
            set
            {
                if (_claveUnidad != value)
                {
                    _claveUnidad = value;
                    OnPropertyChanged();
                    // ¡AVISAR QUE EL ESTATUS DE DATOS FISCALES CAMBIÓ!
                    OnPropertyChanged(nameof(TieneDatosFiscales));
                }
            }
        }

        public virtual Subcategoria Subcategoria { get; set; }

        // --- PROPIEDAD CALCULADA VISUAL ---
        [NotMapped]
        public bool TieneDatosFiscales =>
            !string.IsNullOrEmpty(ClaveSat) &&
            ClaveSat != "01010101" &&
            !string.IsNullOrEmpty(ClaveUnidad);

        public Producto()
        {
            Descripcion = string.Empty;
            ImagenUrl = string.Empty;
            Activo = true;
            ClaveSat = "01010101";
            ClaveUnidad = "H87";
        }

        // --- IMPLEMENTACIÓN DE LA INTERFAZ ---
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}