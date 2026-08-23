import 'zone.js';
import { bootstrapApplication } from '@angular/platform-browser';
import { createAppConfig } from './app/app.config';
import { App } from './app/app';
import { loadAuthConfig } from './app/auth/auth.config';

loadAuthConfig()
  .then((authConfig) => bootstrapApplication(App, createAppConfig(authConfig)))
  .catch((error: unknown) => console.error(error));
