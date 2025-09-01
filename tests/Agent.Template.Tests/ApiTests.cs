using System.Net;
using System.Net.Http.Json;
using Agent.Core;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Agent.Template.Tests;

public class ApiTests : IClassFixture<WebApplicationFactory<ProgramMarker>>
{
    private readonly WebApplicationFactory<ProgramMarker> _factory;
    public ApiTests(WebApplicationFactory<ProgramMarker> factory)
    {
        _factory = factory.WithWebHostBuilder(_ => { });
    }

    [Fact]
    public async Task Healthz_Is_Healthy()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/healthz");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Chat_Returns_BadRequest_On_Empty_Body()
    {
        var client = _factory.CreateClient();
        var resp = await client.PostAsync("/v1/chat", null);
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
