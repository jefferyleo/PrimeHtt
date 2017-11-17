using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.DynamicData;
using System.Web.Mvc;
using PrimeHtt.Models;
using PrimeHtt.Models.ViewModel;
using RestSharp;
using RestSharp.Authenticators;

namespace PrimeHtt.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            using (var db = new PrimeTravelEntities())
            {
                var banner = (from data in db.Banner
                    where !data.IsDeleted && data.IsActive
                    select new BannerModel()
                    {
                        BannerDescription = data.BannerDescription,
                        BImage = data.BannerImage,
                    }).ToList();
                var primeConfiguration = db.PrimeConfiguration.ToList();
                var services = (from data in db.Service
                    where data.IsDeleted == false
                    select new ServiceModel()
                    {
                        ServiceId = data.ServiceId,
                        ServiceType = data.ServiceType,
                        ServiceInfos = (from i in db.ServiceInfo
                            where i.ServiceId == data.ServiceId && !i.IsDeleted
                            select new ServiceInfoModel()
                            {
                                SImage = i.ServiceImage,
                                ServiceName = i.ServiceName,
                            }).ToList(),
                        ServiceViewMore = (from i in db.ServiceViewMore
                            where i.ServiceId == data.ServiceId
                            select new ServiceViewMoreModel()
                            {
                                ServiceViewMoreId = i.ServiceViewMoreId,
                                SVMImage = i.ServiceImage
                            }).FirstOrDefault()
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
                    ContactTitle = e.ContactTitle,
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
                    ServiceTitle = primeConfiguration.Where(e => e.ConfigurationName == "ServiceTitle").Select(e => e.ConfigurationValue).FirstOrDefault(),
                    ServiceSubTitle = primeConfiguration.Where(e => e.ConfigurationName == "ServiceSubTitle").Select(e => e.ConfigurationValue).FirstOrDefault(),
                    ExperienceTitle = primeConfiguration.Where(e => e.ConfigurationName == "ExperienceTitle").Select(e => e.ConfigurationValue).FirstOrDefault(),
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
                        return Json(new { result = "failed", message = "This location has no gallery.", location = locationName }, JsonRequestBehavior.AllowGet);
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

        public ActionResult ServicesInfo(int Id)
        {
            using (var db = new PrimeTravelEntities())
            {
                var service = (from data in db.Service
                                where data.ServiceId == Id
                                select new ServiceModel()
                                {
                                    ServiceType = data.ServiceType,
                                    ServiceViewMore = (from i in db.ServiceViewMore
                                                       where i.ServiceId == data.ServiceId
                                                       select new ServiceViewMoreModel()
                                                       {
                                                           ServiceViewMoreId = i.ServiceViewMoreId,
                                                           SVMImage = i.ServiceImage
                                                       }).FirstOrDefault()
                                }).SingleOrDefault();
                return View(service);
            }
        }

        public JsonResult SendMail(string name, string email, string subject, string message)
        {

            if (name == "")
            {
                return Json(new { result = "failed", message = "Please enter your name." }, JsonRequestBehavior.AllowGet);
            }

            if (email == "")
            {
                return Json(new { result = "failed", message = "Please enter your email address." }, JsonRequestBehavior.AllowGet);
            }

            var isEmail = Regex.IsMatch(email, @"\A(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?)\Z", RegexOptions.IgnoreCase);

            if (!isEmail)
            {
                return Json(new { result = "failed", message = "Please correct your email address." }, JsonRequestBehavior.AllowGet);
            }

            //var smtp = new SmtpClient
            //{
            //    Host = "smtp.gmail.com",
            //    Port = 587,
            //    EnableSsl = true,
            //    DeliveryMethod = SmtpDeliveryMethod.Network,
            //    UseDefaultCredentials = false,
            //    //Credentials = new NetworkCredential("munyew@prime-htt.com.my", "prime2015#")
            //    Credentials = new NetworkCredential("jefferyleo93@gmail.com", "mendy5126690")
            //};

            //using (var msg = new MailMessage(email, "munyew@prime-htt.com.my"))
            //{
            //    msg.Subject = subject;
            //    msg.Body = message;
            //    msg.IsBodyHtml = false;
            //    smtp.Send(msg);
            //}

            SendSimpleMessage(name, email, subject, message);

            return Json(new { result = "success" }, JsonRequestBehavior.AllowGet);
        }

        //public JsonResult sendEmail()
        //{
        //    SendSimpleMessage();
        //    return Json(new{ result = "success"}, JsonRequestBehavior.AllowGet);
        //}

        public static IRestResponse SendSimpleMessage(string name, string email, string subject, string message)
        {
            Uri uri = new Uri("https://api.mailgun.net/v3");
            RestClient client = new RestClient();
            client.BaseUrl = uri;
            client.Authenticator = new HttpBasicAuthenticator(
                "api", "key-496159f2c06a94d8da054f088048e3d5");
            RestRequest request = new RestRequest();
            request.AddParameter("domain",
                "prime-htt.com.my", ParameterType.UrlSegment);
            request.Resource = "{domain}/messages";
            request.AddParameter("from", "Prime Htt <postmaster@prime-htt.com.my>");
            request.AddParameter("to", "royce@prime-htt.com.my");
            request.AddParameter("subject", subject);
            request.AddParameter("text", "Below is the content of the message.\nSubject: " + subject + "\nName: " + name + "\nEmail: " + email + "\nMessage: " + message);
            request.Method = Method.POST;
            return client.Execute(request);
        }
    }
}