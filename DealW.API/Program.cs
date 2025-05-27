using DealW.Infrastructure.Authentication;
using DealW.API.Extensions;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;

services.AddControllers();

services.AddKeycloakServices(configuration);

services.AddAuthenticationWithKeycloak(configuration);

services.AddResponseCaching();
services.AddAuthorization();

services.AddEndpointsApiExplorer();
services.AddSwaggerGenWithAuth(configuration);
// services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");

        c.OAuthClientId(builder.Configuration["Keycloak:ClientId"]);
        c.OAuthClientSecret(builder.Configuration["Keycloak:ClientSecret"]);
        c.OAuthAppName("Swagger UI - Keycloak");
        c.OAuthUsePkce(); 
    });
}

app.UseRouting();
app.UseResponseCaching();
app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
