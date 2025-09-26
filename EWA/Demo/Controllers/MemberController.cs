using Demo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;

namespace Demo.Controllers
{
    public class MemberController : Controller
    {
        private readonly DB db;
        private readonly IWebHostEnvironment en;
        private readonly Helper hp;

        public MemberController(DB db, IWebHostEnvironment en, Helper hp)
        {
            this.db = db;
            this.en = en;
            this.hp = hp;
        }

        // POST: Member/CreateMember
        [HttpPost]
        public IActionResult CreateMember(RegisterVM vm)
        {
            if (ModelState.IsValid("Email") && db.Users.Any(u => u.Email == vm.Email))
            {
                ModelState.AddModelError("Email", "Duplicated Email.");
            }

            if (ModelState.IsValid("Photo"))
            {
                var err = hp.ValidatePhoto(vm.Photo);
                if (err != "") ModelState.AddModelError("Photo", err);
            }

            if (ModelState.IsValid)
            {
                // Retrieve the last member and generate the next ID
                var lastMember = db.Members.OrderByDescending(m => m.Id).FirstOrDefault();
                var newId = lastMember == null ? "M1" : $"M{int.Parse(lastMember.Id.Substring(1)) + 1}";

                // Insert member + save photo
                db.Members.Add(new()
                {
                    Id = newId,
                    Email = vm.Email,
                    Hash = hp.HashPassword(vm.Password),
                    Name = vm.Name,
                    PhotoURL = hp.SavePhoto(vm.Photo, "photos"),
                });

                db.SaveChanges();

                TempData["Info"] = "Member Created Successfully.";
                return RedirectToAction("Member", "Home");
            }

            return View();
        }

        [Authorize]
        public IActionResult UpdateMember(UpdateMemberVM um)
        {
            var m = db.Members.Find(um.Id);
            if (m == null)
            {
                return RedirectToAction("Index");
            }
            var nvm = new UpdateMemberVM
            {
                Id = um.Id,
                Email = m.Email,
                Name = m.Name,
            };

            return View(nvm);
        }

        [Authorize]
        [HttpPost]
        public IActionResult UpdateMember(UpdateMemberVM um, string id, string action)
        {
            if (action == "back")
            {
                return RedirectToAction("Member", "Home");
            }

            var member = db.Members.Find(um.Id);
            if (member == null)
            {
                TempData["Info"] = "Member not found.";
                return RedirectToAction("Index", "Home");
            }

            bool emailExists = db.Users.Any(u => u.Email == um.Email && u.Id != id);
            if (emailExists)
            {
                ModelState.AddModelError("Email", "Duplicated Email.");
            }

            if (ModelState.IsValid)
            {

                // Update member information
                member.Name = um.Name;
                member.Email = um.Email; // Update email

                db.SaveChanges();
                TempData["Info"] = "Member updated successfully.";
                return RedirectToAction("Member", "Home");
            }

            return View(um);
        }

        [HttpPost]
        public IActionResult DeleteMember(string? id)
        {
            var s = db.Members.Find(id);

            if (s != null)
            {
                db.Members.Remove(s);
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
