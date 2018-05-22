using Common.Logging;
using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Linq;
using System.IO;
using System.Threading;

namespace JanoService.Service
{
    /// <summary>
    /// Make coordinated work by steps
    /// </summary>
    public class Process
    {
        private readonly ILog Log;
        /// <summary>
        /// Just assign log using ninject
        /// </summary>
        /// <param name="log">Is Log4net passed by ninject</param>
        public Process(ILog log)
        {
            Log = log;
        }
        /// <summary>
        /// All job is inside this method, maybe would be separated for better in files. Just uses region, but its work.
        /// </summary>
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
                                #region Copy files from Thuban to path destination
                                Log.Info($"Process Copy files from Thuban to path destination: {DateTime.Now} - {Pendiente.NroEnvio} - {pathFind}");
                                if (new[] { TipoDato.FOTO_DNI_FRENTE, TipoDato.FOTO_DNI_DORSO, TipoDato.FIRMA }.Contains(ArchivoThuban.Tipo)
                                        && Pendiente.Datos.Where(pd => pd.TipoDato == ArchivoThuban.Tipo).SingleOrDefault()?.Valor.Length == 0)
                                {
                                    var origen = ArchivoThuban.Ruta.Replace(pathFind, pathReplace);
                                    var carpeta = ArchivoThuban.Tipo == TipoDato.FIRMA
                                        ? "FIRMA"
                                        : "FOTOS";
                                    var archivo = $"{ArchivoThuban.Archivo}";
                                    var destino = $"{pathDestination}{ArchivoThuban.Guia}\\{carpeta}\\";
                                    Directory.CreateDirectory(destino);
                                    destino += $"{archivo}.{ArchivoThuban.Extencion}";
                                    File.Copy($@"{origen}{archivo}", destino, true);

                                    // Update AppDistribuidores_DatosAdicionalesTramitaciones table
                                    var dato = Pendiente.Datos.Find(p => p.TipoDato == ArchivoThuban.Tipo);
                                    dato.Valor = destino;
                                    PendientesTramite.Instance.UpdateDato(Pendiente.IdPieza, dato);
                                }
                                #endregion
                            }, () =>
                            {
                                try {
                                    #region Build signed PDF
                                    Log.Info($"Process Build signed PDF: {DateTime.Now} - {Pendiente.NroEnvio}");
                                    var pdfPath = Pendiente.Datos.Find(p => p.TipoDato == TipoDato.PDF_ORIGINAL)?.Valor;
                                    var firmaPath = Pendiente.Datos.Find(p => p.TipoDato == TipoDato.FIRMA)?.Valor;
                                    var firmaCoord = Pendiente.Datos.Find(p => p.TipoDato == TipoDato.PDF_PAGINA_XY)?.Valor;
                                    var firmaCoords = firmaCoord?.Split(']');
                                    if (firmaCoords?.Length > 1)
                                    {
                                        Array.Resize(ref firmaCoords, firmaCoords.Length - 1);
                                    }
                                    var pdfPathDest = Pendiente.Datos.Find(p => p.TipoDato == TipoDato.PDF_FIRMADO);
                                    if ((pdfPathDest?.Valor ?? "").Length == 0)
                                    {
                                        if ((pdfPath ?? "").Length > 0 && (firmaPath ?? "").Length > 0 && pdfPathDest != null && firmaCoords.Length > 0)
                                        {
                                            pdfPathDest.Valor = $@"{pdfPath}\PDF_FINAL";
                                            Directory.CreateDirectory(pdfPathDest.Valor);
                                            var idx = 1;
                                            foreach (var firma in firmaCoords)
                                            {
                                                new ProcesarPDF
                                                {
                                                    pdfPath = $@"{pdfPath}\{Pendiente.NroEnvio}-{idx}.pdf",
                                                    pdfDestPath = $@"{pdfPathDest.Valor}\{Pendiente.NroEnvio}-{idx++}.pdf",
                                                    signed = firmaPath,
                                                    signedCoords = firma.Substring(1)
                                                }
                                                .procesar();
                                            }
                                            if (!PendientesTramite.Instance.UpdateDato(Pendiente.IdPieza, pdfPathDest))
                                            {
                                                Log.Warn($"Process update PDF_FINAL ERROR: {DateTime.Now} - {Pendiente.NroEnvio}");
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            Log.Warn($"Process sin datos para firma: {DateTime.Now} - {Pendiente.NroEnvio}");
                                            return;
                                        }
                                    }
                                    var tramite = PendientesTramite.Instance.getTramite(Pendiente);
                                    var tipoTramite = Pendiente.Datos.Where(d => d.TipoDato == TipoDato.TIPO_TRAMITEFORMULARY_TYPE).FirstOrDefault();
                                    if (tramite.Length != 2 || tramite[0] == 0 || tramite[1] == 0 || tipoTramite == null)
                                    {
                                        Log.Warn($"Process Upload sin dato de tramite: {DateTime.Now} - {Pendiente.NroEnvio}");
                                        return;
                                    }
                                    #endregion
                                    #region Upload dni photos and signed pdf
                                    Log.Info($"Process Upload dni photos and signed pdf: {DateTime.Now} - {Pendiente.NroEnvio}");
                                    var upload = new ApiJanoService(tramite[0]*10000+tramite[1], tipoTramite.Valor);
                                    var cantPendienteEnviar = 3;
                                    foreach (var tipo in new TipoDato[] { TipoDato.PDF_FIRMADO, TipoDato.FOTO_DNI_FRENTE, TipoDato.FOTO_DNI_DORSO })
                                    {
                                        var dato = Pendiente.Datos.Find(p => p.TipoDato == tipo);
                                        if ((dato?.Valor ?? "").Length > 0)
                                        {
                                            if (dato.IdEstadoTramitacion == 2)
                                            {
                                                dato.FechaEnvio = DateTime.Now;
                                                dato.CantidadReintentos++;
                                                if(!PendientesTramite.Instance.UpdateDato(Pendiente.IdPieza, dato))
                                                {
                                                    Log.Warn($"Process Upload {tipo} failed when was trying to update: {DateTime.Now} - {Pendiente.NroEnvio}");
                                                    return;
                                                }
                                                var isUpload = true;
                                                if (tipo == TipoDato.PDF_FIRMADO)
                                                {
                                                    for(var cant = 1; cant <= firmaCoords.Length && isUpload; cant++)
                                                    {
                                                        Log.Info($"Process Upload {tipo}: {DateTime.Now} - {Pendiente.NroEnvio} - {cant}");
                                                        isUpload = upload.UploadFile($"{pdfPathDest.Valor}\\{Pendiente.NroEnvio}-{cant}.pdf", TypeUpload.PDF, tipo);
                                                        Thread.Sleep(10000);
                                                    }
                                                }
                                                else
                                                {
                                                    Log.Info($"Process Upload {tipo}: {DateTime.Now} - {Pendiente.NroEnvio}");
                                                    isUpload = upload.UploadFile(dato.Valor, TypeUpload.Image, tipo);
                                                    Thread.Sleep(10000);
                                                }
                                                dato.FechaFin = DateTime.Now;

                                                if (isUpload)
                                                {
                                                    dato.IdEstadoTramitacion = 1;
                                                    cantPendienteEnviar--;
                                                }
                                                else
                                                {
                                                    if (dato.CantidadReintentos > Properties.Settings.Default.MaxRetries)
                                                    {
                                                        dato.IdEstadoTramitacion = 3;
                                                    }
                                                }
                                                if (!PendientesTramite.Instance.UpdateDato(Pendiente.IdPieza, dato))
                                                {
                                                    throw new Exception($"Process Upload {tipo} ERROR: {DateTime.Now} - {Pendiente.NroEnvio}");
                                                }
                                            }else
                                            {
                                                cantPendienteEnviar--;
                                            }
                                        }
                                        else
                                        {
                                            Log.Warn($"Process Upload {tipo} failed: {DateTime.Now} - {Pendiente.NroEnvio}");
                                        }
                                    }
                                    #endregion
                                    //return;
                                    #region Upload end
                                    if (cantPendienteEnviar==0)
                                    {
                                        Log.Info($"Process Upload end: {DateTime.Now} - {Pendiente.NroEnvio}");
                                        var email = Pendiente.Datos.Find(p => p.TipoDato == TipoDato.MAIL)?.Valor ?? "";
                                        if (!upload.UploadFileEnd(email, tramite[0], tramite[1]))
                                        {
                                            throw new Exception($"Process Upload end ERROR: {DateTime.Now} - {Pendiente.NroEnvio}");
                                        }
                                        #region Remove from pending
                                        foreach (var d in Pendiente.Datos)
                                        {
                                            if (d.IdEstadoTramitacion == 2)
                                            {
                                                d.IdEstadoTramitacion = 1;
                                                PendientesTramite.Instance.UpdateDato(Pendiente.IdPieza, d);
                                            }
                                        }
                                        #endregion
                                    }
                                    #endregion
                                    Log.Info($"Process end: {DateTime.Now} - {Pendiente.NroEnvio}");
                                    Console.WriteLine($"pendiente:{Pendiente.NroEnvio}");
                                }
                                catch (Exception ex)
                                {
                                    Log.Error($"Error: {ex.Message} \r\n\tSource:{ex.Source} \r\n\tStackTrace:{ex.StackTrace}");
                                }
                            });
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
            Log.Info($"Process end: {DateTime.Now}");
        }
    }
}
