// App.xaml.cs
using System.Windows;
using WpfApp1.Data; // ¡Añade este using para traer tu DbContext!
using QuestPDF.Infrastructure;

namespace WpfApp1
{
    public partial class App : Application
    {
        // Sobrescribimos el método OnStartup
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            QuestPDF.Settings.License = LicenseType.Community;

            // Creamos una instancia de nuestro DbContext
            // El 'using' se asegura de que la conexión se cierre
            // correctamente cuando termine.
            using (var context = new InventarioDbContext())
            {
                // ¡Llamamos a nuestro método "sembrador"!
                context.SeedData();
            }

            // (Aquí puedes dejar cualquier otro código de inicio
            //  que ya tuvieras, como abrir tu MainWindow)
        }
    }
}