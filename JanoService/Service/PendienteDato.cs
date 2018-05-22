using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JanoService.Service
{
    /// <summary>
    /// AppDistribuidores_DatosAdicionalesTramitaciones DTO
    /// </summary>
    public class PendienteDato
    {
        public TipoDato TipoDato { get; set; }
        public string Valor { get; set; }
        public int CantidadReintentos { get; set; }
        public int IdEstadoTramitacion { get; set; }
        public DateTime? FechaEnvio { get; set; }
        public DateTime? FechaFin { get; set; }
        public string Error { get; set; }
    }
}
