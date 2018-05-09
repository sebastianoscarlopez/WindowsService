using Common.Logging;
using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Linq;
using System.IO;

namespace JanoService.Service
{
    public class Process
    {
        private readonly ILog Log;
        public Process(ILog log)
        {
            Log = log;
        }
        internal void run()
        {
            Log.Info($"Process start: { DateTime.Now}");
            PendientesTramite.Instance
                .GetPendientes() // Get list of tramitation's pending
                .ObserveOn(CurrentThreadScheduler.Instance)
                .SubscribeOn(CurrentThreadScheduler.Instance)
                .Subscribe(Pendiente => // For every pending
                {
                    try // All pending would be processed, even when one fail, in that case error is logged
                    {
                        var pathFind = Properties.Settings.Default.PathFind;
                        var pathReplace = Properties.Settings.Default.PathReplace;
                        var pathDestination = Properties.Settings.Default.PathDestination;
                        // Process Thuban's files
                        ArchivosThuban.Instance
                            .GetArchivos(Pendiente.NroEnvio.ToString())
                            .ObserveOn(CurrentThreadScheduler.Instance)
                            .SubscribeOn(CurrentThreadScheduler.Instance)
                            .Subscribe(ArchivoThuban =>
                            {
                                Log.Info($"Process Copy file from Thuban to path destination: {DateTime.Now} - {Pendiente.NroEnvio} - {pathFind}");
                                var origen = ArchivoThuban.Ruta.Replace(pathFind, pathReplace);
                                var carpeta = ArchivoThuban.Tipo == TipoDato.PDF_FIRMA
                                    ? "FIRMA"
                                    : "FOTO";
                                var archivo = $"{ArchivoThuban.Archivo}.{ArchivoThuban.Extencion}";
                                var destino = $"{pathDestination}{ArchivoThuban.Guia}\\{carpeta}\\";
                                Directory.CreateDirectory(destino);
                                destino += archivo;
                                File.Copy($"{origen}{archivo}", destino, true);

                                // Update AppDistribuidores_DatosAdicionalesTramitaciones table
                                if (new[] { TipoDato.FOTO_DNI_FRENTE, TipoDato.FOTO_DNI_DORSO, TipoDato.PDF_FIRMA }.Contains(ArchivoThuban.Tipo)) {
                                    var dato = Pendiente.Datos.Find(p => p.TipoDato == ArchivoThuban.Tipo);
                                    dato.Valor = destino;
                                    PendientesTramite.Instance.UpdateDato(Pendiente.IdPieza, dato);
                                }
                            });                        
                        Log.Info($"Process Build signed PDF: {DateTime.Now} - {Pendiente.NroEnvio}");
                        var pdfPath = Pendiente.Datos.Find(p => p.TipoDato == TipoDato.PDF_ORIGINAL)?.Valor;
                        var firmaPath = Pendiente.Datos.Find(p => p.TipoDato == TipoDato.PDF_FIRMA)?.Valor;
                        var firmaCoord = Pendiente.Datos.Find(p => p.TipoDato == TipoDato.PDF_PAGINA_XY)?.Valor;
                        if ((pdfPath??"").Length > 0 && (firmaPath ?? "").Length > 0)
                        {
                            Directory.CreateDirectory($"{pdfPath}PDF_FINAL");
                            new ProcesarPDF
                            {
                                pdfPath = $"{pdfPath}{Pendiente.NroEnvio}.pdf",
                                pdfDestPath = $"{pdfPath}PDF_FINAL\\{Pendiente.NroEnvio}.pdf",
                                signed = firmaPath,
                                signedCoords = firmaCoord
                            }
                            .procesar();
                        } else
                        {
                            Log.Warn($"Process sin datos para firma: {DateTime.Now} - {Pendiente.NroEnvio}");
                            return;
                        }
                        var tramite = PendientesTramite.Instance.getTramite(Pendiente);
                        var tipoTramite = Pendiente.Datos.Where(d => d.TipoDato == TipoDato.TIPO_TRAMITEFORMULARY_TYPE).FirstOrDefault();
                        if (tramite == 0 || tipoTramite == null)
                        {
                            Log.Warn($"Process Upload sin dato de tramite: {DateTime.Now} - {Pendiente.NroEnvio}");
                            return;
                        }
                        Log.Info($"Process Upload dni photos and signed pdf: {DateTime.Now} - {Pendiente.NroEnvio}");
                        var upload = new UploadFiles(tramite, tipoTramite.Valor, Properties.Settings.Default.urlUpload, Properties.Settings.Default.urlToken);
                        foreach (var tipo in new TipoDato[] { TipoDato.FOTO_DNI_FRENTE, TipoDato.FOTO_DNI_DORSO, TipoDato.PDF_ORIGINAL })
                        {
                            var dato = Pendiente.Datos.Find(p => p.TipoDato == tipo);
                            var path = dato?.Valor + (tipo == TipoDato.PDF_ORIGINAL ? $"{Pendiente.NroEnvio}.pdf" : "");
                            if ((path ?? "").Length > 0)
                            {
                                if (dato.IdEstadoTramitacion == 2)
                                {
                                    dato.FechaEnvio = DateTime.Now;
                                    dato.CantidadReintentos++;
                                    PendientesTramite.Instance.UpdateDato(Pendiente.IdPieza, dato);
                                    var isUpload = upload.uploadFile(path, TypeUpload.Image, tipo);
                                    dato.FechaFin = DateTime.Now;
                                    if (isUpload) {
                                        dato.IdEstadoTramitacion = 1;
                                    }
                                    else
                                    {
                                        if (dato.CantidadReintentos > Properties.Settings.Default.MaxRetries)
                                        {
                                            dato.IdEstadoTramitacion = 3;
                                        }
                                    }
                                    if(!PendientesTramite.Instance.UpdateDato(Pendiente.IdPieza, dato))
                                    {
                                        throw new Exception($"Process Upload {tipo} ERROR: {DateTime.Now} - {Pendiente.NroEnvio} - {path}");
                                    }
                                }
                            }
                            else
                            {
                                Log.Warn($"Process Upload {tipo} failed: {DateTime.Now} - {Pendiente.NroEnvio} - {path}");
                            }
                        }
                        Log.Info($"Process end: {DateTime.Now} - {Pendiente.NroEnvio}");
                        Console.WriteLine($"pendiente:{Pendiente.NroEnvio}");
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Error: {ex.Message} \r\n\tSource:{ex.Source} \r\n\tStackTrace:{ex.StackTrace}");
                    }
                }, (ex) => // Error
                {
                    Log.Error($"Error: {ex.Message} \r\n\tSource:{ex.Source} \r\n\tStackTrace:{ex.StackTrace}");
                }, () => // complete
                {
                    Console.WriteLine($"ultimo pendiente");
                });
            Log.Info($"Process end: { DateTime.Now}");
        }
    }
}
