//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace PrimeHtt.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class ServiceInfo
    {
        public long ServiceInfoId { get; set; }
        public string ServiceName { get; set; }
        public string ServiceDescription { get; set; }
        public string ServiceImage { get; set; }
        public long ServiceId { get; set; }
    
        public virtual Service Service { get; set; }
    }
}
