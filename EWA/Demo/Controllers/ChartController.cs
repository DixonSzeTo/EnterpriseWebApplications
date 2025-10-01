using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using System.Globalization;

namespace Demo.Controllers;

public class ChartController : Controller
{
    private readonly DB db;
    private readonly IWebHostEnvironment en;
    private readonly Helper hp;

    public ChartController(DB db, IWebHostEnvironment en, Helper hp)
    {
        this.db = db;
        this.en = en;
        this.hp = hp;
    }

    public IActionResult YearRange()
    {
        var years = db.OrderLines
                      .Select(ol => ol.Order.Date.Year)
                      .Distinct()
                      .OrderBy(y => y)
                      .ToList();

        return Json(years);
    }



    // GET: Chart/YearlySales
    public IActionResult YearlySales()
    {
        return View();
    }

    // GET: Chart/YearlySales
    public IActionResult Data1()
    {
        var canceledStatus = OrderStatus.Cancelled; 

        var dt = db.OrderLines
                   .Where(ol => ol.Order.Status != canceledStatus)
                   .GroupBy(ol => ol.Order.Date.Year)
                   .OrderBy(g => g.Key)
                   .Select(g => new object[]
                   {
                   g.Key.ToString(),
                   g.Sum(ol => ol.Price * ol.Quantity) 
                   });

        return Json(dt);
    }

    // GET: Chart/MonthlySales
    public IActionResult MonthlySales()
    {
        return View();
    }

    // GET: Chart/MonthlySales
        public IActionResult Data2(int year)
        {
            var canceledStatus = OrderStatus.Cancelled;

            var dt = db.OrderLines
                       .Where(ol => ol.Order.Status != canceledStatus && ol.Order.Date.Year == year)
                       .GroupBy(ol => ol.Order.Date.Month)
                       .OrderBy(g => g.Key) 
                       .Select(g => new object[]
                       {
                       $"{year}-{g.Key:D2}",
                       g.Sum(ol => ol.Price * ol.Quantity)
                       });

            return Json(dt);
        }
    // GET: Chart/DailySales
    public IActionResult DailySales()
    {
        return View();
    }

    public IActionResult Data3(int year)
    {
        
            var canceledStatus = OrderStatus.Cancelled;

            var dt = db.OrderLines
                       .Where(ol => ol.Order.Status != canceledStatus && ol.Order.Date.Year == year)
                       .GroupBy(ol => ol.Order.Date.Date)
                       .OrderBy(g => g.Key)
                       .Select(g => new object[]
                       {
                   g.Key.ToString("yyyy-MM-dd"),
                   g.Sum(ol => ol.Price * ol.Quantity)
                       });

            return Json(dt);
        }



        public IActionResult YearlyProductQuantity()
    {
        return View();
    }

    public IActionResult Data4()
    {
        var data = db.OrderLines
                     .GroupBy(ol => ol.Order.Date.Year)
                     .Select(g => new
                     {
                         Year = g.Key,
                         TotalQuantity = g.Sum(ol => ol.Quantity)
                     })
                     .OrderBy(g => g.Year) 
                     .ToList();

        var chartData = data.Select(d => new object[] { d.Year.ToString(), d.TotalQuantity });

        return Json(chartData); 
    }


    // GET: Chart/MonthlyProductQuantity
    public IActionResult MonthlyProductQuantity()
    {
        return View();
    }

    // GET: Chart/Data5
    public IActionResult Data5(int year)
    {
        var data = db.OrderLines
                     .Where(ol => ol.Order.Date.Year == year)
                     .GroupBy(ol => ol.Order.Date.Month) 
                     .Select(g => new
                     {
                         Month = g.Key, 
                         TotalQuantity = g.Sum(ol => ol.Quantity)
                     })
                     .OrderBy(g => g.Month) 
                     .ToList();

        var chartData = data.Select(d => new object[] {
        CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(d.Month),
        d.TotalQuantity
    });

        return Json(chartData);
    }
    public IActionResult DailyProductQuantity()
    {
        return View();
    }
    public IActionResult Data6(int year)
    {
        var data = db.OrderLines
                     .Where(ol => ol.Order.Date.Year == year) 
                     .GroupBy(ol => ol.Order.Date.Date) 
                     .Select(g => new
                     {
                         Date = g.Key,
                         TotalQuantity = g.Sum(ol => ol.Quantity)
                     })
                     .OrderBy(g => g.Date) 
                     .ToList();

        var chartData = data.Select(d => new object[] { d.Date.ToString("dd/MM"), d.TotalQuantity });

        return Json(chartData); 
    }

    public IActionResult ProductReport()
    {
        return View();
    }
    public IActionResult Data7(int year)
    {
        var data = db.OrderLines
                     .Where(ol => ol.Order.Date.Year == year) 
                     .GroupBy(ol => ol.Product.Id)
                     .Select(g => new
                     {
                         ProductId = g.Key,
                         TotalQuantity = g.Sum(ol => ol.Quantity)
                     })
                     .ToList();

        var chartData = data.Select(d => new object[] { d.ProductId, d.TotalQuantity }).ToList();

        return Json(chartData);
    }

    public IActionResult MemberReport()
    {
        return View();
    }
    public IActionResult Data8()
    {
        var data = db.Members
                     .GroupBy(m => m.IsBlocked)
                     .Select(g => new
                     {
                         IsBlocked = g.Key,
                         Count = g.Count()
                     })
                     .ToList();

        var chartData = data.Select(d => new object[] { d.IsBlocked ? "Blocked" : "Active", d.Count }).ToList();

        return Json(chartData);
    }

    public IActionResult AverageOrderValuePerYear()
    {
        return View();
    }

    public ActionResult Data9()
    {
        var canceledStatus = OrderStatus.Cancelled;

        var results = db.Orders
            .Where(o => o.Status != canceledStatus)
            .GroupBy(o => o.Date.Year)
            .Select(g => new
            {
                Year = g.Key,
                // Average order quantity per year
                AverageOrderQuantity = g.Average(o => o.OrderLines.Sum(ol => ol.Quantity))
            })
            .OrderBy(g => g.Year)
            .ToList();

        var chartData = results.Select(r => new object[] { r.Year.ToString(), r.AverageOrderQuantity }).ToList();

        return Json(chartData);
    }





    public ActionResult Ranking()
    {
        var startDate = DateTime.Today.AddDays(-29);
        var endDate = DateTime.Today; 

        var result = db.OrderLines
            .Where(ol => ol.Order.Date >= startDate && ol.Order.Date <= endDate)
            .GroupBy(ol => ol.Product)
            .Select(g => new
            {
                Product = g.Key,
                Sales = g.Sum(ol => ol.Price * ol.Quantity),
                Quantity = g.Sum(ol => ol.Quantity),
            })
            .ToList();

        ViewBag.TopSales = result.OrderByDescending(r => r.Sales).Take(5).ToList();

        ViewBag.TopQuantity = result.OrderByDescending(r => r.Quantity).Take(5).ToList();

        return View();
    }




}
