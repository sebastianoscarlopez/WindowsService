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
    
    public partial class AppDistribuidores_TiposDatos
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public AppDistribuidores_TiposDatos()
        {
            this.AppDistribuidores_DatosAdicionalesTramitaciones = new HashSet<AppDistribuidores_DatosAdicionalesTramitaciones>();
        }
    
        public int IdTipoDato { get; set; }
        public string Descripcion { get; set; }
        public string Codigo { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<AppDistribuidores_DatosAdicionalesTramitaciones> AppDistribuidores_DatosAdicionalesTramitaciones { get; set; }
    }
}