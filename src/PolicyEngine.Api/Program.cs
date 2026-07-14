using PolicyEngine.Api.Middleware;
using PolicyEngine.Domain.Policies;
using PolicyEngine.Domain.Pricing;
using PolicyEngine.Infrastructure.InMemory;
// [ef-only-start]
using Microsoft.EntityFrameworkCore;
using PolicyEngine.Infrastructure.EntityFramework;
// [ef-only-end]

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSingleton<IPremiumCalculator, StandardPremiumCalculator>();

var persistence = builder.Configuration["Persistence"] ?? "Sqlite";
if (persistence.Equals("InMemory", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddSingleton<IPolicyRepository, InMemoryPolicyRepository>();
}
// [ef-only-start]
else
{
    builder.Services.AddDbContext<PolicyDbContext>(options => options.UseSqlite(
        builder.Configuration.GetConnectionString("Policies") ?? "Data Source=policies.db"));
    builder.Services.AddScoped<IPolicyRepository, EfPolicyRepository>();
}
// [ef-only-end]

// [swagger-start]
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "PolicyEngine API",
        Version = "v1",
        Description = "Insurance policy administration: quote, bind, adjust, cancel, renew."
    });
});
// [swagger-end]

var app = builder.Build();

// [ef-only-start]
if (!persistence.Equals("InMemory", StringComparison.OrdinalIgnoreCase))
{
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<PolicyDbContext>().Database.EnsureCreated();
}
// [ef-only-end]

// [swagger-start]
app.UseSwagger();
app.UseSwaggerUI();
// [swagger-end]

app.UseMiddleware<DomainExceptionMiddleware>();
app.MapControllers();

app.Run();
