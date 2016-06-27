using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Cryptography;
using System.Web;
using System.Data.Entity;
using System.Web.Mvc;
using MiracleSticks.Model;
using MiracleSticks.WebAdmin.Models;

namespace MiracleSticks.WebAdmin.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            if (Request.IsAuthenticated)
                return View();
            else
                return RedirectToAction("LogOn", "Account");
        }

        public ActionResult ListUsers()
        {
            if (!Request.IsAuthenticated)
                return new HttpStatusCodeResult(401);

            DataContext dataContext = DataModel.CreateContext();
            var userList = dataContext.Accounts.ToList();
            return View(userList);
        }

        public ActionResult ListAdminUsers()
        {
            if (!Request.IsAuthenticated)
                return new HttpStatusCodeResult(401);

            DataContext dataContext = DataModel.CreateContext();
            var userList = dataContext.Accounts.Where(x => x.Administrator).ToList();
            return View(userList);
        }

        public ActionResult DeleteUser(string userName)
        {
            if (!Request.IsAuthenticated)
                return new HttpStatusCodeResult(401);

            DataContext dataContext = DataModel.CreateContext();
            var account = dataContext.Accounts.Include(x => x.Registrations).FirstOrDefault(x => x.UserName == userName);
            if(account != null)
            {
                dataContext.Accounts.Remove(account);
                dataContext.SaveChanges();
            }
            return RedirectToAction("ListUsers");
        }

        public ActionResult ListRegistrations(string userName)
        {
            if (!Request.IsAuthenticated)
                return new HttpStatusCodeResult(401);

            ViewBag.UserName = userName;
            List<ServerEndPoint> registrations = new List<ServerEndPoint>();

            DataContext dataContext = DataModel.CreateContext();
            var account = dataContext.Accounts.Include(x => x.Registrations).FirstOrDefault(x => x.UserName == userName);
            if (account != null)
            {
                registrations = account.Registrations;
            }
            
            return View(registrations);
        }

        public ActionResult DeleteRegistration(string userName, int regId)
        {
            if (!Request.IsAuthenticated)
                return new HttpStatusCodeResult(401);

            DataContext dataContext = DataModel.CreateContext();
            var account = dataContext.Accounts.Include(x => x.Registrations).FirstOrDefault(x => x.UserName == userName);
            if (account != null)
            {
                ServerEndPoint registration = account.Registrations.FirstOrDefault(x => x.Id == regId);
                if(registration != null)
                {
                    account.Registrations.Remove(registration);
                    dataContext.SaveChanges();
                }
            }

            return RedirectToAction("ListRegistrations", new { UserName = userName });
        }
    }
}
