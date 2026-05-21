import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { finalize } from 'rxjs/operators';
import { LoadingService } from '../services/loading.service';

// URLs that poll silently in the background — never show the global spinner for these
const SILENT_URLS = [
  '/api/notification/unread-count',
  '/api/notification',
  '/api/publicholidays'
];

export const loadingInterceptor: HttpInterceptorFn = (req, next) => {
  const loadingService = inject(LoadingService);

  // Skip spinner for background polling requests
  const isSilent = SILENT_URLS.some(url => req.url.includes(url));
  if (isSilent) {
    return next(req);
  }

  loadingService.show();

  return next(req).pipe(
    finalize(() => {
      loadingService.hide();
    })
  );
};
