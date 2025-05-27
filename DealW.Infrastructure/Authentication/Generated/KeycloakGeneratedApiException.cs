namespace DealW.Infrastructure.Authentication;

public class KeycloakGeneratedApiException(
    string message,
    int statusCode,
    string response,
    IReadOnlyDictionary<string, IEnumerable<string>> headers,
    Exception innerException)
    : Exception(message, innerException)
{
    public int StatusCode { get; } = statusCode;
    private string Response { get; } = response;
    
    public IReadOnlyDictionary<string, IEnumerable<string>> Headers { get; } = headers;

    public override string ToString()
    {
        return $"HTTP Response: \n\n{Response}\n\n{base.ToString()}";
    }
} 