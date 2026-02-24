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
}
