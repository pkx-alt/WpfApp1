using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text; // Necesario para StringBuilder
using ESCPOS_NET;
using ESCPOS_NET.Emitters;
using ESCPOS_NET.Utilities;

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
        private const string NOMBRE_IMPRESORA = "XP-58"; // ¡Verifica que este sea el nombre en Windows!

        public static void ImprimirTicket(List<ItemTicket> productos, decimal subtotal, decimal iva, decimal descuento, decimal total, decimal pago, decimal cambio, string cliente, string folio)
        {
            try
            {
                var emitter = new EPSON();
                var commands = new List<byte[]>();

                // Inicializamos
                commands.Add(emitter.Initialize());

                // --- ENCABEZADO ---
                commands.Add(emitter.CenterAlign());
                commands.Add(emitter.SetStyles(PrintStyle.Bold | PrintStyle.DoubleHeight | PrintStyle.DoubleWidth));
                commands.Add(emitter.PrintLine("Papeleria OrySi"));
                commands.Add(emitter.SetStyles(PrintStyle.None));

                // Usamos SinTildes() en textos fijos que podrían tener acentos
                commands.Add(emitter.PrintLine(SinTildes("Av. Principal #123, Centro")));
                commands.Add(emitter.PrintLine("Tel: 55 1234 5678"));
                commands.Add(emitter.PrintLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm")));
                commands.Add(emitter.PrintLine(new string('-', 32)));

                // --- DATOS CLIENTE (Limpiamos el nombre del cliente) ---
                commands.Add(emitter.LeftAlign());
                commands.Add(emitter.PrintLine($"Folio: {folio}"));
                // ¡AQUÍ ESTÁ EL TRUCO! Limpiamos la variable cliente
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
                    // 1. Nombre del producto LIMPIO de tildes
                    string nombreLimpio = SinTildes(item.Nombre);
                    commands.Add(emitter.PrintLine(nombreLimpio));

                    // 2. Números
                    string parteIzq = $"{item.Cantidad} x {item.Precio:C}";
                    string parteDer = $"{item.Total:C}";

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
                commands.Add(emitter.PrintLine("Gracias por su compra!")); // Sin signos de apertura ¡ ¿ que a veces fallan
                commands.Add(emitter.PrintLine("Conserve este ticket"));
                commands.Add(emitter.FeedLines(4));
                commands.Add(emitter.FullCut());

                // Enviar a impresora
                byte[] bytes = ByteSplicer.Combine(commands.ToArray());
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

        // --- NUEVA FUNCIÓN MAGICA: ELIMINA ACENTOS ---
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

    // --- TU CLASE RAW PRINTER HELPER (NO CAMBIA) ---
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