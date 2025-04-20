namespace DealW.API.Contracts;

public class KeycloakUserDto
{
    public string Id { get; set; }
    public string Username { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public bool EmailVerified { get; set; }
    public bool? Enabled { get; set; }
}