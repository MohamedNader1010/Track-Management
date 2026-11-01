import { HttpContext, HttpContextToken } from '@angular/common/http';

/** Don't show the global loader for this request (e.g. background refreshes). */
export const SKIP_LOADER = new HttpContextToken<boolean>(() => false);

/** Don't raise a global toast; the caller renders the error itself (e.g. inline form errors). */
export const SKIP_ERROR_TOAST = new HttpContextToken<boolean>(() => false);

export const silentErrors = () => new HttpContext().set(SKIP_ERROR_TOAST, true);
