using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mail;

namespace Demo.Controllers;

public class AccountController : Controller
{
    private readonly DB db;
    private readonly IWebHostEnvironment en;
    private readonly Helper hp;

    public AccountController(DB db, IWebHostEnvironment en, Helper hp)
    {
        this.db = db;
        this.en = en;
        this.hp = hp;
    }

    // GET: Account/Login
    public IActionResult Login()
    {
        return View();
    }
    // POST: Account/Login
    [HttpPost]
    public IActionResult Login(LoginVM vm, string? returnURL)
    {
        const int maxFailedAttempts = 3;
        const int lockoutDurationMinutes = 5;

        var failedAttemptsKey = $"FailedLoginAttempts_{vm.Email}";
        var lockoutEndTimeKey = $"LockoutEndTime_{vm.Email}";

        var failedAttempts = HttpContext.Session.GetInt32(failedAttemptsKey) ?? 0;
        var lockoutEndTime = HttpContext.Session.GetString(lockoutEndTimeKey);

        if (lockoutEndTime != null && DateTime.UtcNow < DateTime.Parse(lockoutEndTime))
        {
            ModelState.AddModelError("", "Your account is locked. Please try again later.");
            return View(vm);
        }

        var u = db.Users.FirstOrDefault(u => u.Email == vm.Email);

        if (u == null || !hp.VerifyPassword(u.Hash, vm.Password))
        {
            failedAttempts++;
            HttpContext.Session.SetInt32(failedAttemptsKey, failedAttempts);

            if (failedAttempts >= maxFailedAttempts)
            {
                var lockoutEnd = DateTime.UtcNow.AddMinutes(lockoutDurationMinutes);
                HttpContext.Session.SetString(lockoutEndTimeKey, lockoutEnd.ToString());
                ModelState.AddModelError("", $"Your account is locked. Please try again after {lockoutDurationMinutes} minutes.");
            }
            else
            {
                ModelState.AddModelError("", "Login credentials not matched.");
            }
            return View(vm);
        }

        // If the user is a Member, check if they are blocked
        if (u is Member m && m.IsBlocked)
        {
            ModelState.AddModelError("", "Your account has been blocked. Please contact support.");
            return View(vm); // Return the login view with the block error
        }

        // Reset login attempts if login is successful
        HttpContext.Session.Remove(failedAttemptsKey);
        HttpContext.Session.Remove(lockoutEndTimeKey);


        if (ModelState.IsValid)
        {
            TempData["Info"] = "Login successfully.";

            // Sign the user in
            hp.SignIn(u!.Email, u.Role, vm.RememberMe);

            if (u is Member member)
            {
                HttpContext.Session.SetString("PhotoURL", member.PhotoURL);
            }

            if (string.IsNullOrEmpty(returnURL))
            {
                return RedirectToAction("Index", "Home");
            }
        }

        return View(vm);
    }



    // GET: Account/Logout
    public IActionResult Logout(string? returnURL)
    {
        TempData["Info"] = "Logout successfully.";

        hp.SignOut();

        HttpContext.Session.Clear();

        return RedirectToAction("Index", "Home");
    }

    // GET: Account/AccessDenied
    public IActionResult AccessDenied(string? returnURL)
    {
        return View();
    }

    // GET: Account/CheckEmail
    public bool CheckEmail(string email)
    {
        return !db.Users.Any(u => u.Email == email);
    }

    // GET: Account/Register
    public IActionResult Register()
    {
        return View();
    }

    // POST: Account/Register
    [HttpPost]
    public IActionResult Register(RegisterVM vm)
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

