import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { BaseChartDirective } from 'ng2-charts';
import { ChartConfiguration } from 'chart.js';
import {
  EtlJobsService,
  EtlJob,
  EtlJobLog,
  SupplierEtlRun,
  SupplierRetryQueueItem,
  SupplierHistoryItem
} from '../services/etl-jobs.service';

type SortDirection = 'asc' | 'desc';

interface SortState {
  key: string;
  direction: SortDirection;
}

@Component({
  selector: 'app-etl-jobs',
  standalone: true,
  imports: [CommonModule, FormsModule, BaseChartDirective],
  templateUrl: './etl-jobs.component.html',
  styleUrl: './etl-jobs.component.scss'
})
export class EtlJobsComponent implements OnInit {
  readonly pageSizeOptions = [10, 25, 50];
  readonly dataModes = [
    { value: 'real', label: 'Real (DB)' },
    { value: 'mock', label: 'Mock' }
  ] as const;

  jobs: EtlJob[] = [];
  selectedJobLogs: EtlJobLog[] = [];
  selectedJobId: number | null = null;
  supplierRuns: SupplierEtlRun[] = [];
  retryQueue: SupplierRetryQueueItem[] = [];
  supplierHistory: SupplierHistoryItem[] = [];
  selectedSupplierId: string | null = null;
  loading = false;
  loadingRuns = false;
  loadingRetryQueue = false;
  loadingHistory = false;
  runFilterFrom: string = '';
  runFilterTo: string = '';
  jobsSearch = '';
  logsSearch = '';
  runsSearch = '';
  retrySearch = '';
  historySearch = '';
  error: string | null = null;
  dataMode: 'real' | 'mock' = 'mock';
  jobsDataOrigin: 'api' | 'fallback' | null = null;
  runsDataOrigin: 'api' | 'fallback' | null = null;
  retryDataOrigin: 'api' | 'fallback' | null = null;
  historyDataOrigin: 'api' | 'fallback' | null = null;

  jobsSort: SortState = { key: 'id', direction: 'asc' };
  logsSort: SortState = { key: 'id', direction: 'desc' };
  runsSort: SortState = { key: 'runId', direction: 'desc' };
  retrySort: SortState = { key: 'updatedAt', direction: 'desc' };
  historySort: SortState = { key: 'changedAt', direction: 'desc' };

  jobsPage = 1;
  logsPage = 1;
  runsPage = 1;
  retryPage = 1;
  historyPage = 1;

  jobsPageSize = 10;
  logsPageSize = 10;
  runsPageSize = 10;
  retryPageSize = 10;
  historyPageSize = 10;

