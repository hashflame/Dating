using Blizka.App.DataExport;
using MediatR;

namespace Blizka.App.UseCases.Users;

/// <summary>Собирает полный снимок данных пользователя (T-16.2) — вызывается фоновым сервисом-обработчиком очереди экспорта, не из HTTP-запроса.</summary>
public sealed record BuildDataExportQuery(Guid UserId) : IRequest<DataExportPayload>;
