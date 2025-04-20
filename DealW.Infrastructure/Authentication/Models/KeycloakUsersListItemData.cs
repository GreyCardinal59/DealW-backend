namespace DealW.Infrastructure.Authentication.Models;

public class KeycloakUsersListItemData(Guid userId)
{
    public Guid UserId { get; } = userId;

    public string? Email { get; set; }
    
    public IReadOnlyCollection<KeycloakUserRoleData> Roles { get; set; } = [];
    
    public bool? IsEnabled { get; set; }
}