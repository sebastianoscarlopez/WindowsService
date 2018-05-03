using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JanoService.Service
{
    public class TipoDatoThuban
    {
        static private TipoDatoThuban instance = null;
        private TipoDatoThuban() { }
        static public TipoDatoThuban Instance
        {
            get
            {
                return instance == null ? new TipoDatoThuban() : instance;
            }
        }
    }
}
