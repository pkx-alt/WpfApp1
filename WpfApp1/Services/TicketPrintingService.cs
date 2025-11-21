using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text; // Necesario para StringBuilder
using ESCPOS_NET;
using ESCPOS_NET.Emitters;
using ESCPOS_NET.Utilities;
// Asegúrate de tener acceso a las propiedades. Si da error, agrega: using WpfApp1.Properties;

namespace WpfApp1.Services
{
    public class ItemTicket
    {
        public string Nombre { get; set; }
        public int Cantidad { get; set; }
        public decimal Precio { get; set; }
        public decimal Total => Cantidad * Precio;
    }

    public class TicketPrintingService
    {
        // CONSEJO PRO: También podrías leer esto de Settings si el usuario cambia de impresora
        // private static string NOMBRE_IMPRESORA => WpfApp1.Properties.Settings.Default.ImpresoraSeleccionada; 
        // Pero por ahora lo dejamos constante como pediste en el código original o fijo.
        private const string NOMBRE_IMPRESORA = "XP-58";

        public static void ImprimirTicket(List<ItemTicket> productos, decimal subtotal, decimal iva, decimal descuento, decimal total, decimal pago, decimal cambio, string cliente, string folio)
        {
            try
            {
                var emitter = new EPSON();
                var commands = new List<byte[]>();

                // Inicializamos
                commands.Add(emitter.Initialize());

                // --- ENCABEZADO DINÁMICO ---
                commands.Add(emitter.CenterAlign());
                commands.Add(emitter.SetStyles(PrintStyle.Bold | PrintStyle.DoubleHeight | PrintStyle.DoubleWidth));

                // 1. NOMBRE DE LA TIENDA (Desde Ajustes)
                string nombreTienda = WpfApp1.Properties.Settings.Default.NombreTienda;
                if (string.IsNullOrWhiteSpace(nombreTienda)) nombreTienda = "Mi Punto de Venta"; // Texto por defecto si está vacío

                commands.Add(emitter.PrintLine(SinTildes(nombreTienda)));

                commands.Add(emitter.SetStyles(PrintStyle.None));

                // 2. DIRECCIÓN (Desde Ajustes)
                string direccion = WpfApp1.Properties.Settings.Default.DireccionTienda;
                if (!string.IsNullOrWhiteSpace(direccion))
                {
                    commands.Add(emitter.PrintLine(SinTildes(direccion)));
                }

                // 3. TELÉFONO (Desde Ajustes)
                string telefono = WpfApp1.Properties.Settings.Default.TelefonoTienda;
                if (!string.IsNullOrWhiteSpace(telefono))
                {
                    commands.Add(emitter.PrintLine($"Tel: {telefono}"));
                }

                // Fecha y Hora
                commands.Add(emitter.PrintLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm")));
                commands.Add(emitter.PrintLine(new string('-', 32)));

                // --- DATOS CLIENTE ---
                commands.Add(emitter.LeftAlign());
                commands.Add(emitter.PrintLine($"Folio: {folio}"));
                commands.Add(emitter.PrintLine($"Cliente: {SinTildes(cliente)}"));

                // --- TABLA DE PRODUCTOS ---
                commands.Add(emitter.PrintLine(new string('-', 32)));
                commands.Add(emitter.SetStyles(PrintStyle.Bold));
                commands.Add(emitter.PrintLine("PRODUCTO"));
                commands.Add(emitter.SetStyles(PrintStyle.None));
                commands.Add(emitter.PrintLine("CANT  x  PRECIO           TOTAL"));
                commands.Add(emitter.PrintLine(new string('-', 32)));

                foreach (var item in productos)
                {
                    // Nombre del producto limpio
                    string nombreLimpio = SinTildes(item.Nombre);
                    commands.Add(emitter.PrintLine(nombreLimpio));

                    // Números
                    string parteIzq = $"{item.Cantidad} x {item.Precio:C}";
                    string parteDer = $"{item.Total:C}";

                    // Cálculo simple de espacios para alinear a la derecha (asumiendo 32 caracteres de ancho)
                    int espacios = 32 - parteIzq.Length - parteDer.Length;
                    if (espacios < 1) espacios = 1;

                    commands.Add(emitter.PrintLine(parteIzq + new string(' ', espacios) + parteDer));
                    commands.Add(emitter.PrintLine(""));
                }

                commands.Add(emitter.PrintLine(new string('-', 32)));

                // --- TOTALES ---
                commands.AddRange(ImprimirLineaTotal(emitter, "Subtotal:", subtotal));

                if (descuento > 0)
                {
                    commands.AddRange(ImprimirLineaTotal(emitter, "Descuento:", -descuento));
                }

                commands.AddRange(ImprimirLineaTotal(emitter, "IVA (16%):", iva));

                commands.Add(emitter.PrintLine(new string(' ', 32)));

                commands.Add(emitter.RightAlign());
                commands.Add(emitter.SetStyles(PrintStyle.Bold | PrintStyle.DoubleWidth));
                commands.Add(emitter.PrintLine($"TOTAL: {total:C}"));
                commands.Add(emitter.SetStyles(PrintStyle.None));

                commands.Add(emitter.PrintLine(new string('-', 32)));

                commands.AddRange(ImprimirLineaTotal(emitter, "Su Pago:", pago));
                commands.AddRange(ImprimirLineaTotal(emitter, "Cambio:", cambio));

                // --- PIE ---
                commands.Add(emitter.FeedLines(2));
                commands.Add(emitter.CenterAlign());

                // 4. MENSAJE PERSONALIZADO (Desde Ajustes)
                string mensajeFinal = WpfApp1.Properties.Settings.Default.MensajeTicket;
                if (string.IsNullOrWhiteSpace(mensajeFinal)) mensajeFinal = "Gracias por su compra!";

                commands.Add(emitter.PrintLine(SinTildes(mensajeFinal)));

                commands.Add(emitter.FeedLines(4));
                commands.Add(emitter.FullCut());

                // Enviar a impresora
                byte[] bytes = ByteSplicer.Combine(commands.ToArray());

                // Aquí podrías también usar Settings.Default.ImpresoraSeleccionada si quisieras hacerlo dinámico
                RawPrinterHelper.SendBytesToPrinter(NOMBRE_IMPRESORA, bytes);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Error de impresión: " + ex.Message);
            }
        }

        private static List<byte[]> ImprimirLineaTotal(EPSON emitter, string etiqueta, decimal valor)
        {
            string valStr = $"{valor:C}";
            int espacios = 32 - etiqueta.Length - valStr.Length;
            if (espacios < 1) espacios = 1;

            var cmds = new List<byte[]>();
            cmds.Add(emitter.LeftAlign());
            cmds.Add(emitter.PrintLine(etiqueta + new string(' ', espacios) + valStr));
            return cmds;
        }

        public static string SinTildes(string texto)
        {
            if (string.IsNullOrEmpty(texto)) return "";

            string original = "áéíóúÁÉÍÓÚñÑ";
            string reemplazo = "aeiouAEIOUnN";

            StringBuilder sb = new StringBuilder(texto);

            for (int i = 0; i < original.Length; i++)
            {
                sb.Replace(original[i], reemplazo[i]);
            }

            return sb.ToString();
        }
    }

