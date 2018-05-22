using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

namespace JanoService.Service
{
    /// <summary>
    /// Parcel with process pending
    /// It's a singleton class
    /// </summary>
    public class PendientesTramite
    {
        static readonly PendientesTramite instance = new PendientesTramite();
        private PendientesTramite() { }
        /// <summary>
        /// Get instance, it's singleton class
        /// </summary>
        static public PendientesTramite Instance
        {
            get
            {
                return instance;
            }
        }
        /// <summary>
        /// Get Pendientes to process
        /// </summary>
        /// <returns>Items to be emmited during process</returns>
        public IObservable<Pendiente> GetPendientes()
        {
            try
            {
                var maxRetries = Properties.Settings.Default.MaxRetries;
                using (var context = new Data.PakBackEndEntities())
                {
                    var tramitaciones = (from t in context.AppDistribuidores_Tramitaciones
                                         join dt in context.AppDistribuidores_DatosAdicionalesTramitaciones on t.IdTramitacion equals dt.IdTramitacion
                                         where t.IdSistema == 1 && dt.CantidadReintentos <= maxRetries
                                         group dt by t into gt
                                         select new Pendiente
                                         {
                                             IdTramitacion = gt.Key.IdTramitacion,
                                             IdPieza = gt.Key.IdPieza,
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
                        if (dato!= null && dato.IdEstadoTramitacion==2 && int.TryParse(dato.Valor, out id))
                        {
                            dato.Valor = (from f in context.AppDistribuidores_TiposFormulariosJANO
                                          where f.IdTipoFormularioJANO == id
                                          select f.CodigoJANO
                                          ).Single();
                        }
                        if(dato?.Valor == null || dato.IdEstadoTramitacion != 2)
                        {
                            t.IdPieza = 0;
                        }
                        if(t.IdPieza > 0)
                        {
                            t.NroEnvio = long.Parse((from p in context.Piezas
                                          where p.IdPieza == t.IdPieza
                                          select p.NumeroEnvio).Single());
                        }
                    }

                    return tramitaciones
                        .Where(t=>t.IdPieza>0)
                        .ToList()
                        .ToObservable();
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
        /// <summary>
        /// Tramite data is lazy obtained
        /// </summary>
        /// <param name="pendiente"></param>
        /// <returns>Producto and FechaCarga</returns>
        public long[] getTramite(Pendiente pendiente)
        {
            using (var context = new Data.PakBackEndEntities())
            {
                var tramite = (from p in context.Piezas
                               join o in context.OrdenRetiro on p.IdOrdenRetiro equals o.IdOrdenRetiro
                               where p.IdPieza == pendiente.IdPieza
                               select new { p.NroProducto, o.FechaCarga }
                              ).Single();
                return new[] { long.Parse(tramite.NroProducto), long.Parse(tramite.FechaCarga?.ToString("yyyy")) };
            }
        }
        /// <summary>
        /// Update data on table AppDistribuidores_DatosAdicionalesTramitaciones
        /// </summary>
        /// <param name="IdPieza">IdPieza on table AppDistribuidores_Tramitaciones</param>
        /// <param name="pendienteDato">Data for update</param>
        /// <returns>True when update</returns>
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
