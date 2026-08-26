using Blizka.App.DataExport;
using MediatR;

namespace Blizka.App.UseCases.Users;

public sealed class RequestDataExportCommandHandler(IDataExportQueue queue) : IRequestHandler<RequestDataExportCommand>
{
    public async Task Handle(RequestDataExportCommand request, CancellationToken cancellationToken) =>
        await queue.EnqueueAsync(new PendingDataExportRequest(request.UserId), cancellationToken);
}
