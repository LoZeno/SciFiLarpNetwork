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
    
    public partial class EventDay
    {
        public long EventID { get; set; }
        public System.DateTime IndividualDay { get; set; }
        public Nullable<long> ExperiencePoints { get; set; }
        public System.DateTime InGameTime { get; set; }
        public System.DateTime OffGameTime { get; set; }
        public Nullable<float> PricePerDay { get; set; }
    
        public virtual Event Event { get; set; }
    }
}
