using AspnetCoreMvcFull.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Register IHttpContextAccessor so you can access HttpContext in non-controller classes
builder.Services.AddHttpContextAccessor();

// Register KUTIPDbContext with the connection string from appsettings.json
builder.Services.AddDbContext<KUTIPDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DbConn")));

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
app.UseAuthorization();

// Map default route for controllers and views
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboards}/{action=Index}/{id?}");

app.Run();

