using Microsoft.EntityFrameworkCore;
using SnackCounter.Api.Middleware;
using SnackCounter.Core.Interfaces;
using SnackCounter.Infrastructure;
using SnackCounter.Infrastructure.Data;
using SnackCounter.Infrastructure.Integrations;
using SnackCounter.Infrastructure.Seed;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Framsikt Snack Counter API", Version = "v1" });
    c.AddSecurityDefinition("DemoUser", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "X-Demo-User-Id",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Demo identity header. Set to an employee ID (e.g. 1). NOT production authentication."
    });
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=snackcounter.db";

builder.Services.AddDbContext<SnackCounterDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddSingleton<FakeHrStore>();
builder.Services.AddScoped<IHRReimbursementGateway, FakeHRReimbursementGateway>(sp =>
    new FakeHRReimbursementGateway(sp.GetRequiredService<FakeHrStore>()));
builder.Services.AddScoped<IChatNotificationService, DbChatNotificationService>();
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddScoped<BudgetService>();
builder.Services.AddScoped<VolunteerService>();
builder.Services.AddScoped<RefillService>();
builder.Services.AddScoped<ReimbursementService>();
builder.Services.AddScoped<ReportingService>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();
app.UseCors();
app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Snack Counter API v1"));
app.MapControllers();

// Auto-migrate and seed on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SnackCounterDbContext>();
    db.Database.Migrate();
    await DatabaseSeeder.SeedAsync(db);
}

app.Run();

public partial class Program { }
