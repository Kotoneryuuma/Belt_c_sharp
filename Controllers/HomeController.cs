using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using belt.Models;
using Microsoft.AspNetCore.Http; 
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace belt.Controllers
{
    public class HomeController : Controller
    {
        private MyContext dbContext;

        public HomeController(MyContext context)
        {
            dbContext = context;
        }

        [Route("")]
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost("new")]
        public IActionResult UserToDB(RegisterForm regData)
        {

            if ( dbContext.Users.Any(u => u.Email == regData.Email))
                ModelState.AddModelError("Email", "Email already in use");
            if (!ModelState.IsValid)
                return View("Index");

            User newUser = new User();
            newUser.UserName = regData.UserName;
            newUser.Email = regData.Email;
            var Hasher = new PasswordHasher<RegisterForm>();
            newUser.Password = Hasher.HashPassword(regData, regData.Password);
            dbContext.Add(newUser);
            dbContext.SaveChanges();
            System.Console.WriteLine($"MADE IT TO USERTODB CONTROLLER! New user id = {newUser.UserID}");

            HttpContext.Session.SetInt32("user", newUser.UserID);
            return RedirectToAction("dashboard");
        }

        [HttpPost("login/process")]
        public IActionResult LogInUser(LoginForm loginData)
        {
            var match = dbContext.Users
                .FirstOrDefault(u => u.Email == loginData.Email);
            if (match == null)
                ModelState.AddModelError("Email", "Invalid email or password");
            var Hasher = new PasswordHasher<LoginForm>();
            var result = Hasher
                .VerifyHashedPassword(
                    loginData, match.Password, loginData.Password
                );
            if (result == 0)
                ModelState.AddModelError("Email", "Invalid email or password");
            if (!ModelState.IsValid)
                return View("Index");
            
            int userID = match.UserID;
            HttpContext.Session.SetInt32("user", userID);
            return RedirectToAction("dashboard");
        }

        [Route("dashboard")]
        [HttpGet]
        public IActionResult Dashboard()
        {
            // データベースにアクセス
            if (!HttpContext.Session.Keys.Contains("user"))
                return Redirect("/");
            List<DojoActivity> AllActivitys = dbContext.DojoActivitys
                // Wedding List<Associate> Guests
                .Include(a => a.Guests)
                // Associate User User 
                .ThenInclude(a => a.User) 
                // creator を 認識するために　User model からCreator　をとってくる
                .Include(a => a.Creator)
                .ToList();

            // List<User> AllAUsers = dbContext.Users
            //     // Wedding List<Associate> Guests
            //     .Include(a => a.ActivitystoAttend)
            //     // Associate User User 
            //     .ThenInclude(a => a.User)
            //     .ToList();


            // ViewModel に db からの情報を渡す
            List<Display> AllDisplays = new List<Display>();
            User thisUser = dbContext.Users
                .Include(e => e.ActivitystoAttend)
                .ThenInclude(att => att.DojoActivity)
                .SingleOrDefault(u =>
                    u.UserID == HttpContext.Session.GetInt32("user"));
            foreach (DojoActivity e in AllActivitys)
            {
                Display thisDisaplay = new Display();
                thisDisaplay.DisplayID = e.DojoActivityID;
                thisDisaplay.ActivityName = e.DojoActivityName;
                thisDisaplay.Date = e.Date;
                thisDisaplay.Time = e.Time;
                thisDisaplay.Duration = e.Duration;
                
                thisDisaplay.UserName = e.Creator.UserName;
                thisDisaplay.Guests = new List<User>();
                if (e.Creator == thisUser)
                    thisDisaplay.IsHosting = true;
                else
                    thisDisaplay.IsHosting = false;
                bool found = false;
                // Wedding の中の　public List<Associate> Guests {get;set;}
                foreach (Associate a in e.Guests)
                {
                    thisDisaplay.Guests.Add(a.User);
                }
                // Wedding の中の　public User Creater {get;set;}
                foreach (User g in thisDisaplay.Guests)
                {
                    if (g.UserID == thisUser.UserID)
                    {
                        thisDisaplay.IsAttending = true;
                        found = true;
                    }
                }
                if (!found)
                    thisDisaplay.IsAttending = false;
                AllDisplays.Add(thisDisaplay);
            }
            return View(AllDisplays);
        }

        [Route("newactivity")]
        [HttpGet]
        public IActionResult newaAtivity()
        {
            return View("NewACTIVITY");
        }

        [Route("ActivityToDB")]
        [HttpPost]
        public IActionResult DisplayToDB(ActivityForm FormData)
        {
            if (!HttpContext.Session.Keys.Contains("user"))
                return RedirectToAction("Logout","User");
            if (!ModelState.IsValid)
                return View("NewActivity");
            User host = dbContext.Users
                .SingleOrDefault(u =>
                    u.UserID == HttpContext.Session.GetInt32("user"));       
        
            DojoActivity newActivity = new DojoActivity();
            //     WedderOne = FormData.WedderOne,
            //     WedderTwo = FormData.WedderTwo,
            //     Date = FormData.Date,
            //     Address = FormData.Address
            // };

            newActivity.DojoActivityName = FormData.Title;
            newActivity.Time = FormData.Time;
            newActivity.Date = FormData.Date;
            newActivity.Duration = FormData.Duration;
            newActivity.Description = FormData.Description;
            newActivity.CreatedAt = DateTime.Now;
            newActivity.UpdatedAt = DateTime.Now;
            newActivity.UserID = host.UserID;
            dbContext.DojoActivitys.Add(newActivity);
            dbContext.SaveChanges();
            
            return RedirectToAction("Dashboard");
        }

        [HttpGet("join/{id}")]
        public IActionResult JoinEvent(int id)
        {
            if (!HttpContext.Session.Keys.Contains("user"))
                return RedirectToAction("Logout");
            User thisUser = dbContext.Users
                .SingleOrDefault(u =>
                    u.UserID == HttpContext.Session.GetInt32("user"));
            DojoActivity thisActivity = dbContext.DojoActivitys
                .SingleOrDefault(e => e.DojoActivityID == id);
            Associate newAtt = new Associate();
            newAtt.DojoActivity= thisActivity;
            newAtt.User = thisUser;
            dbContext.Assosiates.Add(newAtt);
            dbContext.SaveChanges();
            return RedirectToAction("Dashboard");
        }

        [HttpGet("logout")]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return Redirect("/");
        }

        [HttpGet("leave/{id}")]
        public IActionResult LeaveEvent(int id)
        {
            if (!HttpContext.Session.Keys.Contains("user"))
                return RedirectToAction("Logout", "User");
            Associate cancellation = dbContext.Assosiates
                .SingleOrDefault(a =>
                    a.UserID == HttpContext.Session.GetInt32("user")
                        && a.DojoActivityID == id);
            dbContext.Assosiates.Remove(cancellation);
            dbContext.SaveChanges();
            return RedirectToAction("Dashboard");
        }

        [HttpGet("delete/{id}")]
        public IActionResult DestroyActivity(int id)
        {
            if (!HttpContext.Session.Keys.Contains("user"))
                return RedirectToAction("Logout", "User");
            DojoActivity thisActivity = dbContext.DojoActivitys
                .SingleOrDefault(e => e.DojoActivityID == id);
            dbContext.DojoActivitys.Remove(thisActivity);
            dbContext.SaveChanges();
            return RedirectToAction("Dashboard");
        }

        [HttpGet("activity/{id}")]
        public IActionResult ActivityInfo(int id)
        {
            if (!HttpContext.Session.Keys.Contains("user"))
                return RedirectToAction("Logout", "User");
        
            DojoActivity thisActivity = dbContext.DojoActivitys
                .Include(e => e.Guests)
                .ThenInclude(att => att.User)
                .Include(a => a.Creator)
                .SingleOrDefault(e => e.DojoActivityID == id);
            Display thisDisplay = new Display();
            thisDisplay.ActivityName = thisActivity.DojoActivityName;
            thisDisplay.UserName = thisActivity.Creator.UserName;
            thisDisplay.Description = thisActivity.Description;
            thisDisplay.Guests = new List<User>();
            foreach (Associate g in thisActivity.Guests)
                thisDisplay.Guests.Add(g.User);

            return View("ShowActivity", thisDisplay);
        }

        



    }
}
