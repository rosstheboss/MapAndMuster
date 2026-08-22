import type { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';

import { PUBLIC_RUNTIME_CONFIG, apiUrl } from './public-runtime-config';

export const apiBaseInterceptor: HttpInterceptorFn = (req, next) => {
  const config = inject(PUBLIC_RUNTIME_CONFIG);
  if (config.apiBaseUrl.length === 0 || !req.url.startsWith('/')) {
    return next(req);
  }

  return next(req.clone({ url: apiUrl(req.url, config.apiBaseUrl) }));
};
