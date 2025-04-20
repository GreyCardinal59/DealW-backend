using Microsoft.AspNetCore.Authentication;

namespace DealW.Infrastructure.Authentication;

public class KeycloakAuthorizationOptions : AuthenticationSchemeOptions
{
    public static readonly string Scheme = "Keycloak";
    
    public string Realm { get; set; } = string.Empty;
    
    public string LoginUrl { get; set; } = string.Empty;
    
    public string Authority { get; set; } = string.Empty;
    
    public string ClientId { get; set; } = string.Empty;
    
    public string ClientUuid { get; set; } = string.Empty;
    
    public string ClientSecret { get; set; } = string.Empty;
    
    public bool RequireHttpsMetadata { get; set; }
    
    public string MetadataAddress { get; set; } = string.Empty;
    
    public string ApiAdminBaseUrl { get; set; } = string.Empty;
    
    public string ApiClientId { get; set; } = string.Empty;
    
    public string ApiClientSecret { get; set; } = string.Empty;
}