    public class RawPrinterHelper
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public class DOCINFOA
        {
            [MarshalAs(UnmanagedType.LPStr)] public string pDocName;
            [MarshalAs(UnmanagedType.LPStr)] public string pOutputFile;
            [MarshalAs(UnmanagedType.LPStr)] public string pDataType;
        }

        [DllImport("winspool.Drv", EntryPoint = "OpenPrinterA", SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool OpenPrinter([MarshalAs(UnmanagedType.LPStr)] string szPrinter, out IntPtr hPrinter, IntPtr pd);

        [DllImport("winspool.Drv", EntryPoint = "ClosePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool ClosePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "StartDocPrinterA", SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool StartDocPrinter(IntPtr hPrinter, Int32 level, [In, MarshalAs(UnmanagedType.LPStruct)] DOCINFOA di);

        [DllImport("winspool.Drv", EntryPoint = "EndDocPrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool EndDocPrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "StartPagePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool StartPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "EndPagePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool EndPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "WritePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, Int32 dwCount, out Int32 dwWritten);

        public static bool SendBytesToPrinter(string szPrinterName, byte[] pBytes)
        {
            Int32 dwError = 0, dwWritten = 0;
            IntPtr hPrinter = new IntPtr(0);
            DOCINFOA di = new DOCINFOA();
            bool bSuccess = false;

            di.pDocName = "Ticket OrySi";
            di.pDataType = "RAW";

            if (OpenPrinter(szPrinterName.Normalize(), out hPrinter, IntPtr.Zero))
            {
                if (StartDocPrinter(hPrinter, 1, di))
                {
                    if (StartPagePrinter(hPrinter))
                    {
                        IntPtr pUnmanagedBytes = Marshal.AllocCoTaskMem(pBytes.Length);
                        Marshal.Copy(pBytes, 0, pUnmanagedBytes, pBytes.Length);
                        bSuccess = WritePrinter(hPrinter, pUnmanagedBytes, pBytes.Length, out dwWritten);
                        Marshal.FreeCoTaskMem(pUnmanagedBytes);
                        EndPagePrinter(hPrinter);
                    }
                    EndDocPrinter(hPrinter);
                }
                ClosePrinter(hPrinter);
            }
            if (!bSuccess)
            {
                return false;
            }
            return bSuccess;
        }
    }
}