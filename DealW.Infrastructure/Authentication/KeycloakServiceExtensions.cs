using DealW.Infrastructure.Authentication.ExternalClients;
using DealW.Infrastructure.Authentication.Handlers;
using KeycloakClient.Generated;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace DealW.Infrastructure.Authentication;

public static class KeycloakServiceExtensions
{
    public static void AddKeycloakServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<KeycloakOptions>(configuration.GetSection("Keycloak"));
        
        services.AddTransient<KeycloakAdminAuthHandler>();
        
        services.AddHttpContextAccessor();
        
        services.AddDistributedMemoryCache();
        
        services.AddHttpClient<IKeycloakGeneratedClient, KeycloakGeneratedClient>((provider, client) =>
            {
                var options = provider.GetRequiredService<IOptions<KeycloakOptions>>().Value;
                client.BaseAddress = new Uri(options.AdminBaseUrl);  
            })
            .AddHttpMessageHandler<KeycloakAdminAuthHandler>();
        
        services.AddScoped<IKeycloakUserApiClient, KeycloakUserApiClient>();
        services.AddScoped<IKeycloakAuthorizeApiClient, KeycloakAuthorizeApiClient>();
    }
    
    public static void AddAuthenticationWithKeycloak(this IServiceCollection services, IConfiguration configuration)
    {
        var keycloakConfig = configuration.GetSection("Keycloak").Get<KeycloakOptions>()
                             ?? throw new Exception("Нету конфига для кейклока");
        
        var keycloakAuthority = keycloakConfig.Authority;
        
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = keycloakAuthority; // Это ключевая строка
                options.RequireHttpsMetadata = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = false,
                    ClockSkew = TimeSpan.FromMinutes(5)
                };
            
                // Добавляем метадату для автоматического получения ключей
                options.MetadataAddress = keycloakConfig.MetadataAddress;
            
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        Console.WriteLine($"Auth failed: {context.Exception}");
                        return Task.CompletedTask;
                    },
                
                    OnChallenge = context =>
                    {
                        Console.WriteLine($"Challenge: {context.Error}, {context.ErrorDescription}");
                        return Task.CompletedTask;
                    }
                };
        });

    services.AddAuthorization();
    }
}