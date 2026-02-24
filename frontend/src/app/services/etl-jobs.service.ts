import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface EtlJob {
  id: number;
  name: string;
  description: string | null;
  cronSchedule: string;
  isEnabled: boolean;
  createdAt: string;
}

export interface EtlJobLog {
  id: number;
  status: string;
  startedAt: string | null;
  finishedAt: string | null;
  rowsProcessed: number | null;
  errorMessage: string | null;
}

export interface SupplierEtlRun {
  runId: number;
  triggerSource: string;
  correlationId: string | null;
  status: string;
  startedAt: string;
  finishedAt: string | null;
  recordsIn: number;
  recordsValidated: number;
  recordsSent: number;
  recordsFailed: number;
  recordsSkipped: number;
  validationFailureCount: number;
  apiFailureCount: number;
  retryCount: number;
  failedBatchesCount: number;
  p95LatencyMs: number | null;
  slaCompliancePct: number | null;
  totalProcessingMs: number | null;
  errorRatePct: number;
  durationMs: number | null;
}

export interface SupplierRetryQueueItem {
  supplierId: string;
  supplierName: string | null;
  deliveryStatus: string;
  retryAttemptCount: number;
  lastRetryAt: string | null;
  nextRetryAt: string | null;
  lastSeenRunId: number | null;
  updatedAt: string;
}

export interface SupplierHistoryItem {
  supplierId: string;
  snapshotId: number;
  etlRunId: number;
  changeType: string;
  snapshotHash: string | null;
  snapshotPayload: string;
  changedAt: string;
}

@Injectable({ providedIn: 'root' })
export class EtlJobsService {
  private readonly apiBase = environment.apiUrl;

  constructor(private http: HttpClient) {}

  getJobs(): Observable<EtlJob[]> {
    return this.http.get<EtlJob[]>(`${this.apiBase}/api/etl-jobs`);
  }

  getJobLogs(jobId: number): Observable<EtlJobLog[]> {
    return this.http.get<EtlJobLog[]>(`${this.apiBase}/api/etl-jobs/${jobId}/logs`);
  }

  getSupplierEtlRuns(fromUtc?: string, toUtc?: string): Observable<SupplierEtlRun[]> {
    const query = new URLSearchParams();
    if (fromUtc) {
      query.set('fromUtc', fromUtc);
    }
    if (toUtc) {
      query.set('toUtc', toUtc);
    }

    const suffix = query.toString() ? `?${query.toString()}` : '';
    return this.http.get<SupplierEtlRun[]>(`${this.apiBase}/api/supplier-etl/runs${suffix}`);
  }

  getSupplierRetryQueue(): Observable<SupplierRetryQueueItem[]> {
    return this.http.get<SupplierRetryQueueItem[]>(`${this.apiBase}/api/supplier-etl/retry-queue`);
  }

  getSupplierHistory(supplierId: string): Observable<SupplierHistoryItem[]> {
    return this.http.get<SupplierHistoryItem[]>(`${this.apiBase}/api/supplier-etl/suppliers/${encodeURIComponent(supplierId)}/history`);
  }
}
