global using Demo;
global using Demo.Models;

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
// WebOptimizer removed for brevity
// TODO: Add SignalR
builder.Services.AddSignalR();


var app = builder.Build();
app.UseHttpsRedirection();
// WebOptimizer removed for brevity
app.UseStaticFiles();
app.UseSession();
app.UseRequestLocalization("en-MY");
// TODO: Map SignalR hub --> "/hub"
app.MapHub<ChatHub>("/hub");


app.MapDefaultControllerRoute();
app.Run();
