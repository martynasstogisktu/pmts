using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PMTS.Authentication;
using PMTS.Models;
using System.Configuration;
using System.Globalization;

CultureInfo.CurrentCulture = new CultureInfo("lt-LT");

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddScoped<PmtsJwt>();

builder.Services.AddDbContext<PSQLcontext>(options => options.UseNpgsql(Environment.GetEnvironmentVariable("AzureDB")));

Environment.SetEnvironmentVariable("RegisterEnabled", "true");

builder.Services.Configure<CookieTempDataProviderOptions>(options => {
    options.Cookie.IsEssential = true;
});

var app = builder.Build();


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
