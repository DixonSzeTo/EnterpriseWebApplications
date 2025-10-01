using Demo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;
using System.Net.Mail;
using System.Net.Mime;

namespace Demo.Controllers;

public class OrderMController : Controller
{
    private readonly DB db;
    private readonly IWebHostEnvironment en;
    private readonly Helper hp;
    private readonly IConfiguration cf;


    public OrderMController(DB db, IWebHostEnvironment en, Helper hp)
    {
        this.db = db;
        this.en = en;
        this.hp = hp;

    }

    [Authorize(Roles = "Admin")]
    public IActionResult SaveOrderStatus(int id)
    {
        var order = db.Orders.Find(id);
        if (order != null)
        {
            var vm = new OrderMVM
            {
                OrderId = order.Id,
                Status = order.Status,
            };

            return View(vm);
        }

        return NotFound();
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public IActionResult SaveOrderStatus(OrderMVM ovm)
    {
        var order = db.Orders.Find(ovm.OrderId);
        if (order != null)
        {
            if (ovm.Status == OrderStatus.Cancelled && order.Status != OrderStatus.Cancelled)
            {
                var orderLines = db.OrderLines.Where(ol => ol.OrderId == ovm.OrderId).ToList();

                foreach (var orderLine in orderLines)
                {
                    var product = db.Products.Find(orderLine.ProductId);
                    if (product != null)
                    {
                        product.Stock += orderLine.Quantity; 
                    }
                }
            }
            else if (order.Status == OrderStatus.Cancelled && ovm.Status != OrderStatus.Cancelled)
            {
                var orderLines = db.OrderLines.Where(ol => ol.OrderId == ovm.OrderId).ToList();

                foreach (var orderLine in orderLines)
                {
                    var product = db.Products.Find(orderLine.ProductId);
                    if (product != null)
                    {
                        product.Stock -= orderLine.Quantity; 
                    }
                }
            }
            order.Status = ovm.Status;

            try
            {
                db.SaveChanges(); 
                TempData["Info"] = "Order status updated successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error occurred while updating the order status. Please try again.";
            }
        }
        else
        {
            TempData["Error"] = "Order ID not found.";
        }

        return RedirectToAction("Order", "Home");
    }



}
