using System.Security.Claims;
using Core;
using Core.Config;
using Core.Secrets;
using Cuplan.Authentication.Models;
using Cuplan.Authentication.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<ISecretsManager, BitwardenSecretsManager>();

ServiceProvider provider = builder.Services.BuildServiceProvider();
string? accessToken = (provider.GetService(typeof(ISecretsManager)) as ISecretsManager).Get(
    builder.Configuration["ConfigProvider:AccessTokenSecret"]);
HttpClientWrapper httpClientWrapper = new();
httpClientWrapper.AuthorizationBearerToken = accessToken!;

IConfigDownloader configDownloader = new ServerConfigDownloader(httpClientWrapper);
ConfigManager configManager = new(configDownloader);

Result<IConfigProvider, Error<string>> configResult = await configManager.Download(
    builder.Configuration["ConfigProvider:Url"]!,
    builder.Configuration["ConfigProvider:Component"]!);

if (!configResult.IsOk)
    throw new InvalidOperationException(
        $"Failed to get the configuration provider: {configResult.UnwrapErr().Message}");

IConfigProvider configProvider = configResult.Unwrap();
builder.Services.AddSingleton(configProvider);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.Authority = configProvider.Get<string>("application.yaml|IdentityProvider:Authority").Result.Unwrap();
    options.Audience = configProvider.Get<string>("application.yaml|IdentityProvider:Audience").Result.Unwrap();
    options.TokenValidationParameters = new TokenValidationParameters
    {
        NameClaimType = ClaimTypes.NameIdentifier
    };
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend",
        policy =>
        {
            policy.WithOrigins(configProvider.Get<string[]>("application.yaml|Cors:Origins").Result.Unwrap())
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
});

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers().AddNewtonsoftJson();

// Services
builder.Services.AddSingleton<IAuthProvider, Auth0Provider>();

// Models
builder.Services.AddScoped<Authenticator>();

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program
{
}