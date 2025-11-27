using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MimeKit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OrySiPOS.Models;
// Necesario para acceder a los ajustes guardados
using OrySiPOS.Properties;

namespace OrySiPOS.Services
{
    public class EmailImportService
    {
        // Configuración fija de Gmail (esto rara vez cambia)
        private string _servidor = "imap.gmail.com";
        private int _puerto = 993;

        public List<InfoCorreo> DescargarCorreosConDetalles()
        {
            var listaCorreos = new List<InfoCorreo>();

            // 1. LEEMOS LAS CREDENCIALES DE LA CONFIGURACIÓN
            string usuario = Settings.Default.EmailInventario;
            string password = Settings.Default.PassEmailInventario;
            string keyword = Settings.Default.EmailKeyword; // <--- NUEVO


            // Validación básica antes de intentar conectar
            if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(password))
            {
                throw new Exception("No has configurado el correo o la contraseña en Ajustes.");
            }

            if (string.IsNullOrWhiteSpace(keyword)) keyword = "Factura";

            try
            {
                using (var client = new ImapClient())
                {
                    // Conexión segura
                    client.Connect(_servidor, _puerto, true);

                    // 2. USAMOS LAS VARIABLES QUE LEÍMOS DE SETTINGS
                    client.Authenticate(usuario, password);

                    var inbox = client.Inbox;
                    inbox.Open(FolderAccess.ReadWrite);

                    // Buscamos correos NO LEÍDOS que contengan "Factura" en el asunto
                    // (Puedes ajustar este filtro según tus necesidades)
                    var query = SearchQuery.NotSeen.And(SearchQuery.SubjectContains(keyword));
                    var uids = inbox.Search(query);

                    foreach (var uid in uids)
                    {
                        var message = inbox.GetMessage(uid);

                        // Crear objeto de información
                        var info = new InfoCorreo
                        {
                            Remitente = message.From.ToString(),
                            Asunto = message.Subject,
                            Fecha = message.Date.DateTime
                        };

                        // Buscar adjuntos XML
                        foreach (var attachment in message.Attachments)
                        {
                            if (attachment is MimePart part && part.FileName.ToLower().EndsWith(".xml"))
                            {
                                // Guardar temporalmente el XML para procesarlo
                                string tempPath = Path.Combine(Path.GetTempPath(), part.FileName);
                                using (var stream = File.Create(tempPath))
                                {
                                    part.Content.DecodeTo(stream);
                                }

                                info.ArchivosAdjuntos.Add(tempPath);
                            }
                        }

                        // Solo procesamos el correo si traía facturas XML válidas
                        if (info.ArchivosAdjuntos.Count > 0)
                        {
                            listaCorreos.Add(info);

                            // Marcamos como LEÍDO para no procesarlo dos veces
                            inbox.AddFlags(uid, MessageFlags.Seen, true);
                        }
                    }

                    client.Disconnect(true);
                }
            }
            catch (Exception ex)
            {
                // Lanzamos el error hacia arriba para que la Pantalla lo muestre en un MessageBox
                throw new Exception("Error al conectar con Gmail: " + ex.Message);
            }

            return listaCorreos;
        }
    }
}