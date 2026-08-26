using System.Threading.Channels;

namespace Blizka.App.DataExport;

/// <summary>Реализация <see cref="IDataExportQueue"/> поверх <see cref="Channel{T}"/> — неограниченная, процесс не переживает рестарт (T-16.2, MVP: без персистентной очереди).</summary>
public sealed class DataExportQueue : IDataExportQueue
{
    private readonly Channel<PendingDataExportRequest> _channel = Channel.CreateUnbounded<PendingDataExportRequest>();

    public ValueTask EnqueueAsync(PendingDataExportRequest request, CancellationToken cancellationToken) =>
        _channel.Writer.WriteAsync(request, cancellationToken);

    public IAsyncEnumerable<PendingDataExportRequest> ReadAllAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);
}
