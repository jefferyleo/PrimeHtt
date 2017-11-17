using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Web.Mvc;
using System.Web.Security;
using GoogleMaps.LocationServices;
using PrimeHtt.Helper.Authorization;
using PrimeHtt.Helper.Services;
using PrimeHtt.Models.ViewModel;
using PrimeHtt.Models;
using RestSharp;
using RestSharp.Authenticators;
using HttpCookie = System.Web.HttpCookie;

namespace PrimeHtt.Controllers
{
    public class AdminController : Controller
    {
        // GET: Admin
        [Authorize]
        public ActionResult Index()
        {
            return RedirectToAction("BannerList");
        }

        [AllowAnonymous]
        public ActionResult Login()
        {
            var model = new LoginViewModel();
            //var newPassword = CreatePassword("123");
            return View(model);
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model)
        {
            using (var db = new PrimeTravelEntities())
            {
                var password = db.User.Where(e => e.Username == model.Username).Select(e => e.Password).FirstOrDefault();
                if (password == null)
                {
                    ViewBag.LoginStatus = "Your Username or Password is wrong.";
                    return View(model);
                }
                var isPasswordCorrect = PasswordHash.ValidatePassword(model.Password, password);
                if (isPasswordCorrect)
                {
                    var user = db.User.FirstOrDefault(e => e.Username == model.Username);
                    FormsAuthentication.SetAuthCookie(model.Username, true);
                    ModelState.Remove("Password");

                    var authTicket = new FormsAuthenticationTicket(
                        1, // version
                        user.UserId.ToString(), // user name
                        DateTime.Now, // created
                        DateTime.Now.AddDays(7), // expires
                        true, // persistent?
                        user.Username
                    );

                    string encryptedTicket = FormsAuthentication.Encrypt(authTicket);

                    var authCookie = new HttpCookie(FormsAuthentication.FormsCookieName, encryptedTicket);
                    HttpContext.Response.Cookies.Add(authCookie);

                    return RedirectToAction("BannerList", "Admin");

                }
                else
                {
                    ViewBag.LoginStatus = "Your Username or Password is wrong.";
                    return View(model);
                }
            }
        }

        [Authorize]
        public ActionResult ChangePassword(long id)
        {
            using (var db = new PrimeTravelEntities())
            {
                var user = db.User.FirstOrDefault(e => e.UserId == id);
                if (user == null)
                {
                    return RedirectToAction("Logout");
                }
                return View(new ChangePasswordViewModel()
                {
                    UserId = user.UserId,
                    Username = user.Username,
                });
            }
        }

        [Authorize]
        [HttpPost]
        public ActionResult ChangePassword(ChangePasswordViewModel model)
        {
            var formsAuthentication = HttpContext.Request.Cookies[FormsAuthentication.FormsCookieName] != null ? FormsAuthentication.Decrypt(HttpContext.Request.Cookies[FormsAuthentication.FormsCookieName].Value) : null;
            using (var db = new PrimeTravelEntities())
            {
                if (model.NewPassword != model.ConfirmNewPassword)
                {
                    ModelState.AddModelError("ConfirmNewPassword", "Passwords do not match.");

                    return View(model);
                }

                if (!(model.NewPassword == null && model.ConfirmNewPassword == null))
                {
                    if (model.NewPassword == null)
                    {
                        ModelState.AddModelError("NewPassword", "Please enter New Password.");

                        return View(model);
                    }

                    if (model.ConfirmNewPassword == null)
                    {
                        ModelState.AddModelError("ConfirmNewPassword", "Please enter Confirm New Password.");

                        return View(model);
                    }
                }

                var user = db.User.Find(model.UserId);

                if (user == null)
                {
                    return new HttpNotFoundResult("User not found.");
                }

                if (model.ConfirmNewPassword != null)
                {
                    if (user.Username != model.Username)
                    {
                        user.NewUsername = model.Username;
                    }
                    var passwordHash = PasswordHash.CreateHash(model.ConfirmNewPassword);
                    user.UpdatedAt = MetadataServices.GetCurrentDateTime();
                    user.UpdatedBy = GetAuthorization.GetUsername(formsAuthentication.Name);
                    user.NewPassword = passwordHash;
                    user.IsPasswordChanging = true;
                    db.SaveChanges();

                    SendChangePasswordEmail(user.UserId);
                }
                

                return RedirectToAction("Logout");
            }
        }

        public static IRestResponse SendChangePasswordEmail(long id)
        {
            using (var db = new PrimeTravelEntities())
            {
                var user = db.User.Find(id);
                var encryptedUserId = CipherEncrypt.Encrypt(id.ToString(), "password");
                var currentDomain = System.Web.HttpContext.Current.Request.Url.Authority;
                var resetLink = "" + currentDomain + "/Admin/UpdateAccount?id=" + encryptedUserId + "";
                Uri uri = new Uri("https://api.mailgun.net/v3");
                RestClient client = new RestClient();
                client.BaseUrl = uri;
                client.Authenticator = new HttpBasicAuthenticator(
                    "api", "key-496159f2c06a94d8da054f088048e3d5");
                RestRequest request = new RestRequest();
                request.AddParameter("domain",
                    "prime-htt.com.my", ParameterType.UrlSegment);
                request.Resource = "{domain}/messages";
                request.AddParameter("from", "Prime Htt Website<postmaster@prime-htt.com.my>");
                request.AddParameter("to", "royce@prime-htt.com.my");
                request.AddParameter("subject", "Change Password - Prime Htt");
                //request.AddParameter("text", "Click the link to update account details.\n Email: "+ user.Username +"");
                request.AddParameter("html",
                    "<html>Click the link below to update account details.\n Email: " + user.Username + "\n <a href='"+ resetLink + "'>Click Here</a></html>");
                request.Method = Method.POST;
                return client.Execute(request);
            }
            
        }

        public ActionResult UpdateAccount(string id)
        {
            using (var db = new PrimeTravelEntities())
            {
                id = id.Replace(" ", "+");
                var decryptedId = CipherEncrypt.Decrypt(id, "password");
                var userId = Convert.ToInt64(decryptedId);
                var user = db.User.Find(userId);
                if (user != null)
                {
                    if (user.NewUsername != "")
                    {
                        user.Username = user.NewUsername;
                    }
                    if (user.NewPassword != "")
                    {
                        user.Password = user.NewPassword;
                    }
                    user.IsPasswordChanging = false;
                    user.NewUsername = "";
                    user.NewPassword = "";
                    user.UpdatedAt = MetadataServices.GetCurrentDateTime();
                    db.SaveChanges();
                }
                return View();
            }
        }

        [Authorize]
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            return RedirectToAction("Login", "Admin");
        }

        public string CreatePassword(string password)
        {
            var generatePassword = PasswordHash.CreateHash(password);
            return generatePassword;
        }

        [Authorize]
        public ActionResult AboutUs()
        {
            using (var db = new PrimeTravelEntities())
            {
                var aboutUs = db.AboutUs.FirstOrDefault();
                if (aboutUs != null)
                {
                    return View(new AboutUsViewModel()
                    {
                        AboutUsId = aboutUs.AboutUsId,
                        AboutUsTitle = System.Security.SecurityElement.Escape(aboutUs.AboutUsTitle),
                        AboutUsTopDescription = System.Security.SecurityElement.Escape(aboutUs.AboutUsTopDescription),
                        AboutUsBottomDescription = System.Security.SecurityElement.Escape(aboutUs.AboutUsBottomDescription),
                        AboutUsImageUrl = aboutUs.AboutUsImage,
                    });
                }
                else
                {
                    return View(new AboutUsViewModel()
                    {
                        AboutUsId = 0,
                        AboutUsTitle = "",
                        AboutUsTopDescription = "",
                        AboutUsBottomDescription = "",
                        AboutUsImageUrl = "",
                    });
                }
            }
        }

