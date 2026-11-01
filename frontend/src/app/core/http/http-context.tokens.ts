import { HttpContext, HttpContextToken } from '@angular/common/http';

export const SKIP_LOADER = new HttpContextToken<boolean>(() => false);

export const SKIP_ERROR_TOAST = new HttpContextToken<boolean>(() => false);

export const silentErrors = () => new HttpContext().set(SKIP_ERROR_TOAST, true);
