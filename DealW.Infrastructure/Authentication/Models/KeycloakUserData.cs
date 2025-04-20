namespace DealW.Infrastructure.Authentication.Models;

/// <summary>
/// Данные пользователя для Keycloak.
/// </summary>
public class KeycloakUserData(Guid userId)
{
    public Guid UserId { get; } = userId;

    public string? Email { get; set; }
    
    public bool IsEmailVerified { get; set; }
    
    public DateTime? CreateDate { get; set; }
    
    public IReadOnlyCollection<KeycloakUserRoleData> Roles { get; set; } = [];
    
    /// <summary>
    /// Признак, что пользователь активен.
    /// </summary>
    public bool? IsEnabled { get; set; }
    
    /// <summary>
    /// Признак, что первый логин за всё существование.
    /// </summary>
    public bool IsFirstLogin { get; set; }
}