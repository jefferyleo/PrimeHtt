using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using GoogleMaps.LocationServices;
using PrimeHtt.Helper.Authorization;
using PrimeHtt.Helper.Services;
using PrimeHtt.Models.ViewModel;
using PrimeHtt.Models;

namespace PrimeHtt.Controllers
{
    public class AdminController : Controller
    {
        // GET: Admin
        [Authorize]
        public ActionResult Index()
        {
            return View();
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

                    return RedirectToAction("Index", "Admin");

                }
                else
                {
                    ViewBag.LoginStatus = "Your Username or Password is wrong.";
                    return View(model);
                }
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
                return View(contactUs);
            }
        }

        [Authorize]
        [HttpPost]
        public ActionResult ContactUs(ContactUsViewModel model)
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
                    var locationService = new GoogleLocationService();
                    var point = locationService.GetLatLongFromAddress(model.ContactAddress);
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
                    if (point == null)
                    {
                        ModelState.AddModelError("ContactAddress", "" + model.ContactAddress + " not found.");
                        return View(error);
                    }
                    if (contactUs != null) //there's an exist contact us data in db
                    {
                        contactUs.ContactName = model.ContactName;
                        contactUs.ContactAddress = model.ContactAddress;
                        contactUs.ContactLatitude = point.Latitude;
                        contactUs.ContactLongitude = point.Longitude;
                        contactUs.CreatedBy = contactUs.CreatedBy;
                        contactUs.CreatedAt = contactUs.CreatedAt;
                        contactUs.UpdatedBy = formsAuthentication.Name ?? "";
                        contactUs.UpdatedAt = MetadataServices.GetCurrentDateTime();
                        db.SaveChanges();
                    }
                    else
                    {
                        var newContact = new Contact()
                        {
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
                    return View(result);
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
                    return View(contact);
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
                if (model.Latitude == 0 && model.Longitude == 0)
                {
                    var point = new MapPoint();
                    try
                    {
                        point = locationService.GetLatLongFromAddress(model.LocationName);
                    }
                    catch (Exception e)
                    {
                        ModelState.AddModelError("LocationName", "" + model.LocationName + " not found.");
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
                    location.CountryName = address.Country;
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

        [Authorize]
        [HttpPost]
        public ActionResult AddGallery(AddGalleryViewModel model)
        {
            using (var db = new PrimeTravelEntities())
            {
                if (!ModelState.IsValid)
                {
                    return View(new AddGalleryViewModel()
                    {

                        LocationName = (from e in db.Experience
                                        select new { e.LocationId, e.LocationName }).ToList().Select(e => new SelectListItem
                                        {
                                            Text = e.LocationName,
                                            Value = e.LocationId.ToString(),
                                        }).ToList(),
                        LocationId = model.LocationId,
                    });
                }
                var formsAuthentication = HttpContext.Request.Cookies[FormsAuthentication.FormsCookieName] != null
                    ? FormsAuthentication.Decrypt(
                        HttpContext.Request.Cookies[FormsAuthentication.FormsCookieName].Value)
                    : null;
                if (!model.LocationContentType) //image
                {
                    string newFileName = "";
                    var galleryImage = MetadataServices.UploadToCloud("gallery", model.ContentImage, out newFileName);
                    var IsGalleryExist = db.ExperienceDetail.Any(e => e.LocationId == model.LocationId && !e.LocationContentType);
                    if (IsGalleryExist)
                    {
                        var index = db.ExperienceDetail.Where(e => e.LocationId == model.LocationId && !e.LocationContentType).OrderByDescending(e => e.LocationIndex).Select(e => e.LocationIndex).FirstOrDefault();
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
                    return RedirectToAction("Gallery", "Admin", new { id = model.LocationId });
                }
                else //video
                {
                    if (model.ContentType == "")
                    {
                        ModelState.AddModelError("LocationContent", "Please Enter Video Link.");
                        return View(new AddGalleryViewModel()
                        {

                            LocationName = (from e in db.Experience
                                            select new { e.LocationId, e.LocationName }).ToList().Select(e => new SelectListItem
                                            {
                                                Text = e.LocationName,
                                                Value = e.LocationId.ToString(),
                                            }).ToList(),
                            LocationId = model.LocationId,
                        });
                    }
                    var IsGalleryExist = db.ExperienceDetail.Any(e => e.LocationId == model.LocationId && e.LocationContentType);
                    if (IsGalleryExist)
                    {
                        var index = db.ExperienceDetail.Where(e => e.LocationId == model.LocationId && !e.LocationContentType).OrderByDescending(e => e.LocationIndex).Select(e => e.LocationIndex).FirstOrDefault();
                        var gallery = new ExperienceDetail()
                        {
                            LocationId = model.LocationId,
                            LocationContentType = model.LocationContentType,
                            LocationContent = model.LocationContent,
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
                            LocationContent = model.LocationContent,
                            LocationIndex = 1,
                            CreatedBy = formsAuthentication.Name ?? "",
                            CreatedAt = MetadataServices.GetCurrentDateTime(),
                            UpdatedBy = formsAuthentication.Name ?? "",
                            UpdatedAt = MetadataServices.GetCurrentDateTime(),
                        };
                        db.ExperienceDetail.Add(gallery);
                    }
                    db.SaveChanges();
                    return RedirectToAction("Gallery", "Admin", new { id = model.LocationId });
                }
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
                MetadataServices.DeleteFromCloud("gallery", gallery.LocationImageName);
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

        //YUAN YEE
        public ActionResult AddBanner()
        {
            return View();
        }

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

        public ActionResult AddSocialNetwork()
        {
            return View();
        }

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

        public ActionResult AddService()
        {
            return View();
        }

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
                    db.ServiceInfo.Add(serviceInfo);
                    db.SaveChanges();

                    for (int i = 0; i < model.ServiceItems.Length; i++)
                    {
                        serviceInfo.ServiceId = service.ServiceId;
                        serviceInfo.ServiceImage = MetadataServices.UploadToCloud("service", model.ServiceImage, out newFileName);
                        serviceInfo.ServiceName = model.ServiceItems[i];
                        serviceInfo.ServiceImageName = model.ServiceImage.FileName + "-" + MetadataServices.GetDateWithoutSlash();
                        db.ServiceInfo.Add(serviceInfo);
                        db.SaveChanges();
                    }
                }
                return Json(new { }, JsonRequestBehavior.AllowGet);
            }
        }

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

        [HttpPost]
        public ActionResult ChangeServiceTitle(string ServiceTitle)
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

                }
                return RedirectToAction("ServiceList");
            }
        }

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
                                                   where u.ServiceId == item.ServiceId
                                                   select new ServiceInfoModel()
                                                   {
                                                       ServiceInfoId = u.ServiceInfoId,
                                                       ServiceName = u.ServiceName,
                                                       SImage = u.ServiceImage
                                                   }).ToList(),
                               }).SingleOrDefault();

                return View(service);
            }
        }

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
                    string getImageLink = "";
                    string getImageName = "";
                    for (int i = 0; i < model.OldServiceItems.Length; i++)
                    {
                        ServiceInfo serviceInfo = db.ServiceInfo.Find(Convert.ToInt32(model.OldServiceItemId[i]));
                        if (model.ServiceImage != null)
                        {
                            MetadataServices.DeleteFromCloud("service", serviceInfo.ServiceImageName);
                            var newFileName = "";
                            serviceInfo.ServiceImage = MetadataServices.UploadToCloud("service", model.ServiceImage, out newFileName);
                            serviceInfo.ServiceImageName = model.ServiceImage.FileName + "-" + MetadataServices.GetDateWithoutSlash();
                            getImageLink = serviceInfo.ServiceImage;
                            getImageName = serviceInfo.ServiceImageName;
                        }
                        else
                        {
                            getImageLink = serviceInfo.ServiceImage;
                            getImageName = serviceInfo.ServiceImageName;
                        }
                        serviceInfo.ServiceName = model.OldServiceItems[i];
                        db.SaveChanges();
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
                            db.ServiceInfo.Add(newServiceInfo);
                            db.SaveChanges();
                        }
                    }
                }
                return Json(new { }, JsonRequestBehavior.AllowGet);
            }
        }

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