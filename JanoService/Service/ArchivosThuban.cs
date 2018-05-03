using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

namespace JanoService.Service
{
    /// Singleton class
    public class ArchivosThuban
    {
        static ArchivosThuban instance = new ArchivosThuban();
        static private KeyValuePair<int, string>[] tipos;
        private ArchivosThuban() { }
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
                                       Ruta = s.STORE_FILE_PATH.Substring(0, s.STORE_FILE_PATH.Length - s.STORE_ITEM_ID.Length),
                                       Archivo = s.STORE_ITEM_ID,
                                       Extencion = s.STORE_FILE_TYPE
                                   }).ToList();
                    archivos.ForEach(a => a.Tipo = (TipoDato)tipos.Where(t => t.Value.Equals(a.TipoThuban)).SingleOrDefault().Key);
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
