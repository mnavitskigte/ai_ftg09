using EtlApi.Models;

namespace EtlApi.Data;

public interface ISupplierEtlReadRepository
{
    Task<bool> IsDatabaseReachableAsync(CancellationToken cancellationToken);

    Task<IReadOnlyCollection<EtlJobDto>> GetEtlJobsAsync(CancellationToken cancellationToken);

    Task<IReadOnlyCollection<EtlJobLogDto>> GetEtlJobLogsAsync(int jobId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<SupplierEtlRunDto>> GetSupplierRunsAsync(DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<SupplierRetryQueueItemDto>> GetRetryQueueAsync(CancellationToken cancellationToken);

    Task<IReadOnlyCollection<SupplierChangeHistoryItemDto>> GetSupplierHistoryAsync(string supplierId, CancellationToken cancellationToken);
}
