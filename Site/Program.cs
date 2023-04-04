using System.Globalization;
using Core;
using Microsoft.AspNetCore.Localization;
using Site.Middlewares;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.Local.json", true);

// Add services to the container.
builder.Services.AddLocalization(o =>
    o.ResourcesPath = "Resources"
);

var supportedCultures = new List<CultureInfo>
{
    new("nl-BE"),
    new("en-BE"),
    new("nl"),
    new("en")
};

builder.Services.Configure<RequestLocalizationOptions>(o =>
{
    o.DefaultRequestCulture = new RequestCulture("nl-BE");
    o.SupportedCultures = supportedCultures;
    o.SupportedUICultures = supportedCultures;
    o.ApplyCurrentCultureToResponseHeaders = true;
    o.FallBackToParentCultures = true;
    o.FallBackToParentUICultures = true;
});

builder.Services.AddScoped<RequestLocalizationCookiesMiddleware>();

builder.Services.AddRazorPages(o =>
        o.Conventions
            .AddPageRoute("/Sensor", "/sensor/{SensorLink}")
            .AddPageRoute("/Sensor", "/s/{SensorLink}")
            .AddPageRoute("/AccountSensor", "/a/{AccountLink}/s/{SensorLink}")
    )
    .AddViewLocalization();
builder.Services.AddControllers();

builder.Services.AddWaterAlarmCore(builder.Configuration, typeof(Program).Assembly);

var app = builder.Build();

app.MigrateWaterAlarmDb();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRequestLocalization();
app.UseRequestLocalizationCookies();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();

app.Run();