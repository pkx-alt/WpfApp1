using System;
using System.Collections.Generic;

namespace WpfApp1.Models
{
    public class InfoCorreo
    {
        public string Remitente { get; set; }    // Quién lo envió (ej: facturacion@adosa.com)
        public string Asunto { get; set; }       // Ej: "Factura Compra #12345"
        public DateTime Fecha { get; set; }      // Cuándo llegó
        public List<string> ArchivosAdjuntos { get; set; } = new List<string>(); // Lista de rutas de archivos descargados

        // Propiedad extra para mostrar en un log o lista
        public string Resumen => $"{Fecha:dd/MM HH:mm} - De: {Remitente} - {Asunto} ({ArchivosAdjuntos.Count} facturas)";
    }
}