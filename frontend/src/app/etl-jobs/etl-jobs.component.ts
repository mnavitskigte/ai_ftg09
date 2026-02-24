import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  EtlJobsService,
  EtlJob,
  EtlJobLog,
  SupplierEtlRun,
  SupplierRetryQueueItem,
  SupplierHistoryItem
} from '../services/etl-jobs.service';

@Component({
  selector: 'app-etl-jobs',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './etl-jobs.component.html',
  styleUrl: './etl-jobs.component.scss'
})
export class EtlJobsComponent implements OnInit {
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
  error: string | null = null;

  constructor(private etlJobsService: EtlJobsService) {}

  ngOnInit(): void {
    this.loadJobs();
    this.loadRuns();
    this.loadRetryQueue();
  }

  loadJobs(): void {
    this.loading = true;
    this.etlJobsService.getJobs().subscribe({
      next: jobs => { this.jobs = jobs; this.loading = false; },
      error: err => { this.error = err.message; this.loading = false; }
    });
  }

  viewLogs(job: EtlJob): void {
    this.selectedJobId = job.id;
    this.etlJobsService.getJobLogs(job.id).subscribe({
      next: logs => { this.selectedJobLogs = logs; },
      error: err => { this.error = err.message; }
    });
  }

  loadRuns(fromUtc?: string, toUtc?: string): void {
    this.loadingRuns = true;
    this.etlJobsService.getSupplierEtlRuns(fromUtc, toUtc).subscribe({
      next: runs => {
        this.supplierRuns = runs;
        this.loadingRuns = false;
      },
      error: err => {
        this.error = err.message;
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
    this.etlJobsService.getSupplierRetryQueue().subscribe({
      next: retries => {
        this.retryQueue = retries;
        this.loadingRetryQueue = false;
      },
      error: err => {
        this.error = err.message;
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
    this.etlJobsService.getSupplierHistory(supplierId).subscribe({
      next: history => {
        this.supplierHistory = history;
        this.loadingHistory = false;
      },
      error: err => {
        this.error = err.message;
        this.loadingHistory = false;
      }
    });
  }
}
