using AspnetCoreMvcFull.Data;
using AspnetCoreMvcFull.Services; // Add this using statement
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Register IHttpContextAccessor so you can access HttpContext in non-controller classes
builder.Services.AddHttpContextAccessor();

// Register KUTIPDbContext with the connection string from appsettings.json
builder.Services.AddDbContext<KUTIPDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DbConn")));

// Add Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
      options.LoginPath = "/Auth/LoginBasic";
      options.LogoutPath = "/Auth/Logout";
      options.AccessDeniedPath = "/Auth/AccessDenied";
      options.ExpireTimeSpan = TimeSpan.FromHours(8);
      options.SlidingExpiration = true;
    });

// Add Authorization
builder.Services.AddAuthorization();

// Add the background service for automatic missed pickup detection
builder.Services.AddHostedService<MissedPickupDetectionService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
  // In production, you might want to show more detailed errors
  app.UseExceptionHandler("/Home/Error");
  app.UseHsts();
}
else
{
  // Additional development-specific settings
  app.UseDeveloperExceptionPage(); // Show detailed error page in development
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Add Authentication and Authorization middleware (ORDER MATTERS!)
app.UseAuthentication();
app.UseAuthorization();

// Map default route for controllers and views
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=LoginBasic}/{id?}"); // Changed default to Auth/LoginBasic

app.Run();
