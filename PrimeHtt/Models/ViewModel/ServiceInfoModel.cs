using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PrimeHtt.Models.ViewModel
{
    public class ServiceInfoModel
    {
        public long ServiceInfoId { get; set; }
        public string ServiceName { get; set; }
        public string ServiceDescription { get; set; }
        public HttpPostedFileBase ServiceImage { get; set; }
        public ServiceModel SIImage { get; set; }
        public string SImage { get; set; }
        public string[] ServiceItems { get; set; }
        public string[] OldServiceItems { get; set; }
        public string[] OldServiceItemId { get; set; }
        public string ServiceImageName { get; set; }
        public long ServiceId { get; set; }
    }
}