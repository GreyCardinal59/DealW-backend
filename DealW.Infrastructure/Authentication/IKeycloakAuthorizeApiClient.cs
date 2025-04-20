using DealW.API.Contracts;

namespace DealW.Infrastructure.Authentication;

public interface IKeycloakAuthorizeApiClient
{
    Task<List<KeycloakUserDto>> GetUsers(int take, int? skip, CancellationToken cancellationToken = default);
} 