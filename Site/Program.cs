using System.Globalization;
using Core;
using Microsoft.AspNetCore.Localization;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.Local.json", true);

// Add services to the container.
builder.Services.AddRazorPages(o =>
    o.Conventions
        .AddPageRoute("/Sensor", "/sensor/{SensorLink}")
        .AddPageRoute("/Sensor", "/s/{SensorLink}")
        .AddPageRoute("/AccountSensor", "/a/{AccountLink}/s/{SensorLink}")
);
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

app.UseRouting();

app.UseAuthorization();

var supportedCultures = new List<CultureInfo>
{
    new("nl-BE"),
    new("en-BE")
};
var options = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("nl-BE"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
};
app.UseRequestLocalization(options);

app.MapRazorPages();
app.MapControllers();

app.Run();