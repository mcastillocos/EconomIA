using System.Net;

namespace EconomIA.API.Tests.Controllers;

public class HealthControllerTests
{
    private EconomIAWebFactory CreateFactory() => new();

    [Fact]
    public async Task HealthLive_Devuelve200()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health/live");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task HealthReady_DevuelveRespuesta()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health/ready");
        Assert.True(
            response.StatusCode is HttpStatusCode.OK or HttpStatusCode.ServiceUnavailable,
            $"Esperado 200 o 503, recibido {response.StatusCode}");
    }

    [Fact]
    public async Task HealthGeneral_DevuelveRespuesta()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health");
        Assert.True(
            response.StatusCode is HttpStatusCode.OK or HttpStatusCode.ServiceUnavailable,
            $"Esperado 200 o 503, recibido {response.StatusCode}");
    }
}
