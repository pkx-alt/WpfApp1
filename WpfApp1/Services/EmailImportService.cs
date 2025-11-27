using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MimeKit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OrySiPOS.Models;

namespace OrySiPOS.Services
{
    public class EmailImportService
    {
        // --- CONFIGURACIÓN DE GMAIL ---
        private string _servidor = "imap.gmail.com";
        private int _puerto = 993;

        // ⚠️ AQUÍ PONES TUS DATOS REALES ⚠️
        private string _correo = "samuel.moralesont@gmail.com";
        private string _password = "kqaf hovv xtvv zecd";

        // Cambia el tipo de retorno a List<InfoCorreo>
        public List<InfoCorreo> DescargarCorreosConDetalles()
        {
            var listaCorreos = new List<InfoCorreo>();

            try
            {
                using (var client = new ImapClient())
                {
                    client.Connect(_servidor, _puerto, true);
                    client.Authenticate(_correo, _password);

                    var inbox = client.Inbox;
                    inbox.Open(FolderAccess.ReadWrite);

                    // Buscamos correos no leídos con "Factura"
                    var query = SearchQuery.NotSeen.And(SearchQuery.SubjectContains("Factura"));
                    var uids = inbox.Search(query);

                    foreach (var uid in uids)
                    {
                        var message = inbox.GetMessage(uid);

                        // --- AQUÍ EXTRAEMOS LOS DETALLES ---
                        var info = new InfoCorreo
                        {
                            // MailKit nos da una lista de remitentes, tomamos el primero
                            Remitente = message.From.ToString(),
                            Asunto = message.Subject,
                            Fecha = message.Date.DateTime // Convertimos a DateTime local
                        };

                        // Buscar adjuntos
                        foreach (var attachment in message.Attachments)
                        {
                            if (attachment is MimePart part && part.FileName.ToLower().EndsWith(".xml"))
                            {
                                string tempPath = Path.Combine(Path.GetTempPath(), part.FileName);
                                using (var stream = File.Create(tempPath))
                                {
                                    part.Content.DecodeTo(stream);
                                }

                                // Agregamos la ruta a la lista del objeto info
                                info.ArchivosAdjuntos.Add(tempPath);
                            }
                        }

                        // Solo agregamos el correo a la lista final si traía facturas XML
                        if (info.ArchivosAdjuntos.Count > 0)
                        {
                            listaCorreos.Add(info);

                            // Marcamos como leído SOLO si procesamos algo útil
                            inbox.AddFlags(uid, MessageFlags.Seen, true);
                        }
                    }

                    client.Disconnect(true);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error en correo: " + ex.Message);
            }

            return listaCorreos;
        }
    }
}