using Demo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;
using System.Net.Mail;
using System.Net.Mime;

namespace Demo.Controllers;

public class ProductController : Controller
{
    private readonly DB db;
    private readonly IWebHostEnvironment en;
    private readonly Helper hp;
    private readonly IConfiguration cf;


    public ProductController(DB db, IWebHostEnvironment en, Helper hp, IConfiguration cf)
    {
        this.db = db;
        this.en = en;
        this.hp = hp;

        StripeConfiguration.ApiKey = cf["Stripe:SK"];

    }

    // GET: Product/Index
    public IActionResult Index()
    {
        ViewBag.Cart = hp.GetCart();
        var model = db.Products;

        if (Request.IsAjax()) return PartialView("_Index", model);

        return View(model);
    }

    public IActionResult Run()
    {
        ViewBag.Cart = hp.GetCart();
        var model = db.Products;

        if (Request.IsAjax()) return PartialView("_Run", model);

        return View(model);
    }

    // POST: Product/UpdateCart
    [HttpPost]
    public IActionResult UpdateCart(string productId, int quantity, int stock)
    {
        // TODO
        var cart = hp.GetCart();

        if (quantity >= 1 && quantity <= stock)
        {
            cart[productId] = quantity;
        }
        else
        {
            cart.Remove(productId);
        }

        hp.SetCart(cart);

        return Redirect(Request.Headers.Referer.ToString() ?? Url.Action("Index", "Home")!);
    }


    // GET: Product/ShoppingCart
    public IActionResult ShoppingCart()
    {
        // TODO
        var cart = hp.GetCart();
        var model = cart.Select(item => new CartItem
        {
            Product = db.Products.Find(item.Key)!,
            Quantity = item.Value,
        });

        if (Request.IsAjax()) return PartialView("_ShoppingCart", model);

        return View(model);
    }

    // GET: Product/Order
    // TODO
    [Authorize(Roles = "Member")]

    public IActionResult Order()
    {
        // TODO
        var model = db.Orders
            .Include(o => o.OrderLines)
            .Where(o => o.MemberEmail == User.Identity!.Name)
            .OrderByDescending(o => o.Id);

        return View(model);
    }

    // GET: Product/OrderDetail
    // TODO
    [Authorize(Roles = "Member")]
    public IActionResult OrderDetail(int id)
    {
        // TODO
        var model = db.Orders
            .Include(o => o.OrderLines)
            .ThenInclude(ol => ol.Product)
            .FirstOrDefault(o => o.Id == id &&
            o.MemberEmail == User.Identity!.Name
            );

        if (model == null) return RedirectToAction("Order");
        return View(model);
    }

    // POST: Product/Checkout
    [Authorize(Roles = "Member")]
    [HttpPost]
    public IActionResult Checkout()
    {
        var cart = hp.GetCart();
        if (cart.Count == 0) return RedirectToAction("ShoppingCart");
        var domain = Url.Action("", "", null, "https");

        var metadata = cart.ToDictionary(x => x.Key, x => x.Value.ToString());

        var lineItems = new List<SessionLineItemOptions>();
        foreach (var (productId, quantity) in cart)
        {
            var p = db.Products.Find(productId)!;
            lineItems.Add(new()
            {
                PriceData = new()
                {
                    ProductData = new()
                    {
                        Name = $"{p.Id} - {p.Name}",
                    },
                    Currency = "myr",
                    UnitAmount = Convert.ToInt64(p.Price * 100),
                },
                Quantity = quantity,
            });

        }

        var options = new SessionCreateOptions
        {
            Mode = "payment",
            SuccessUrl = domain + "Product/Success?sessionId={CHECKOUT_SESSION_ID}",
            CancelUrl = domain + "Product/Cancel",
            CustomerEmail = User.Identity!.Name,
            ClientReferenceId = "XXx", 
            Metadata = metadata, 
            LineItems = lineItems,
        };

        var session = new SessionService().Create(options);

        // TODO: Remember session id
        HttpContext.Session.SetString("SessionId", session.Id);


        // TODO: Redirect to Stripe checkout page
        return Redirect(session.Url);
    }

    public IActionResult Cancel()
    {
        return View();
    }

public IActionResult Success(string? sessionId)
{
    if (Request.Headers.Referer == "https://checkout.stripe.com/" &&
        sessionId != null &&
        sessionId == HttpContext.Session.GetString("SessionId"))
    {
        hp.SetCart(null);
        HttpContext.Session.Remove("SessionId");

        var session = new SessionService().Get(sessionId);
        TempData["OrderId"] = FulfillOrder(session);
        
        var user = db.Users.FirstOrDefault(u => u.Email == session.CustomerEmail);
        if (user != null)
        {
            var order = db.Orders.FirstOrDefault(o => o.Id == (int)TempData["OrderId"]!);
            if (order != null)
            {
                SendReceipt(user, order);
            }
        }

        return RedirectToAction("Success");
    }

    return View();
}


