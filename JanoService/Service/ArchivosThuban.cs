using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

namespace JanoService.Service
{
    /// <summary>
    /// Get data from Thuban database
    /// It's a singleton class
    /// </summary>
    public class ArchivosThuban
    {
        static ArchivosThuban instance = new ArchivosThuban();
        static private KeyValuePair<int, string>[] tipos;
        private ArchivosThuban() { }
        /// <summary>
        /// Get instance, it's singleton class
        /// </summary>
        static public ArchivosThuban Instance
        {
            get
            {
                using (var context = new Data.PakBackEndEntities())
                {
                    tipos = (from t in (
                                from td in context.AppDistribuidores_TiposDatos
                                select td
                            ).ToArray()
                            select new KeyValuePair<int, string>(t.IdTipoDato, t.Codigo)
                            ).ToArray();
                        
                }
                return instance;
            }
        }

        /// <summary>
        /// Get files from thuban to be processes
        /// </summary>
        /// <param name="guia">Parcel number</param>
        /// <returns>Files to be emmited, subscripted on process</returns>
        public IObservable<ArchivoThuban> GetArchivos(string guia)
        {
            try
            {
                using (var context = new Data.TH_OCA_DESAEntities())
                {
                    var archivos = (from a in context.THUBAN_INDEXES_DISTRIBUCION
                                   join s in context.THUBAN_STORE on a.INDEX_ITEM_ID equals s.STORE_ITEM_ID
                                   where a.N_GUIA.Equals(guia)
                                   select new ArchivoThuban
                                   {
                                       Guia = guia,
                                       TipoThuban = a.TIPO_FOTO,
                                       Ruta = s.STORE_FILE_PATH,
                                       Extencion = s.STORE_FILE_TYPE
                                   }).ToList();
                    archivos.ForEach(a =>
                    {
                        a.Tipo = (TipoDato)tipos.Where(t => t.Value.Equals(a.TipoThuban)).SingleOrDefault().Key;
                        var aux = a.Ruta;
                        a.Ruta = new string(a.Ruta.Reverse().SkipWhile(r=>r!='\\').Reverse().ToArray());
                        a.Archivo = aux.Substring(a.Ruta.Length);
                    });
                    return archivos.ToObservable();
                }
            }
            catch (Exception ex)
            {
                return Observable.Create((IObserver<ArchivoThuban> observer) =>
                {
                    observer.OnError(ex);
                    return () => { };
                });
            }
        }
    }
}
