using MediatR;

namespace Blizka.App.UseCases.Photos;

/// <param name="Content">Тело файла. Владелец потока — вызывающий код (контроллер), хендлер его не закрывает.</param>
public sealed record UploadPhotoCommand(Guid UserId, Stream Content, string ContentType, long ContentLength)
    : IRequest<PhotoResult>;
