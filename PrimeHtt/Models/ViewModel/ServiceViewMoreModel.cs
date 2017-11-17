using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PrimeHtt.Models.ViewModel
{
    public class ServiceViewMoreModel
    {
        public long ServiceViewMoreId { get; set; }
        public HttpPostedFileBase ServiceViewMoreImage { get; set; }
        public string SVMImage { get; set; }
        public string ServiceViewMoreImageName { get; set; }
    }
}