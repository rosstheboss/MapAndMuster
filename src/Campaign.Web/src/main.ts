import { bootstrapApplication } from '@angular/platform-browser';
import { App } from './app/app';
import { createAppConfig } from './app/app.config';
import { loadPublicRuntimeConfig } from './app/core/config/public-runtime-config';

const runtimeConfig = await loadPublicRuntimeConfig();
bootstrapApplication(App, createAppConfig(runtimeConfig)).catch((err: unknown) => {
  console.error(err);
});
