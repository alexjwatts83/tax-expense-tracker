import { InjectionToken } from '@angular/core';

export interface AuthConfig {
  enabled: boolean;
  tenantId: string;
  clientId: string;
  apiScope: string;
  apiBasePath: string;
}

export const AUTH_CONFIG = new InjectionToken<AuthConfig>('AUTH_CONFIG');

export const disabledAuthConfig: AuthConfig = {
  enabled: false,
  tenantId: '',
  clientId: '',
  apiScope: '',
  apiBasePath: '/api',
};

export async function loadAuthConfig(): Promise<AuthConfig> {
  const response = await fetch('/auth-config.json', { cache: 'no-store' });
  if (!response.ok)
    throw new Error(`Unable to load authentication configuration (${response.status}).`);

  return parseAuthConfig(await response.json());
}

export function parseAuthConfig(value: unknown): AuthConfig {
  if (!isRecord(value))
    throw new Error('Authentication configuration must be a JSON object.');

  const config: AuthConfig = {
    enabled: value['enabled'] === true,
    tenantId: readString(value, 'tenantId'),
    clientId: readString(value, 'clientId'),
    apiScope: readString(value, 'apiScope'),
    apiBasePath: readString(value, 'apiBasePath') || '/api',
  };

  if (config.enabled) {
    const missingKeys = (['tenantId', 'clientId', 'apiScope'] as const)
      .filter((key) => !config[key]);
    if (missingKeys.length > 0)
      throw new Error(`Enabled authentication requires: ${missingKeys.join(', ')}.`);
  }

  if (!config.apiBasePath.startsWith('/'))
    throw new Error('Authentication apiBasePath must begin with /.');

  return config;
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}

function readString(value: Record<string, unknown>, key: string): string {
  return typeof value[key] === 'string' ? value[key].trim() : '';
}