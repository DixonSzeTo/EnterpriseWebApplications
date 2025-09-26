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

        // Get failed attempts and lockout time specifically for the provided email
        var failedAttemptsKey = $"FailedLoginAttempts_{vm.Email}";
        var lockoutEndTimeKey = $"LockoutEndTime_{vm.Email}";

        var failedAttempts = HttpContext.Session.GetInt32(failedAttemptsKey) ?? 0;
        var lockoutEndTime = HttpContext.Session.GetString(lockoutEndTimeKey);

        // Check if the user is locked out
        if (lockoutEndTime != null && DateTime.UtcNow < DateTime.Parse(lockoutEndTime))
        {
            ModelState.AddModelError("", "Your account is locked. Please try again later.");
            return View(vm);
        }

        var u = db.Users.FirstOrDefault(u => u.Email == vm.Email);

        // Check if user exists and if password matches
        if (u == null || !hp.VerifyPassword(u.Hash, vm.Password))
        {
            // Increment failed login attempts for this email
            failedAttempts++;
            HttpContext.Session.SetInt32(failedAttemptsKey, failedAttempts);

            if (failedAttempts >= maxFailedAttempts)
            {
                // Set lockout end time for this email
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

        // reset login attempts if login successfully
        HttpContext.Session.Remove(failedAttemptsKey);
        HttpContext.Session.Remove(lockoutEndTimeKey);

        if (ModelState.IsValid)
        {
            TempData["Info"] = "Login successfully.";

            hp.SignIn(u!.Email, u.Role, vm.RememberMe);

            if (u is Member m)
            {
                HttpContext.Session.SetString("PhotoURL", m.PhotoURL);
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



    // ------------------------------------------------------------------------
    // Others
    // ------------------------------------------------------------------------

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

            TempData["Info"] = "Register successfully. Please login.";
            return RedirectToAction("Login");
        }

        return View();
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
            PhotoURL = m.PhotoURL
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
            if (err != "") ModelState.AddModelError("Photo", err);
        }

        if (ModelState.IsValid)
        {
            // TODO: Update member + delete and save photo + set session
            m.Name= vm.Name;

            if (vm.Photo != null)
            {
                hp.DeletePhoto(m.PhotoURL, "photos");
                m.PhotoURL = hp.SavePhoto(vm.Photo, "photos");
                HttpContext.Session.SetString("PhotoURL",m.PhotoURL);
            }
            db.SaveChanges();


            TempData["Info"] = "Profile updated.";
            return RedirectToAction();
        }

        vm.Email = m.Email;
        vm.PhotoURL = m.PhotoURL;
        return View(vm);
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
