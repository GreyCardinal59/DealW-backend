namespace DealW.Infrastructure.Authentication.Models;

public class KeycloakUserRoleData(string id)
{
    public string Id { get; } = id;

    public string Name { get; set; } = string.Empty;
}