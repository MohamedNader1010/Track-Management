import { HttpClient } from '@angular/common/http';
import { DestroyRef, Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';

import { TokenResponse } from '../api/api.models';
import { silentErrors } from '../http/http-context.tokens';
import { LoggerService } from '../logging/logger.service';

export interface Session {
  token: string;
  username: string;
  roles: string[];
  expiresAt: number;
}

const STORAGE_KEY = 'takwene.session';
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly logger = inject(LoggerService);

  private readonly session = signal<Session | null>(this.restore());
  private expiryTimer?: ReturnType<typeof setTimeout>;

  readonly currentUser = this.session.asReadonly();
  readonly isAuthenticated = computed(() => this.session() !== null);
  readonly token = computed(() => this.session()?.token ?? null);

  constructor() {
    this.scheduleExpiry(this.session());
    inject(DestroyRef).onDestroy(() => clearTimeout(this.expiryTimer));
  }

  hasRole(role: string): boolean {
    return this.session()?.roles.includes(role) ?? false;
  }

  login(username: string, password: string): Observable<TokenResponse> {
    return this.http
      .post<TokenResponse>('/api/auth/token', { username, password }, { context: silentErrors() })
      .pipe(tap((response) => this.start(response)));
  }

  logout(reason?: string): void {
    if (this.session()) {
      this.logger.info('Signing out', reason ?? '');
    }
    clearTimeout(this.expiryTimer);
    this.session.set(null);
    sessionStorage.removeItem(STORAGE_KEY);
  }


  expireAndRedirect(): void {
    const returnUrl = this.router.url;
    this.logout('token rejected by API');
    void this.router.navigate(['/login'], { queryParams: { returnUrl, expired: true } });
  }

  private start(response: TokenResponse): void {
    const claims = decodeJwtPayload(response.accessToken);
    const session: Session = {
      token: response.accessToken,
      username: String(claims['sub'] ?? ''),
      roles: toArray(claims['role']),
      expiresAt: Date.parse(response.expiresAtUtc),
    };

    this.session.set(session);
    sessionStorage.setItem(STORAGE_KEY, JSON.stringify(session));
    this.scheduleExpiry(session);
    this.logger.info(`Signed in as ${session.username}`, session.roles);
  }

  private restore(): Session | null {
    try {
      const raw = sessionStorage.getItem(STORAGE_KEY);
      const session = raw ? (JSON.parse(raw) as Session) : null;
      return session && session.expiresAt > Date.now() ? session : null;
    } catch {
      return null;
    }
  }

  private scheduleExpiry(session: Session | null): void {
    clearTimeout(this.expiryTimer);
    if (session) {
      this.expiryTimer = setTimeout(() => this.logout('token expired'), session.expiresAt - Date.now());
    }
  }
}

function decodeJwtPayload(token: string): Record<string, unknown> {
  try {
    const payload = token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/');
    const json = decodeURIComponent(
      atob(payload)
        .split('')
        .map((c) => '%' + c.charCodeAt(0).toString(16).padStart(2, '0'))
        .join(''),
    );
    return JSON.parse(json) as Record<string, unknown>;
  } catch {
    return {};
  }
}

function toArray(value: unknown): string[] {
  if (Array.isArray(value)) return value.map(String);
  return value === undefined || value === null ? [] : [String(value)];
}
