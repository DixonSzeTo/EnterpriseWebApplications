using Demo.Migrations;
using Demo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using System.Diagnostics;
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


    public IActionResult Member(string? name)
    {
        var model = db.Members
            .Where(s => string.IsNullOrEmpty(name) || s.Name.Contains(name))
            .ToList() 
            .OrderBy(s => int.Parse(s.Id.Substring(1)))
            .ToList(); 

        return View(model);
    }





    // GET: Home/Admin
    public IActionResult Admin(string? name)
    {
        name = name?.Trim() ?? "";
        var model = db.Admins
    .Where(s => string.IsNullOrEmpty(name) || s.Name.Contains(name))
    .ToList()
    .OrderBy(s => int.Parse(s.Id.Substring(1)))
    .ToList();
        return View(model);
    }


    public IActionResult Product()
    {
        return View();
    }

    public IActionResult Map() { return View(); }



    public IActionResult ProductCRUD(string? name)
    {

        name = name?.Trim() ?? "";

        var model = db.Products.Where(s => s.Name.Contains(name)).ToList();

        return View(model);
    }

    public IActionResult Order(OrderStatus? orderStatus)
    {        var orders = db.Orders
            .Include(o => o.OrderLines)
            .ThenInclude(ol => ol.Product) 
            .ToList();

        if (orderStatus.HasValue)
        {
            orders = orders.Where(o => o.Status == orderStatus.Value).ToList();
        }

        if (orders == null || !orders.Any())
        {
            ViewBag.Message = "No orders available.";
        }
        return View(orders); 
    }

    public IActionResult Chat()
    {
        var userName = User.Identity?.Name; 
        ViewBag.Name = userName;
        return View();
    }


}
