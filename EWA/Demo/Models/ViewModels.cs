using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Demo.Models;

// View Models ----------------------------------------------------------------

#nullable disable warnings

public class LoginVM
{
    [StringLength(100)]
    [EmailAddress]
    public string Email { get; set; }

    [StringLength(100, MinimumLength = 5)]
    [DataType(DataType.Password)]
    public string Password { get; set; }

    public bool RememberMe { get; set; }
}

public class RegisterVM
{

    [StringLength(100)]
    [EmailAddress]
    [Remote("CheckEmail", "Account", ErrorMessage = "Duplicated {0}.")]
    public string Email { get; set; }

    [StringLength(100, MinimumLength = 5)]
    [DataType(DataType.Password)]
    public string Password { get; set; }

    [StringLength(100, MinimumLength = 5)]
    [Compare("Password")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm Password")]
    public string Confirm { get; set; }

    [StringLength(100)]
    public string Name { get; set; }

    [StringLength(100)]
    public string Address { get; set; }

    public IFormFile Photo { get; set; }
}

public class CreateAdminVM
{
    [StringLength(100)]
    [EmailAddress]
    [Remote("CheckEmail", "Account", ErrorMessage = "Duplicated {0}.")]
    public string Email { get; set; }

    [StringLength(100, MinimumLength = 5)]
    [DataType(DataType.Password)]
    public string Password { get; set; }

    [StringLength(100, MinimumLength = 5)]
    [Compare("Password")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm Password")]
    public string Confirm { get; set; }

    [StringLength(100)]
    public string Name { get; set; }
}


public class UpdateProfileVM
{
    public string Id { get; set; }

    [StringLength(100)]
    [EmailAddress]
    public string Email { get; set; }

    [StringLength(100)]
    public string Name { get; set; }
    [StringLength(100)]
    public string Address { get; set; }

    public string? PhotoURL { get; set; }

    public IFormFile? Photo { get; set; }
}

public class UpdatePasswordVM
{

    [StringLength(100, MinimumLength = 5)]
    [DataType(DataType.Password)]
    [Display(Name = "Current Password")]
    public string Current { get; set; }

    [StringLength(100, MinimumLength = 5)]
    [DataType(DataType.Password)]
    [Display(Name = "New Password")]
    public string New { get; set; }

    [StringLength(100, MinimumLength = 5)]
    [Compare("New")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm Password")]
    public string Confirm { get; set; }
}

public class ResetPasswordVM
{
    [StringLength(100)]
    [EmailAddress]
    public string Email { get; set; }
}

public class EmailVM
{
    [StringLength(100)]
    [EmailAddress]
    public string Email { get; set; }

    public string Subject { get; set; }

    public string Body { get; set; }

    public bool IsBodyHtml { get; set; }
}

public class UpdateMemberVM
{
    public string Id { get; set; }

    [StringLength(100)]
    [EmailAddress]
    public string Email { get; set; }

    [StringLength(100)]
    public string Name { get; set; }

    [StringLength(100)]
    public string Address { get; set; }
}

public class UpdateAdminVM
{
    public string Id { get; set; }

    [StringLength(100)]
    [EmailAddress]
    public string Email { get; set; }

    [StringLength(100)]
    public string Name { get; set; }
}

public class CartItem
{
    public Product Product { get; set; }
    public int Quantity { get; set; }
}

public class CreateProductVM
{
    [Key, MaxLength(4)]
    public string Id { get; set; }
    [MaxLength(100)]
    public string Name { get; set; }
    [Precision(6, 2)]
    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0.")]
    public decimal Price { get; set; }

    [MaxLength(50)]
    [Required]
    public string Category { get; set; }

    public List<SelectListItem> Categories { get; set; }

    public IFormFile Photo { get; set; }

    [MaxLength(100)]
    public string PhotoURL { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Stock cannot be negative.")]
    public int Stock { get; set; }
}

public class UpdateProductVM
{
    [Key, MaxLength(4)]
    public string Id { get; set; }
    [MaxLength(100)]
    public string Name { get; set; }
    [Precision(6, 2)]
    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0.")]
    public decimal Price { get; set; }

    [MaxLength(50)]
    [Required]
    public string Category { get; set; }

    public List<SelectListItem> Categories { get; set; }

    public IFormFile? Photo { get; set; }

    [MaxLength(100)]
    public string PhotoURL { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Stock cannot be negative.")]
    public int Stock { get; set; }
}

public class OrderReceiptVM
{
    [Required]
    public int OrderId { get; set; }
    [Required]
    [EmailAddress]
    public string Email { get; set; }
}


public class OrderMVM
{
    public int OrderId { get; set; }
    public OrderStatus Status { get; set; }
}