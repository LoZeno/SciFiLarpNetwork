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
    
    public partial class AddressBook
    {
        public long CharacterID { get; set; }
        public long SavedCharacterID { get; set; }
        public string SavedCharacterName { get; set; }
    
        public virtual HeroCharacter HeroCharacter { get; set; }
    }
}
