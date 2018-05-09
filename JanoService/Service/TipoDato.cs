using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JanoService.Service
{
    public enum TipoDato
    {
        DESCONOCIDO = 0,
        FOTO_DNI_FRENTE = 1,
        FOTO_DNI_DORSO = 2,
        PDF_ORIGINAL = 3,
        PDF_FIRMA = 4,
        MAIL = 5,
        PDF_PAGINA_XY = 6,
        TIPO_TRAMITEFORMULARY_TYPE = 8
    }
}
