using System.Globalization; // Necesario
using System.Threading;     // Necesario
using System.Windows;
using System.Windows.Markup; // <--- ¡NUEVO! Necesario para XmlLanguage
using OrySiPOS.Data;
using QuestPDF.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace OrySiPOS
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // 1. Configurar cultura en C# (Para DateTime.Now.ToString() en código)
            var cultura = new CultureInfo("es-MX");

            // Ajuste opcional: asegurar que el símbolo de moneda sea $
            cultura.NumberFormat.CurrencySymbol = "$";

            Thread.CurrentThread.CurrentCulture = cultura;
            Thread.CurrentThread.CurrentUICulture = cultura;
            CultureInfo.DefaultThreadCurrentCulture = cultura;
            CultureInfo.DefaultThreadCurrentUICulture = cultura;

            // 2. ¡EL TRUCO PARA EL XAML! (Para {Binding StringFormat...})
            // Esto le dice a todos los controles visuales (WPF) que usen el idioma que acabamos de configurar
            FrameworkElement.LanguageProperty.OverrideMetadata(
                typeof(FrameworkElement),
                new FrameworkPropertyMetadata(
                    XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));

            // --- Tu código de inicio original ---
            base.OnStartup(e);

            QuestPDF.Settings.License = LicenseType.Community;

            using (var context = new InventarioDbContext())
            {
                context.SeedData();
            }

            // 1. PRIMERO: Preparamos la Base de Datos
            InicializarBaseDeDatos();

            // 2. LUEGO: Abrimos la ventana manualmente
            MainWindow ventanaPrincipal = new MainWindow();
            ventanaPrincipal.Show();
        }

        private void InicializarBaseDeDatos()
        {
            using (var db = new InventarioDbContext())
            {
                // Esto crea las tablas si no existen
                db.Database.Migrate();

                // Seed básico de seguridad (solo si está vacío)
                if (!db.Categorias.Any())
                {
                    db.Categorias.Add(new OrySiPOS.Models.Categoria { Nombre = "General" });
                    db.SaveChanges();
                }

                if (!db.Subcategorias.Any())
                {
                    var catId = db.Categorias.First().Id;
                    db.Subcategorias.Add(new OrySiPOS.Models.Subcategoria { Nombre = "General", CategoriaId = catId });
                    db.SaveChanges();
                }
            }
        }
    }
}