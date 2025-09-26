using Demo.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mail;

namespace Demo.Controllers;

public class HomeController : Controller
{
    private readonly DB db;
    private readonly IWebHostEnvironment en;
    private readonly Helper hp;

    public HomeController(DB db, IWebHostEnvironment en, Helper hp)
    {
        this.db = db;
        this.en = en;
        this.hp = hp;
    }

    // GET: Home/Index
    public IActionResult Index()
    {
        return View();
    }

    // GET: Home/Member
    public IActionResult Member()
    {
        var members = db.Members;

        return View(members);
    }
    // GET: Home/Admin
    public IActionResult Admin()
    {
        var admins = db.Admins;

        return View(admins);
    }

}
