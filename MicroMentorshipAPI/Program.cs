
using MicroMentorshipAPI.Hubs;
using MicroMentorshipAPI.Data;
using MicroMentorshipAPI.Services;
using MicroMentorshipAPI.Processors;
using MicroMentorshipAPI.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Microsoft.IdentityModel.Tokens;
using System.Reflection;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSignalR();
builder.Services.AddSwaggerGen(options =>
{
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);

    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "My API",
        Version = "v1"
    });
});

builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<AuthorizeProcessor>();
builder.Services.AddScoped<ProfileProcessor>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddSingleton<ChatMatchService>();

var postgresConnectionString = GetRequiredConfigurationValue(
    builder.Configuration,
    "ConnectionStrings:postgreConnection");
var jwtSecurityKey = GetRequiredConfigurationValue(
    builder.Configuration,
    "JwtSettings:securityKey");
var allowedOrigins = GetAllowedOrigins(builder.Configuration);

builder.Services.AddDbContext<AppDBContext>(options =>
    options.UseNpgsql(postgresConnectionString));

builder.Services.AddCors(options =>
{
    options.AddPolicy("UiClients", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddAuthentication(item =>
{
    item.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    item.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(item =>
{
    item.RequireHttpsMetadata = true;
    item.SaveToken = true;
    item.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecurityKey)),
        ClockSkew = TimeSpan.Zero
    };
    item.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;

            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/chat-hub"))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        }
    };
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("UiClients");

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();
app.MapHub<ChatHub>("/chat-hub");

app.Run();

static string GetRequiredConfigurationValue(IConfiguration configuration, string key)
{
    var value = configuration[key];

    if (!string.IsNullOrWhiteSpace(value))
    {
        return value;
    }

    throw new InvalidOperationException(
        $"Missing required configuration value '{key}'. " +
        "Use ASP.NET Core user secrets for local development or environment variables for deployment.");
}

static string[] GetAllowedOrigins(IConfiguration configuration)
{
    var configuredOrigins = configuration["Cors:AllowedOrigins"];

    if (string.IsNullOrWhiteSpace(configuredOrigins))
    {
        return ["http://localhost:4200"];
    }

    return configuredOrigins
        .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
}
