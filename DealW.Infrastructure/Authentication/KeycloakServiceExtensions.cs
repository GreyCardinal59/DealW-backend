using DealW.Infrastructure.Authentication.ExternalClients;
using DealW.Infrastructure.Authentication.Handlers;
using KeycloakClient.Generated;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace DealW.Infrastructure.Authentication;

public class DevelopmentSecurityTokenHandler : JwtSecurityTokenHandler
{
    public override ClaimsPrincipal ValidateToken(string token, TokenValidationParameters validationParameters, out SecurityToken validatedToken)
    {
        // Просто парсим токен без валидации
        var jwtToken = ReadJwtToken(token);
        validatedToken = jwtToken;
        
        // Создаем клеймы
        var claims = jwtToken.Claims.ToList();
        
        // Создаем identity
        var identity = new ClaimsIdentity(claims, "DevelopmentJwtBearer");
        
        // Добавляем стандартные клеймы для ASP.NET Core
        if (!identity.HasClaim(c => c.Type == ClaimTypes.NameIdentifier))
        {
            var subClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "sub");
            if (subClaim != null)
            {
                identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, subClaim.Value));
            }
        }
        
        return new ClaimsPrincipal(identity);
    }
}

public static class KeycloakServiceExtensions
{
    public static IServiceCollection AddKeycloakServices(this IServiceCollection services, IConfiguration configuration)
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
        return services;
    }
    
    public static IServiceCollection AddAuthenticationWithKeycloak(this IServiceCollection services, IConfiguration configuration)
    {
        var keycloakConfig = configuration.GetSection("Keycloak").Get<KeycloakOptions>()
                             ?? throw new Exception("Нету конфига для кейклока");
        
        // Логируем конфигурацию для отладки
        Console.WriteLine($"Keycloak MetadataAddress: {keycloakConfig.MetadataAddress}");
        Console.WriteLine($"Keycloak Authority: {keycloakConfig.Authority}");
        
        // Создаем конфигурацию OIDC
        var configManager = new ConfigurationManager<OpenIdConnectConfiguration>(
            keycloakConfig.MetadataAddress,
            new OpenIdConnectConfigurationRetriever(),
            new HttpDocumentRetriever());
        
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // Настраиваем валидацию JWT
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = keycloakConfig.Authority,
                    ValidateAudience = false, // В Keycloak аудитория может не быть настроена
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKeyResolver = (token, securityToken, kid, validationParameters) =>
                    {
                        // Принудительно загружаем ключи без использования кэширования
                        var openIdConfig = configManager.GetConfigurationAsync(CancellationToken.None).GetAwaiter().GetResult();
                        Console.WriteLine($"Загружено {openIdConfig.SigningKeys.Count} ключей подписи");
                        foreach (var key in openIdConfig.SigningKeys)
                        {
                            Console.WriteLine($"Key: {key.KeyId}, Type: {key.GetType().Name}");
                        }
                        return openIdConfig.SigningKeys;
                    },
                    // ClockSkew устанавливаем для большей точности проверки
                    ClockSkew = TimeSpan.FromMinutes(5)
                };
                
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        Console.WriteLine($"Auth failed: {context.Exception}");
                        return Task.CompletedTask;
                    },
                    OnMessageReceived = context =>
                    {
                        if (!string.IsNullOrEmpty(context.Token))
                        {
                            Console.WriteLine($"Token received: {context.Token.Substring(0, Math.Min(20, context.Token.Length))}...");
                            
                            // Добавляем отладочный вывод данных из токена
                            try
                            {
                                var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                                var token = handler.ReadJwtToken(context.Token);
                                
                                Console.WriteLine($"Token issuer: {token.Issuer}");
                                Console.WriteLine($"Token audiences: {string.Join(", ", token.Audiences)}");
                                Console.WriteLine($"Token kid: {token.Header.Kid}");
                                Console.WriteLine($"Token alg: {token.Header.Alg}");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error parsing token: {ex.Message}");
                            }
                        }
                        else
                        {
                            Console.WriteLine("No token in request");
                        }
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        Console.WriteLine("Token validated successfully");
                        
                        // Дополнительно логируем информацию о токене
                        if (context.SecurityToken is System.IdentityModel.Tokens.Jwt.JwtSecurityToken jwtToken)
                        {
                            var identity = context.Principal.Identity as ClaimsIdentity;
                            
                            // Убедимся, что все необходимые клеймы есть
                            if (identity != null)
                            {
                                if (!identity.HasClaim(c => c.Type == ClaimTypes.NameIdentifier))
                                {
                                    // Добавляем sub как nameidentifier если он есть
                                    var subClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "sub");
                                    if (subClaim != null)
                                    {
                                        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, subClaim.Value));
                                    }
                                }
                                
                                // Добавим другие полезные клеймы
                                if (!identity.HasClaim(c => c.Type == ClaimTypes.Name))
                                {
                                    var nameClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "preferred_username" || c.Type == "name");
                                    if (nameClaim != null)
                                    {
                                        identity.AddClaim(new Claim(ClaimTypes.Name, nameClaim.Value));
                                    }
                                }
                            }
                        }
                        
                        return Task.CompletedTask;
                    },
                    OnChallenge = context =>
                    {
                        Console.WriteLine($"Challenge: {context.Error}, {context.ErrorDescription}");
                        return Task.CompletedTask;
                    }
                };
            });
            
        services.AddAuthorization(options =>
        {
            // Добавляем дефолтную политику авторизации
            options.AddPolicy("default", policy =>
            {
                policy.RequireAuthenticatedUser();
            });
        });
        
        return services;
    }
} 