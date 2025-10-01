using Demo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

        public IActionResult CreateMember()
        {

            return View();
        }

        // POST: Member/CreateMember
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public IActionResult CreateMember(RegisterVM vm)
        {
            // Email duplication validation
            if (db.Users.Any(u => u.Email == vm.Email))
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
                var lastMember = db.Members
                    .Where(m => m.Id.StartsWith("M"))
                    .OrderByDescending(m => m.Id)
                    .FirstOrDefault();

                int newIdNumber = lastMember == null ? 1 : int.Parse(lastMember.Id.Substring(1)) + 1;
                string newId = $"M{newIdNumber}";

                while (db.Members.Any(m => m.Id == newId))
                {
                    newIdNumber++;
                    newId = $"M{newIdNumber}";
                }
                // Insert member + save photo
                db.Members.Add(new()
                {
                    Id = newId,
                    Email = vm.Email,
                    Hash = hp.HashPassword(vm.Password),
                    Name = vm.Name,
                    Address = vm.Address,
                    PhotoURL = hp.SavePhoto(vm.Photo, "photos"),
                });

                db.SaveChanges();

                TempData["Info"] = "Member Created Successfully.";
                return RedirectToAction("Member", "Home");
            }

            // If validation fails, re-render the view with validation messages
            return View();
        }


        [Authorize(Roles = "Admin")]
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
                Address = m.Address,
            };

            return View(nvm);
        }

        [Authorize(Roles = "Admin")]
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

                member.Name = um.Name;
                member.Email = um.Email; 
                member.Address = um.Address;

                db.SaveChanges();
                TempData["Info"] = "Member updated successfully.";
                return RedirectToAction("Member", "Home");
            }

            return View(um);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public IActionResult DeleteMember(string? id)
        {
            var member = db.Members.Find(id);

            if (member != null)
            {
                db.Members.Remove(member);
                db.SaveChanges();
                if (!Request.IsAjax())
                {
                    TempData["Info"] = "Member Deleted!";
                }
            }
            return Redirect(Request.Headers.Referer.ToString());
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public IActionResult BlockMember(string id)
        {
            var member = db.Members.Find(id);
            if (member != null)
            {
                member.IsBlocked = true;
                db.SaveChanges();

                TempData["Info"] = $"[MemberID: {id}] Blocked Successfully.";

            }
            return RedirectToAction("Member", "Home");

        }



        [Authorize(Roles = "Admin")]
        [HttpPost]
        public IActionResult UnblockMember(string id)
        {
            var member = db.Members.Find(id);

            if (member != null)
            {
                member.IsBlocked = false;
                db.SaveChanges();

                TempData["Info"] = $"[MemberID: {id}] Unblocked Successfully.";

            }
            return RedirectToAction("Member", "Home");


        }
    }


}
