using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Blizka.Api;
using Blizka.Api.Common;
using Blizka.Api.ErrorHandling;
using Blizka.Api.Photos;
using Blizka.App;
using Blizka.App.Auth;
using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Repositories;
using Blizka.App.Domain.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;

namespace Blizka.IntegrationTests.Controllers;

/// <summary>
/// Проверяет фото-эндпоинты <see cref="Blizka.Api.Controllers.UsersController"/> (T-3.1) через реальный
/// HTTP-конвейер — JWT bearer, [Authorize], multipart-биндинг, MediatR, FluentValidation — но с фейковыми
/// репозиторием/хранилищем/загрузчиком вместо Blizka.Data/S3/сети, по тому же минимальному тестовому хосту,
/// что и <see cref="UsersControllerTests"/>.
/// </summary>
public sealed class PhotosControllerTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions ResponseJsonOptions = CreateResponseJsonOptions();

    private IHost _host = null!;
    private HttpClient _client = null!;
    private FakePhotoRepository _photoRepository = null!;
    private FakePhotoStorageService _photoStorageService = null!;
    private FakeTelegramAvatarDownloader _telegramAvatarDownloader = null!;

    public async Task InitializeAsync()
    {
        _photoRepository = new FakePhotoRepository();
        _photoStorageService = new FakePhotoStorageService();
        _telegramAvatarDownloader = new FakeTelegramAvatarDownloader();

        _host = await new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseTestServer();
                webBuilder.ConfigureAppConfiguration(config =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Jwt:Secret"] = "test-only-secret-not-used-outside-this-test-host",
                        ["Jwt:Issuer"] = "blizka-tests",
                        ["Jwt:Audience"] = "blizka-tests-clients",
                    });
                });
                webBuilder.ConfigureServices((context, services) =>
                {
                    services.AddApiLayer(context.Configuration);
                    services.AddAppLayer();
                    services.AddSingleton<IPhotoRepository>(_photoRepository);
                    services.AddSingleton<IPhotoStorageService>(_photoStorageService);
                    services.AddSingleton<ITelegramAvatarDownloader>(_telegramAvatarDownloader);
                    services.AddExceptionHandler<BlizkaExceptionHandler>();
                    services.AddProblemDetails();
                });
                webBuilder.Configure(app =>
                {
                    app.UseExceptionHandler();
                    app.UseRouting();
                    app.UseAuthentication();
                    app.UseAuthorization();
                    app.UseEndpoints(endpoints => endpoints.MapControllers());
                });
            })
            .StartAsync();

        _client = _host.GetTestClient();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _host.StopAsync();
        _host.Dispose();
    }

    [Fact(DisplayName = "КОГДА запрос без токена ТОГДА загрузка фото отклоняется с 401")]
    public async Task UploadPhoto_without_token_returns_401()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/users/me/photos") { Content = CreateJpegFormContent() };

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact(DisplayName = "КОГДА загружается валидный JPEG ТОГДА фото сохраняется и первое становится главным")]
    public async Task UploadPhoto_with_a_valid_jpeg_saves_it_as_the_main_photo()
    {
        var userId = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/users/me/photos") { Content = CreateJpegFormContent() };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", IssueToken(userId));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PhotoResponse>>(ResponseJsonOptions);
        Assert.True(body!.Data.IsMain);
        var stored = Assert.Single(_photoRepository.Photos);
        Assert.Equal(userId, stored.UserId);
    }

    [Fact(DisplayName = "КОГДА Content-Type файла не поддерживается ТОГДА ответ 400 VALIDATION_ERROR")]
    public async Task UploadPhoto_with_an_unsupported_content_type_returns_400_validation_error()
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent([1, 2, 3]);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        content.Add(fileContent, "file", "not-a-photo.txt");
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/users/me/photos") { Content = content };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", IssueToken(Guid.NewGuid()));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.Equal("VALIDATION_ERROR", body!.Error.Code);
    }

    [Fact(DisplayName = "КОГДА фото существует ТОГДА удаление возвращает 204 и убирает запись из репозитория")]
    public async Task DeletePhoto_removes_an_existing_photo()
    {
        var userId = Guid.NewGuid();
        var photo = new Photo { Id = Guid.NewGuid(), UserId = userId, Url = "u", ThumbnailUrl = "t", MediumUrl = "m" };
        _photoRepository.Photos.Add(photo);
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/users/me/photos/{photo.Id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", IssueToken(userId));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(_photoRepository.Photos);
    }

    [Fact(DisplayName = "КОГДА фото не существует ТОГДА удаление возвращает 404 PHOTO_NOT_FOUND")]
    public async Task DeletePhoto_for_a_missing_photo_returns_404()
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/users/me/photos/{Guid.NewGuid()}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", IssueToken(Guid.NewGuid()));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.Equal("PHOTO_NOT_FOUND", body!.Error.Code);
    }

    [Fact(DisplayName = "КОГДА order не совпадает с текущим набором фото пользователя ТОГДА ответ 400 VALIDATION_ERROR")]
    public async Task ReorderPhotos_with_a_mismatched_order_returns_400_validation_error()
    {
        var request = new HttpRequestMessage(HttpMethod.Patch, "/api/users/me/photos/reorder")
        {
            Content = JsonContent.Create(new { order = new[] { Guid.NewGuid() }, mainPhotoId = Guid.NewGuid() }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", IssueToken(Guid.NewGuid()));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.Equal("VALIDATION_ERROR", body!.Error.Code);
    }

    [Fact(DisplayName = "КОГДА photoUrl не ссылается на Telegram CDN ТОГДА импорт возвращает 400 VALIDATION_ERROR и файл не скачивается")]
    public async Task ImportTelegramPhoto_with_a_non_telegram_host_returns_400_validation_error()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/users/me/photos/import-telegram")
        {
            Content = JsonContent.Create(new { photoUrl = "https://evil.example.com/x.jpg" }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", IssueToken(Guid.NewGuid()));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.Equal("VALIDATION_ERROR", body!.Error.Code);
        Assert.False(_telegramAvatarDownloader.WasCalled);
    }

    private static MultipartFormDataContent CreateJpegFormContent()
    {
        var buffer = new MemoryStream();
        using (var image = new Image<Rgba32>(200, 200))
        {
            image.Save(buffer, new JpegEncoder());
        }

        buffer.Position = 0;
        var content = new MultipartFormDataContent();
        var fileContent = new StreamContent(buffer);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(fileContent, "file", "photo.jpg");
        return content;
    }

    private static JsonSerializerOptions CreateResponseJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }

    private string IssueToken(Guid userId)
    {
        var jwtTokenService = _host.Services.GetRequiredService<IJwtTokenService>();
        var user = new User { Id = userId, TelegramId = 1, Locale = "ru", Status = UserStatus.New };
        return jwtTokenService.IssueToken(user).Token;
    }

    private sealed class FakePhotoRepository : IPhotoRepository
    {
        public List<Photo> Photos { get; } = [];

        public Task<int> CountByUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(Photos.Count(p => p.UserId == userId));

        public Task<List<Photo>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(Photos.Where(p => p.UserId == userId).OrderBy(p => p.SortOrder).ToList());

        public Task AddAsync(Photo photo, CancellationToken cancellationToken)
        {
            Photos.Add(photo);
            return Task.CompletedTask;
        }

        public void Remove(Photo photo) => Photos.Remove(photo);

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakePhotoStorageService : IPhotoStorageService
    {
        public Task<string> UploadAsync(string key, Stream content, string contentType, CancellationToken cancellationToken) =>
            Task.FromResult($"https://cdn.test/{key}");

        public Task DeleteAsync(string key, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeTelegramAvatarDownloader : ITelegramAvatarDownloader
    {
        public bool WasCalled { get; private set; }

        public Task<TelegramAvatarDownload> DownloadAsync(Uri photoUrl, CancellationToken cancellationToken)
        {
            WasCalled = true;
            throw new NotSupportedException("Не ожидается в тестах, где photoUrl отклоняется валидатором до скачивания.");
        }
    }
}
