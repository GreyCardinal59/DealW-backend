using DealW.API.Contracts;
using DealW.Infrastructure.Authentication.Models;

namespace DealW.Infrastructure.Authentication.ExternalClients;

public interface IKeycloakUserApiClient
{
    Task<KeycloakUserData?> GetUserAsync(Guid userId, CancellationToken cancellationToken);
    
    Task<IReadOnlyCollection<KeycloakUsersListItemData>> GetUsersByFilterAsync(KeycloakUserFilter filter, CancellationToken cancellationToken);
    
    // Task EmailVerificationAsync(Guid userId, string email, CancellationToken cancellationToken);
    //
    // Task UpdateUserAsync(KeycloakUserData userData, CancellationToken cancellationToken);
    //
    // Task UpdateUserFieldAsync(KeycloakUserData userData, CancellationToken cancellationToken);
    
}