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
    
    public partial class Event
    {
        public Event()
        {
            this.EventDatapads = new HashSet<EventDatapad>();
            this.EventDays = new HashSet<EventDay>();
            this.EventElements = new HashSet<EventElement>();
        }
    
        public long EventID { get; set; }
        public string Title { get; set; }
        public System.DateTime StartDate { get; set; }
        public long TotalExperiencePoints { get; set; }
        public string Description { get; set; }
        public float FullEventPrice { get; set; }
        public Nullable<float> Collection { get; set; }
        public bool IsClosed { get; set; }
    
        public virtual ICollection<EventDatapad> EventDatapads { get; set; }
        public virtual ICollection<EventDay> EventDays { get; set; }
        public virtual ICollection<EventElement> EventElements { get; set; }
    }
}