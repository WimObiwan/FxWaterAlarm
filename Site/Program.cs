using System.Globalization;
using Core;
using Core.Communication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Site;
using Site.Identity;
using Site.Middlewares;
using Site.Pages;
using Site.Utilities;
using Westwind.AspNetCore.Markdown;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.Local.json", true, true);

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

builder.Services.Configure<MessagesOptions>(builder.Configuration.GetSection(MessagesOptions.Location));

builder.Services.AddRazorPages(o =>
        o.Conventions
            .AddPageRoute("/Sensor", "/sensor/{SensorLink}")
            .AddPageRoute("/Sensor", "/s/{SensorLink}")
            .AddPageRoute("/Account", "/a/{AccountLink}")
            .AddPageRoute("/AccountSensor", "/a/{AccountLink}/s/{SensorLink}")
            .AddPageRoute("/AdminOverview", "/adm")
            .AddPageRoute("/AdminAccounts", "/adm/accounts")
            .AddPageRoute("/AdminSensors", "/adm/sensors")
            .AddPageRoute("/AccountLogin", "/account/login")
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

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)  
    .AddCookie(options =>  
    {  
        options.LoginPath = "/Account/Login";  
    });
builder.Services.AddSingleton<IAuthorizationHandler, AdminRequirementHandler>(); 

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy =>
        policy.Requirements.Add(new AdminRequirement()));
});

//builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
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
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
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