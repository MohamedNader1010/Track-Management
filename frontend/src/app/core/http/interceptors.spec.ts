import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import { ApiError } from '../api/api-error';
import { LoadingService } from '../loading/loading.service';
import { NotificationService } from '../notifications/notification.service';
import { authInterceptor, errorInterceptor, loadingInterceptor } from './interceptors';
import { silentErrors } from './http-context.tokens';

describe('HTTP interceptors', () => {
  let http: HttpClient;
  let backend: HttpTestingController;

  beforeEach(() => {
    sessionStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        provideHttpClient(withInterceptors([loadingInterceptor, authInterceptor, errorInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    http = TestBed.inject(HttpClient);
    backend = TestBed.inject(HttpTestingController);
  });

  afterEach(() => backend.verify());

  it('tracks loading state for the duration of a request', () => {
    const loading = TestBed.inject(LoadingService);

    http.get('/api/tracks').subscribe();
    expect(loading.isLoading()).toBe(true);

    backend.expectOne('/api/tracks').flush({});
    expect(loading.isLoading()).toBe(false);
  });

  it('does not attach a token when signed out', () => {
    http.get('/api/tracks').subscribe();

    expect(backend.expectOne('/api/tracks').request.headers.has('Authorization')).toBe(false);
    backend.match(() => true).forEach((r) => r.flush({}));
  });

  it('converts failures to ApiError and raises a toast', async () => {
    const notifications = TestBed.inject(NotificationService);
    const result = firstValueFrom(http.get('/api/tracks/x'));

    backend.expectOne('/api/tracks/x').flush({ title: 'Track not found', status: 404 }, { status: 404, statusText: 'Not Found' });

    await expect(result).rejects.toBeInstanceOf(ApiError);
    expect(notifications.toasts().map((t) => t.message)).toContain('Track not found');
  });

  it('respects SKIP_ERROR_TOAST', async () => {
    const notifications = TestBed.inject(NotificationService);
    const result = firstValueFrom(http.get('/api/x', { context: silentErrors() }));

    backend.expectOne('/api/x').flush({ title: 'nope', status: 409 }, { status: 409, statusText: 'Conflict' });

    await expect(result).rejects.toBeInstanceOf(ApiError);
    expect(notifications.toasts()).toHaveLength(0);
  });
});
