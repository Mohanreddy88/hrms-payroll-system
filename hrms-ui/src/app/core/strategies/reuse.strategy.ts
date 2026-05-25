import { RouteReuseStrategy, ActivatedRouteSnapshot, DetachedRouteHandle } from '@angular/router';

/**
 * NoReuseStrategy — disables Angular's default route reuse completely.
 *
 * WHY: Angular by default reuses the same component instance when navigating
 * to the same route (e.g. sidebar click while already on /employees).
 * This meant ngOnInit never fired again → no new API call → data appeared
 * stale or the loading spinner never appeared at all on revisit.
 *
 * With this strategy:
 *   - Every navigation always destroys and recreates the component
 *   - ngOnInit always fires → always hits the API
 *   - Data is always fresh from the server
 *
 * Trade-off: slight re-render on every navigation, but ensures data freshness.
 */
export class NoReuseStrategy implements RouteReuseStrategy {

  /** Never store a detached route tree */
  shouldDetach(_route: ActivatedRouteSnapshot): boolean {
    return false;
  }

  /** Never store anything */
  store(_route: ActivatedRouteSnapshot, _handle: DetachedRouteHandle | null): void {}

  /** Never reattach a stored route */
  shouldAttach(_route: ActivatedRouteSnapshot): boolean {
    return false;
  }

  /** Nothing to retrieve */
  retrieve(_route: ActivatedRouteSnapshot): DetachedRouteHandle | null {
    return null;
  }

  /**
   * shouldReuseRoute — the key method.
   * Returning false here forces Angular to recreate the component
   * on EVERY navigation, including sidebar clicks to the current route.
   */
  shouldReuseRoute(_future: ActivatedRouteSnapshot, _curr: ActivatedRouteSnapshot): boolean {
    return false;
  }
}
