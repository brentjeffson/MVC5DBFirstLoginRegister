using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using MVC5DBFirstLoginRegister.Models;
using MVC5DBFirstLoginRegister.ViewModels;

namespace MVC5DBFirstLoginRegister.Controllers
{
    public class UserController : Controller
    {
        // Dashboard
        [Authorize]
        public ActionResult Dashboard()
        {
            using (MyDatabaseEntities dbe = new MyDatabaseEntities())
            {
                var res = dbe.Users.Where(user => !user.IsEmailVerified);
                if (res != null)
                {
                    var viewModel = new DashboardViewModel
                    {
                        IsLoggedIn = true,
                        Users = res.ToList()
                    };
                    return View(viewModel);
                }
            }
            return View();
        }
        
        // Registration
        [HttpGet]
        public ActionResult Registration()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Registration([Bind(Exclude = "UserID,IsEmailVerified,ActivationCode")] RegistrationViewModel registrationViewModel)
        {
            bool status = false;
            string message = "";
            #region Validation
            if (ModelState.IsValid)
            {
                # region Email duplicate 
                var emailExists = IsEmailExists(registrationViewModel.User.Email);
                if (emailExists)
                {
                    ModelState.AddModelError("EmailExist", "Email already exists");
                    return View(registrationViewModel);
                }
                #endregion

                #region Generate activation code
                registrationViewModel.User.ActivationCode = Guid.NewGuid();
                #endregion

                #region Password hashing
                registrationViewModel.User.Password = Crypto.Hash(registrationViewModel.User.Password);
                registrationViewModel.User.ConfirmPassword = Crypto.Hash(registrationViewModel.User.ConfirmPassword);
                #endregion

                #region Save user
                using (MyDatabaseEntities dbe = new MyDatabaseEntities())
                {
                    dbe.Users.Add(registrationViewModel.User);
                    dbe.SaveChanges();
                }
                #endregion

                status = true;
                message = $"Registration successfull, activation link sent to {registrationViewModel.User.Email}";
            }
            else
            {
                message = "Invalid request";
            }

            ViewBag.Message = message;
            ViewBag.Status = status;
            
            return View(registrationViewModel);
            #endregion
        }

        // Email Verification
        [HttpGet]
        public ActionResult VerifyAcount(string code)
        {
            bool status = false;
            using (MyDatabaseEntities dbe = new MyDatabaseEntities())
            {
                var res = dbe.Users.Where(user => user.ActivationCode == new Guid(code)).FirstOrDefault();
                if (res != null)
                {
                    res.IsEmailVerified = true;
                    res.ConfirmPassword = res.Password;
                    dbe.SaveChanges();
                    status = true;
                }
                else
                {
                    ViewBag.Message = "Invalid request";
                }
            }

            ViewBag.Status = status;
            return View();
        }

        // Login
        [HttpGet]
        public ActionResult Login()
        {
            
            return View();
        }

        [HttpPost]
        public ActionResult Login(LoginViewModel loginViewModel, string returnURL)
        {
            string message = "";
            using (MyDatabaseEntities dbe = new MyDatabaseEntities())
            {
                var res = dbe.Users.Where(user => user.Email == loginViewModel.Email).FirstOrDefault();
                if (res != null)
                {
                    if (string.Compare(Crypto.Hash(loginViewModel.Password), res.Password) == 0)
                    {
                        int timeout = loginViewModel.RememberMe ? 525600 : 1;
                        var ticket = new FormsAuthenticationTicket(loginViewModel.Email, loginViewModel.RememberMe, timeout);
                        string encrypted = FormsAuthentication.Encrypt(ticket);
                        var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, encrypted);
                        cookie.Expires = DateTime.Now.AddMinutes(timeout);
                        cookie.HttpOnly = true;
                        Response.Cookies.Add(cookie);

                        if (Url.IsLocalUrl(returnURL))
                        {
                            return RedirectToAction("Dashboard", "User");
                        }
                        else
                        {
                            return RedirectToAction("Dashboard", "User");
                        }
                    }
                    else
                    {
                        message = "Invalid credential";
                    }
                }
                else
                {
                    message = "Invalid credentials";
                }
            }

            ViewBag.Message = message;
            return View();
        }

        // Logout
        [Authorize]
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            return RedirectToAction("Login", "User");
        }

        // Helper functions
        [NonAction]
        private bool IsEmailExists(string email)
        {
            using (MyDatabaseEntities dbe = new MyDatabaseEntities())
            {
                var res = dbe.Users.Where(user => user.Email == email).FirstOrDefault();
                return res != null;
            }
        }
    }
}