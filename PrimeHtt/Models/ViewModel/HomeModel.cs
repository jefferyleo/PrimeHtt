using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PrimeHtt.Models.ViewModel
{
    public class HomeModel
    {
        public List<BannerModel> Banners { get; set; }
        public string ServiceTitle { get; set; }
        public List<ServiceModel> Services { get; set; }
        public AboutUsViewModel AboutUs { get; set; }
        public ContactUsViewModel ContactUs { get; set; }
        public List<ExperienceViewModel> Experiences { get; set; }
    }
}