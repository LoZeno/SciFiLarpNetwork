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
    
    public partial class ElementQRCode
    {
        public long ElementID { get; set; }
        public System.Guid QRCode { get; set; }
    
        public virtual Element Element { get; set; }
    }
}
