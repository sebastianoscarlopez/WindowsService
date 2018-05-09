using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

namespace JanoService.Service
{
    /// <summary>
    /// Parcel with process pending
    /// </summary>
    public class PendientesTramite
    {
        static readonly PendientesTramite instance = new PendientesTramite();
        private PendientesTramite() { }
        static public PendientesTramite Instance
        {
            get
            {
                return instance;
            }
        }
        public IObservable<Pendiente> GetPendientes()
        {
            try
            {
                var maxRetries = Properties.Settings.Default.MaxRetries;
                using (var context = new Data.PakBackEndEntities())
                {
                    var tramitaciones = (from t in context.AppDistribuidores_Tramitaciones
                                         join dt in context.AppDistribuidores_DatosAdicionalesTramitaciones on t.IdTramitacion equals dt.IdTramitacion
                                         where t.IdSistema == 1 && dt.CantidadReintentos <= maxRetries && t.NroEnvio > 0
                                         group dt by t into gt
                                         select new Pendiente
                                         {
                                             IdTramitacion = gt.Key.IdTramitacion,
                                             IdPieza = gt.Key.IdPieza,
                                             NroEnvio = gt.Key.NroEnvio ?? 0,
                                             Datos = (from d in gt
                                                      where d.IdTipoDato != (int)TipoDato.MAIL
                                                      select new PendienteDato
                                                      {
                                                          TipoDato = (TipoDato)d.IdTipoDato,
                                                          CantidadReintentos = d.CantidadReintentos,
                                                          IdEstadoTramitacion = d.IdEstadoTramitacion,
                                                          Valor = d.Valor,
                                                          FechaEnvio = d.FechaEnvio,
                                                          FechaFin = d.FechaFin,
                                                          Error = d.Error
                                                      }).ToList()
                                         }).ToList();
                    foreach(var t in tramitaciones)
                    {
                        var dato = t.Datos.Where(d => d.TipoDato == TipoDato.TIPO_TRAMITEFORMULARY_TYPE).FirstOrDefault();
                        int id;
                        if (dato!= null && int.TryParse(dato.Valor, out id))
                        {
                            dato.Valor = (from f in context.AppDistribuidores_TiposFormulariosJANO
                                          where f.IdTipoFormularioJANO == id
                                          select f.CodigoJANO
                                          ).Single();
                        }
                    }

                    return tramitaciones.ToObservable();
                }
            }
            catch (Exception ex)
            {
                return Observable.Create((IObserver<Pendiente> observer) =>
                {
                    observer.OnError(ex);
                    return () => { };
                });
            }
        }
        public long getTramite(Pendiente pendiente)
        {
            using (var context = new Data.PakBackEndEntities())
            {
                var tramite = (from p in context.Piezas
                               join o in context.OrdenRetiro on p.IdOrdenRetiro equals o.IdOrdenRetiro
                               where p.IdPieza == pendiente.IdPieza
                               select new { p.NroProducto, o.FechaCarga }
                              ).Single();
                return long.Parse($"{tramite.NroProducto}{tramite.FechaCarga?.ToString("yyyy")}");
            }
        }
        public bool UpdateDato(int IdPieza, PendienteDato pendienteDato)
        {
            using (var context = new Data.PakBackEndEntities())
            {
                var dato = (from t in context.AppDistribuidores_Tramitaciones
                           join dt in context.AppDistribuidores_DatosAdicionalesTramitaciones on t.IdTramitacion equals dt.IdTramitacion
                           where t.IdSistema == 1 && t.IdPieza == IdPieza && dt.IdTipoDato == (int)pendienteDato.TipoDato
                            select dt
                           ).Single();
                dato.Valor = pendienteDato.Valor;
                dato.IdEstadoTramitacion = pendienteDato.IdEstadoTramitacion;
                dato.CantidadReintentos = pendienteDato.CantidadReintentos;
                dato.Error = pendienteDato.Error;
                dato.FechaEnvio = pendienteDato.FechaEnvio;
                dato.FechaFin = pendienteDato.FechaFin;

                return context.SaveChanges() > 0;
            }
        }
    }
}
