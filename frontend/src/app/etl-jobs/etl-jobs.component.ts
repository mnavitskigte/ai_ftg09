import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { EtlJobsService, EtlJob, EtlJobLog } from '../services/etl-jobs.service';

@Component({
  selector: 'app-etl-jobs',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './etl-jobs.component.html',
  styleUrl: './etl-jobs.component.scss'
})
export class EtlJobsComponent implements OnInit {
  jobs: EtlJob[] = [];
  selectedJobLogs: EtlJobLog[] = [];
  selectedJobId: number | null = null;
  loading = false;
  error: string | null = null;

  constructor(private etlJobsService: EtlJobsService) {}

  ngOnInit(): void {
    this.loadJobs();
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
}
