using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Demo.Models;

public class DB : DbContext
{
    public DB(DbContextOptions<DB> options) : base(options) { }

    // DB Sets
    public DbSet<User> Users { get; set; }
    public DbSet<Admin> Admins { get; set; }
    public DbSet<Member> Members { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderLine> OrderLines { get; set; }
}
// Entity Classes

#nullable disable warnings

public class User
{
    [Key,MaxLength(5)]
    public string Id { get; set; }
    [MaxLength(100)]
    public string Email { get; set; }
    [MaxLength(100)]
    public string Hash { get; set; }
    [MaxLength(100)]
    public string Name { get; set; }

    [NotMapped]
    public string Role => GetType().Name;
}

public class Admin : User
{

}

public class Member : User
{
    [MaxLength(100)]
    public string PhotoURL { get; set; }

    [MaxLength(100)]
    public string Address { get; set; }
    
    public bool IsBlocked { get; set; } = false;
}

public class Product
{
    [Key, MaxLength(4)]
    public string Id { get; set; }
    [MaxLength(100)]
    public string Name { get; set; }
    [Precision(6, 2)]
    public decimal Price { get; set; }
    [MaxLength(100)]
    public string PhotoURL { get; set; }
    [Required]
    [MaxLength(50)]
    public string Category { get; set; }

    public int Stock { get; set; }
    public List<OrderLine> OrderLines { get; set; } = [];
}

public enum OrderStatus
{
    Pending ,
    Shipped ,
    Delivered ,
    Cancelled
}

public class Order
{
    [Key,DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }


    [Column(TypeName = "DATE")]
    public DateTime Date { get; set; }

    public string MemberEmail { get; set; }

    public string MemberId { get; set; }

    public Member Member { get; set; }

    public List<OrderLine> OrderLines { get; set; } = [];

    public OrderStatus Status { get; set; } = OrderStatus.Pending;



}

public class OrderLine
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Precision(6, 2)]
    public decimal Price { get; set; }
    public int Quantity { get; set; }

    public int OrderId { get; set; }
    public string ProductId { get; set; }

    public Order Order { get; set; }
    public Product Product { get; set; }

}
