using Demo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;

namespace Demo.Controllers
{
    public class AdminController : Controller
    {
        private readonly DB db;
        private readonly IWebHostEnvironment en;
        private readonly Helper hp;

        public AdminController(DB db, IWebHostEnvironment en, Helper hp)
        {
            this.db = db;
            this.en = en;
            this.hp = hp;
        }

        // POST: Admin/CreateAdmin
        [HttpPost]
        public IActionResult CreateAdmin(CreateAdminVM cavm)
        {
            if (ModelState.IsValid("Email") && db.Users.Any(u => u.Email == cavm.Email))
            {
                ModelState.AddModelError("Email", "Duplicated Email.");
            }

            if (ModelState.IsValid)
            {
                // Retrieve the last member and generate the next ID
                var lastAdmin = db.Admins.OrderByDescending(m => m.Id).FirstOrDefault();
                var newId = lastAdmin == null ? "A1" : $"A{int.Parse(lastAdmin.Id.Substring(1)) + 1}";

                // Insert member + save photo
                db.Admins.Add(new()
                {
                    Id = newId,
                    Email = cavm.Email,
                    Hash = hp.HashPassword(cavm.Password),
                    Name = cavm.Name,
                });

                db.SaveChanges();

                TempData["Info"] = "Admin Created Successfully.";
                return RedirectToAction("Admin", "Home");
            }

            return View();
        }

        //GET
        [Authorize]
        public IActionResult UpdateAdmin(UpdateAdminVM ua)
        {
            var m = db.Admins.Find(ua.Id);
            if (m == null)
            {
                return RedirectToAction("Admin", "Home");
            }
            var uavm = new UpdateAdminVM
            {
                Id = ua.Id,
                Email = m.Email,
                Name = m.Name,
            };

            return View(uavm);
        }

        //Post
        [Authorize]
        [HttpPost]
        public IActionResult UpdateAdmin(UpdateAdminVM ua, string id, string action)
        {
            if (action == "back")
            {
                return RedirectToAction("Admin", "Home");
            }

            var admin = db.Admins.Find(ua.Id);
            if (admin == null)
            {
                TempData["Info"] = "Admin not found.";
                return RedirectToAction("Index", "Home");
            }

            bool emailExists = db.Users.Any(u => u.Email == ua.Email && u.Id != id);
            if (emailExists)
            {
                ModelState.AddModelError("Email", "Duplicated Email.");
            }

            if (ModelState.IsValid)
            {

                admin.Name = ua.Name;
                admin.Email = ua.Email; 

                db.SaveChanges();
                TempData["Info"] = "Admin updated successfully.";
                return RedirectToAction("Admin", "Home");
            }

            return View(ua);
        }

        [Authorize]
        [HttpPost]
        public IActionResult DeleteAdmin(string? id)
        {
            var s = db.Admins.Find(id);

            if (s != null)
            {
                db.Admins.Remove(s);
                db.SaveChanges();

                if (Request.IsAjax())
                {
                    return Json(new { success = true, message = "Record deleted." });
                }

                TempData["Info"] = "Record deleted.";
            }

            // Return a fallback redirect if not using AJAX
            return Redirect(Request.Headers.Referer.ToString());
        }
    }

}