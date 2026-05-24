using System.Globalization;
using System.Net;
using Core;
using Core.Configuration;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Site.Services;
using Site;
using Site.Authentication;
using Site.Identity;
using Site.Middlewares;
using Site.Pages;
using Site.Security;
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
builder.Services.Configure<LoginSecurityOptions>(builder.Configuration.GetSection(LoginSecurityOptions.Location));
builder.Services.Configure<MeasurementDisplayOptions>(builder.Configuration.GetSection(MeasurementDisplayOptions.Location));
builder.Services.Configure<MeasurementRemovalOptions>(builder.Configuration.GetSection(MeasurementRemovalOptions.Location));
builder.Services.Configure<ApiKeysOptions>(builder.Configuration.GetSection(ApiKeysOptions.Location));

builder.Services.AddScoped<RequestLocalizationCookiesMiddleware>();
builder.Services.AddScoped<AuditContextMiddleware>();

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
            .AddPageRoute("/AccountUsers", "/a/{AccountLink}/users")
            .AddPageRoute("/AdminOverview", "/adm")
            .AddPageRoute("/AdminAccounts", "/adm/accounts")
            .AddPageRoute("/AdminSensors", "/adm/sensors")
            .AddPageRoute("/AdminQr", "/adm/qr")
            .AddPageRoute("/AccountLoginMessage", "/account/loginmessage")
        .AddPageRoute("/AccountLoginMessageConfirmation", "/account/loginmessageconfirmation")
        .AddPageRoute("/AccountPicker", "/account-picker")
    )
    .AddViewLocalization();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

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
// The app uses a custom auth flow (manual SignInAsync) with claims that do not include the
// standard ClaimTypes.NameIdentifier.  AddIdentity registers SecurityStampValidator which
// fires every 30 minutes, fails to look up the user, and rejects the principal (logging the
// user out).  Disable periodic re-validation by setting the interval to match the token lifespan.
{
    AccountLoginMessageOptions accountLoginMessageOptions =
        builder.Configuration
            .GetSection(AccountLoginMessageOptions.Location)
            .Get<AccountLoginMessageOptions>()
        ?? throw new Exception("AccountLoginMessageOptions not configured");
    builder.Services.Configure<SecurityStampValidatorOptions>(options =>
    {
        options.ValidationInterval = accountLoginMessageOptions.TokenLifespan;
    });
}
builder.Services.AddTransient<IUserStore<IdentityUser>, UserStore>();
builder.Services.AddTransient<IRoleStore<IdentityRole>, RoleStore>();
builder.Services.AddTransient<ITrendService, TrendService>();
builder.Services.AddSingleton<IMcpDocumentationService, McpDocumentationService>();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ILoginSecurityService, LoginSecurityService>();

builder.Services.Configure<GoogleAuthOptions>(builder.Configuration.GetSection(GoogleAuthOptions.Location));
var googleAuthOptions = builder.Configuration.GetSection(GoogleAuthOptions.Location).Get<GoogleAuthOptions>() ?? new GoogleAuthOptions();

{
    AccountLoginMessageOptions accountLoginMessageOptions =
        builder.Configuration
            .GetSection(AccountLoginMessageOptions.Location)
            .Get<AccountLoginMessageOptions>()
        ?? throw new Exception("AccountLoginMessageOptions not configured");
    builder.Services.ConfigureApplicationCookie(options =>
    {
        options.LoginPath = "/login";
        options.ReturnUrlParameter = "r";
        options.ExpireTimeSpan = accountLoginMessageOptions.TokenLifespan;
        options.SlidingExpiration = false;
        options.Cookie.Name = "WaterAlarm.Auth";
        options.Cookie.Path = "/";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.IsEssential = true;
    });
}

// AddIdentity already registered the default scheme; just get the builder to add extra schemes
var authBuilder = builder.Services.AddAuthentication()
    .AddCookie("ExternalCookie", options =>
    {
        options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
        options.Cookie.Name = "__Host-WaterAlarm.External";
        options.Cookie.Path = "/";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.IsEssential = true;
    })
    .AddScheme<ApiKeyAuthenticationSchemeOptions, ApiKeyAuthenticationHandler>("ApiKey", null);

if (googleAuthOptions.IsConfigured)
{
    authBuilder.AddGoogle(options =>
    {
        options.ClientId = googleAuthOptions.ClientId!;
        options.ClientSecret = googleAuthOptions.ClientSecret!;
        options.SignInScheme = "ExternalCookie";
        // Let the Google middleware handle this path internally, then redirect to /GoogleCallback.
        options.CallbackPath = "/signin-google";
        options.CorrelationCookie.SameSite = SameSiteMode.Lax;
        options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Events.OnRemoteFailure = context =>
        {
            context.HandleResponse();
            context.Response.Redirect("/login?error=google_failed");
            return Task.CompletedTask;
        };
    });
}
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
app.UseAuditContext();
app.UseAuthorization();

app.MapRazorPages();
app.MapDefaultControllerRoute();

app.MapControllers();

app.Run();