using System.Text.Json.Serialization;
using FluentValidation;
using SWCare.Api.BLC;
using SWCare.Api.BLC.Control.Controls;
using SWCare.Api.BLC.Control.IControls;
using SWCare.Api.Controllers;
using SWCare.Api.Data;
using SWCare.Api.Data.Interceptors;
using SWCare.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, logger) => logger
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

// Data
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<AuditContext>();
builder.Services.AddScoped<AuditSaveChangesInterceptor>();
builder.Services.AddDbContext<SWCareDbContext>((services, db) => db
    .UseSqlServer(builder.Configuration.GetConnectionString("SWCare"))
    .AddInterceptors(services.GetRequiredService<AuditSaveChangesInterceptor>()));
builder.Services.AddScoped<DbSeeder>();

// Login: JWT for who the user is; roles come from the database on each request (CurrentUserFilter).
var jwt = JwtOptions.Load(builder.Configuration, builder.Environment);
builder.Services.AddSingleton(jwt);
builder.Services.AddSingleton<JwtService>();
builder.Services.AddScoped<CurrentUser>();
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = jwt.ValidationParameters();
        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();
                await ApiResponses.WriteAsync(
                    context.Response,
                    StatusCodes.Status401Unauthorized,
                    "Not logged in, or the access token has expired.",
                    ErrorCodes.Unauthorized);
            }
        };
    });

// Every endpoint needs a login unless it says [AllowAnonymous].
builder.Services.AddAuthorization(options =>
    options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

// Business logic used by the controllers
builder.Services.AddScoped<IAuthControl, AuthControl>();
builder.Services.AddScoped<IUserControl, UserControl>();
builder.Services.AddScoped<IDepartmentControl, DepartmentControl>();
builder.Services.AddScoped<IProductControl, ProductControl>();
builder.Services.AddScoped<IDeviceModelControl, DeviceModelControl>();
builder.Services.AddScoped<ISettingControl, SettingControl>();
builder.Services.AddScoped<ITicketControl, TicketControl>();
builder.Services.AddScoped<ITicketActionControl, TicketActionControl>();
builder.Services.AddScoped<INotificationControl, NotificationControl>();
builder.Services.AddScoped<IDashboardControl, DashboardControl>();
builder.Services.AddScoped<TicketNotifier>();
builder.Services.AddScoped<DailyTicketJob>();
builder.Services.AddHostedService<DailyTicketJobRunner>();
builder.Services.AddSingleton<AttachmentStorage>();

builder.Services.AddValidatorsFromAssemblyContaining<Program>(includeInternalTypes: false);
builder.Services
    .AddControllers(mvc =>
    {
        mvc.Filters.Add<CurrentUserFilter>();
        mvc.Filters.Add<FluentValidationFilter>();
    })
    .ConfigureApiBehaviorOptions(api => api.InvalidModelStateResponseFactory = ApiResponses.ValidationFailed)
    .AddJsonOptions(json =>
    {
        // PascalCase, like EasyPark / Easy Next: { Success, Message, Result, Code }.
        json.JsonSerializerOptions.PropertyNamingPolicy = null;
        json.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddOpenApi(openApi => openApi
    .AddDocumentTransformer<BearerSecurityTransformer>()
    .AddOperationTransformer<BearerSecurityTransformer>());

// The web is a static site on another origin (plan 10).
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins(corsOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()));

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
    {
        await scope.ServiceProvider.GetRequiredService<SWCareDbContext>().Database.MigrateAsync();
    }

    await scope.ServiceProvider.GetRequiredService<DbSeeder>().SeedAsync();
}

app.UseExceptionHandler();
app.UseCors();

if (app.Environment.IsDevelopment())
{
    // Swagger UI at /swagger, reading the same document as Scalar (/openapi/v1.json).
    // It is middleware, not an endpoint, so it must come before UseAuthorization or the fallback policy answers 401.
    app.UseSwaggerUI(swagger =>
    {
        swagger.SwaggerEndpoint("/openapi/v1.json", "SWCare.Api v1");
        swagger.DocumentTitle = "SWCare.Api - Swagger";
        swagger.EnablePersistAuthorization();
    });
}

app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
    app.MapScalarApiReference().AllowAnonymous();
}

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous().ExcludeFromDescription();

app.Run();

public partial class Program;
