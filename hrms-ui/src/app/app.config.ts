import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import {
  provideRouter,
  RouteReuseStrategy,
  withInMemoryScrolling,
  withRouterConfig
} from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';

import { routes } from './app.routes';
import { authInterceptor } from './core/interceptors/auth.interceptor';
import { loadingInterceptor } from './core/interceptors/loading.interceptor';
import { NoReuseStrategy } from './core/strategies/reuse.strategy';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(
      routes,
      // Reload component even when navigating to the same URL (e.g. clicking
      // the active sidebar item again refreshes the data)
      withRouterConfig({ onSameUrlNavigation: 'reload' }),
      // Restore scroll position when navigating back to a list page
      withInMemoryScrolling({ scrollPositionRestoration: 'enabled' })
    ),
    // withFetch() removed — it uses browser fetch API which silently drops
    // responses when CORS preflight is missing Access-Control-Allow-Headers.
    // Angular's default XMLHttpRequest-based client works correctly.
    provideHttpClient(withInterceptors([loadingInterceptor, authInterceptor])),
    // NoReuseStrategy: every route navigation recreates the component fresh
    // so ngOnInit always fires and data is always fetched from the API
    { provide: RouteReuseStrategy, useClass: NoReuseStrategy }
  ]
};
