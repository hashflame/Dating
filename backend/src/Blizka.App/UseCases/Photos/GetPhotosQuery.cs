using MediatR;

namespace Blizka.App.UseCases.Photos;

/// <summary><c>GET /api/users/me/photos</c> (T-3.1) — список фото профиля, чтобы клиент видел их после перезагрузки.</summary>
public sealed record GetPhotosQuery(Guid UserId) : IRequest<IReadOnlyList<PhotoResult>>;
