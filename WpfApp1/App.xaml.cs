using System.Globalization; // Necesario
using System.Windows;
using System.Windows.Markup; // <--- ¡NUEVO! Necesario para XmlLanguage
using OrySiPOS.Data;
using QuestPDF.Infrastructure;
using Microsoft.EntityFrameworkCore;

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

            // 2. ¡EL TRUCO PARA EL XAML! 
            FrameworkElement.LanguageProperty.OverrideMetadata(
                typeof(FrameworkElement),
                new FrameworkPropertyMetadata(
                    XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));

            base.OnStartup(e);


            QuestPDF.Settings.License = LicenseType.Community;

            // 1. PRIMERO: Preparamos la Base de Datos
            InicializarBaseDeDatos();

            // 2. ¡NUEVO! VERIFICACIÓN DE CAJA
            // Antes de abrir la ventana principal, revisamos si hay turno abierto.
            bool cajaAbierta = false;

            using (var db = new InventarioDbContext())
            {
                // Buscamos si existe algún registro en CortesCaja que NO tenga fecha de cierre
                cajaAbierta = db.CortesCaja.Any(c => c.FechaCierre == null);
            }
            /*
            if (!cajaAbierta)
            {
                // Si NO hay caja abierta, forzamos la apertura
                AperturaCajaWindow ventanaApertura = new AperturaCajaWindow();

                // Mostramos la ventana como diálogo (bloquea el código hasta que se cierre)
                bool? resultado = ventanaApertura.ShowDialog();

                if (resultado == true)
                {
                    // ¡Éxito! El usuario abrió la caja correctamente.
                    // Podemos continuar hacia la ventana principal.
                }
                else
                {
                    // El usuario cerró la ventana o le dio Cancelar sin abrir la caja.
                    // En este caso, NO debemos dejarlo entrar al sistema.
                    Application.Current.Shutdown();
                    return; // Salimos del método para que no ejecute lo de abajo
                }
            }
            */

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