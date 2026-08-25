using Blizka.App.Domain.Services;
using MediatR;

namespace Blizka.App.UseCases.Dev;

public sealed class ReseedDemoDataCommandHandler(IDemoSeedService demoSeedService)
    : IRequestHandler<ReseedDemoDataCommand, IReadOnlyList<DemoSeedResultUser>>
{
    public Task<IReadOnlyList<DemoSeedResultUser>> Handle(ReseedDemoDataCommand request, CancellationToken cancellationToken) =>
        demoSeedService.ReseedAsync(cancellationToken);
}
