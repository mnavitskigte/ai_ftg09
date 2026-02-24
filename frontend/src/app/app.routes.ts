import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    redirectTo: 'etl-jobs',
    pathMatch: 'full'
  },
  {
    path: 'etl-jobs',
    loadComponent: () =>
      import('./etl-jobs/etl-jobs.component').then(m => m.EtlJobsComponent)
  }
];