  readonly lineChartType: 'line' = 'line';
  readonly lineChartOptions: ChartConfiguration<'line'>['options'] = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: { display: true }
    }
  };

  readonly barChartType: 'bar' = 'bar';
  readonly barChartOptions: ChartConfiguration<'bar'>['options'] = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: { display: true }
    }
  };

  runTrendData: ChartConfiguration<'line'>['data'] = {
    labels: [],
    datasets: [
      {
        data: [],
        label: 'Records Sent'
      },
      {
        data: [],
        label: 'Records Failed'
      }
    ]
  };

  qualityTrendData: ChartConfiguration<'bar'>['data'] = {
    labels: [],
    datasets: [
      {
        data: [],
        label: 'Error Rate %'
      },
      {
        data: [],
        label: 'SLA %'
      }
    ]
  };

  readonly sparklineChartType: 'line' = 'line';
  readonly sparklineOptions: ChartConfiguration<'line'>['options'] = {
    responsive: true,
    maintainAspectRatio: false,
    elements: {
      line: {
        borderWidth: 2,
        tension: 0.35
      },
      point: {
        radius: 0,
        hoverRadius: 3
      }
    },
    scales: {
      x: {
        display: false,
        grid: { display: false }
      },
      y: {
        display: false,
        grid: { display: false }
      }
    },
    plugins: {
      legend: { display: false },
      tooltip: { enabled: true }
    }
  };

  recordsSentSparklineData: ChartConfiguration<'line'>['data'] = {
    labels: [],
    datasets: [
      {
        data: [],
        label: 'Records Sent'
      }
    ]
  };

  successRateSparklineData: ChartConfiguration<'line'>['data'] = {
    labels: [],
    datasets: [
      {
        data: [],
        label: 'Success Rate %'
      }
    ]
  };

  constructor(private etlJobsService: EtlJobsService) {}

  ngOnInit(): void {
    this.setDefaultRunDateFilter();
    this.onDataModeChange();
  }

  loadJobs(): void {
    this.loading = true;
    this.etlJobsService.getJobs(this.isMockMode).subscribe({
      next: jobs => {
        const normalizedJobs = this.normalizeArrayResponse<EtlJob>(jobs);
        this.jobs = this.isMockMode && normalizedJobs.length === 0
          ? this.buildLocalMockJobs()
          : normalizedJobs;
        this.jobsDataOrigin = this.isMockMode
          ? (normalizedJobs.length === 0 ? 'fallback' : 'api')
          : null;
        this.error = null;
        this.loading = false;
      },
      error: err => {
        if (this.isMockMode)
        {
          this.jobs = this.buildLocalMockJobs();
          this.jobsDataOrigin = 'fallback';
          this.error = null;
        }
        else
        {
          this.jobsDataOrigin = null;
          this.error = err.message;
        }

        this.loading = false;
      }
    });
  }

  viewLogs(job: EtlJob): void {
    this.selectedJobId = job.id;
    this.etlJobsService.getJobLogs(job.id, this.isMockMode).subscribe({
      next: logs => {
        const normalizedLogs = this.normalizeArrayResponse<EtlJobLog>(logs);
        this.selectedJobLogs = this.isMockMode && normalizedLogs.length === 0
          ? this.buildLocalMockJobLogs(job.id)
          : normalizedLogs;
        this.error = null;
      },
      error: err => {
        if (this.isMockMode)
        {
          this.selectedJobLogs = this.buildLocalMockJobLogs(job.id);
          this.error = null;
        }
        else
        {
          this.error = err.message;
        }
      }
    });
  }

  loadRuns(fromUtc?: string, toUtc?: string): void {
    this.loadingRuns = true;
    this.etlJobsService.getSupplierEtlRuns(fromUtc, toUtc, this.isMockMode).subscribe({
      next: runs => {
        const normalizedRuns = this.normalizeRunsResponse(runs);
        const effectiveRuns = this.isMockMode && normalizedRuns.length === 0
          ? this.buildLocalMockRuns()
          : normalizedRuns;

        this.supplierRuns = effectiveRuns;
        this.runsDataOrigin = this.isMockMode
          ? (normalizedRuns.length === 0 ? 'fallback' : 'api')
          : null;
        this.updateRunCharts(effectiveRuns);
        this.loadingRuns = false;
      },
      error: err => {
        if (this.isMockMode)
        {
          const fallbackRuns = this.buildLocalMockRuns();
          this.supplierRuns = fallbackRuns;
          this.runsDataOrigin = 'fallback';
          this.updateRunCharts(fallbackRuns);
          this.error = null;
        }
        else
        {
          this.runsDataOrigin = null;
          this.error = err.message;
        }

        this.loadingRuns = false;
      }
    });
  }

  applyRunFilter(): void {
    const fromUtc = this.runFilterFrom ? new Date(this.runFilterFrom).toISOString() : undefined;
    const toUtc = this.runFilterTo ? new Date(this.runFilterTo).toISOString() : undefined;
    this.loadRuns(fromUtc, toUtc);
  }

  clearRunFilter(): void {
    this.runFilterFrom = '';
    this.runFilterTo = '';
    this.loadRuns();
  }

  loadRetryQueue(): void {
    this.loadingRetryQueue = true;
    this.etlJobsService.getSupplierRetryQueue(this.isMockMode).subscribe({
      next: retries => {
        const normalizedRetries = this.normalizeArrayResponse<SupplierRetryQueueItem>(retries);
        this.retryQueue = this.isMockMode && normalizedRetries.length === 0
          ? this.buildLocalMockRetryQueue()
          : normalizedRetries;
        this.retryDataOrigin = this.isMockMode
          ? (normalizedRetries.length === 0 ? 'fallback' : 'api')
          : null;

        if (this.isMockMode && this.retryQueue.length > 0 && !this.selectedSupplierId) {
          this.loadSupplierHistory(this.retryQueue[0].supplierId);
        }

        this.error = null;
        this.loadingRetryQueue = false;
      },
      error: err => {
        if (this.isMockMode)
        {
          this.retryQueue = this.buildLocalMockRetryQueue();
          this.retryDataOrigin = 'fallback';
          if (this.retryQueue.length > 0 && !this.selectedSupplierId) {
            this.loadSupplierHistory(this.retryQueue[0].supplierId);
          }
          this.error = null;
        }
        else
        {
          this.retryDataOrigin = null;
          this.error = err.message;
        }

        this.loadingRetryQueue = false;
      }
    });
  }

  loadSupplierHistory(supplierId: string): void {
    if (!supplierId.trim()) {
      return;
    }

    this.selectedSupplierId = supplierId;
    this.loadingHistory = true;
    this.etlJobsService.getSupplierHistory(supplierId, this.isMockMode).subscribe({
      next: history => {
        const normalizedHistory = this.normalizeArrayResponse<SupplierHistoryItem>(history);
        this.supplierHistory = this.isMockMode && normalizedHistory.length === 0
          ? this.buildLocalMockSupplierHistory(supplierId)
          : normalizedHistory;
        this.historyDataOrigin = this.isMockMode
          ? (normalizedHistory.length === 0 ? 'fallback' : 'api')
          : null;
        this.error = null;
        this.loadingHistory = false;
      },
      error: err => {
        if (this.isMockMode)
        {
          this.supplierHistory = this.buildLocalMockSupplierHistory(supplierId);
          this.historyDataOrigin = 'fallback';
          this.error = null;
        }
        else
        {
          this.historyDataOrigin = null;
          this.error = err.message;
        }

        this.loadingHistory = false;
      }
    });
  }

  get completedRunsCount(): number {
    return this.supplierRuns.filter(run => run.status.toLowerCase() === 'completed').length;
  }

  get totalRecordsSent(): number {
    return this.supplierRuns.reduce((sum, run) => sum + run.recordsSent, 0);
  }

  get averageErrorRate(): number {
    if (this.supplierRuns.length === 0) {
      return 0;
    }

    const totalErrorRate = this.supplierRuns.reduce((sum, run) => sum + run.errorRatePct, 0);
    return totalErrorRate / this.supplierRuns.length;
  }

  get runSuccessRate(): number {
    if (this.supplierRuns.length === 0) {
      return 0;
    }

    return (this.completedRunsCount / this.supplierRuns.length) * 100;
  }

  get latestRunStatus(): string {
    return this.supplierRuns[0]?.status ?? 'N/A';
  }

  get latestRunDurationMs(): number | null {
    return this.supplierRuns[0]?.durationMs ?? null;
  }

  get isMockMode(): boolean {
    return this.dataMode === 'mock';
  }

  onDataModeChange(): void {
    this.selectedJobLogs = [];
    this.selectedJobId = null;
    this.supplierHistory = [];
    this.selectedSupplierId = null;

    this.loadJobs();
    this.applyRunFilter();
    this.loadRetryQueue();
  }

  get displayedJobs(): EtlJob[] {
    return this.sortItems(this.filterItems(this.jobs, this.jobsSearch), this.jobsSort);
  }

  get pagedJobs(): EtlJob[] {
    return this.paginate(this.displayedJobs, this.getCurrentPage('jobs', this.displayedJobs.length), this.getPageSize('jobs'));
  }

  get displayedJobLogs(): EtlJobLog[] {
    return this.sortItems(this.filterItems(this.selectedJobLogs, this.logsSearch), this.logsSort);
  }

  get pagedJobLogs(): EtlJobLog[] {
    return this.paginate(this.displayedJobLogs, this.getCurrentPage('logs', this.displayedJobLogs.length), this.getPageSize('logs'));
  }

  get displayedRuns(): SupplierEtlRun[] {
    return this.sortItems(this.filterItems(this.supplierRuns, this.runsSearch), this.runsSort);
  }

  get pagedRuns(): SupplierEtlRun[] {
    return this.paginate(this.displayedRuns, this.getCurrentPage('runs', this.displayedRuns.length), this.getPageSize('runs'));
  }

  get displayedRetryQueue(): SupplierRetryQueueItem[] {
    return this.sortItems(this.filterItems(this.retryQueue, this.retrySearch), this.retrySort);
  }

  get pagedRetryQueue(): SupplierRetryQueueItem[] {
    return this.paginate(this.displayedRetryQueue, this.getCurrentPage('retry', this.displayedRetryQueue.length), this.getPageSize('retry'));
  }

  get displayedSupplierHistory(): SupplierHistoryItem[] {
    return this.sortItems(this.filterItems(this.supplierHistory, this.historySearch), this.historySort);
  }

  get pagedSupplierHistory(): SupplierHistoryItem[] {
    return this.paginate(this.displayedSupplierHistory, this.getCurrentPage('history', this.displayedSupplierHistory.length), this.getPageSize('history'));
  }

  getStatusBadgeClass(status: string): string {
    const normalized = status.toLowerCase();

    if (normalized === 'completed') {
      return 'bg-success-subtle text-success-emphasis';
    }

    if (normalized === 'failed') {
      return 'bg-danger-subtle text-danger-emphasis';
    }

    if (normalized === 'running') {
      return 'bg-primary-subtle text-primary-emphasis';
    }

    return 'bg-secondary-subtle text-secondary-emphasis';
  }

  toggleSort(target: 'jobs' | 'logs' | 'runs' | 'retry' | 'history', key: string): void {
    const state = this.getSortState(target);
    if (state.key === key) {
      state.direction = state.direction === 'asc' ? 'desc' : 'asc';
      return;
    }

    state.key = key;
    state.direction = 'asc';
  }

  getSortIndicator(target: 'jobs' | 'logs' | 'runs' | 'retry' | 'history', key: string): string {
    const state = this.getSortState(target);
    if (state.key !== key) {
      return '';
    }

    return state.direction === 'asc' ? '↑' : '↓';
  }

  clearGridFilter(target: 'jobs' | 'logs' | 'runs' | 'retry' | 'history'): void {
    if (target === 'jobs') {
      this.jobsSearch = '';
      return;
    }

    if (target === 'logs') {
      this.logsSearch = '';
      return;
    }

    if (target === 'runs') {
      this.runsSearch = '';
      return;
    }

    if (target === 'retry') {
      this.retrySearch = '';
      return;
    }

    this.historySearch = '';
  }

  resetPage(target: 'jobs' | 'logs' | 'runs' | 'retry' | 'history'): void {
    this.setPage(target, 1);
  }

  onPageSizeChange(target: 'jobs' | 'logs' | 'runs' | 'retry' | 'history'): void {
    this.resetPage(target);
  }

  goToPreviousPage(target: 'jobs' | 'logs' | 'runs' | 'retry' | 'history', totalItems: number): void {
    const currentPage = this.getCurrentPage(target, totalItems);
    this.setPage(target, Math.max(1, currentPage - 1));
  }

  goToNextPage(target: 'jobs' | 'logs' | 'runs' | 'retry' | 'history', totalItems: number): void {
    const currentPage = this.getCurrentPage(target, totalItems);
    const totalPages = this.getTotalPages(target, totalItems);
    this.setPage(target, Math.min(totalPages, currentPage + 1));
  }

  getCurrentPage(target: 'jobs' | 'logs' | 'runs' | 'retry' | 'history', totalItems: number): number {
    return this.clampPage(target, this.getPage(target), totalItems);
  }

  getTotalPages(target: 'jobs' | 'logs' | 'runs' | 'retry' | 'history', totalItems: number): number {
    const pageSize = this.getPageSize(target);
    return Math.max(1, Math.ceil(totalItems / pageSize));
  }

  private getSortState(target: 'jobs' | 'logs' | 'runs' | 'retry' | 'history'): SortState {
    if (target === 'jobs') {
      return this.jobsSort;
    }

    if (target === 'logs') {
      return this.logsSort;
    }

    if (target === 'runs') {
      return this.runsSort;
    }

    if (target === 'retry') {
      return this.retrySort;
    }

    return this.historySort;
  }

  private getPage(target: 'jobs' | 'logs' | 'runs' | 'retry' | 'history'): number {
    if (target === 'jobs') {
      return this.jobsPage;
    }

    if (target === 'logs') {
      return this.logsPage;
    }

    if (target === 'runs') {
      return this.runsPage;
    }

    if (target === 'retry') {
      return this.retryPage;
    }

    return this.historyPage;
  }

  private getPageSize(target: 'jobs' | 'logs' | 'runs' | 'retry' | 'history'): number {
    if (target === 'jobs') {
      return this.jobsPageSize;
    }

    if (target === 'logs') {
      return this.logsPageSize;
    }

    if (target === 'runs') {
      return this.runsPageSize;
    }

    if (target === 'retry') {
      return this.retryPageSize;
    }

    return this.historyPageSize;
  }

  private setPage(target: 'jobs' | 'logs' | 'runs' | 'retry' | 'history', page: number): void {
    if (target === 'jobs') {
      this.jobsPage = page;
      return;
    }

    if (target === 'logs') {
      this.logsPage = page;
      return;
    }

    if (target === 'runs') {
      this.runsPage = page;
      return;
    }

    if (target === 'retry') {
      this.retryPage = page;
      return;
    }

    this.historyPage = page;
  }

  private clampPage(target: 'jobs' | 'logs' | 'runs' | 'retry' | 'history', page: number, totalItems: number): number {
    const totalPages = this.getTotalPages(target, totalItems);
    return Math.min(Math.max(1, page), totalPages);
  }

  private paginate<T>(items: T[], page: number, pageSize: number): T[] {
    const start = (page - 1) * pageSize;
    return items.slice(start, start + pageSize);
  }

  private setDefaultRunDateFilter(): void {
    const now = new Date();
    const from = new Date(now);
    from.setDate(now.getDate() - 15);

    this.runFilterFrom = this.toDateTimeLocalValue(from);
    this.runFilterTo = this.toDateTimeLocalValue(now);
  }

  private toDateTimeLocalValue(value: Date): string {
    const year = value.getFullYear();
    const month = String(value.getMonth() + 1).padStart(2, '0');
    const day = String(value.getDate()).padStart(2, '0');
    const hour = String(value.getHours()).padStart(2, '0');
    const minute = String(value.getMinutes()).padStart(2, '0');
    return `${year}-${month}-${day}T${hour}:${minute}`;
  }

  private filterItems<T>(items: T[], searchText: string): T[] {
    const normalizedSearch = searchText.trim().toLowerCase();
    if (!normalizedSearch) {
      return [...items];
    }

    return items.filter(item =>
      Object.values(item as Record<string, unknown>)
        .some(value => String(value ?? '').toLowerCase().includes(normalizedSearch)));
  }

  private sortItems<T>(items: T[], sortState: SortState): T[] {
    return [...items].sort((left, right) => {
      const leftValue = this.normalizeSortValue((left as Record<string, unknown>)[sortState.key]);
      const rightValue = this.normalizeSortValue((right as Record<string, unknown>)[sortState.key]);

      if (leftValue < rightValue) {
        return sortState.direction === 'asc' ? -1 : 1;
      }

      if (leftValue > rightValue) {
        return sortState.direction === 'asc' ? 1 : -1;
      }

      return 0;
    });
  }

  private normalizeSortValue(value: unknown): string | number {
    if (value === null || value === undefined) {
      return '';
    }

    if (typeof value === 'number') {
      return value;
    }

    if (typeof value === 'string') {
      const parsedDate = Date.parse(value);
      if (!Number.isNaN(parsedDate)) {
        return parsedDate;
      }

      return value.toLowerCase();
    }

    if (value instanceof Date) {
      return value.getTime();
    }

    return String(value).toLowerCase();
  }

  private updateRunCharts(runs: SupplierEtlRun[]): void {
    const orderedRuns = [...runs].sort((a, b) => a.runId - b.runId);
    const labels = orderedRuns.map(run => `Run ${run.runId}`);

    this.runTrendData = {
      labels,
      datasets: [
        {
          data: orderedRuns.map(run => run.recordsSent),
          label: 'Records Sent'
        },
        {
          data: orderedRuns.map(run => run.recordsFailed),
          label: 'Records Failed'
        }
      ]
    };

    this.qualityTrendData = {
      labels,
      datasets: [
        {
          data: orderedRuns.map(run => run.errorRatePct),
          label: 'Error Rate %'
        },
        {
          data: orderedRuns.map(run => run.slaCompliancePct ?? 0),
          label: 'SLA %'
        }
      ]
    };

    this.recordsSentSparklineData = {
      labels,
      datasets: [
        {
          data: orderedRuns.map(run => run.recordsSent),
          label: 'Records Sent'
        }
      ]
    };

    this.successRateSparklineData = {
      labels,
      datasets: [
        {
          data: orderedRuns.map(run => {
            if (run.recordsIn <= 0) {
              return 0;
            }

            return (run.recordsSent / run.recordsIn) * 100;
          }),
          label: 'Success Rate %'
        }
      ]
    };
  }

  private normalizeRunsResponse(response: unknown): SupplierEtlRun[] {
    if (Array.isArray(response)) {
      return response as SupplierEtlRun[];
    }

    if (response && typeof response === 'object') {
      const maybeWrapped = response as { value?: unknown };
      if (Array.isArray(maybeWrapped.value)) {
        return maybeWrapped.value as SupplierEtlRun[];
      }
    }

    return [];
  }

  private normalizeArrayResponse<T>(response: unknown): T[] {
    if (Array.isArray(response)) {
      return response as T[];
    }

    if (response && typeof response === 'object') {
      const maybeWrapped = response as { value?: unknown };
      if (Array.isArray(maybeWrapped.value)) {
        return maybeWrapped.value as T[];
      }
    }

    return [];
  }

  private buildLocalMockJobs(): EtlJob[] {
    return Array.from({ length: 8 }).map((_, index) => ({
      id: index + 1,
      name: `Mock Job ${index + 1}`,
      description: 'Local mock ETL job',
      cronSchedule: '0 */30 * * * *',
      isEnabled: Math.random() > 0.25,
      createdAt: new Date(Date.now() - (index + 3) * 86400000).toISOString()
    }));
  }

  private buildLocalMockJobLogs(jobId: number): EtlJobLog[] {
    const statuses = ['Completed', 'Failed', 'Running', 'PartialFailure'];

    return Array.from({ length: 12 }).map((_, index) => {
      const startedAt = new Date(Date.now() - (index + 1) * 2 * 3600000);
      const status = statuses[Math.floor(Math.random() * statuses.length)];
      const finishedAt = status === 'Running'
        ? null
        : new Date(startedAt.getTime() + (5 + Math.random() * 40) * 60000).toISOString();

      return {
        id: jobId * 1000 + index + 1,
        status,
        startedAt: startedAt.toISOString(),
        finishedAt,
        rowsProcessed: status === 'Running' ? null : Math.floor(100 + Math.random() * 5000),
        errorMessage: status === 'Failed' ? 'Local mock downstream failure.' : null
      };
    });
  }

  private buildLocalMockRetryQueue(): SupplierRetryQueueItem[] {
    const statuses = ['Pending', 'Scheduled', 'Retrying'];

    return Array.from({ length: 10 }).map((_, index) => ({
      supplierId: `SUP-${2000 + index}`,
      supplierName: `Local Mock Supplier ${index + 1}`,
      deliveryStatus: statuses[Math.floor(Math.random() * statuses.length)],
      retryAttemptCount: 1 + Math.floor(Math.random() * 6),
      lastRetryAt: new Date(Date.now() - (index + 1) * 1800000).toISOString(),
      nextRetryAt: new Date(Date.now() + (index + 1) * 1200000).toISOString(),
      lastSeenRunId: 70000 + index,
      updatedAt: new Date(Date.now() - index * 600000).toISOString()
    }));
  }

  private buildLocalMockSupplierHistory(supplierId: string): SupplierHistoryItem[] {
    const changeTypes = ['NEW', 'UPDATED', 'RETRY'];

    return Array.from({ length: 12 }).map((_, index) => ({
      supplierId,
      snapshotId: 900000 + index,
      etlRunId: 70000 + index,
      changeType: changeTypes[Math.floor(Math.random() * changeTypes.length)],
      snapshotHash: Math.random().toString(36).slice(2, 18),
      snapshotPayload: JSON.stringify({ supplierId, revision: index + 1 }),
      changedAt: new Date(Date.now() - (index + 1) * 8 * 3600000).toISOString()
    }));
  }

  private buildLocalMockRuns(): SupplierEtlRun[] {
    const statuses = ['Completed', 'Failed', 'PartialFailure', 'Running'];
    const now = new Date();

    return Array.from({ length: 15 }).map((_, index) => {
      const startedAt = new Date(now.getTime() - index * 6 * 60 * 60 * 1000);
      const recordsIn = 500 + Math.floor(Math.random() * 3500);
      const recordsSent = Math.floor(recordsIn * (0.6 + Math.random() * 0.35));
      const recordsFailed = recordsIn - recordsSent;
      const status = statuses[Math.floor(Math.random() * statuses.length)];

      return {
        runId: 70000 + index,
        triggerSource: Math.random() > 0.5 ? 'Scheduler' : 'Webhook',
        correlationId: `local-mock-${70000 + index}`,
        status,
        startedAt: startedAt.toISOString(),
        finishedAt: status === 'Running' ? null : new Date(startedAt.getTime() + (5 + Math.random() * 35) * 60 * 1000).toISOString(),
        recordsIn,
        recordsValidated: recordsIn - Math.floor(Math.random() * 50),
        recordsSent,
        recordsFailed,
        recordsSkipped: Math.floor(Math.random() * 30),
        validationFailureCount: Math.floor(Math.random() * 40),
        apiFailureCount: Math.floor(Math.random() * 20),
        retryCount: Math.floor(Math.random() * 10),
        failedBatchesCount: Math.floor(Math.random() * 4),
        p95LatencyMs: 60 + Math.floor(Math.random() * 850),
        slaCompliancePct: Math.round((85 + Math.random() * 15) * 100) / 100,
        totalProcessingMs: 500 + Math.floor(Math.random() * 120000),
        errorRatePct: Math.round((recordsFailed / Math.max(recordsIn, 1)) * 10000) / 100,
        durationMs: 1000 + Math.floor(Math.random() * 180000)
      };
    });
  }
}
