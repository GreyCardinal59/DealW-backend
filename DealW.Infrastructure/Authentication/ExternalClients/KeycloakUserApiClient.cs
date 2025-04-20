using System.Text.Json;
using DealW.API.Contracts;
using DealW.Infrastructure.Authentication.Models;
using KeycloakClient.Generated;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DealW.Infrastructure.Authentication.ExternalClients;

public class KeycloakUserApiClient : IKeycloakUserApiClient
{
    
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IKeycloakGeneratedClient _generatedApi;
    private readonly KeycloakAuthorizationOptions _options;
    private readonly ILogger<KeycloakUserApiClient> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IDistributedCache _distributedCache;
    
    private static readonly string UserRolesCacheKey = "UserRoles_{0}";
    
    public KeycloakUserApiClient(
        IOptions<KeycloakAuthorizationOptions> options,
        IKeycloakGeneratedClient generatedApi,
        ILogger<KeycloakUserApiClient> logger,
        IHttpClientFactory httpClientFactory,
        IHttpContextAccessor httpContextAccessor,
        IDistributedCache distributedCache)
    {
        _options = options.Value;
        _generatedApi = generatedApi;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _httpContextAccessor = httpContextAccessor;
        _distributedCache = distributedCache;
    }

    public async Task<KeycloakUserData?> GetUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await GetKeycloakUserAsync(userId, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var userRoles = await _generatedApi.ClientsAll9Async(_options.Realm, user.Id!.ToString(), _options.ClientUuid, cancellationToken);

        var userLoginEvents = await _generatedApi.EventsAllAsync(
            client: _options.ClientId, 
            dateFrom: null, 
            dateTo: null, 
            direction: null, 
            first: null, 
            ipAddress: null, 
            max: null, 
            type: ["LOGIN", "CLIENT_LOGIN"], 
            user: user.Id!, 
            realm: _options.Realm,
            cancellationToken: cancellationToken);
            
        return MapUserResponse(userId, user, userRoles, userLoginEvents);
    }
    
    public async Task<IReadOnlyCollection<KeycloakUsersListItemData>> GetUsersByFilterAsync(KeycloakUserFilter filter, CancellationToken cancellationToken)
    {
        var users = await GetUsersByFilterInternalAsync(filter, cancellationToken);
        if (users == null || users.Count == 0)
        {
            return [];
        }

        var roles = await GetUserRolesAsync(users, cancellationToken);

        return users.Select(u => MapUsersListItemResponse(Guid.Parse(u.Id!), u, roles[u.Id!])).ToArray();
    }
    
    private async Task<UserRepresentation?> GetKeycloakUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        try
        {
            return await _generatedApi.UsersGET2Async(
                userProfileMetadata: true, 
                realm: _options.Realm, 
                user_id: userId.ToString(), 
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            const string message = "Произошла ошибка при получении пользователя по идентификатору";
            _logger.LogError(ex, message + ": {UserId}", userId);
            throw new KeycloakApiException(message, ex);
        }
    }
    
    private async Task<UserRepresentation?> GetUserByFilterAsync(KeycloakUserFilter filter, CancellationToken cancellationToken)
    {
        var users = await GetUsersByFilterInternalAsync(filter, cancellationToken);
        return users.FirstOrDefault();
    }
    
    private async Task<ICollection<UserRepresentation>> GetUsersByFilterInternalAsync(KeycloakUserFilter filter, CancellationToken cancellationToken)
    {
        try
        {
            var search = GetSearchUserFilter(filter);

            var users = await _generatedApi.UsersAll3Async(
                briefRepresentation: null,
                email: filter.Email, 
                emailVerified: null,
                enabled: filter.Enabled,
                exact: null,
                first: null,
                firstName: null,
                idpAlias: null,
                idpUserId: null,
                lastName: null,
                max: filter.Max,
                q: null,
                search: search,
                username: filter.Username,
                realm: _options.Realm,
                cancellationToken: cancellationToken);
            return users;
        }
        catch (Exception ex)
        {
            const string message = "Произошла ошибка при получении списка пользователей по фильтру";
            _logger.LogError(ex, message + ": {Filter}", JsonSerializer.Serialize(filter));
            throw new KeycloakApiException(message, ex);
        }
    }
    
