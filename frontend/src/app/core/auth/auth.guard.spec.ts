import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, Router, RouterStateSnapshot, UrlTree, provideRouter } from '@angular/router';

import { roleGuard } from './auth.guard';
import { AuthService } from './auth.service';

describe('roleGuard', () => {
  const run = () =>
    TestBed.runInInjectionContext(() =>
      roleGuard('catalog-manager')({} as ActivatedRouteSnapshot, { url: '/artists/new' } as RouterStateSnapshot),
    );

  beforeEach(() => {
    sessionStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    });
  });

  it('sends signed-out users to login with a returnUrl', () => {
    const result = run() as UrlTree;

    expect(TestBed.inject(Router).serializeUrl(result)).toBe('/login?returnUrl=%2Fartists%2Fnew');
  });

  it('lets catalog managers through', () => {
    const auth = TestBed.inject(AuthService);
    vi.spyOn(auth, 'isAuthenticated').mockReturnValue(true);
    vi.spyOn(auth, 'hasRole').mockReturnValue(true);

    expect(run()).toBe(true);
  });

  it('redirects signed-in users without the role', () => {
    const auth = TestBed.inject(AuthService);
    vi.spyOn(auth, 'isAuthenticated').mockReturnValue(true);
    vi.spyOn(auth, 'hasRole').mockReturnValue(false);

    expect(TestBed.inject(Router).serializeUrl(run() as UrlTree)).toBe('/tracks');
  });
});
