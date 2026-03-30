using HabitTrackerWeb.Core.Contracts.Infrastructure;
using HabitTrackerWeb.Core.Contracts.Observers;
using HabitTrackerWeb.Core.Contracts.Repositories;
using HabitTrackerWeb.Core.Contracts.Services;
using HabitTrackerWeb.Core.Entities;
using HabitTrackerWeb.Data;
using HabitTrackerWeb.Data.Seed;
using HabitTrackerWeb.Repositories;
using HabitTrackerWeb.Services;
using HabitTrackerWeb.Services.Infrastructure;
using HabitTrackerWeb.Services.Logging;
using HabitTrackerWeb.Services.Observers;
using HabitTrackerWeb.Services.ExternalSync;
using HabitTrackerWeb.Services.Push;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=habittracker.db"));

builder.Services
    .AddDefaultIdentity<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

builder.Services.Configure<PushNotificationOptions>(
    builder.Configuration.GetSection(PushNotificationOptions.SectionName));

builder.Services.AddHttpContextAccessor();
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddHttpClient("ExternalSync.GitHub", client =>
{
    client.DefaultRequestHeaders.UserAgent.ParseAdd("HabitTrackerWeb-Sync/1.0");
});
builder.Services.AddHttpClient("ExternalSync.LeetCode", client =>
{
    client.DefaultRequestHeaders.UserAgent.ParseAdd("HabitTrackerWeb-Sync/1.0");
});
builder.Services.AddHttpClient("ExternalSync.Codeforces", client =>
{
    client.DefaultRequestHeaders.UserAgent.ParseAdd("HabitTrackerWeb-Sync/1.0");
});

builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IHabitRepository, HabitRepository>();
builder.Services.AddScoped<IHabitLogRepository, HabitLogRepository>();
builder.Services.AddScoped<IHabitMetricRepository, HabitMetricRepository>();
builder.Services.AddScoped<IAchievementRepository, AchievementRepository>();
builder.Services.AddScoped<IExternalAccountLinkRepository, ExternalAccountLinkRepository>();
builder.Services.AddScoped<IEloRatingRepository, EloRatingRepository>();
builder.Services.AddScoped<IEloRatingChangeRepository, EloRatingChangeRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

builder.Services.AddScoped<IHabitService, HabitService>();
builder.Services.AddScoped<IExternalIntegrationService, ExternalIntegrationService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
builder.Services.AddScoped<IHabitLoggingService, HabitLoggingService>();
builder.Services.AddScoped<IHabitOutcomeMonitorService, HabitOutcomeMonitorService>();
builder.Services.AddScoped<IExternalSyncService, ExternalSyncService>();
builder.Services.AddScoped<IRewardService, RewardService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IPsychologyTipsService, PsychologyTipsService>();
builder.Services.AddSingleton<IPushNotificationService, PushNotificationService>();
builder.Services.AddHostedService<PushNotificationBackgroundService>();
builder.Services.AddHostedService<ExternalSyncBackgroundService>();
builder.Services.AddHostedService<HabitOutcomeBackgroundService>();

builder.Services.AddScoped<IStreakCalculatorObserver, StreakCalculatorObserver>();
builder.Services.AddScoped<IAchievementObserver, AchievementObserver>();
builder.Services.AddScoped<IEloCalculationObserver, EloCalculationObserver>();
builder.Services.AddScoped<IHabitOutcomeSubscriber>(sp => sp.GetRequiredService<IEloCalculationObserver>());
builder.Services.AddScoped<IHabitOutcomePublisher, HabitOutcomePublisher>();

builder.Services.AddScoped<IExternalActivityProvider, GitHubActivityProvider>();
builder.Services.AddScoped<IExternalActivityProvider, LeetCodeActivityProvider>();
builder.Services.AddScoped<IExternalActivityProvider, CodeforcesActivityProvider>();

builder.Services.AddScoped<IExternalSyncHandler, GitHubExternalSyncHandler>();
builder.Services.AddScoped<IExternalSyncHandler, LeetCodeExternalSyncHandler>();
builder.Services.AddScoped<IExternalSyncHandler, CodeforcesExternalSyncHandler>();

builder.Services.AddTransient<ValidateHabitIsActiveHandler>();
builder.Services.AddTransient<ValidateNotLoggedTodayHandler>();
builder.Services.AddTransient<PersistHabitLogHandler>();

builder.Services.AddScoped<IDataSeeder, DataSeeder>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages().WithStaticAssets();

using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<IDataSeeder>();
    await seeder.SeedAsync();
}

app.Run();