    private async Task<Dictionary<string, ICollection<RoleRepresentation>>> GetUserRolesAsync(
        ICollection<UserRepresentation> users, CancellationToken cancellationToken)
    {
        var roles = new Dictionary<string, ICollection<RoleRepresentation>>();
        foreach (var user in users)
        {
            try
            {
                var userRoles = await _generatedApi.ClientsAll9Async(_options.Realm, user.Id!.ToString(), _options.ClientUuid, cancellationToken);
                roles.Add(user.Id!, userRoles);
            }
            catch (Exception ex)
            {
                const string message = "Произошла ошибка при получении ролей пользователя по идентификатору";
                _logger.LogError(ex, message + ": {UserId}", user.Id);
                throw new KeycloakApiException(message, ex);
            }
        }

        return roles;
    }
    
    private static string? GetSearchUserFilter(KeycloakUserFilter filter)
    {
        var search = new List<string>();

        // if (!string.IsNullOrEmpty(filter.PhoneNumber))
        // {
        //     search.Add($"*{filter.PhoneNumber}*");
        // }
        if (!string.IsNullOrEmpty(filter.FirstName))
        {
            search.Add($"*{filter.FirstName}*");
        }
        if (!string.IsNullOrEmpty(filter.LastName))
        {
            search.Add($"*{filter.LastName}*");
        }

        return search.Count == 0 ? null : string.Join(" ", search);
    }
    
    private static KeycloakUserData MapUserResponse(Guid userId, UserRepresentation userData, ICollection<RoleRepresentation> roles, IEnumerable<EventRepresentation> events)
    {
        return new KeycloakUserData(userId)
        {
            // FirstName = userData.FirstName!,
            // LastName = userData.LastName!,
            // Sex = EnumHelper.GetEnumValue<KeycloakSexType>(GetFromAttributes(userData.Attributes, UserAttributes.Sex)),
            // BirthDate = GetFromAttributes(userData.Attributes, UserAttributes.BirthDate).ToDateTime().SpecifyKind(DateTimeKind.Utc),
            // IsBirthDateChanged = GetFromAttributes(userData.Attributes, UserAttributes.IsBirthDateChanged).ToNullBool(),
            Email = userData.Email!,
            // IsEmailVerified = userData.EmailVerified ?? false,
            // PhoneNumber = GetFromAttributes(userData.Attributes, UserAttributes.PhoneNumber) ?? string.Empty,
            // IsPhoneNumberVerified = GetFromAttributes(userData.Attributes, UserAttributes.PhoneNumberVerified)?.ToBool() ?? false,
            IsEnabled = userData.Enabled,  // ?? false,
            CreateDate = userData.CreatedTimestamp.HasValue ? DateTime.UnixEpoch.AddMilliseconds(userData.CreatedTimestamp.Value) : null,
            Roles = roles.Select(r => new KeycloakUserRoleData(r.Id!) { Name = r.Name! }).ToArray(),
            IsFirstLogin = events.Count() == 1
        };
    }
    
    private static KeycloakUsersListItemData MapUsersListItemResponse(Guid userId, UserRepresentation userData, ICollection<RoleRepresentation> roles)
    {
        return new KeycloakUsersListItemData(userId)
        {
            Email = userData.Email!,
            // FirstName = userData.FirstName!,
            // LastName = userData.LastName!,
            // PhoneNumber = GetFromAttributes(userData.Attributes, UserAttributes.PhoneNumber) ?? string.Empty,
            IsEnabled = userData.Enabled, //  ?? false,
            Roles = roles.Select(r => new KeycloakUserRoleData (r.Id!) { Name = r.Name! }).ToArray()
        };
    }
}