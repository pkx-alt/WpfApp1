using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations; // Necesario para [Key]
using System.ComponentModel.DataAnnotations.Schema; // <-- ¡ESTA FALTABA! (Para [NotMapped])
using System.Runtime.CompilerServices; // <-- ¡ESTA FALTABA! (Para [CallerMemberName])

namespace WpfApp1.Models
{
    public class Cliente : INotifyPropertyChanged
    {
        [Key] // Esto le dice a EF Core que 'ID' es la llave primaria
        public int ID { get; set; }

        [Required] // Opcional, pero buena práctica
        [MaxLength(13)]
        public string RFC { get; set; }

        [Required]
        public string RazonSocial { get; set; }

        public string Telefono { get; set; }

        // Vamos a ponerle un valor por defecto
        public DateTime Creado { get; set; } = DateTime.Now;

        // --- ¡AÑADE ESTA LÍNEA! ---
        public bool Activo { get; set; } = true;
        // --- 2. PROPIEDAD DE ESTADO DE LA VISTA ---

        private bool _isSelected; // Variable privada

        [NotMapped] // <-- ¡CRÍTICO! Le dice a EF Core: "Ignora esta propiedad, no es una columna"
        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                _isSelected = value;
                OnPropertyChanged(); // ¡Aquí "grita" que cambió!
            }
        }

        // --- 3. IMPLEMENTACIÓN DE INotifyPropertyChanged ---
        // Este es el "motor" que permite los avisos.
        // (Puedes copiar y pegar esto en cualquier modelo)
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
