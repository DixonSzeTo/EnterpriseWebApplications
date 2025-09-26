global using Demo.Models;
global using Demo;

var builder = WebApplication.CreateBuilder(args);

var cs = string.Format(
    builder.Configuration.GetConnectionString("DB")!,
    builder.Environment.ContentRootPath
);

builder.Services.AddControllersWithViews();
builder.Services.AddSqlServer<DB>(cs);
builder.Services.AddScoped<Helper>();
builder.Services.AddAuthentication().AddCookie();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSession();

var app = builder.Build();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseSession();
app.MapDefaultControllerRoute();
app.Run();
