using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PrimeHtt.Models.ViewModel
{
    public class ServiceModel
    {
        public long ServiceId { get; set; }
        public string ServiceType { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
        public string DateAdded { get; set; }
        public long ServiceInfoId { get; set; }
        public string ServiceName { get; set; }
        public string ServiceDescription { get; set; }
        public HttpPostedFileBase ServiceImage { get; set; }
        public ServiceInfoModel SIImage { get; set; }
        public List<ServiceInfoModel> ServiceInfos { get; set; }
        public string SImage { get; set; }
        public string[] ServiceItems { get; set; }
        public string[] OldServiceItems { get; set; }
        public string[] OldServiceItemId { get; set; }
        public string ServiceImageName { get; set; }
        public PrimeConfigurationModel ServiceTitle { get; set; }
        public PrimeConfigurationModel ServiceSubTitle { get; set; }
        public long ServiceViewMoreId { get; set; }
        public HttpPostedFileBase ServiceViewMoreImage { get; set; }
        public ServiceInfoModel SVMImage { get; set; }
        public string ServiceViewMoreImageName { get; set; }
        public ServiceViewMoreModel ServiceViewMore { get; set; }
    }
}