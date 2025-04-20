namespace DealW.API.Contracts;

public class KeycloakUserFilter
{
    public bool? BriefRepresentation { get; set; }
    public string Email { get; set; }
    public bool? EmailVerified { get; set; }
    public bool? Enabled { get; set; }
    public bool? Exact { get; set; }
    public int? First { get; set; }
    public string FirstName { get; set; }
    public string IdpAlias { get; set; }
    public string IdpUserId { get; set; }
    public string LastName { get; set; }
    public int? Max { get; set; }
    public string Query { get; set; }
    public string Search { get; set; }
    public string Username { get; set; }
}