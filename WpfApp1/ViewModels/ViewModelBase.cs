// ViewModels/ViewModelBase.cs
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace OrySiPOS.ViewModels
{
    // Esta es nuestra clase "radio"
    public class ViewModelBase : INotifyPropertyChanged
    {
        // El evento que la Vista estará "escuchando"
        public event PropertyChangedEventHandler PropertyChanged;

        // Este es el método que "emite el boletín"
        // [CallerMemberName] es un truco genial de C# que
        // pone automáticamente el nombre de la propiedad que llamó a este método.
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}