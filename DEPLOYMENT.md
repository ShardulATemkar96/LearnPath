# LearnPath — Production Deployment Checklist

## Pre-Deployment

### Secrets (never commit)
- [ ] DB_SA_PASSWORD — strong password, 16+ chars
- [ ] JWT_SECRET — random 64+ char string
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
- DB: docker compose exec db /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P $DB_SA_PASSWORD
