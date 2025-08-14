using System.Globalization;
using System.Net;
using Core;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Site;
using Site.Authentication;
using Site.Identity;
using Site.Middlewares;
using Site.Pages;
using Site.Utilities;
using Westwind.AspNetCore.Markdown;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.Local.json", true, true);

var dataProtectionConfig = builder.Configuration.GetSection("DataProtection");
if (dataProtectionConfig != null)
{
    var keyPath = dataProtectionConfig["KeyStoragePath"];
    if (!string.IsNullOrEmpty(keyPath))
    {
        var dp = builder.Services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(keyPath));
        var applicationName = dataProtectionConfig["ApplicationName"];
        if (!string.IsNullOrEmpty(applicationName))
            dp.SetApplicationName("WaterAlarm");
    }
}

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
builder.Services.Configure<MeasurementDisplayOptions>(builder.Configuration.GetSection(MeasurementDisplayOptions.Location));
builder.Services.Configure<ApiKeysOptions>(builder.Configuration.GetSection(ApiKeysOptions.Location));

builder.Services.AddScoped<RequestLocalizationCookiesMiddleware>();

builder.Services.Configure<DataProtectionTokenProviderOptions>(
    x =>
    {
        // AccountLoginMessageOptions accountLoginMessageOptions =
        //     builder.Configuration
        //         .GetSection(AccountLoginMessageOptions.Location)
        //         .Get<AccountLoginMessageOptions>()
        //     ?? throw new Exception("AccountLoginMessageOptions not configured");
        // x.TokenLifespan = accountLoginMessageOptions.TokenLifespan;
    });

builder.Services.Configure<MessagesOptions>(builder.Configuration.GetSection(MessagesOptions.Location));

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

builder.Services.AddRazorPages(o =>
        o.Conventions
            .AddPageRoute("/Short", "/s")
            .AddPageRoute("/Account", "/a/{AccountLink}")
            .AddPageRoute("/AccountSensor", "/a/{AccountLink}/s/{SensorLink}")
            .AddPageRoute("/AdminOverview", "/adm")
            .AddPageRoute("/AdminAccounts", "/adm/accounts")
            .AddPageRoute("/AdminSensors", "/adm/sensors")
            .AddPageRoute("/AdminQr", "/adm/qr")
            .AddPageRoute("/AccountLoginMessage", "/account/loginmessage")
            .AddPageRoute("/AccountLoginMessageConfirmation", "/account/loginmessageconfirmation")
    )
    .AddViewLocalization();
builder.Services.AddControllers();

builder.Services.AddMarkdown(config =>
        // just add a folder as is
        config.AddMarkdownProcessingFolder("/Docs/", "~/Pages/__MarkdownPageTemplate.cshtml")
    );
        
builder.Services.AddMvc()
    .AddApplicationPart(typeof(MarkdownPageProcessorMiddleware).Assembly);

builder.Services.AddWaterAlarmCore(builder.Configuration, typeof(Program).Assembly);

// Identity
builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    //.AddEntityFrameworkStores<IdentityDbContext>()
    .AddDefaultTokenProviders();
builder.Services.AddTransient<IUserStore<IdentityUser>, UserStore>();
builder.Services.AddTransient<IRoleStore<IdentityRole>, RoleStore>();
builder.Services.AddTransient<ITrendService, TrendService>();

builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme)  
    .AddCookie(options =>
    {
        AccountLoginMessageOptions accountLoginMessageOptions =
            builder.Configuration
                .GetSection(AccountLoginMessageOptions.Location)
                .Get<AccountLoginMessageOptions>()
            ?? throw new Exception("AccountLoginMessageOptions not configured");
        options.LoginPath = "/Account/LoginMessage";
        options.ExpireTimeSpan = accountLoginMessageOptions.TokenLifespan;
        options.SlidingExpiration = false;
    })
    .AddScheme<ApiKeyAuthenticationSchemeOptions, ApiKeyAuthenticationHandler>("ApiKey", null);
builder.Services.AddSingleton<IAuthorizationHandler, AdminRequirementHandler>(); 

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy =>
        policy.Requirements.Add(new AdminRequirement()));
    options.AddPolicy("ApiKey", policy =>
    {
        policy.AuthenticationSchemes.Add("ApiKey");
        policy.RequireAuthenticatedUser();
    });
});

//builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<IUserInfo, UserInfo>();

var app = builder.Build();

app.MigrateWaterAlarmDb();

app.UseDefaultFiles(new DefaultFilesOptions()
{
    DefaultFileNames = new List<string> { "index.md", "index.html" }
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseForwardedHeaders();
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
    app.UseForwardedHeaders();
}

app.UseHttpsRedirection();
app.UseMarkdown();
app.UseStaticFiles();

app.UseRequestLocalization();
app.UseRequestLocalizationCookies();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapDefaultControllerRoute();

app.MapControllers();

app.Run();