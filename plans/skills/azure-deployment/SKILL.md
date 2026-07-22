# Skill: Azure Deployment

## Objective

Deploy the tax expense tracker to Azure using an App Service-hosted API and a static frontend hosting path.

## Scope

- API: Azure App Service (free tier where available)
- Frontend: Azure Static Web Apps
- Security: API key baseline, JWT-ready path

## Implementation Checklist

1. Create Azure resource group.
2. Create App Service plan.
3. Create API web app for .NET runtime.
4. Publish API:
   - `dotnet publish`
   - deploy build artifact to App Service
5. Build frontend for production:
   - `ng build --configuration production`
6. Configure static hosting workflow (Azure Static Web Apps).
7. Set environment variables/secrets in Azure:
   - connection string
   - API key
   - JWT settings
8. Restrict CORS to approved frontend origin.
9. Enforce HTTPS and verify API accessibility.

## Security Checklist

1. No secrets in source control.
2. API key validation middleware active if used.
3. JWT auth path documented and testable.
4. Logs/monitoring enabled for failed auth and 5xx errors.

## Validation Steps

1. Deployed API health endpoint is reachable over HTTPS.
2. Frontend can call production API without CORS failures.
3. Basic CRUD flow works in deployed environment.

## Definition of Done

- Backend and frontend are deployed on Azure.
- Required environment settings are configured.
- Security baseline is active and verified.
