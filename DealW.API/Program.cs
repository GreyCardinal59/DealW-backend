using DealW.Infrastructure.Authentication;
using DealW.API.Extensions;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;

services.AddControllers();

services.AddKeycloakServices(configuration);

services.AddAuthenticationWithKeycloak(configuration);

services.AddAuthorization();

services.AddEndpointsApiExplorer();
services.AddSwaggerGenWithAuth(configuration);
// services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
