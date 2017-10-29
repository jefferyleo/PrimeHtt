using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PrimeHtt.Models.ViewModel
{
    public class SocialNetworkModel
    {
        public long SocialNetworkId { get; set; }
        public string SocialNetworkName { get; set; }
        public string SocialNetworkType { get; set; }
        public string SocialNetworkLink { get; set; }
        public bool IsActive { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Boolean IsDeleted { get; set; }
        public string DateAdded { get; set; }
        public string Status { get; set; }
    }
}