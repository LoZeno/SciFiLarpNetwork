//------------------------------------------------------------------------------
// <auto-generated>
//    This code was generated from a template.
//
//    Manual changes to this file may cause unexpected behavior in your application.
//    Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace DataMappings
{
    using System;
    using System.Collections.Generic;
    
    public partial class SubstanceType
    {
        public SubstanceType()
        {
            this.Substances = new HashSet<Substance>();
        }
    
        public long SubstanceTypeID { get; set; }
        public string Description { get; set; }
    
        public virtual ICollection<Substance> Substances { get; set; }
    }
}
