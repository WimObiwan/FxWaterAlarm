using System.Globalization;
using Core;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Site;
using Site.Identity;
using Site.Communication;
using Site.Middlewares;
using Site.Pages;

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

builder.Services.Configure<AccountLoginMessageOptions>(builder.Configuration.GetSection(AccountLoginMessageOptions.Location));
builder.Services.Configure<MessengerOptions>(builder.Configuration.GetSection(MessengerOptions.Location));

builder.Services.AddScoped<RequestLocalizationCookiesMiddleware>();

builder.Services.Configure<DataProtectionTokenProviderOptions>(
    x =>
    {
        AccountLoginMessageOptions accountLoginMessageOptions =
            builder.Configuration
                .GetSection(AccountLoginMessageOptions.Location)
                .Get<AccountLoginMessageOptions>()
            ?? throw new Exception("AccountLoginMessageOptions not configured");
        x.TokenLifespan = accountLoginMessageOptions.TokenLifespan;
    });

builder.Services.AddRazorPages(o =>
        o.Conventions
            .AddPageRoute("/Sensor", "/sensor/{SensorLink}")
            .AddPageRoute("/Sensor", "/s/{SensorLink}")
            .AddPageRoute("/AccountSensor", "/a/{AccountLink}/s/{SensorLink}")
            .AddPageRoute("/AdminOnBoarding", "/admin/onboarding")
            .AddPageRoute("/AccountLogin", "/account/login")
            .AddPageRoute("/AccountLoginMessage", "/account/loginmessage")
            .AddPageRoute("/AccountLoginMessageConfirmation", "/account/loginmessageconfirmation")
    )
    .AddViewLocalization();
builder.Services.AddControllers();

builder.Services.AddWaterAlarmCore(builder.Configuration, typeof(Program).Assembly);

// Identity
builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    //.AddEntityFrameworkStores<IdentityDbContext>()
    .AddDefaultTokenProviders();
builder.Services.AddTransient<IUserStore<IdentityUser>, UserStore>();
builder.Services.AddTransient<IRoleStore<IdentityRole>, RoleStore>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)  
    .AddCookie(options =>  
    {  
        options.LoginPath = "/Account/Login";  
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy =>
        policy.RequireAssertion(context => context.User.HasClaim(c =>
            c is { Type: "admin", Value: "1" })));
});

builder.Services.AddTransient<IMessenger, Messenger>();

var app = builder.Build();

app.MigrateWaterAlarmDb();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRequestLocalization();
app.UseRequestLocalizationCookies();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();

app.Run();