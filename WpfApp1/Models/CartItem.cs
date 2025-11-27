// --- Models/CartItem.cs ---

// ¡OJO! Asegúrate de que el namespace sea el de tu carpeta Models
namespace OrySiPOS.Models
{
    // Necesitamos estos 'usings' para que funcione INotifyPropertyChanged
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Representa un item dentro del carrito de compras.
    /// Esta es nuestra clase "Modelo" para el carrito.
    /// </summary>
    public class CartItem : INotifyPropertyChanged
    {
        public string ID { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }

        private int _quantity;
        public int Quantity
        {
            get { return _quantity; }
            set
            {
                if (_quantity != value)
                {
                    _quantity = value;
                    OnPropertyChanged();

                    // Avisa que Subtotal también cambió
                    OnPropertyChanged(nameof(Subtotal));
                }
            }
        }

        // Esta es una "propiedad calculada"
        public decimal Subtotal => Quantity * Price;


        // --- Lógica de INotifyPropertyChanged ---
        // (Esto es lo que permite que la UI se actualice
        // cuando cambia la Cantidad)

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}