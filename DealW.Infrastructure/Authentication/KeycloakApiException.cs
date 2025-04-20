namespace DealW.Infrastructure.Authentication;

public class KeycloakApiException : Exception
{
    /// <summary>
    /// Создаёт экземпляр <see cref="KeycloakApiException"/>
    /// </summary>
    /// <param name="message">Сообщение об ошибке.</param>
    public KeycloakApiException(string? message) : base(message)
    {
    }

    /// <summary>
    /// Создаёт экземпляр <see cref="KeycloakApiException"/>
    /// </summary>
    /// <param name="message">Сообщение об ошибке.</param>
    /// <param name="innerException">Внутренее исключение.</param>
    public KeycloakApiException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}