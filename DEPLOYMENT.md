# LearnPath — Production Deployment Checklist

## Pre-Deployment

### Secrets (never commit)
- [ ] DB_SA_PASSWORD — strong password, 16+ chars
- [ ] JWT_SECRET — random 64+ char string (exposed to backend as LEARNPATH_JWT_SECRET)
- [ ] LEARNPATH_NVIDIA_API_KEY — NVIDIA API key for AI feedback (startup validation fails without it)
- [ ] VITE_API_BASE_URL — production API URL
- [ ] DEPLOY_HOST, DEPLOY_USER, DEPLOY_SSH_KEY — GitHub secrets

### Backend
- [ ] appsettings.Production.json configured
- [ ] All EF migrations applied: `dotnet ef database update`
- [ ] Roles seeded (auto on startup)
- [ ] HTTPS enforced
- [ ] CORS origins locked to production domain
- [ ] JWT expiry tuned (60min access, 7d refresh)
- [ ] Rate limiting enabled
- [ ] Logging level set to Warning in production
- [ ] Health endpoint responding: GET /api/v1/health

### Frontend
- [ ] VITE_API_BASE_URL set to production API
- [ ] npm run build succeeds
- [ ] npm run type-check passes
- [ ] dist/ folder served via nginx

### Docker
- [ ] docker-compose.yml reviewed
- [ ] .env file created from .env.example
- [ ] Images build successfully: docker compose build
- [ ] Stack starts: docker compose up -d
- [ ] DB health check passes
- [ ] Backend reachable on :5000
- [ ] Frontend reachable on :80

### Docker quick start

```bash
# 1. Configure (copies docker .env template; fill in the three LEARNPATH_* vars)
cp .env.example .env   # then edit .env

# 2. Build all three images
docker compose build

# 3. Start the stack (MigrationsExecuteOnStartup applies the schema;
#    roles are seeded automatically on first API boot)
docker compose up -d

# 4. Verify health (container HEALTHCHECK does this every 15s; to check manually)
curl http://localhost:5000/api/v1/health

# 5. Tear down (add -v to also delete the SQL Server data volume)
docker compose down
```

Notes:
- The backend requires the `LEARNPATH_DB_CONNECTION`, `LEARNPATH_JWT_SECRET`, and
  `LEARNPATH_NVIDIA_API_KEY` environment variables — startup validation aborts
  with a list of what is missing if any are absent (deliberate design; do not
  weaken the validation to "fix" deployment failures).
- The SQL Server 2022 container health check uses
  `/opt/mssql-tools18/bin/sqlcmd` and needs the `-C` flag to trust the
  self-signed certificate.

## Post-Deployment

- [ ] Login flow works end to end
- [ ] JWT refresh works
- [ ] Dashboard loads data
- [ ] Learning path creation works
- [ ] Classroom join/create works
- [ ] Community posts load with pagination
- [ ] Notifications load in topbar
- [ ] Admin panel accessible with Admin role
- [ ] Certificates page works
- [ ] Analytics charts render

## Rollback Plan

# Pull previous image
docker compose pull
docker compose up -d --remove-orphans

# Or roll back migration
dotnet ef database update PreviousMigrationName

## Monitoring

- Health: GET /api/v1/health
- Logs: docker compose logs -f backend
- DB: docker compose exec db /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$DB_SA_PASSWORD" -C
