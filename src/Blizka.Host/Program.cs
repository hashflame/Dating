using Blizka.Api;
using Blizka.Api.Auth;
using Blizka.Api.ErrorHandling;
using Blizka.App;
using Blizka.Data;
using Quartz;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddYamlFile("appsettings.yaml", optional: false, reloadOnChange: true)
    .AddYamlFile($"appsettings.{builder.Environment.EnvironmentName}.yaml", optional: true, reloadOnChange: true);

builder.Host.UseSerilog((context, services, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
const string corsPolicyName = "TelegramMiniApp";
builder.Services.AddCors(options =>
{
    options.AddPolicy(corsPolicyName, policy =>
        policy.WithOrigins(corsOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod());
});

builder.Services.AddExceptionHandler<BlizkaExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

builder.Services.AddApiLayer(builder.Configuration);
builder.Services.AddAppLayer();
builder.Services.AddDataLayer(builder.Configuration);

builder.Services.AddQuartz();
builder.Services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors(corsPolicyName);
app.UseMiddleware<TelegramAuthMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program;
