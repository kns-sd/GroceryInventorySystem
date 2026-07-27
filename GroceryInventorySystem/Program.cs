using GroceryInventorySystem.Data;
using GroceryInventorySystem.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Identity Setup
builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

// Add services to the container.
builder.Services.AddControllersWithViews();

// MySQL Connection
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        new MySqlServerVersion(new Version(8, 0, 21))
    ));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Default route → Login page
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

// Seed Data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    if (!context.Products.Any())
    {
        context.Products.AddRange(
            new Product { Name = "Rice (5kg)", Category = "Rice", Price = 325, StockQuantity = 50, MinStockLevel = 10, Supplier = "Basmati Ltd" },
            new Product { Name = "Vegetable Oil (1L)", Category = "Oil", Price = 180, StockQuantity = 30, MinStockLevel = 5, Supplier = "Fresh Oil" },
            new Product { Name = "Milk (1L)", Category = "Dairy", Price = 80, StockQuantity = 8, MinStockLevel = 10, Supplier = "Dairy Farm" },
            new Product { Name = "Sugar (1kg)", Category = "Sugar", Price = 85, StockQuantity = 25, MinStockLevel = 8, Supplier = "Sweet Co" },
            new Product { Name = "Salt (500g)", Category = "Spices", Price = 35, StockQuantity = 5, MinStockLevel = 10, Supplier = "Spice World" }
        );
        context.SaveChanges();
    }
}

app.Run();