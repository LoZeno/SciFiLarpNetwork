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
    
    public partial class MailOutBox
    {
        public long MessageID { get; set; }
        public long CharacterID { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsRead { get; set; }
        public Nullable<System.DateTime> DateOfDeletion { get; set; }
    
        public virtual HeroCharacter HeroCharacter { get; set; }
        public virtual Message Message { get; set; }
    }
}