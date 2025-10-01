using Demo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;

namespace Demo.Controllers
{
    public class ProductCRUDController : Controller
    {
        private readonly DB db;
        private readonly IWebHostEnvironment en;
        private readonly Helper hp;

        public ProductCRUDController(DB db, IWebHostEnvironment en, Helper hp)
        {
            this.db = db;
            this.en = en;
            this.hp = hp;
        }
        public IActionResult CreateProduct()
        {
            var vm = new CreateProductVM
            {
                Categories = GetCategories() 
            };

            return View(vm);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public IActionResult CreateProduct(CreateProductVM vm)
        {
            if (string.IsNullOrWhiteSpace(vm.Category))
            {
                ModelState.AddModelError("Category", "Category is required.");
                vm.Categories = GetCategories();
                return View(vm);
            }

            if (ModelState.IsValid("Photo"))
            {
                var err = hp.ValidatePhoto(vm.Photo);
                if (err != "") ModelState.AddModelError("Photo", err);
            }

            var lastProduct = db.Products.OrderByDescending(m => m.Id).FirstOrDefault();
            var newId = lastProduct == null
                ? "P001"
                : $"P{(int.Parse(lastProduct.Id.Substring(1)) + 1).ToString("D3")}";

                // Insert product + save photo
                db.Products.Add(new()
                {
                    Id = newId,
                    Name = vm.Name,
                    Price = vm.Price,
                    Stock = vm.Stock,
                    PhotoURL = hp.SavePhoto(vm.Photo, "products"),
                    Category = vm.Category,
                });

                db.SaveChanges();

            
            TempData["Info"] = $"Product {newId} Created Successfully";
            vm.Categories = GetCategories();
            return RedirectToAction("ProductCRUD", "Home");
        }
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public IActionResult DeleteProduct(string? id)
        {
            var product = db.Products.Find(id);

            if (product != null)
            {
                db.Products.Remove(product);
                db.SaveChanges();
                if (!Request.IsAjax())
                {
                    TempData["Info"] = $"Product {id} deleted.";
                }
            }

            return Redirect(Request.Headers.Referer.ToString());
        }

        // GET: ProductCRUD/UpdateProduct
        public IActionResult UpdateProduct(string? id)
        {

            var product = db.Products.Find(id);
            var vm = new UpdateProductVM
            {
                Id = product!.Id,
                Name = product.Name,
                Price = product.Price,
                Stock = product.Stock,
                PhotoURL = product.PhotoURL,
                Category = product.Category,
                Categories = GetCategories()
            };

            return View(vm);
        }


        [Authorize(Roles = "Admin")]
        [HttpPost]
        public IActionResult UpdateProduct(string? id, UpdateProductVM cp)
        {
            var product = db.Products.Find(cp.Id);

            if (product == null) return RedirectToAction("Index", "Home");

            if (product == null)
                return RedirectToAction("Index", "Home");

            if (cp.Photo != null)
            {
                var err = hp.ValidatePhoto(cp.Photo);
                if (!string.IsNullOrEmpty(err))
                {
                    ModelState.AddModelError("Photo", err);
                }
            }
            product.Name = cp.Name;
                product.Price = cp.Price;
                product.Stock = cp.Stock;
                product.Category = cp.Category;
                cp.PhotoURL = product.PhotoURL;
            cp.Categories = GetCategories();
            if (cp.Photo != null)
                {
                    hp.DeletePhoto(product.PhotoURL, "products");
                    product.PhotoURL = hp.SavePhoto(cp.Photo, "products"); 
                    HttpContext.Session.SetString("PhotoURL", product.PhotoURL); 
                }

                db.SaveChanges();
                TempData["Info"] = "Product Updated successfully.";
                return RedirectToAction("ProductCRUD", "Home");
        }




        private List<SelectListItem> GetCategories()
        {
            return new List<SelectListItem>
        {
        new SelectListItem { Value = "Top", Text = "Top" },
        new SelectListItem { Value = "Run", Text = "Run" },
        new SelectListItem { Value = "Block", Text = "Block" },

           };
        }




    }
}
