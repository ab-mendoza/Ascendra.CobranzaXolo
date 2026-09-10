using Ascendra.CobranzaXolo.Api.Security;
using Ascendra.CobranzaXolo.Application.Dashboard;
using Ascendra.CobranzaXolo.Application.Diagnostics;
using Ascendra.CobranzaXolo.Domain.Collections;
using Ascendra.CobranzaXolo.Infrastructure.SqlServer;

using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

const string FrontendCorsPolicy = "Frontend";
var frontendOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? ["http://localhost:5173"];

// SQL Server is configured through protected configuration; the API remains read-only.
builder.Services.AddControllers();
builder.Services.AddHealthChecks();
builder.Services.AddProblemDetails();
builder.Services.Configure<SimpleAuthenticationOptions>(
    builder.Configuration.GetSection("Authentication:Simple"));
builder.Services.AddAuthentication(BasicAuthenticationHandler.AuthenticationScheme)
    .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>(
        BasicAuthenticationHandler.AuthenticationScheme, _ => { });
builder.Services.AddAuthorization();
builder.Services.AddCors(options => options.AddPolicy(FrontendCorsPolicy, policy =>
    policy
        .WithOrigins(frontendOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()));
builder.Services.AddSingleton(
    new SqlServerConnectionFactory(builder.Configuration.GetConnectionString("SqlServer")));
builder.Services.AddScoped<IDataSourceDiagnostics, SqlServerDataSourceDiagnostics>();
builder.Services.AddScoped<IDashboardDataSource, SqlServerDashboardDataSource>();
builder.Services.AddSingleton<PaymentAttributionEngine>();
builder.Services.AddScoped<DashboardReadService>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseCors(FrontendCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers().RequireAuthorization();
app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Ok(new
{
    application = "Ascendra · Cobranza Xolo",
    status = "ready"
}));

app.Run();

public partial class Program
{
}