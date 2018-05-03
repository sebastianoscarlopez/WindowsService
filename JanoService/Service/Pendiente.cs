using System.Collections.Generic;

namespace JanoService.Service
{
    /// <summary>
    /// Simple parcel data
    /// It's used by PendientesTramitar
    /// </summary>
    public class Pendiente
    {
        public int IdTramitacion { get; set; }
        public int IdPieza { get; set; }
        public long NroEnvio { get; set; }
        public List<PendienteDato> Datos { get; set; }
    }
}