        [Authorize]
        [HttpPost]
        public JsonResult AboutUs(AboutUsViewModel model)
        {
            if (ModelState.IsValid)
            {
                using (var db = new PrimeTravelEntities())
                {
                    var formsAuthentication = HttpContext.Request.Cookies[FormsAuthentication.FormsCookieName] != null
                        ? FormsAuthentication.Decrypt(
                            HttpContext.Request.Cookies[FormsAuthentication.FormsCookieName].Value)
                        : null;

                    var aboutUs = db.AboutUs.FirstOrDefault(e => e.AboutUsId == model.AboutUsId);
                    string newFileName = "";
                    if (aboutUs != null)
                    {
                        if (model.AboutUsImage != null)
                        {
                            var aboutUsImage =
                                MetadataServices.UploadToCloud("aboutus", model.AboutUsImage, out newFileName);
                            if (aboutUs.AboutUsImageName != "")
                            {
                                MetadataServices.DeleteFromCloud("aboutus", aboutUs.AboutUsImageName);
                            }
                            aboutUs.AboutUsTitle = model.AboutUsTitle;
                            aboutUs.AboutUsTopDescription = model.AboutUsTopDescription;
                            aboutUs.AboutUsBottomDescription = model.AboutUsBottomDescription;
                            aboutUs.AboutUsImage = aboutUsImage;
                            aboutUs.AboutUsImageName = newFileName;
                            aboutUs.CreatedBy = aboutUs.CreatedBy;
                            aboutUs.CreatedAt = aboutUs.CreatedAt;
                            aboutUs.UpdatedBy = GetAuthorization.GetUsername(formsAuthentication.Name ?? "");
                            aboutUs.UpdatedAt = MetadataServices.GetCurrentDateTime();
                        }
                        else
                        {
                            aboutUs.AboutUsTitle = model.AboutUsTitle;
                            aboutUs.AboutUsTopDescription = model.AboutUsTopDescription;
                            aboutUs.AboutUsBottomDescription = model.AboutUsBottomDescription;
                            aboutUs.CreatedBy = aboutUs.CreatedBy;
                            aboutUs.CreatedAt = aboutUs.CreatedAt;
                            aboutUs.UpdatedBy = GetAuthorization.GetUsername(formsAuthentication.Name ?? "");
                            aboutUs.UpdatedAt = MetadataServices.GetCurrentDateTime();
                        }

                        db.SaveChanges();
                    }
                    else
                    {
                        var aboutUsImage = MetadataServices.UploadToCloud("aboutus", model.AboutUsImage, out newFileName);
                        var newAboutUs = new AboutUs()
                        {
                            AboutUsTitle = model.AboutUsTitle,
                            AboutUsTopDescription = model.AboutUsTopDescription,
                            AboutUsBottomDescription = model.AboutUsBottomDescription,
                            AboutUsImage = aboutUsImage,
                            AboutUsImageName = newFileName,
                            CreatedBy = GetAuthorization.GetUsername(formsAuthentication.Name ?? ""),
                            CreatedAt = MetadataServices.GetCurrentDateTime(),
                            UpdatedBy = GetAuthorization.GetUsername(formsAuthentication.Name ?? ""),
                            UpdatedAt = MetadataServices.GetCurrentDateTime(),
                        };
                        db.AboutUs.Add(newAboutUs);
                        db.SaveChanges();
                    }
                    return Json(new { result = "success", message = "" }, JsonRequestBehavior.AllowGet);
                }
            }
            else
            {
                return Json(new { result = "failed", Errors = ModelState.Errors() }, JsonRequestBehavior.AllowGet);
            }
        }

        [Authorize]
        public ActionResult ContactUs()
        {
            using (var db = new PrimeTravelEntities())
            {
                var contactUs = db.Contact.Select(e => new ContactUsViewModel()
                {
                    ContactTitle = e.ContactTitle,
                    ContactId = e.ContactId,
                    ContactName = e.ContactName,
                    ContactAddress = e.ContactAddress,
                    ContactLatitude = e.ContactLatitude,
                    ContactLongitude = e.ContactLongitude,
                    IsContactExist = db.Contact.Any(),
                    ContactDetails = db.ContactDetail.Select(c => new ContactDetailViewModel()
                    {
                        ContactDetailId = c.ContactDetailId,
                        ReservationTitle = c.ReservationTitle,
                        ContactNumber = c.ContactNumber,
                    }).ToList(),
                    ContactEmails = db.ContactEmail.Select(f => new ContactEmailViewModel()
                    {
                        ContactEmailId = f.ContactEmailId,
                        ContactEmailAddress = f.ContactEmailAddress,
                    }).ToList(),
                }).FirstOrDefault();
                if (contactUs.ContactTitle != "")
                {
                    contactUs.ContactTitle = System.Security.SecurityElement.Escape(contactUs.ContactTitle);
                }
                if (contactUs.ContactAddress != "")
                {
                    contactUs.ContactAddress = System.Security.SecurityElement.Escape(contactUs.ContactAddress);
                }
                return View(contactUs);
            }
        }

