namespace DealW.Infrastructure.Authentication;

public class KeycloakOptions
{
    public string ClientId { get; set; }
    public string ClientUuid { get; set; }
    public string ClientSecret { get; set; }
    public string Realm { get; set; }
    public string MetadataAddress { get; set; }
    public string AdminClient { get; set; }
    public string AdminSecret { get; set; }
    public string AdminBaseUrl { get; set; }
    public string AdminRealm { get; set; }
    public string Authority { get; set; }
    public string AuthorizationUrl { get; set; }
} 