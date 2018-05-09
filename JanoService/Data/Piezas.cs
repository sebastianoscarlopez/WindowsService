//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace JanoService.Data
{
    using System;
    using System.Collections.Generic;
    
    public partial class Piezas
    {
        public int IdPieza { get; set; }
        public string NroProducto { get; set; }
        public int IdProducto { get; set; }
        public int IdSucursalOCA { get; set; }
        public Nullable<int> IdDestinatario { get; set; }
        public int IdTipoPieza { get; set; }
        public Nullable<int> IdOrdenRetiro { get; set; }
        public Nullable<int> IdTipoEntrega { get; set; }
        public string NumeroEnvio { get; set; }
        public Nullable<int> IdUltimoLogAccion { get; set; }
        public int CantidadPaquetes { get; set; }
        public decimal VolumenTotal { get; set; }
        public decimal PesoTotal { get; set; }
        public Nullable<int> DiasEntrega { get; set; }
        public Nullable<decimal> PrecioEnvio { get; set; }
        public Nullable<int> IdTipoServicio { get; set; }
        public Nullable<int> IdAccion { get; set; }
        public int IdEstado { get; set; }
        public Nullable<int> IdMotivo { get; set; }
        public Nullable<int> IdAccionOperativa { get; set; }
        public System.DateTime FechaEstado { get; set; }
        public Nullable<int> IdUsuario { get; set; }
        public int IdCliente { get; set; }
        public string NroRemito { get; set; }
        public Nullable<decimal> ImporteRemito { get; set; }
        public string NroCheque { get; set; }
        public string Banco { get; set; }
        public Nullable<decimal> PesoAforado { get; set; }
        public Nullable<decimal> Seguro { get; set; }
        public Nullable<int> Facturado { get; set; }
        public Nullable<int> IdRemitente { get; set; }
        public Nullable<System.DateTime> FechaImposicion { get; set; }
        public Nullable<int> IdSucImposicion { get; set; }
        public Nullable<int> IdSucTramitacion { get; set; }
        public Nullable<int> IdSucRendicion { get; set; }
        public Nullable<int> IdRendicionOperativa { get; set; }
        public Nullable<int> idRendicionAdminis { get; set; }
        public Nullable<int> IdPiezaPadre { get; set; }
        public bool Activa { get; set; }
        public Nullable<int> Imp_QRemito { get; set; }
        public string NombreReceptor { get; set; }
        public Nullable<int> IdEstadoRetail { get; set; }
        public Nullable<int> IdPPadreRetail { get; set; }
        public Nullable<int> IdOperativa { get; set; }
    }
}