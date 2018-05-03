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
                            //.Where(ArchivoThuban => tipos.Contains(ArchivoThuban.Tipo))
                            .Subscribe(ArchivoThuban =>
                            {
                                // Copy all files from Thuban to path destination
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
                                    PendientesTramite.Instance.UpdateDato(Pendiente.IdPieza, ArchivoThuban.Tipo, destino);
                                }
                                //PendientesTramite.Instance.UpdateDato(Pendiente.IdPieza, ArchivoThuban.)
                            });

                        // Build signed PDF

                        Log.Info($"Process end: { DateTime.Now}");
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
