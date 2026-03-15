# Deployment Runbook

## 1. Purpose

This runbook defines how to deploy, verify, and operate Online Exam System in a controlled way.

## 2. Runtime Topology

- Frontend: Vite dev server (local development)
- API: ASP.NET Core Web API container or local process
- Database: PostgreSQL
- Cache: Redis
- DB Admin UI: Adminer

Main ports:

- Frontend: 3000
- API: 5000 (mapped to container 8080)
- PostgreSQL: 5433
- Redis: 6379
- Adminer: 8080

## 3. Prerequisites

- Docker Desktop with Compose
- .NET SDK
- Node.js and npm

## 4. Required Configuration

### 4.1 API settings (appsettings.json)

- ConnectionStrings.DefaultConnection
- JwtSettings.SecretKey
- JwtSettings.ExpirationMinutes
- JwtSettings.Issuer
- JwtSettings.Audience

### 4.2 Docker environment (docker-compose)

- ASPNETCORE_ENVIRONMENT
- ConnectionStrings__DefaultConnection
- JwtSettings__SecretKey
- JwtSettings__Issuer
- JwtSettings__Audience
- JwtSettings__ExpiryMinutes

## 5. Deployment Modes

### 5.1 Local Hybrid (recommended for development)

1. Start infrastructure:

```bash
docker-compose up -d postgres redis adminer
```

2. Run database migrations:

```bash
cd src/OnlineExamSystem.Infrastructure
dotnet ef database update --startup-project ../OnlineExamSystem.API
```

3. Start API:

```bash
cd ../OnlineExamSystem.API
dotnet run
```

4. Start frontend:

```bash
cd ../../../frontend
npm install
npm run dev
```

### 5.2 Full Docker stack

```bash
docker-compose up -d
```

## 6. Post-Deployment Validation

### 6.1 Service availability checks

- API health endpoint: GET /api/health
- Swagger endpoint: GET /swagger
- Database connectivity from API logs

### 6.2 Functional smoke checks

1. Login with a seeded account.
2. Fetch current profile endpoint.
3. List exams endpoint.
4. Start an exam attempt for a valid student.
5. Submit at least one answer.

## 7. Migration and Data Safety

### 7.1 Before applying migrations

1. Confirm target connection string.
2. Take database backup.
3. Ensure no migration conflicts in branch.

### 7.2 Apply migration

```bash
cd src/OnlineExamSystem.Infrastructure
dotnet ef database update --startup-project ../OnlineExamSystem.API
```

### 7.3 Rollback strategy

- Prefer forward-fix migration for shared environments.
- For local recovery only, recreate environment:

```bash
docker-compose down -v
docker-compose up -d
```

## 8. Operations and Monitoring

### 8.1 Logs

- API container logs:

```bash
docker logs -f onlineexam_api
```

- Database container logs:

```bash
docker logs -f onlineexam_db
```

### 8.2 Typical incident checks

- 401 spikes: inspect token expiry and auth configuration
- 429 spikes: inspect rate limit policy and client retry behavior
- DB errors: verify postgres container health and credentials
- Failed imports: inspect row-level import errors in API response

## 9. Backup and Restore (minimum baseline)

### 9.1 Backup

```bash
docker exec onlineexam_db pg_dump -U postgres onlineexam_dev > backup.sql
```

### 9.2 Restore

```bash
cat backup.sql | docker exec -i onlineexam_db psql -U postgres -d onlineexam_dev
```

## 10. Release Checklist

1. Build backend and frontend successfully.
2. Run automated tests.
3. Apply migrations on target environment.
4. Run smoke tests.
5. Verify health endpoint and logs.
6. Confirm default admin login works (or designated admin account).
7. Communicate release status and known issues.

## 11. Recovery Checklist

1. Detect and classify incident (auth, API, DB, import, grading).
2. Stabilize service (restart failing container/process).
3. Validate data integrity (attempt status, grading results, logs).
4. Apply fix (config, migration, code hotfix).
5. Re-run smoke tests.
6. Document root cause and preventive action.
