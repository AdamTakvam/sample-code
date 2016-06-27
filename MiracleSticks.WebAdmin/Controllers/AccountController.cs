using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Security;
using MiracleSticks.Model;
using MiracleSticks.WebAdmin.Models;

namespace MiracleSticks.WebAdmin.Controllers
{
    public class AccountController : Controller
    {

        //
        // GET: /Account/LogOn

        public ActionResult LogOn()
        {
            return View();
        }

        //
        // POST: /Account/LogOn

        [HttpPost]
        public ActionResult LogOn(LogOnModel model, string returnUrl)
        {
            if (ModelState.IsValid)
            {
                if (ValidateUser(model.UserName, model.Password))
                {
                    FormsAuthentication.SetAuthCookie(model.UserName, model.RememberMe);
                    if (Url.IsLocalUrl(returnUrl) && returnUrl.Length > 1 && returnUrl.StartsWith("/")
                        && !returnUrl.StartsWith("//") && !returnUrl.StartsWith("/\\"))
                    {
                        return Redirect(returnUrl);
                    }
                    else
                    {
                        return RedirectToAction("Index", "Home");
                    }
                }
                else
                {
                    ModelState.AddModelError("", "The user name or password provided is incorrect.");
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/LogOff

        public ActionResult LogOff()
        {
            FormsAuthentication.SignOut();

            return RedirectToAction("Index", "Home");
        }

        //
        // GET: /Account/Register

        public ActionResult Register()
        {
            return View();
        }

        //
        // POST: /Account/Register

        [HttpPost]
        public ActionResult Register(RegisterModel model)
        {
            if (ModelState.IsValid)
            {
                // Attempt to register the user
                MembershipCreateStatus createStatus = CreateUser(model.UserName, model.Password, model.FirstName, model.LastName, model.Administrator, model.Comments);

                if (createStatus == MembershipCreateStatus.Success)
                {
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    ModelState.AddModelError("", ErrorCodeToString(createStatus));
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/ChangePassword

        [Authorize]
        public ActionResult ChangePassword()
        {
            return View();
        }

        //
        // POST: /Account/ChangePassword

        [Authorize]
        [HttpPost]
        public ActionResult ChangePassword(ChangePasswordModel model)
        {
            if (ModelState.IsValid)
            {
                // ChangePassword will throw an exception rather
                // than return false in certain failure scenarios.
                bool changePasswordSucceeded = false;

                try
                {
                    DataContext dataContext = DataModel.CreateContext();
                    UserAccount account = dataContext.Accounts.FirstOrDefault(x => x.UserName == User.Identity.Name);
                    if (account.PasswordHash == GetPasswordHash(User.Identity.Name, model.OldPassword))
                    {
                        account.PasswordHash = GetPasswordHash(User.Identity.Name, model.NewPassword);
                        dataContext.SaveChanges();
                        changePasswordSucceeded = true;
                    }
                }
                catch { }

                if (changePasswordSucceeded)
                {
                    return RedirectToAction("ChangePasswordSuccess");
                }
                else
                {
                    ModelState.AddModelError("", "The current password is incorrect or the new password is invalid.");
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/ChangePasswordSuccess

        public ActionResult ChangePasswordSuccess()
        {
            return View();
        }

        #region Status Codes
        private static string ErrorCodeToString(MembershipCreateStatus createStatus)
        {
            // See http://go.microsoft.com/fwlink/?LinkID=177550 for
            // a full list of status codes.
            switch (createStatus)
            {
                case MembershipCreateStatus.DuplicateUserName:
                    return "User name already exists. Please enter a different user name.";

                case MembershipCreateStatus.DuplicateEmail:
                    return "A user name for that e-mail address already exists. Please enter a different e-mail address.";

                case MembershipCreateStatus.InvalidPassword:
                    return "The password provided is invalid. Please enter a valid password value.";

                case MembershipCreateStatus.InvalidEmail:
                    return "The e-mail address provided is invalid. Please check the value and try again.";

                case MembershipCreateStatus.InvalidAnswer:
                    return "The password retrieval answer provided is invalid. Please check the value and try again.";

                case MembershipCreateStatus.InvalidQuestion:
                    return "The password retrieval question provided is invalid. Please check the value and try again.";

                case MembershipCreateStatus.InvalidUserName:
                    return "The user name provided is invalid. Please check the value and try again.";

                case MembershipCreateStatus.ProviderError:
                    return "The authentication provider returned an error. Please verify your entry and try again. If the problem persists, please contact your system administrator.";

                case MembershipCreateStatus.UserRejected:
                    return "The user creation request has been canceled. Please verify your entry and try again. If the problem persists, please contact your system administrator.";

                default:
                    return "An unknown error occurred. Please verify your entry and try again. If the problem persists, please contact your system administrator.";
            }
        }
        #endregion

        #region Private helper methods

        private MembershipCreateStatus CreateUser(string userName, string password, string firstName, string lastName, bool administrator, string comments)
        {
            if (String.IsNullOrEmpty(userName))
                return MembershipCreateStatus.InvalidUserName;

            if (password == null || password.Length < 6)
                return MembershipCreateStatus.InvalidPassword;

            if (String.IsNullOrEmpty(firstName) || String.IsNullOrEmpty(lastName))
                return MembershipCreateStatus.UserRejected;

            DataContext dataContext = DataModel.CreateContext();
            UserAccount account = dataContext.Accounts.FirstOrDefault(x => x.UserName == userName);
            if (account != null)
                return MembershipCreateStatus.DuplicateUserName;

            UserAccount acct = new UserAccount()
            {
                UserName = userName,
                FirstName = firstName,
                LastName = lastName,
                PasswordHash = GetPasswordHash(userName, password),
                Administrator = administrator,
                Comments = comments,
                GroupID = CreateNewGroupId(),
                Created = DateTime.Now
            };
            
            dataContext.Accounts.Add(acct);
            dataContext.SaveChanges();

            return MembershipCreateStatus.Success;
        }

        private bool ValidateUser(string username, string password)
        {
            DataContext dataContext = DataModel.CreateContext();
            UserAccount account = dataContext.Accounts.FirstOrDefault(x => x.UserName == username);
            if (account != null && account.Administrator)
            {
                return GetPasswordHash(username, password) == account.PasswordHash;
            }
            return false;
        }

        private static string GetPasswordHash(string username, string password)
        {
            string saltPass = password + username;
            SHA1CryptoServiceProvider crypto = new SHA1CryptoServiceProvider();
            byte[] passwordHash = crypto.ComputeHash(System.Text.Encoding.Default.GetBytes(saltPass));
            return Convert.ToBase64String(passwordHash);
        }

        private string CreateNewGroupId()
        {
            DataContext dataContext = DataModel.CreateContext();

            string newGroupID = Guid.NewGuid().ToString();
            bool groupIdInUse = true;
            while (groupIdInUse)
            {
                var newGroupIdCopy = newGroupID;  // Google: "Access to modified closure"
                UserAccount account = dataContext.Accounts.FirstOrDefault(x => x.GroupID == newGroupIdCopy);
                if (account == null)
                    groupIdInUse = false;
                else
                    newGroupID = Guid.NewGuid().ToString();
            }
            return newGroupID;
        }

        #endregion
    }
}