        [Authorize]
        [HttpPost]
        public JsonResult ContactUs(ContactUsViewModel model)
        {
            using (var db = new PrimeTravelEntities())
            {
                if (ModelState.IsValid)
                {
                    var formsAuthentication = HttpContext.Request.Cookies[FormsAuthentication.FormsCookieName] != null
                        ? FormsAuthentication.Decrypt(
                            HttpContext.Request.Cookies[FormsAuthentication.FormsCookieName].Value)
                        : null;
                    var contactUs = db.Contact.FirstOrDefault();
                    //var locationService = new GoogleLocationService();
                    //var point = locationService.GetLatLongFromAddress(model.ContactAddress);
                    var error = db.Contact.Select(e => new ContactUsViewModel()
                    {
                        ContactId = e.ContactId,
                        ContactName = e.ContactName,
                        ContactAddress = e.ContactAddress,
                        ContactLatitude = e.ContactLatitude,
                        ContactLongitude = e.ContactLongitude,
                        IsContactExist = db.Contact.Any(),
                        ContactDetails = db.ContactDetail.Select(c => new ContactDetailViewModel()
                        {
                            ContactDetailId = c.ContactDetailId,
                            ReservationTitle = c.ReservationTitle,
                            ContactNumber = c.ContactNumber,
                        }).ToList(),
                        ContactEmails = db.ContactEmail.Select(f => new ContactEmailViewModel()
                        {
                            ContactEmailId = f.ContactEmailId,
                            ContactEmailAddress = f.ContactEmailAddress,
                        }).ToList(),
                    }).FirstOrDefault();
                    //if (point == null)
                    //{
                    //    ModelState.AddModelError("ContactAddress", "" + model.ContactAddress + " not found.");
                    //    return Json(new { result = "failed", Errors = ModelState.Errors() }, JsonRequestBehavior.AllowGet);
                    //}
                    if (contactUs != null) //there's an exist contact us data in db
                    {
                        contactUs.ContactTitle = model.ContactTitle;
                        contactUs.ContactName = model.ContactName;
                        contactUs.ContactAddress = model.ContactAddress;
                        contactUs.ContactLatitude = contactUs.ContactLatitude;
                        contactUs.ContactLongitude = contactUs.ContactLongitude;
                        contactUs.CreatedBy = contactUs.CreatedBy;
                        contactUs.CreatedAt = contactUs.CreatedAt;
                        contactUs.UpdatedBy = formsAuthentication.Name ?? "";
                        contactUs.UpdatedAt = MetadataServices.GetCurrentDateTime();
                        db.SaveChanges();
                    }
                    else
                    {
                        var locationService = new GoogleLocationService();
                        var point = locationService.GetLatLongFromAddress(model.ContactAddress);
                        var newContact = new Contact()
                        {
                            ContactTitle = model.ContactTitle,
                            ContactName = model.ContactName,
                            ContactAddress = model.ContactAddress,
                            ContactLatitude = point.Latitude,
                            ContactLongitude = point.Longitude,
                            CreatedBy = formsAuthentication.Name ?? "",
                            CreatedAt = MetadataServices.GetCurrentDateTime(),
                            UpdatedBy = formsAuthentication.Name ?? "",
                            UpdatedAt = MetadataServices.GetCurrentDateTime(),
                        };
                        db.Contact.Add(newContact);
                        db.SaveChanges();
                    }
                    var result = db.Contact.Select(e => new ContactUsViewModel()
                    {
                        ContactId = e.ContactId,
                        ContactName = e.ContactName,
                        ContactAddress = e.ContactAddress,
                        ContactLatitude = e.ContactLatitude,
                        ContactLongitude = e.ContactLongitude,
                        IsContactExist = db.Contact.Any(),
                        ContactDetails = db.ContactDetail.Select(c => new ContactDetailViewModel()
                        {
                            ContactDetailId = c.ContactDetailId,
                            ReservationTitle = c.ReservationTitle,
                            ContactNumber = c.ContactNumber,
                        }).ToList(),
                        ContactEmails = db.ContactEmail.Select(f => new ContactEmailViewModel()
                        {
                            ContactEmailId = f.ContactEmailId,
                            ContactEmailAddress = f.ContactEmailAddress,
                        }).ToList(),
                    }).FirstOrDefault();
                    return Json(new { result = "success" }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    var contact = db.Contact.Select(e => new ContactUsViewModel()
                    {
                        ContactId = e.ContactId,
                        ContactName = e.ContactName,
                        ContactAddress = e.ContactAddress,
                        ContactLatitude = e.ContactLatitude,
                        ContactLongitude = e.ContactLongitude,
                        IsContactExist = db.Contact.Any(),
                        ContactDetails = db.ContactDetail.Select(c => new ContactDetailViewModel()
                        {
                            ContactDetailId = c.ContactDetailId,
                            ReservationTitle = c.ReservationTitle,
                            ContactNumber = c.ContactNumber,
                        }).ToList(),
                        ContactEmails = db.ContactEmail.Select(f => new ContactEmailViewModel()
                        {
                            ContactEmailId = f.ContactEmailId,
                            ContactEmailAddress = f.ContactEmailAddress,
                        }).ToList(),
                    }).FirstOrDefault();
                    return Json(new { result = "success" }, JsonRequestBehavior.AllowGet);
                }
            }
        }

        [Authorize]
        public ActionResult AddReservationContact()
        {
            return View(new AddReservationContact());
        }

        [Authorize]
        [HttpPost]
        public ActionResult AddReservationContact(AddReservationContact model)
        {
            if (!ModelState.IsValid) return View(model);
            using (var db = new PrimeTravelEntities())
            {
                var formsAuthentication = HttpContext.Request.Cookies[FormsAuthentication.FormsCookieName] != null
                    ? FormsAuthentication.Decrypt(HttpContext.Request.Cookies[FormsAuthentication.FormsCookieName]
                        .Value)
                    : null;
                var reservationContact = new ContactDetail()
                {
                    ReservationTitle = model.ReservationTitle,
                    ContactNumber = model.ContactNumber,
                    CreatedBy = formsAuthentication.Name ?? "",
                    CreatedAt = MetadataServices.GetCurrentDateTime(),
                    UpdatedBy = formsAuthentication.Name ?? "",
                    UpdatedAt = MetadataServices.GetCurrentDateTime(),
                };
                db.ContactDetail.Add(reservationContact);
                db.SaveChanges();
                return RedirectToAction("ContactUs", "Admin");
            }
        }

        [Authorize]
        public ActionResult EditReservationContact(long id)
        {
            using (var db = new PrimeTravelEntities())
            {
                var contact = db.ContactDetail.Find(id);
                if (contact == null)
                {
                    return new HttpNotFoundResult("Reservation Contact not found.");
                }
                var model = new AddReservationContact()
                {
                    ContactDetailId = contact.ContactDetailId,
                    ReservationTitle = contact.ReservationTitle,
                    ContactNumber = contact.ContactNumber,
                };

                return View(model);
            }
        }

        [Authorize]
        [HttpPost]
        public ActionResult EditReservationContact(AddReservationContact model)
        {
            if (ModelState.IsValid)
            {
                var formsAuthentication = HttpContext.Request.Cookies[FormsAuthentication.FormsCookieName] != null ? FormsAuthentication.Decrypt(HttpContext.Request.Cookies[FormsAuthentication.FormsCookieName].Value) : null;
                using (var db = new PrimeTravelEntities())
                {
                    var contact = db.ContactDetail.Find(model.ContactDetailId);
                    if (contact == null)
                    {
                        return new HttpNotFoundResult("Reservation Contact not found.");
                    }
                    contact.ReservationTitle = model.ReservationTitle;
                    contact.ContactNumber = model.ContactNumber;
                    contact.CreatedBy = contact.CreatedBy;
                    contact.CreatedAt = contact.CreatedAt;
                    contact.UpdatedBy = formsAuthentication.Name ?? "";
                    contact.UpdatedAt = MetadataServices.GetCurrentDateTime();
                    db.SaveChanges();
                    return RedirectToAction("ContactUs", "Admin");
                }
            }
            return View(model);
        }

        [Authorize]
        public ActionResult DeleteReservationContact(long id)
        {
            using (var db = new PrimeTravelEntities())
            {
                var reservation = db.ContactDetail.Find(id);
                if (reservation == null)
                {
                    return new HttpNotFoundResult("Reservation Contact not found.");
                }
                db.ContactDetail.Remove(reservation);
                db.SaveChanges();
                return RedirectToAction("ContactUs", "Admin");
            }
        }

        [Authorize]
        public ActionResult AddEmailAddress()
        {
            return View(new AddEmailAddress());
        }

        [Authorize]
        [HttpPost]
        public ActionResult AddEmailAddress(AddEmailAddress model)
        {
            if (!ModelState.IsValid) return View(model);
            using (var db = new PrimeTravelEntities())
            {
                var formsAuthentication = HttpContext.Request.Cookies[FormsAuthentication.FormsCookieName] != null
                    ? FormsAuthentication.Decrypt(HttpContext.Request.Cookies[FormsAuthentication.FormsCookieName]
                        .Value)
                    : null;
                var email = new ContactEmail()
                {
                    ContactEmailAddress = model.ContactEmailAddress,
                    CreatedBy = formsAuthentication.Name ?? "",
                    CreatedAt = MetadataServices.GetCurrentDateTime(),
                    UpdatedBy = formsAuthentication.Name ?? "",
                    UpdatedAt = MetadataServices.GetCurrentDateTime(),
                };
                db.ContactEmail.Add(email);
                db.SaveChanges();
                return RedirectToAction("ContactUs", "Admin");
            }
        }

        [Authorize]
        public ActionResult EditEmailAddress(long id)
        {
            using (var db = new PrimeTravelEntities())
            {
                var email = db.ContactEmail.Find(id);
                if (email == null)
                {
                    return new HttpNotFoundResult("Email Address not found.");
                }
                var model = new AddEmailAddress()
                {
                    ContactEmailId = email.ContactEmailId,
                    ContactEmailAddress = email.ContactEmailAddress,
                };
                return View(model);
            }
        }

        [Authorize]
        [HttpPost]
        public ActionResult EditEmailAddress(AddEmailAddress model)
        {
            if (!ModelState.IsValid) return View(model);
            using (var db = new PrimeTravelEntities())
            {
                var formsAuthentication = HttpContext.Request.Cookies[FormsAuthentication.FormsCookieName] != null
                    ? FormsAuthentication.Decrypt(HttpContext.Request.Cookies[FormsAuthentication.FormsCookieName]
                        .Value)
                    : null;
                var email = db.ContactEmail.Find(model.ContactEmailId);
                if (email == null)
                {
                    return new HttpNotFoundResult("Email Address not found.");
                }
                email.ContactEmailAddress = model.ContactEmailAddress;
                email.UpdatedBy = formsAuthentication.Name ?? "";
                email.UpdatedAt = MetadataServices.GetCurrentDateTime();
                db.SaveChanges();
                return RedirectToAction("ContactUs", "Admin");
            }
        }

        [Authorize]
        public ActionResult DeleteEmailAddress(long id)
        {
            using (var db = new PrimeTravelEntities())
            {
                var email = db.ContactEmail.Find(id);
                if (email == null)
                {
                    return new HttpNotFoundResult("Email Address not found.");
                }
                db.ContactEmail.Remove(email);
                db.SaveChanges();
                return RedirectToAction("ContactUs", "Admin");
            }
        }

        [Authorize]
        public ActionResult Experience()
        {
            using (var db = new PrimeTravelEntities())
            {
                var experiences = db.Experience.Select(e => new ExperienceViewModel()
                {
                    LocationId = e.LocationId,
                    Latitude = e.Latitude,
                    Longitude = e.Longitude,
                    LocationName = e.LocationName,
                }).ToList();
                return View(experiences);
            }
        }

        [Authorize]
        [ValidateInput(false)]
        public JsonResult UpdateExperienceTitle(string title)
        {
            using (var db = new PrimeTravelEntities())
            {
                var experienceTitle = db.PrimeConfiguration.FirstOrDefault(e => e.ConfigurationName == "ExperienceTitle");
                if (experienceTitle != null)
                {
                    experienceTitle.ConfigurationValue = title;
                    db.SaveChanges();
                }
                return Json(new{ result = "success" }, JsonRequestBehavior.AllowGet);
            }
        }

        [Authorize]
        public JsonResult BatchDeleteLocation(string[] id) //experience id
        {
            using (var db = new PrimeTravelEntities())
            {
                if (id.Length == 0)
                {
                    return Json(new { result = "failed", message = "Please select location to delete." }, JsonRequestBehavior.AllowGet);
                }
                foreach (var item in id)
                {
                    var locationId = Convert.ToInt64(item);
                    var location = db.Experience.Find(locationId);
                    if (location == null)
                    {
                        continue;
                    }
                    var galleries = db.ExperienceDetail.Where(e => e.LocationId == location.LocationId).ToList();
                    foreach (var gallery in galleries)
                    {
                        if (gallery.LocationContentType == false)
                        {
                            MetadataServices.DeleteFromCloud("gallery", gallery.LocationImageName);
                        }
                    }

                    db.ExperienceDetail.RemoveRange(galleries);
                    db.Experience.Remove(location);
                }
                db.SaveChanges();
                return Json(new { result = "success" }, JsonRequestBehavior.AllowGet);
            }
        }

        [Authorize]
        public ActionResult AddLocation()
        {
            return View(new AddLocationViewModel());
        }

        [Authorize]
        [HttpPost]
        public ActionResult AddLocation(AddLocationViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            using (var db = new PrimeTravelEntities())
            {
                var formsAuthentication = HttpContext.Request.Cookies[FormsAuthentication.FormsCookieName] != null
                    ? FormsAuthentication.Decrypt(HttpContext.Request.Cookies[FormsAuthentication.FormsCookieName]
                        .Value)
                    : null;
                var locationService = new GoogleLocationService();
                if ((model.Latitude == 0 || model.Latitude == null) && (model.Longitude == 0 || model.Longitude == null))
                {
                    var point = new MapPoint();
                    try
                    {
                        point = locationService.GetLatLongFromAddress(model.LocationName);
                    }
                    catch (Exception e)
                    {
                        ModelState.AddModelError("LocationName", "Something wrong with google map, please try again in a moment.");
                        return View(model);
                    }
                    if (point == null)
                    {
                        ModelState.AddModelError("LocationName", "" + model.LocationName + " not found.");
                        return View(model);
                    }
                    model.Latitude = point.Latitude;
                    model.Longitude = point.Longitude;
                    var address = locationService.GetAddressFromLatLang(model.Latitude, model.Longitude);
                    var location = new Experience()
                    {
                        LocationName = model.LocationName,
                        Latitude = model.Latitude,
                        Longitude = model.Longitude,
                        CountryName = address.Country,
                        CreatedBy = formsAuthentication.Name ?? "",
                        CreatedAt = MetadataServices.GetCurrentDateTime(),
                        UpdatedBy = formsAuthentication.Name ?? "",
                        UpdatedAt = MetadataServices.GetCurrentDateTime(),
                    };
                    db.Experience.Add(location);
                    db.SaveChanges();
                }
                else
                {
                    var address = locationService.GetAddressFromLatLang(model.Latitude, model.Longitude);
                    var location = new Experience()
                    {
                        LocationName = model.LocationName,
                        Latitude = model.Latitude,
                        Longitude = model.Longitude,
                        CountryName = address.Country,
                        CreatedBy = formsAuthentication.Name ?? "",
                        CreatedAt = MetadataServices.GetCurrentDateTime(),
                        UpdatedBy = formsAuthentication.Name ?? "",
                        UpdatedAt = MetadataServices.GetCurrentDateTime(),
                    };
                    db.Experience.Add(location);
                    db.SaveChanges();
                }
                return RedirectToAction("Experience", "Admin");
            }
        }

        [Authorize]
        public ActionResult EditLocation(long id)
        {
            using (var db = new PrimeTravelEntities())
            {
                var location = db.Experience.Find(id);
                if (location == null)
                {
                    return new HttpNotFoundResult("Location Record not found.");
                }
                var model = new AddLocationViewModel()
                {
                    LocationName = location.LocationName,
                    Latitude = location.Latitude,
                    Longitude = location.Longitude,
                    LocationId = location.LocationId,
                };
                return View(model);
            }
        }

        [Authorize]
        [HttpPost]
        public ActionResult EditLocation(AddLocationViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            using (var db = new PrimeTravelEntities())
            {
                var formsAuthentication = HttpContext.Request.Cookies[FormsAuthentication.FormsCookieName] != null
                    ? FormsAuthentication.Decrypt(HttpContext.Request.Cookies[FormsAuthentication.FormsCookieName]
                        .Value)
                    : null;
                var location = db.Experience.Find(model.LocationId);
                if (location == null)
                {
                    return new HttpNotFoundResult("Location record not found.");
                }
                var locationService = new GoogleLocationService();
                if (model.Latitude == 0 && model.Longitude == 0)
                {
                    var point = locationService.GetLatLongFromAddress(model.LocationName);
                    if (point == null)
                    {
                        ModelState.AddModelError("LocationName", "" + model.LocationName + " not found.");
                        return View(model);
                    }
                    model.Latitude = point.Latitude;
                    model.Longitude = point.Longitude;
                    var address = locationService.GetAddressFromLatLang(model.Latitude, model.Longitude);
                    location.LocationName = model.LocationName;
                    location.Latitude = location.Latitude;
                    location.Longitude = location.Longitude;
                    location.CountryName = address.Country;
                    location.CreatedBy = location.CreatedBy;
                    location.CreatedAt = location.CreatedAt;
                    location.UpdatedBy = formsAuthentication.Name ?? "";
                    location.UpdatedAt = MetadataServices.GetCurrentDateTime();
                    db.SaveChanges();
                }
                else
                {
                    var address = locationService.GetAddressFromLatLang(model.Latitude, model.Longitude);
                    location.LocationName = model.LocationName;
                    location.Latitude = model.Latitude;
                    location.Longitude = model.Longitude;
                    location.CountryName = location.CountryName;
                    location.CreatedBy = location.CreatedBy;
                    location.CreatedAt = location.CreatedAt;
                    location.UpdatedBy = formsAuthentication.Name ?? "";
                    location.UpdatedAt = MetadataServices.GetCurrentDateTime();
                    db.SaveChanges();
                }
                return RedirectToAction("Experience", "Admin");
            }
        }

        [Authorize]
        public ActionResult DeleteLocation(long id)
        {
            using (var db = new PrimeTravelEntities())
            {
                var location = db.Experience.Find(id);
                if (location == null)
                {
                    return new HttpNotFoundResult("Location record not found.");
                }
                var galleries = db.ExperienceDetail.Where(e => e.LocationId == id).ToList();
                db.ExperienceDetail.RemoveRange(galleries);
                db.Experience.Remove(location);
                db.SaveChanges();
                return RedirectToAction("Experience", "Admin");
            }
        }

        [Authorize]
        public ActionResult Gallery(long? id = null)
        {
            using (var db = new PrimeTravelEntities())
            {
                if (id == null)
                {
                    var galleries = db.ExperienceDetail.Select(e => new ExperienceDetailViewModel()
                    {
                        LocationDetailId = e.LocationDetailId,
                        LocationContentType = e.LocationContentType,
                        LocationContent = e.LocationContent,
                        LocationIndex = e.LocationIndex,
                        LocationId = e.LocationId,
                        LocationName = db.Experience.Where(f => f.LocationId == e.LocationId).Select(f => f.LocationName).FirstOrDefault(),
                    }).OrderBy(e => e.LocationIndex).ToList();
                    return View(new GalleryViewModel()
                    {
                        LocationId = 0,
                        LocationName = "",
                        Galleries = galleries,
                    });
                }
                else
                {

                    var galleries = db.ExperienceDetail.Where(e => e.LocationId == id).Select(e => new ExperienceDetailViewModel()
                    {
                        LocationDetailId = e.LocationDetailId,
                        LocationContentType = e.LocationContentType,
                        LocationContent = e.LocationContent,
                        LocationIndex = e.LocationIndex,
                    }).OrderBy(e => e.LocationIndex).ToList();
                    return View(new GalleryViewModel()
                    {
                        LocationId = id.Value,
                        LocationName = db.Experience.Where(e => e.LocationId == id).Select(e => e.LocationName).FirstOrDefault(),
                        Galleries = galleries,
                    });
                }
            }
        }

        [Authorize]
        public ActionResult AddGallery(long? id = null)
        {
            using (var db = new PrimeTravelEntities())
            {
                if (id == null)
                {
                    return View(new AddGalleryViewModel()
                    {

                        LocationName = (from e in db.Experience
                                        select new { e.LocationId, e.LocationName }).ToList().Select(e => new SelectListItem
                                        {
                                            Text = e.LocationName,
                                            Value = e.LocationId.ToString(),
                                        }).ToList(),
                        LocationId = 0,
                    });
                }
                else
                {
                    return View(new AddGalleryViewModel()
                    {

                        LocationName = (from e in db.Experience
                                        select new { e.LocationId, e.LocationName }).ToList().Select(e => new SelectListItem
                                        {
                                            Text = e.LocationName,
                                            Value = e.LocationId.ToString(),
                                        }).ToList(),
                        LocationId = id.Value,
                    });
                }
            }

        }

        //[Authorize]
        //[HttpPost]
        //public ActionResult AddGallery(AddGalleryViewModel model)
        //{
        //    using (var db = new PrimeTravelEntities())
        //    {
        //        if (!ModelState.IsValid)
        //        {
        //            return View(new AddGalleryViewModel()
        //            {

        //                LocationName = (from e in db.Experience
        //                                select new { e.LocationId, e.LocationName }).ToList().Select(e => new SelectListItem
        //                                {
        //                                    Text = e.LocationName,
        //                                    Value = e.LocationId.ToString(),
        //                                }).ToList(),
        //                LocationId = model.LocationId,
        //            });
        //        }
        //        var formsAuthentication = HttpContext.Request.Cookies[FormsAuthentication.FormsCookieName] != null
        //            ? FormsAuthentication.Decrypt(
        //                HttpContext.Request.Cookies[FormsAuthentication.FormsCookieName].Value)
        //            : null;
        //        if (!model.LocationContentType) //image
        //        {
        //            string newFileName = "";
        //            var galleryImage = MetadataServices.UploadToCloud("gallery", model.ContentImage, out newFileName);
        //            var IsGalleryExist = db.ExperienceDetail.Any(e => e.LocationId == model.LocationId && !e.LocationContentType);
        //            if (IsGalleryExist)
        //            {
        //                var index = db.ExperienceDetail.Where(e => e.LocationId == model.LocationId && !e.LocationContentType).OrderByDescending(e => e.LocationIndex).Select(e => e.LocationIndex).FirstOrDefault();
        //                var index = (from e in db.ExperienceDetail
        //                             where e.LocationId == model.LocationId
        //                             select e.LocationIndex).LastOrDefault();
        //                var gallery = new ExperienceDetail()
        //                {
        //                    LocationId = model.LocationId,
        //                    LocationContentType = model.LocationContentType,
        //                    LocationContent = galleryImage,
        //                    LocationImageName = newFileName,
        //                    LocationIndex = index + 1,
        //                    CreatedBy = formsAuthentication.Name ?? "",
        //                    CreatedAt = MetadataServices.GetCurrentDateTime(),
        //                    UpdatedBy = formsAuthentication.Name ?? "",
        //                    UpdatedAt = MetadataServices.GetCurrentDateTime(),
        //                };
        //                db.ExperienceDetail.Add(gallery);
        //            }
        //            else
        //            {
        //                var gallery = new ExperienceDetail()
        //                {
        //                    LocationId = model.LocationId,
        //                    LocationContentType = model.LocationContentType,
        //                    LocationContent = galleryImage,
        //                    LocationImageName = newFileName,
        //                    LocationIndex = 1,
        //                    CreatedBy = formsAuthentication.Name ?? "",
        //                    CreatedAt = MetadataServices.GetCurrentDateTime(),
        //                    UpdatedBy = formsAuthentication.Name ?? "",
        //                    UpdatedAt = MetadataServices.GetCurrentDateTime(),
        //                };
        //                db.ExperienceDetail.Add(gallery);
        //            }
        //            db.SaveChanges();
        //            return RedirectToAction("Gallery", "Admin", new { id = model.LocationId });
        //        }
        //        else //video
        //        {
        //            if (model.ContentType == "")
        //            {
        //                ModelState.AddModelError("LocationContent", "Please Enter Video Link.");
        //                return View(new AddGalleryViewModel()
        //                {

        //                    LocationName = (from e in db.Experience
        //                                    select new { e.LocationId, e.LocationName }).ToList().Select(e => new SelectListItem
        //                                    {
        //                                        Text = e.LocationName,
        //                                        Value = e.LocationId.ToString(),
        //                                    }).ToList(),
        //                    LocationId = model.LocationId,
        //                });
        //            }
        //            var IsGalleryExist = db.ExperienceDetail.Any(e => e.LocationId == model.LocationId && e.LocationContentType);
        //            if (IsGalleryExist)
        //            {
        //                var index = db.ExperienceDetail.Where(e => e.LocationId == model.LocationId && !e.LocationContentType).OrderByDescending(e => e.LocationIndex).Select(e => e.LocationIndex).FirstOrDefault();
        //                var gallery = new ExperienceDetail()
        //                {
        //                    LocationId = model.LocationId,
        //                    LocationContentType = model.LocationContentType,
        //                    LocationContent = model.LocationContent,
        //                    LocationIndex = index + 1,
        //                    CreatedBy = formsAuthentication.Name ?? "",
        //                    CreatedAt = MetadataServices.GetCurrentDateTime(),
        //                    UpdatedBy = formsAuthentication.Name ?? "",
        //                    UpdatedAt = MetadataServices.GetCurrentDateTime(),
        //                };
        //                db.ExperienceDetail.Add(gallery);
        //            }
        //            else
        //            {
        //                var gallery = new ExperienceDetail()
        //                {
        //                    LocationId = model.LocationId,
        //                    LocationContentType = model.LocationContentType,
        //                    LocationContent = model.LocationContent,
        //                    LocationIndex = 1,
        //                    CreatedBy = formsAuthentication.Name ?? "",
        //                    CreatedAt = MetadataServices.GetCurrentDateTime(),
        //                    UpdatedBy = formsAuthentication.Name ?? "",
        //                    UpdatedAt = MetadataServices.GetCurrentDateTime(),
        //                };
        //                db.ExperienceDetail.Add(gallery);
        //            }
        //            db.SaveChanges();
        //            return RedirectToAction("Gallery", "Admin", new { id = model.LocationId });
        //        }
        //    }
        //}

        [Authorize]
        [HttpPost]
        public JsonResult AddGallery(AddGalleryViewModel model)
        {
            using (var db = new PrimeTravelEntities())
            {
                var formsAuthentication = HttpContext.Request.Cookies[FormsAuthentication.FormsCookieName] != null
                    ? FormsAuthentication.Decrypt(
                        HttpContext.Request.Cookies[FormsAuthentication.FormsCookieName].Value)
                    : null;
                if (!ModelState.IsValid)
                {
                    return Json(new { result = "failed", message = "Please fill in all the mandatory field." }, JsonRequestBehavior.AllowGet);
                }

                if (!model.LocationContentType) //image
                {
                    if (!model.ContentImageArray.Any())
                    {
                        return Json(new {result = "failed", message = "no image uploaded."},
                            JsonRequestBehavior.AllowGet);
                    }
                    for (int i = 0; i < model.ContentImageArray.Count(); i++)
                    {
                        string newFileName = "";
                        var galleryImage =
                            MetadataServices.UploadToCloud("gallery", model.ContentImageArray[i], out newFileName);
                        var IsGalleryExist =
                            db.ExperienceDetail.Any(e => e.LocationId == model.LocationId && !e.LocationContentType);
                        if (IsGalleryExist)
                        {
                            var index = db.ExperienceDetail
                                .Where(e => e.LocationId == model.LocationId && !e.LocationContentType)
                                .OrderByDescending(e => e.LocationIndex).Select(e => e.LocationIndex).FirstOrDefault();
                            //var index = (from e in db.ExperienceDetail
                            //    where e.LocationId == model.LocationId
                            //    select e.LocationIndex).LastOrDefault();
                            var gallery = new ExperienceDetail()
                            {
                                LocationId = model.LocationId,
                                LocationContentType = model.LocationContentType,
                                LocationContent = galleryImage,
                                LocationImageName = newFileName,
                                LocationIndex = index + 1,
                                CreatedBy = formsAuthentication.Name ?? "",
                                CreatedAt = MetadataServices.GetCurrentDateTime(),
                                UpdatedBy = formsAuthentication.Name ?? "",
                                UpdatedAt = MetadataServices.GetCurrentDateTime(),
                            };
                            db.ExperienceDetail.Add(gallery);
                        }
                        else
                        {
                            var gallery = new ExperienceDetail()
                            {
                                LocationId = model.LocationId,
                                LocationContentType = model.LocationContentType,
                                LocationContent = galleryImage,
                                LocationImageName = newFileName,
                                LocationIndex = 1,
                                CreatedBy = formsAuthentication.Name ?? "",
                                CreatedAt = MetadataServices.GetCurrentDateTime(),
                                UpdatedBy = formsAuthentication.Name ?? "",
                                UpdatedAt = MetadataServices.GetCurrentDateTime(),
                            };
                            db.ExperienceDetail.Add(gallery);
                        }
                        db.SaveChanges();
                    }
                    return Json(new {result = "success", returnUrl = "/Admin/Gallery?id=" + model.LocationId + ""},
                        JsonRequestBehavior.AllowGet);
                }
                else //video
                {
                    if (model.ContentType == "")
                    {
                        ModelState.AddModelError("LocationContent", "Please Enter Video Link.");
                        return Json(new { result = "failed", message = "Please Enter Video Link." }, JsonRequestBehavior.AllowGet);
                    }
                    for (int i = 0; i < model.LocationContentArray.Length; i++)
                    {
                        if (model.LocationContentArray[i] == "")
                        {
                            continue;
                        }
                        var isGalleryExist = db.ExperienceDetail.Any(e => e.LocationId == model.LocationId && e.LocationContentType);
                        if (isGalleryExist)
                        {
                            var index = db.ExperienceDetail.Where(e => e.LocationId == model.LocationId && !e.LocationContentType).OrderByDescending(e => e.LocationIndex).Select(e => e.LocationIndex).FirstOrDefault();
                            var gallery = new ExperienceDetail()
                            {
                                LocationId = model.LocationId,
                                LocationContentType = model.LocationContentType,
                                LocationContent = model.LocationContentArray[i],
                                LocationIndex = index + 1,
                                CreatedBy = formsAuthentication.Name ?? "",
                                CreatedAt = MetadataServices.GetCurrentDateTime(),
                                UpdatedBy = formsAuthentication.Name ?? "",
                                UpdatedAt = MetadataServices.GetCurrentDateTime(),
                            };
                            db.ExperienceDetail.Add(gallery);
                        }
                        else
                        {
                            var gallery = new ExperienceDetail()
                            {
                                LocationId = model.LocationId,
                                LocationContentType = model.LocationContentType,
                                LocationContent = model.LocationContentArray[i],
                                LocationIndex = 1,
                                CreatedBy = formsAuthentication.Name ?? "",
                                CreatedAt = MetadataServices.GetCurrentDateTime(),
                                UpdatedBy = formsAuthentication.Name ?? "",
                                UpdatedAt = MetadataServices.GetCurrentDateTime(),
                            };
                            db.ExperienceDetail.Add(gallery);
                        }
                        db.SaveChanges();
                    }
                    return Json(new { result = "success", returnUrl = "/Admin/Gallery?id=" + model.LocationId + "" },
                        JsonRequestBehavior.AllowGet);
                }
                return Json(new { result = "failed", message = "failed" }, JsonRequestBehavior.AllowGet);
            }
        }

        [Authorize]
        public ActionResult EditGallery(long id)
        {
            using (var db = new PrimeTravelEntities())
            {
                var gallery = db.ExperienceDetail.Find(id);
                if (gallery == null)
                {
                    return new HttpNotFoundResult("Gallery not found.");
                }
                return View(new AddGalleryViewModel()
                {
                    LocationDetailId = gallery.LocationDetailId,
                    LocationId = gallery.LocationId,
                    Location = db.Experience.Where(e => e.LocationId == gallery.LocationId).Select(e => e.LocationName).FirstOrDefault(),
                    LocationContentType = gallery.LocationContentType,
                    LocationContent = gallery.LocationContent,
                });
            }
        }

        [Authorize]
        [HttpPost]
        public ActionResult EditGallery(AddGalleryViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            using (var db = new PrimeTravelEntities())
            {
                var formsAuthentication = HttpContext.Request.Cookies[FormsAuthentication.FormsCookieName] != null
                    ? FormsAuthentication.Decrypt(
                        HttpContext.Request.Cookies[FormsAuthentication.FormsCookieName].Value)
                    : null;
                var gallery = db.ExperienceDetail.Find(model.LocationDetailId);
                if (gallery == null)
                {
                    return new HttpNotFoundResult("Gallery not found.");
                }
                if (!model.LocationContentType) //image
                {
                    string newFileName = "";
                    var galleryImage = MetadataServices.UploadToCloud("gallery", model.ContentImage, out newFileName);
                    if (gallery.LocationImageName != "")
                    {
                        MetadataServices.DeleteFromCloud("gallery", gallery.LocationImageName);
                    }
                    gallery.LocationContent = galleryImage;
                    gallery.UpdatedBy = formsAuthentication.Name ?? "";
                    gallery.UpdatedAt = MetadataServices.GetCurrentDateTime();
                    db.SaveChanges();
                }
                else //video
                {
                    gallery.LocationContent = model.LocationContent;
                    gallery.UpdatedBy = formsAuthentication.Name ?? "";
                    gallery.UpdatedAt = MetadataServices.GetCurrentDateTime();
                    db.SaveChanges();
                }
                return RedirectToAction("Gallery", "Admin", new { id = model.LocationId });
            }
        }

        [Authorize]
        public ActionResult DeleteGallery(long id, long locationId)
        {
            using (var db = new PrimeTravelEntities())
            {
                var gallery = db.ExperienceDetail.Find(id);
                if (gallery == null)
                {
                    return new HttpNotFoundResult("Gallery not found.");
                }
                if (!gallery.LocationContentType) //image file
                {
                    MetadataServices.DeleteFromCloud("gallery", gallery.LocationImageName);
                }
                
                db.ExperienceDetail.Remove(gallery);
                db.SaveChanges();
                if (locationId != 0)
                {
                    return RedirectToAction("Gallery", "Admin", new { id = locationId });
                }
                else
                {
                    return RedirectToAction("Gallery", "Admin");
                }
            }
        }

        [Authorize]
        public JsonResult BatchDeleteGallery(string[] id)
        {
            using (var db = new PrimeTravelEntities())
            {
                if (id.Length == 0)
                {
                    return Json(new { result = "failed", message = "Please select gallery to delete." }, JsonRequestBehavior.AllowGet);
                }
                foreach (var item in id)
                {
                    var galleryId = Convert.ToInt64(item);
                    var gallery = db.ExperienceDetail.Find(galleryId);
                    if (gallery == null)
                    {
                        continue;
                    }
                    if (gallery.LocationContentType == false)
                    {
                        MetadataServices.DeleteFromCloud("gallery", gallery.LocationImageName);
                    }
                    db.ExperienceDetail.Remove(gallery);
                }
                db.SaveChanges();
                return Json(new { result = "success"}, JsonRequestBehavior.AllowGet);
            }
        }

        //YUAN YEE
        [Authorize]
        public ActionResult AddBanner()
        {
            return View();
        }

        [Authorize]
        [HttpPost]
        public JsonResult AddBanner(BannerModel model)
        {
            using (var db = new PrimeTravelEntities())
            {
                if (ModelState.IsValid)
                {
                    Banner banner = new Banner();
                    if (model.BannerDescription == null)
                    {
                        banner.BannerDescription = "";
                    }
                    else
                    {
                        banner.BannerDescription = model.BannerDescription;
                    }
                    string newFileName;
                    banner.BannerImage = MetadataServices.UploadToCloud("banner", model.BannerImage, out newFileName);
                    banner.IsActive = Convert.ToBoolean(model.IsActive);
                    banner.BannerImageName = newFileName;
                    banner.CreatedAt = MetadataServices.GetCurrentDateTime();
                    banner.CreatedBy = "Admin";    //change to login username
                    banner.UpdatedAt = MetadataServices.GetCurrentDateTime();
                    banner.UpdatedBy = "Admin";    //change to login username
                    db.Banner.Add(banner);
                    db.SaveChanges();
                }
                return Json(new { }, JsonRequestBehavior.AllowGet);
            }
        }

        [Authorize]
        public ActionResult BannerList()
        {
            using (var db = new PrimeTravelEntities())
            {
                List<BannerModel> banner = (from data in db.Banner
                                            where data.IsDeleted == false
                                            select new BannerModel()
                                            {
                                                BannerId = data.BannerId,
                                                BannerDescription = data.BannerDescription,
                                                BImage = data.BannerImage,
                                                DateAdded = data.CreatedAt.ToString(),
                                                CreatedAt = data.CreatedAt,
                                                IsActive = data.IsActive,
                                            }).ToList();
                foreach (var item in banner)
                {
                    item.DateAdded = String.Format("{0:dd-MM-yyyy}", Convert.ToDateTime(item.DateAdded));
                    if (item.IsActive == true)
                    {
                        item.Status = "Active";
                    }
                    else
                    {
                        item.Status = "De-Active";
                    }
                }
                return View(banner);
            }
        }

        [Authorize]
        public ActionResult EditBanner(int Id)
        {
            using (var db = new PrimeTravelEntities())
            {
                var banner = (from item in db.Banner
                              where item.BannerId == Id
                              select new BannerModel()
                              {
                                  BannerId = item.BannerId,
                                  BannerDescription = item.BannerDescription,
                                  BImage = item.BannerImage,
                                  IsActive = item.IsActive,
                              }).SingleOrDefault();

                if (banner.IsActive == true)
                {
                    banner.Status = "true";
                }
                else
                {
                    banner.Status = "false";
                }

                return View(banner);
            }
        }

        [Authorize]
        [HttpPost]
        public JsonResult EditBanner(BannerModel model)
        {
            using (var db = new PrimeTravelEntities())
            {
                if (ModelState.IsValid)
                {
                    Banner banner = db.Banner.Find(model.BannerId);
                    if (model.BannerDescription == null)
                    {
                        banner.BannerDescription = "";
                    }
                    else
                    {
                        banner.BannerDescription = model.BannerDescription;
                    }

                    if (model.BannerImage != null)
                    {
                        MetadataServices.DeleteFromCloud("banner", banner.BannerImageName);
                        string newFileName;
                        banner.BannerImage = MetadataServices.UploadToCloud("banner", model.BannerImage, out newFileName);
                        banner.BannerImageName = newFileName;
                    }
                    banner.IsActive = Convert.ToBoolean(model.IsActive);
                    banner.UpdatedAt = MetadataServices.GetCurrentDateTime();
                    banner.UpdatedBy = "Admin";    //change to login username
                    db.SaveChanges();
                }
                return Json(new { }, JsonRequestBehavior.AllowGet);
            }
        }

        [Authorize]
        [HttpPost]
        public ActionResult DeleteBanner(int BannerId)
        {
            using (var db = new PrimeTravelEntities())
            {
                if (ModelState.IsValid)
                {
                    Banner banner = db.Banner.Find(BannerId);
                    MetadataServices.DeleteFromCloud("banner", banner.BannerImageName);
                    db.Banner.Remove(banner);
                    db.SaveChanges();
                }
                return RedirectToAction("BannerList");
            }
        }

        [Authorize]
        public ActionResult AddSocialNetwork()
        {
            return View();
        }

        [Authorize]
        [HttpPost]
        public ActionResult AddSocialNetwork(string SocialNetworkName, string SocialNetworkType, string SocialNetworkLink, string IsActive)
        {
            using (var db = new PrimeTravelEntities())
            {
                if (ModelState.IsValid)
                {
                    SocialNetwork social = new SocialNetwork();
                    social.SocialNetworkName = SocialNetworkName;
                    social.SocialNetworkLink = SocialNetworkLink;
                    social.SocialNetworkType = SocialNetworkType;
                    social.IsActive = Convert.ToBoolean(IsActive);
                    social.CreatedAt = MetadataServices.GetCurrentDateTime();
                    social.CreatedBy = "Admin";    //change to login username
                    social.UpdatedAt = MetadataServices.GetCurrentDateTime();
                    social.UpdatedBy = "Admin";    //change to login username
                    db.SocialNetwork.Add(social);
                    db.SaveChanges();
                }
                return RedirectToAction("SocialNetworkList");
            }
        }

        [Authorize]
        public ActionResult SocialNetworkList()
        {
            using (var db = new PrimeTravelEntities())
            {
                List<SocialNetworkModel> social = (from data in db.SocialNetwork
                                                   where data.IsDeleted == false
                                                   select new SocialNetworkModel()
                                                   {
                                                       SocialNetworkId = data.SocialNetworkId,
                                                       SocialNetworkType = data.SocialNetworkType,
                                                       SocialNetworkName = data.SocialNetworkName,
                                                       SocialNetworkLink = data.SocialNetworkLink,
                                                       DateAdded = data.CreatedAt.ToString(),
                                                       CreatedAt = data.CreatedAt,
                                                       IsActive = data.IsActive,
                                                   }).ToList();
                foreach (var item in social)
                {
                    item.DateAdded = String.Format("{0:dd-MM-yyyy}", Convert.ToDateTime(item.DateAdded));
                    if (item.IsActive == true)
                    {
                        item.Status = "Active";
                    }
                    else
                    {
                        item.Status = "De-Active";
                    }
                }
                return View(social);
            }
        }

        [Authorize]
        public ActionResult EditSocialNetwork(int Id)
        {
            using (var db = new PrimeTravelEntities())
            {
                var social = (from item in db.SocialNetwork
                              where item.SocialNetworkId == Id
                              select new SocialNetworkModel()
                              {
                                  SocialNetworkId = item.SocialNetworkId,
                                  SocialNetworkName = item.SocialNetworkName,
                                  SocialNetworkLink = item.SocialNetworkLink,
                                  SocialNetworkType = item.SocialNetworkType,
                                  IsActive = item.IsActive,
                              }).SingleOrDefault();

                if (social.IsActive == true)
                {
                    social.Status = "true";
                }
                else
                {
                    social.Status = "false";
                }

                return View(social);
            }
        }

        [Authorize]
        [HttpPost]
        public JsonResult EditSocialNetwork(int SocialNetworkId, string SocialNetworkLink, string IsActive)
        {
            using (var db = new PrimeTravelEntities())
            {
                if (ModelState.IsValid)
                {
                    SocialNetwork social = db.SocialNetwork.Find(SocialNetworkId);
                    social.SocialNetworkLink = SocialNetworkLink;
                    social.IsActive = Convert.ToBoolean(IsActive);
                    social.UpdatedAt = MetadataServices.GetCurrentDateTime();
                    social.UpdatedBy = "Admin";    //change to login username
                    db.SaveChanges();
                }
                return Json(new { }, JsonRequestBehavior.AllowGet);
            }
        }

        [Authorize]
        [HttpPost]
        public ActionResult DeleteSocialNetwork(int SocialNetworkId)
        {
            using (var db = new PrimeTravelEntities())
            {
                if (ModelState.IsValid)
                {
                    SocialNetwork social = db.SocialNetwork.Find(SocialNetworkId);
                    social.IsDeleted = true;
                    social.UpdatedAt = MetadataServices.GetCurrentDateTime();
                    social.UpdatedBy = "Admin";    //change to login username
                    db.SaveChanges();
                }
                return RedirectToAction("SocialNetworkList");
            }
        }

        [Authorize]
        public ActionResult AddService()
        {
            return View();
        }

        [Authorize]
        [HttpPost]
        public JsonResult AddService(ServiceModel model)
        {
            using (var db = new PrimeTravelEntities())
            {
                string newFileName = "";
                if (ModelState.IsValid)
                {
                    Service service = new Service();
                    ServiceInfo serviceInfo = new ServiceInfo();
                    ServiceViewMore serviceViewMore = new ServiceViewMore();
                    service.ServiceType = model.ServiceType;
                    service.CreatedAt = MetadataServices.GetCurrentDateTime();
                    service.CreatedBy = "Admin";    //change to login username
                    service.UpdatedAt = MetadataServices.GetCurrentDateTime();
                    service.UpdatedBy = "Admin";    //change to login username
                    db.Service.Add(service);
                    serviceInfo.ServiceId = service.ServiceId;
                    serviceInfo.ServiceName = model.ServiceName;
                    serviceInfo.ServiceImage = MetadataServices.UploadToCloud("service", model.ServiceImage, out newFileName);
                    serviceInfo.ServiceImageName = model.ServiceImage.FileName + "-" + MetadataServices.GetDateWithoutSlash();
                    serviceInfo.CreatedAt = MetadataServices.GetCurrentDateTime();
                    serviceInfo.CreatedBy = "Admin";    //change to login username
                    serviceInfo.UpdatedAt = MetadataServices.GetCurrentDateTime();
                    serviceInfo.UpdatedBy = "Admin";    //change to login username
                    db.ServiceInfo.Add(serviceInfo);
                    serviceViewMore.ServiceId = service.ServiceId;
                    serviceViewMore.ServiceImage = MetadataServices.UploadToCloud("service", model.ServiceViewMoreImage, out newFileName);
                    serviceViewMore.ServiceImageName = model.ServiceViewMoreImage.FileName + "-" + MetadataServices.GetDateWithoutSlash();
                    db.ServiceViewMore.Add(serviceViewMore);
                    db.SaveChanges();

                    for (int i = 0; i < model.ServiceItems.Length; i++)
                    {
                        serviceInfo.ServiceId = service.ServiceId;
                        serviceInfo.ServiceImage = MetadataServices.UploadToCloud("service", model.ServiceImage, out newFileName);
                        serviceInfo.ServiceName = model.ServiceItems[i];
                        serviceInfo.ServiceImageName = model.ServiceImage.FileName + "-" + MetadataServices.GetDateWithoutSlash();
                        serviceInfo.CreatedAt = MetadataServices.GetCurrentDateTime();
                        serviceInfo.CreatedBy = "Admin";    //change to login username
                        serviceInfo.UpdatedAt = MetadataServices.GetCurrentDateTime();
                        serviceInfo.UpdatedBy = "Admin";    //change to login username
                        db.ServiceInfo.Add(serviceInfo);
                        db.SaveChanges();
                    }
                }
                return Json(new { }, JsonRequestBehavior.AllowGet);
            }
        }

        [Authorize]
        public ActionResult ServiceList()
        {
            using (var db = new PrimeTravelEntities())
            {
                List<ServiceModel> service = (from data in db.Service
                                              where data.IsDeleted == false
                                              select new ServiceModel()
                                              {
                                                  ServiceId = data.ServiceId,
                                                  ServiceType = data.ServiceType,
                                                  ServiceTitle = (from t in db.PrimeConfiguration
                                                                  where t.ConfigurationName == "ServiceTitle"
                                                                  select new PrimeConfigurationModel()
                                                                  {
                                                                      ConfigurationValue = t.ConfigurationValue
                                                                  }).FirstOrDefault(),
                                                  ServiceSubTitle = (from t in db.PrimeConfiguration
                                                                  where t.ConfigurationName == "ServiceSubTitle"
                                                                  select new PrimeConfigurationModel()
                                                                  {
                                                                      ConfigurationValue = t.ConfigurationValue
                                                                  }).FirstOrDefault(),
                                                  SIImage = (from i in db.ServiceInfo
                                                             where i.ServiceId == data.ServiceId
                                                             select new ServiceInfoModel()
                                                             {
                                                                 SImage = i.ServiceImage
                                                             }).FirstOrDefault(),
                                                  DateAdded = data.CreatedAt.ToString(),
                                                  CreatedAt = data.CreatedAt,
                                              }).ToList();
                foreach (var item in service)
                {
                    item.DateAdded = String.Format("{0:dd-MM-yyyy}", Convert.ToDateTime(item.DateAdded));
                }
                return View(service);
            }
        }

        [Authorize]
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult ChangeServiceTitle(string ServiceTitle, string ServiceSubTitle)
        {
            using (var db = new PrimeTravelEntities())
            {
                if (ModelState.IsValid)
                {
                    var findST = (from t in db.PrimeConfiguration
                                  where t.ConfigurationName == "ServiceTitle"
                                  select new PrimeConfigurationModel()
                                  {
                                      ConfigurationId = t.ConfigurationId
                                  }).SingleOrDefault();
                    var subTitle = (from t in db.PrimeConfiguration
                                  where t.ConfigurationName == "ServiceSubTitle"
                                  select new PrimeConfigurationModel()
                                  {
                                      ConfigurationId = t.ConfigurationId
                                  }).SingleOrDefault();
                    if (findST == null)
                    {
                        PrimeConfiguration con = new PrimeConfiguration();
                        con.ConfigurationName = "ServiceTitle";
                        con.ConfigurationValue = ServiceTitle;
                        con.CreatedAt = MetadataServices.GetCurrentDateTime();
                        con.CreatedBy = "Admin";    //change to login username
                        con.UpdatedAt = MetadataServices.GetCurrentDateTime();
                        con.UpdatedBy = "Admin";    //change to login username
                        db.PrimeConfiguration.Add(con);
                        db.SaveChanges();
                    }
                    else
                    {
                        PrimeConfiguration con = db.PrimeConfiguration.Find(findST.ConfigurationId);
                        con.ConfigurationValue = ServiceTitle;
                        con.UpdatedAt = MetadataServices.GetCurrentDateTime();
                        con.UpdatedBy = "Admin";    //change to login username
                        db.SaveChanges();
                    }

                    if (subTitle == null)
                    {
                        PrimeConfiguration con = new PrimeConfiguration();
                        con.ConfigurationName = "ServiceSubTitle";
                        con.ConfigurationValue = ServiceSubTitle;
                        con.CreatedAt = MetadataServices.GetCurrentDateTime();
                        con.CreatedBy = "Admin";
                        con.UpdatedAt = MetadataServices.GetCurrentDateTime();
                        con.UpdatedBy = "Admin";
                        db.PrimeConfiguration.Add(con);
                        db.SaveChanges();
                    }
                    else
                    {
                        PrimeConfiguration con = db.PrimeConfiguration.Find(subTitle.ConfigurationId);
                        con.ConfigurationValue = ServiceSubTitle;
                        con.UpdatedAt = MetadataServices.GetCurrentDateTime();
                        con.UpdatedBy = "Admin";    //change to login username
                        db.SaveChanges();
                    }

                }
                return RedirectToAction("ServiceList");
            }
        }

        [Authorize]
        public ActionResult EditService(int Id)
        {
            using (var db = new PrimeTravelEntities())
            {
                var service = (from item in db.Service
                               where item.ServiceId == Id
                               select new ServiceModel()
                               {
                                   ServiceId = item.ServiceId,
                                   ServiceType = item.ServiceType,
                                   ServiceInfos = (from u in db.ServiceInfo
                                                   where u.ServiceId == item.ServiceId && !u.IsDeleted
                                                   select new ServiceInfoModel()
                                                   {
                                                       ServiceInfoId = u.ServiceInfoId,
                                                       ServiceName = u.ServiceName,
                                                       SImage = u.ServiceImage
                                                   }).ToList(),
                                   ServiceViewMore = (from v in db.ServiceViewMore
                                                      where v.ServiceId == item.ServiceId
                                                      select new ServiceViewMoreModel()
                                                      {
                                                          ServiceViewMoreId = v.ServiceViewMoreId,
                                                          ServiceViewMoreImageName = v.ServiceImageName,
                                                          SVMImage = v.ServiceImage
                                                      }).FirstOrDefault(),
                               }).SingleOrDefault();

                return View(service);
            }
        }

        [Authorize]
        [HttpPost]
        public JsonResult EditService(ServiceModel model)
        {
            using (var db = new PrimeTravelEntities())
            {
                if (ModelState.IsValid)
                {
                    Service service = db.Service.Find(model.ServiceId);
                    service.ServiceType = model.ServiceType;
                    service.UpdatedAt = MetadataServices.GetCurrentDateTime();
                    service.UpdatedBy = "Admin";    //change to login username
                    db.SaveChanges();
                    ServiceViewMore serviceViewMore = db.ServiceViewMore.Find(model.ServiceViewMoreId);
                    if (model.ServiceViewMoreImage != null)
                    {
                        MetadataServices.DeleteFromCloud("service", serviceViewMore.ServiceImageName);
                        var newViewFileName = "";
                        serviceViewMore.ServiceImage = MetadataServices.UploadToCloud("service", model.ServiceViewMoreImage, out newViewFileName);
                        serviceViewMore.ServiceImageName = newViewFileName;
                    }

                    string getImageLink = "";
                    string getImageName = "";
                    string currentImageName = "";
                    string newFileName = "";
                    var currentServiceItems = (from c in db.ServiceInfo
                                               where c.ServiceId == service.ServiceId
                                               select new ServiceInfoModel()
                                               {
                                                   ServiceInfoId = c.ServiceInfoId
                                               }).ToList();
                    for (int main=0; main<currentServiceItems.Count(); main++)
                    {
                        ServiceInfo serviceInfo = db.ServiceInfo.Find(currentServiceItems[main].ServiceInfoId);
                        if (model.ServiceImage != null)
                        {
                            currentImageName = serviceInfo.ServiceImageName;
                            if (main == 0)
                            {
                                serviceInfo.ServiceImage = MetadataServices.UploadToCloud("service", model.ServiceImage, out newFileName);
                                getImageLink = serviceInfo.ServiceImage;
                                getImageName = serviceInfo.ServiceImageName;
                            }
                            else
                            {
                                serviceInfo.ServiceImage = getImageLink;
                            }
                            serviceInfo.ServiceImageName = newFileName;
                        }
                        else
                        {
                            getImageLink = serviceInfo.ServiceImage;
                            getImageName = serviceInfo.ServiceImageName;
                        }
                        serviceInfo.UpdatedAt = MetadataServices.GetCurrentDateTime();
                        serviceInfo.UpdatedBy = "Admin";
                        serviceInfo.IsDeleted = true;
                        db.SaveChanges();
                    }

                    for (int i = 0; i < model.OldServiceItems.Length; i++)
                    {
                        ServiceInfo serviceInfo = db.ServiceInfo.Find(Convert.ToInt32(model.OldServiceItemId[i]));
                        serviceInfo.ServiceName = model.OldServiceItems[i];
                        serviceInfo.IsDeleted = false;
                        db.SaveChanges();
                    }

                    if (model.ServiceImage != null)
                    {
                        MetadataServices.DeleteFromCloud("service", currentImageName);
                    }

                    if (model.ServiceItems != null)
                    {
                        for (int i = 0; i < model.ServiceItems.Length; i++)
                        {
                            ServiceInfo newServiceInfo = new ServiceInfo();
                            newServiceInfo.ServiceId = service.ServiceId;
                            newServiceInfo.ServiceImage = getImageLink;
                            newServiceInfo.ServiceName = model.ServiceItems[i];
                            newServiceInfo.ServiceImageName = getImageName;
                            newServiceInfo.CreatedAt = MetadataServices.GetCurrentDateTime();
                            newServiceInfo.CreatedBy = "Admin";    //change to login username
                            newServiceInfo.UpdatedAt = MetadataServices.GetCurrentDateTime();
                            newServiceInfo.UpdatedBy = "Admin";    //change to login username
                            db.ServiceInfo.Add(newServiceInfo);
                            db.SaveChanges();
                        }
                    }
                }
                return Json(new { }, JsonRequestBehavior.AllowGet);
            }
        }

        [Authorize]
        [HttpPost]
        public ActionResult DeleteService(int ServiceId)
        {
            using (var db = new PrimeTravelEntities())
            {
                if (ModelState.IsValid)
                {
                    Service service = db.Service.Find(ServiceId);
                    service.IsDeleted = true;
                    service.UpdatedAt = MetadataServices.GetCurrentDateTime();
                    service.UpdatedBy = "Admin";    //change to login username
                    db.SaveChanges();
                }
                return RedirectToAction("ServiceList");
            }
        }
    }

    public static class ModelStateHelper
    {
        public static IEnumerable Errors(this ModelStateDictionary modelState)
        {
            if (!modelState.IsValid)
            {
                return modelState.ToDictionary(kvp => kvp.Key,
                        kvp => kvp.Value.Errors
                            .Select(e => e.ErrorMessage).ToArray())
                    .Where(m => m.Value.Count() > 0);
            }
            return null;
        }
    }
}