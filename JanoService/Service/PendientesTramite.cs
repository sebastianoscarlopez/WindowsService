using System;
using System.Linq;
using System.Reactive.Linq;

namespace JanoService.Service
{
    /// <summary>
    /// Parcel with processing pending
    /// </summary>
    public class PendientesTramite
    {
        static readonly PendientesTramite instance = new PendientesTramite();
        /// Singleton class, private method prevent instansiation
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
                                         where t.IdSistema == 1 && dt.CantidadReintentos <= maxRetries
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
                                                          Valor = d.Valor
                                                      }).ToList()
                                         });
                    return tramitaciones.ToList().ToObservable();
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
        public bool UpdateDato(int IdPieza, TipoDato tipo, string valor)
        {
            using (var context = new Data.PakBackEndEntities())
            {
                var dato = (from t in context.AppDistribuidores_Tramitaciones
                           join dt in context.AppDistribuidores_DatosAdicionalesTramitaciones on t.IdTramitacion equals dt.IdTramitacion
                           where t.IdSistema == 1 && t.IdPieza == IdPieza && dt.IdTipoDato == (int)tipo
                           select dt
                           ).Single();
                dato.Valor = valor;

                return context.SaveChanges() > 0;
            }
        }
    }
}