    private int FulfillOrder(Session session)
    {
        var member = db.Users.FirstOrDefault(u => u.Email == session.CustomerEmail);

        var order = new Models.Order
        {
            Date = DateTime.Today,
            MemberEmail = session.CustomerEmail ,
            MemberId = member!.Id,
        };
        db.Orders.Add(order);

        foreach (var (productId, quantityString) in session.Metadata)
        {
            var product = db.Products.Find(productId);
            if (product != null)
            {
                var quantity = int.Parse(quantityString);

                order.OrderLines.Add(new OrderLine
                {
                    Price = product.Price,    
                    Quantity = quantity,    
                    ProductId = productId    
                });

                product.Stock -= quantity;

                db.SaveChanges();
            }
        }

        db.SaveChanges();

        return order.Id; 
    }


    // TODO: Webhook
    [Route("Webhook")]
    [HttpPost]
    public async Task<IActionResult> Webhook()
    {
        // TODO: Handle CheckoutSessionCompleted event

        await Task.CompletedTask;

        return Ok();
    }


    private void SendReceipt(User u, Models.Order order)
    {
        var mail = new MailMessage
        {
            Subject = "(Receipt) NICEY FOOT WEAR",
            IsBodyHtml = true
        };
        mail.To.Add(new MailAddress(u.Email, u.Name));

        var url = Url.Action("Login", "Account", null, "https");

        var body = $@"
    <h1>Thank You For Your Order!</h1>
    <p>Your order ID: {order.Id}</p>
    <p>Date: {order.Date.ToShortDateString()}</p>
    <p>Email: {order.MemberEmail}</p>
    <h2>Order Details:</h2>";

        foreach (var line in order.OrderLines)
        {
            body += $"<p>Product ID: {line.ProductId}</p>" +
                $"<p>Quantity: {line.Quantity}</p> " +
                $"<p>Price: {line.Price}</p>";
        }

        body += $@"
    <br />
    <p>For more details, you can login here: <a href='{url}'>Login</a></p>
    <p>From, 🐱 Super Admin</p>";

        mail.Body = body;

        var attachmentPath = Path.Combine(Path.GetTempPath(), $"Receipt_{order.Id}.txt");

        var plainTextBody = System.Text.RegularExpressions.Regex.Replace(body, "<.*?>", string.Empty); // Remove HTML tags
        System.IO.File.WriteAllText(attachmentPath, plainTextBody);

        var textAttachment = new Attachment(attachmentPath, MediaTypeNames.Text.Plain);
        mail.Attachments.Add(textAttachment);

        hp.SendEmail(mail);

    }

    [HttpPost]
    public IActionResult Receipt(OrderReceiptVM vm)
    {
        var u = db.Users.FirstOrDefault(u => u.Email == vm.Email);

        if (u == null)
        {
            ModelState.AddModelError("Email", "Email not found.");
        }

        var order = db.Orders.FirstOrDefault(o => o.Id == vm.OrderId && o.MemberEmail == vm.Email);

        if (order == null)
        {
            ModelState.AddModelError("Order", "Order not found.");
        }

        if (ModelState.IsValid)
        {
            SendReceipt(u, order);
            TempData["Info"] = "Receipt sent to your email.";

            return RedirectToAction("OrderDetails", new { id = order.Id });
        }
        return View(vm);
    }

    public IActionResult OrderCancel()
    {
        return View();
    }

    [HttpPost]
    public IActionResult OrderCancel(int? orderId)
    {
        var order = db.Orders.Find(orderId);

        if (order != null && order.Status != OrderStatus.Cancelled)
        {
            var orderLines = db.OrderLines.Where(ol => ol.OrderId == orderId).ToList();

            foreach (var orderLine in orderLines)
            {
                var product = db.Products.Find(orderLine.ProductId);
                if (product != null)
                {
                    product.Stock += orderLine.Quantity;
                }
            }

            order.Status = OrderStatus.Cancelled;
            db.SaveChanges();

            TempData["Info"] = $"Order {orderId} Cancelled Successfully and product stock updated.";
        }

        return Redirect(Request.Headers["Referer"].ToString());
    }

}
