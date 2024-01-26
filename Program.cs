using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using dAIlog.Data;
using dAIlog.Services;
using Microsoft.AspNetCore.Http;
using OpenAI.Net;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
                       throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddControllersWithViews();

// Make IConfiguration available for dependency injection
builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

// Add session services
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Set session timeout as needed
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddOpenAIServices(options => {
    options.ApiKey = builder.Configuration["OpenAI:ApiKey"]; // Ensure your API key is in appsettings or environment variables
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication(); // Ensure UseAuthentication is called before UseSession
app.UseAuthorization();

// Use session middleware
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

// Run Python script in parallel
Task.Run(() =>
{
    var scriptRunner = new PythonScriptService();
    scriptRunner.RunPythonScript("dAiLogPythonService/app.py", "dAiLogPythonService/venv");
});


app.Run();
