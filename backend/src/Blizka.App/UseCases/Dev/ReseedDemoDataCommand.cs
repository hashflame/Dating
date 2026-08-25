using Blizka.App.Domain.Services;
using MediatR;

namespace Blizka.App.UseCases.Dev;

public sealed record ReseedDemoDataCommand : IRequest<IReadOnlyList<DemoSeedResultUser>>;
