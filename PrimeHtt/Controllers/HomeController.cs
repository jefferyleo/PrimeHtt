using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.DynamicData;
using System.Web.Mvc;
using PrimeHtt.Models;
using PrimeHtt.Models.ViewModel;

namespace PrimeHtt.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            using (var db = new PrimeTravelEntities())
            {
                var banner = (from data in db.Banner
                    where data.IsDeleted == false && data.IsActive == true
                    select new BannerModel()
                    {
                        BannerDescription = data.BannerDescription,
                        BImage = data.BannerImage,
                    }).ToList();
                var serviceTitle = (from s in db.PrimeConfiguration
                    where s.ConfigurationName == "ServiceTitle"
                    select new PrimeConfigurationModel()
                    {
                        ConfigurationValue = s.ConfigurationValue
                    }).FirstOrDefault();
                var services = (from data in db.Service
                    where data.IsDeleted == false
                    select new ServiceModel()
                    {
                        ServiceType = data.ServiceType,
                        ServiceInfos = (from i in db.ServiceInfo
                            where i.ServiceId == data.ServiceId
                            select new ServiceInfoModel()
                            {
                                SImage = i.ServiceImage,
                                ServiceName = i.ServiceName,
                            }).ToList(),
                    }).ToList();
                var aboutUs = db.AboutUs.Select(e => new AboutUsViewModel
                {
                    AboutUsTitle = e.AboutUsTitle,
                    AboutUsTopDescription = e.AboutUsTopDescription,
                    AboutUsBottomDescription = e.AboutUsBottomDescription,
                    AboutUsImageUrl = e.AboutUsImage,
                }).FirstOrDefault();
                var experience = db.Experience.Select(e => new ExperienceViewModel()
                {
                     LocationId = e.LocationId,
                     LocationName = e.LocationName,
                     CountryName = e.CountryName,
                     Latitude = e.Latitude,
                     Longitude = e.Longitude
                }).ToList();
                var contact = db.Contact.Select(e => new ContactUsViewModel()
                {
                    ContactName = e.ContactName,
                    ContactAddress = e.ContactAddress,
                    ContactLatitude = e.ContactLatitude,
                    ContactLongitude = e.ContactLongitude,
                    ContactDetails = db.ContactDetail.Select(f => new ContactDetailViewModel()
                    {
                        ReservationTitle = f.ReservationTitle,
                        ContactNumber = f.ContactNumber,
                    }).ToList(),
                    ContactEmails = db.ContactEmail.Select(a => new ContactEmailViewModel()
                    {
                        ContactEmailAddress = a.ContactEmailAddress,
                    }).ToList(),
                }).FirstOrDefault();
                return View(new HomeModel()
                {
                    Banners = banner,
                    ServiceTitle = serviceTitle.ConfigurationValue,
                    Services = services,
                    AboutUs = aboutUs,
                    Experiences = experience,
                    ContactUs = contact,
                });
            }
        }


        public JsonResult GetGalleryFromLocation(long id)
        {
            using (var db = new PrimeTravelEntities())
            {
                if (id == 0)
                {
                    return Json(new { result = "failed", message = "Location not found." }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    var galleries = db.ExperienceDetail.Where(e => e.LocationId == id).ToList();
                    var locationName = db.Experience.Where(e => e.LocationId == id).Select(e => e.LocationName).FirstOrDefault();
                    if (galleries.Count == 0)
                    {
                        return Json(new { result = "failed", message = "This location has no gallery." }, JsonRequestBehavior.AllowGet);
                    }
                    else
                    {
                        return Json(new {result = "success", galleries = galleries, LocationName = locationName }, JsonRequestBehavior.AllowGet);
                    }
                }
            }
        }

        public ActionResult AboutUsReadMore()
        {
            using (var db = new PrimeTravelEntities())
            {
                var aboutUs = db.AboutUs.FirstOrDefault();
                return View(aboutUs);
            }
        }
    }
}