            TempData["Info"] = "Register successfully. Please login.";
            return RedirectToAction("Login");
        }

        // If validation fails, return the view
        return View(vm);
    }

    // GET: Account/UpdateProfile
    // TODO: Authorize --> Member
    [Authorize(Roles = "Member")]
    public IActionResult UpdateProfile()
    {
        var m = db.Members.FirstOrDefault(m => m.Email == User.Identity!.Name);
        if (m == null) return RedirectToAction("Index", "Home");

        var vm = new UpdateProfileVM
        {
            Email = m.Email,
            Name = m.Name,
            PhotoURL = m.PhotoURL,
            Address = m.Address,
        };

        return View(vm);
    }

    // POST: Account/UpdateProfile
    // TODO: Authorize --> Member
    [HttpPost]
    [Authorize]
    public IActionResult UpdateProfile(UpdateProfileVM vm)
    {
        var m = db.Members.FirstOrDefault(m => m.Email == User.Identity!.Name);
        if (m == null) return RedirectToAction("Index", "Home");

        if (vm.Photo != null)
        {
            var err = hp.ValidatePhoto(vm.Photo);
            if (!string.IsNullOrEmpty(err))
            {
                TempData["Error"] = "Invalid photo: " + err;
                return View(vm);
            }
        }

        m.Name = vm.Name;
        m.Address = vm.Address;

        if (vm.Photo != null)
        {
            hp.DeletePhoto(m.PhotoURL, "photos");
            m.PhotoURL = hp.SavePhoto(vm.Photo, "photos"); 
            HttpContext.Session.SetString("PhotoURL", m.PhotoURL);  
        }
        db.SaveChanges();

        TempData["Info"] = "Profile updated.";
        return RedirectToAction("Index", "Home");
    }


    // GET: Account/UpdatePassword
    // TODO: Authorize
    [Authorize]
    public IActionResult UpdatePassword()
    {
        return View();
    }

    // POST: Account/UpdatePassword
    // TODO: Authorize
    [Authorize]
    [HttpPost]
    public IActionResult UpdatePassword(UpdatePasswordVM vm)
    {
        var u = db.Users.FirstOrDefault(u => u.Email == User.Identity!.Name);
        if (u == null) return RedirectToAction("Index", "Home");

        // Check if current password is correct
        if (!hp.VerifyPassword(u.Hash, vm.Current))
        {
            ModelState.AddModelError("Current", "Current password is incorrect.");
        }

        // Validate new password (add any custom rules here if needed)
        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        // Update password if validation passes
        u.Hash = hp.HashPassword(vm.New);
        db.SaveChanges();

        TempData["Info"] = "Password updated successfully.";
        return RedirectToAction("UpdatePassword");
    }


    // GET: Account/ResetPassword
    public IActionResult ResetPassword()
    {
        return View();
    }

    // POST: Account/ResetPassword
    [HttpPost]
    public IActionResult ResetPassword(ResetPasswordVM vm)
    {
        var u = db.Users.FirstOrDefault(u => u.Email == vm.Email);

        if (u == null)
        {
            ModelState.AddModelError("Email", "Email not found.");
        }

        if (ModelState.IsValid)
        {
            // TODO: Generate random password
            string password = hp.RandomPassword();

            // TODO: Update user (admin or member) record
            u!.Hash = hp.HashPassword(password);
            db.SaveChanges();

            // TODO: Send reset password email
            SendResetPasswordEmail(u, password);

            TempData["Info"] = "Password reset. Check your email.";
            return RedirectToAction();
        }

        return View();
    }

    private void SendResetPasswordEmail(User u, string password)
    {
        // TODO: Construct email
        var mail = new MailMessage();
        mail.To.Add(new MailAddress(u.Email, u.Name));
        mail.Subject = "Reset Password";
        mail.IsBodyHtml = true;

        // TODO: URL
        var url = Url.Action("Login", "Account",null,"https");

        // TODO: Image attachment
        var path = u switch
        {
            Admin    => Path.Combine(en.WebRootPath, "images", "admin.png"),
            Member m => Path.Combine(en.WebRootPath, "photos", m.PhotoURL),
            _        => ""
        };
        var att = new Attachment(path) { ContentId = "photo"};
        mail.Attachments.Add(att);

        mail.Body = $@"
            <img src='cid:photo' style='width: 200px; height: 200px; border: 1px solid #333'>
            <p>Dear {u.Name},<p>
            <p>Your password has been reset to:</p>
            <h1 style='color: red'>{password}</h1>
            <p>
                Please <a href='{url}'>login</a>
                with your new password.
            </p>
            <p>From, 🐱 Super Admin</p>
        ";

        // TODO: Send email
        hp.SendEmail(mail);
    }
}
