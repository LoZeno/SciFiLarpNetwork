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
    
    public partial class SavedMessage
    {
        public long MessageID { get; set; }
        public long CharacterID { get; set; }
        public string Subject { get; set; }
        public string Source { get; set; }
        public string Message { get; set; }
        public long HackingLevel { get; set; }
    
        public virtual HeroCharacter HeroCharacter { get; set; }
    }
}