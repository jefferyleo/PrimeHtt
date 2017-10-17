using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
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
            using (var db = new PrimetravelEntities())
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
                        DateTime.Now.AddMinutes(20), // expires
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
            return RedirectToAction("Login","Admin");
        }

        public string CreatePassword(string password)
        {
            var generatePassword = PasswordHash.CreateHash(password);
            return generatePassword;
        }
    }
}