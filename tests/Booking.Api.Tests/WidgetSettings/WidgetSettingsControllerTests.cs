using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Booking.Api.Contracts.WidgetSettings;
using Booking.Api.Tests.Support;
using Booking.Infrastructure.Identity;

namespace Booking.Api.Tests.WidgetSettings;

public sealed class WidgetSettingsControllerTests : IClassFixture<BookingApiFactory>
{
    private static readonly byte[] ValidPng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");

    private readonly BookingApiFactory _factory;

    public WidgetSettingsControllerTests(BookingApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task OwnerCanManageAndPublicCanReadWidgetOrigins()
    {
        await _factory.ResetDatabaseAsync();
        using var ownerClient = _factory.CreateClient();
        var seeded = await _factory.SeedUserAsync(BookingRoles.Owner);
        var token = await _factory.LoginAsync(ownerClient, seeded.User.Email!);
        ownerClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        using var updateResponse = await ownerClient.PutAsJsonAsync(
            "/api/admin/restaurant/widget-origins",
            new SetWidgetOriginsApiRequest(
                ["https://example.com/", "http://localhost:8080"]));

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        using var publicClient = _factory.CreateClient();
        var origins = await publicClient.GetFromJsonAsync<WidgetOriginsApiResponse>(
            $"/api/restaurants/{seeded.Restaurant.Id}/widget-origins");

        Assert.NotNull(origins);
        Assert.Equal(
            ["http://localhost:8080", "https://example.com"],
            origins.Origins);
    }

    [Fact]
    public async Task StaffCannotUpdateWidgetOrigins()
    {
        await _factory.ResetDatabaseAsync();
        using var client = _factory.CreateClient();
        var token = await _factory.SeedUserAndLoginAsync(client, BookingRoles.Staff);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        using var response = await client.PutAsJsonAsync(
            "/api/admin/restaurant/widget-origins",
            new SetWidgetOriginsApiRequest(["https://example.com"]));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task OwnerCanManageAndPublicCanReadWidgetBranding()
    {
        await _factory.ResetDatabaseAsync();
        using var ownerClient = _factory.CreateClient();
        var seeded = await _factory.SeedUserAsync(BookingRoles.Owner);
        var token = await _factory.LoginAsync(ownerClient, seeded.User.Email!);
        ownerClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        using var updateResponse = await ownerClient.PutAsJsonAsync(
            "/api/admin/restaurant/widget-branding",
            new SetWidgetBrandingApiRequest(
                "#345678",
                "#c98754",
                "Welkom bij de testtafel.",
                "https://example.com/logo.png"));

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        using var publicClient = _factory.CreateClient();
        var branding = await publicClient.GetFromJsonAsync<WidgetBrandingApiResponse>(
            $"/api/restaurants/{seeded.Restaurant.Id}/widget-branding");

        Assert.NotNull(branding);
        Assert.Equal("#345678", branding.PrimaryColor);
        Assert.Equal("#c98754", branding.AccentColor);
        Assert.Equal("Welkom bij de testtafel.", branding.WelcomeText);
        Assert.Equal("https://example.com/logo.png", branding.LogoUrl);
    }

    [Fact]
    public async Task StaffCannotUpdateWidgetBranding()
    {
        await _factory.ResetDatabaseAsync();
        using var client = _factory.CreateClient();
        var token = await _factory.SeedUserAndLoginAsync(client, BookingRoles.Staff);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        using var response = await client.PutAsJsonAsync(
            "/api/admin/restaurant/widget-branding",
            new SetWidgetBrandingApiRequest(
                "#345678",
                "#c98754",
                null,
                null));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task OwnerCanUploadReadAndDeleteWidgetLogo()
    {
        await _factory.ResetDatabaseAsync();
        using var ownerClient = _factory.CreateClient();
        var seeded = await _factory.SeedUserAsync(BookingRoles.Owner);
        var token = await _factory.LoginAsync(ownerClient, seeded.User.Email!);
        ownerClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        using var uploadContent = CreateLogoUpload(ValidPng, "logo.png", "image/png");
        using var uploadResponse = await ownerClient.PostAsync(
            "/api/admin/restaurant/widget-logo",
            uploadContent);

        Assert.Equal(HttpStatusCode.OK, uploadResponse.StatusCode);
        var branding = await uploadResponse.Content.ReadFromJsonAsync<WidgetBrandingApiResponse>();
        Assert.NotNull(branding);
        Assert.NotNull(branding.LogoUrl);
        Assert.Contains(
            $"/api/restaurants/{seeded.Restaurant.Id}/widget-logo?v=",
            branding.LogoUrl,
            StringComparison.Ordinal);

        using var publicClient = _factory.CreateClient();
        var logoPath = new Uri(branding.LogoUrl).PathAndQuery;
        using var logoResponse = await publicClient.GetAsync(logoPath);

        Assert.Equal(HttpStatusCode.OK, logoResponse.StatusCode);
        Assert.Equal("image/png", logoResponse.Content.Headers.ContentType?.MediaType);
        Assert.Contains(
            "immutable",
            logoResponse.Headers.GetValues("Cache-Control").Single(),
            StringComparison.Ordinal);
        Assert.Equal(ValidPng, await logoResponse.Content.ReadAsByteArrayAsync());

        using var deleteResponse = await ownerClient.DeleteAsync("/api/admin/restaurant/widget-logo");
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
        var deletedBranding = await deleteResponse.Content.ReadFromJsonAsync<WidgetBrandingApiResponse>();
        Assert.NotNull(deletedBranding);
        Assert.Null(deletedBranding.LogoUrl);

        using var missingLogoResponse = await publicClient.GetAsync(logoPath);
        Assert.Equal(HttpStatusCode.NotFound, missingLogoResponse.StatusCode);
    }

    [Fact]
    public async Task UploadWidgetLogoRejectsInvalidImageContent()
    {
        await _factory.ResetDatabaseAsync();
        using var ownerClient = _factory.CreateClient();
        var seeded = await _factory.SeedUserAsync(BookingRoles.Owner);
        var token = await _factory.LoginAsync(ownerClient, seeded.User.Email!);
        ownerClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        using var uploadContent = CreateLogoUpload(
            "not-an-image"u8.ToArray(),
            "logo.png",
            "image/png");
        using var response = await ownerClient.PostAsync(
            "/api/admin/restaurant/widget-logo",
            uploadContent);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task StaffCannotUploadWidgetLogo()
    {
        await _factory.ResetDatabaseAsync();
        using var client = _factory.CreateClient();
        var token = await _factory.SeedUserAndLoginAsync(client, BookingRoles.Staff);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        using var uploadContent = CreateLogoUpload(ValidPng, "logo.png", "image/png");
        using var response = await client.PostAsync(
            "/api/admin/restaurant/widget-logo",
            uploadContent);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static MultipartFormDataContent CreateLogoUpload(
        byte[] content,
        string fileName,
        string contentType)
    {
        var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(content);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        form.Add(file, "file", fileName);

        return form;
    }
}
