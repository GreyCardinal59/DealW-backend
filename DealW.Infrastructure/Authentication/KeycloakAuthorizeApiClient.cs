using System.Net;
using DealW.API.Contracts;
using KeycloakClient.Generated;
using Microsoft.Extensions.Options;

namespace DealW.Infrastructure.Authentication;

public class KeycloakAuthorizeApiClient(
    IKeycloakGeneratedClient keycloakGeneratedClient,
    IOptions<KeycloakOptions> keycloakOptions)
    : IKeycloakAuthorizeApiClient
{
    private readonly KeycloakOptions _keycloakOptions = keycloakOptions.Value;

    public async Task<List<KeycloakUserDto>> GetUsers(int take, int? skip, CancellationToken cancellationToken = default)
    {
        var users = await GetUsersByFilterAsync(new KeycloakUserFilter
        {
            First = skip,
            Max = take
        }, cancellationToken);

        var response = users
            .OrderBy(u => u.Id)
            .Select(MapUserResponse)
            .ToList();
        return response;
    }

    private async Task<ICollection<UserRepresentation>> GetUsersByFilterAsync(KeycloakUserFilter filter = null, CancellationToken cancellationToken = default)
    {
        try
        {
            filter ??= new KeycloakUserFilter();

            var users = await keycloakGeneratedClient.UsersAll3Async(
                briefRepresentation: filter.BriefRepresentation,
                email: filter.Email,
                emailVerified: filter.EmailVerified,
                enabled: filter.Enabled,
                exact: filter.Exact,
                first: filter.First,
                firstName: filter.FirstName,
                idpAlias: filter.IdpAlias,
                idpUserId: filter.IdpUserId,
                lastName: filter.LastName,
                max: filter.Max,
                q: filter.Query,
                search: filter.Search,
                username: filter.Username,
                realm: _keycloakOptions.Realm,
                cancellationToken: cancellationToken
            );

            return users.Count == 0 ? [] : users;
        }
        catch (Exception ex)
        {
            throw new KeycloakGeneratedApiException(ex.Message, (int)HttpStatusCode.InternalServerError, null, null, ex);
        }
    }

    private static KeycloakUserDto MapUserResponse(UserRepresentation user)
    {
        return new KeycloakUserDto
        {
            Id = user.Id,
            Username = user.Username,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            EmailVerified = user.EmailVerified,
            Enabled = user.Enabled
        };
    }
} 