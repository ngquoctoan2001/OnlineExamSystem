# System Design Analysis and Architecture Diagrams

## 1. Scope and Method

This document is based on a source-level review of the current implementation in:

- Backend API bootstrapping and middleware
- Domain entities and EF Core model configuration
- Core services for auth, exam lifecycle, answer handling, grading, and import
- Frontend routing, auth context, and API client behavior
- Runtime topology from docker-compose

Primary references in source tree:

- src/OnlineExamSystem.API/Program.cs
- src/OnlineExamSystem.API/Controllers/
- src/OnlineExamSystem.Domain/Entities/AllEntities.cs
- src/OnlineExamSystem.Infrastructure/Data/ApplicationDbContext.cs
- src/OnlineExamSystem.Infrastructure/Services/
- frontend/src/App.tsx
- frontend/src/contexts/AuthContext.tsx
- frontend/src/api/client.ts
- docker-compose.yml

## 2. Architectural Summary

The system follows a practical Clean Architecture style with 4 projects:

- OnlineExamSystem.Domain: business entities
- OnlineExamSystem.Application: DTOs and interfaces/contracts
- OnlineExamSystem.Infrastructure: repositories, EF Core, service implementations
- OnlineExamSystem.API: controllers, middleware, runtime composition

Notes from implementation:

- Most business logic implementations currently live in Infrastructure services.
- API enforces JWT authentication, role-based authorization, CORS, and fixed-window rate limits.
- Data persistence is PostgreSQL via EF Core.
- Redis is provisioned in infrastructure but currently not deeply integrated in reviewed core flows.

## 3. C4 Level 1 - System Context

```mermaid
flowchart LR
    admin[Admin]
    teacher[Teacher]
    student[Student]

    sys[Online Exam System]

    admin -->|Manage users, classes, system| sys
    teacher -->|Create exams, grade attempts| sys
    student -->|Take exams, view results| sys

    sys -->|Persist transactional data| pg[(PostgreSQL)]
    sys -->|Cache and future session acceleration| redis[(Redis)]
    sys -->|Store uploaded files/assets| storage[(File Storage / MinIO)]
```

## 4. C4 Level 2 - Container Diagram

```mermaid
flowchart TB
    subgraph Client
      web[React SPA\nVite + TypeScript]
    end

    subgraph Backend
      api[ASP.NET Core Web API]
      ctl[Controllers]
      svc[Infrastructure Services\nExamService, ExamAttemptService,\nAnswerService, GradingService, AuthService]
      repo[Repositories]
      orm[EF Core DbContext]
    end

    subgraph Data
      pg[(PostgreSQL)]
      redis[(Redis)]
      fs[(File Storage / MinIO)]
    end

    web -->|HTTP JSON /api| api
    api --> ctl
    ctl --> svc
    svc --> repo
    repo --> orm
    orm --> pg

    svc -.optional / partial.- redis
    svc --> fs
```

## 5. Runtime Deployment View

```mermaid
flowchart LR
    browser[User Browser]

    subgraph Docker Host
      front[Frontend Dev Server\nlocalhost:3000]
      api[API Container\nlocalhost:5000 -> :8080]
      db[Postgres Container\nlocalhost:5433 -> :5432]
      cache[Redis Container\nlocalhost:6379]
      adminer[Adminer\nlocalhost:8080]
    end

    browser --> front
    front -->|Vite proxy /api| api
    api --> db
    api --> cache
    browser --> adminer
```

## 6. Core Domain Model (High-Level ER)

```mermaid
erDiagram
    USER ||--o{ USER_ROLE : has
    ROLE ||--o{ USER_ROLE : grants
    ROLE ||--o{ ROLE_PERMISSION : includes
    PERMISSION ||--o{ ROLE_PERMISSION : defines

    USER ||--|| TEACHER : profile
    USER ||--|| STUDENT : profile

    SCHOOL ||--o{ CLASS : owns
    CLASS ||--o{ CLASS_STUDENT : contains
    STUDENT ||--o{ CLASS_STUDENT : joins

    SUBJECT ||--o{ QUESTION : has
    QUESTION_TYPE ||--o{ QUESTION : categorizes
    QUESTION ||--o{ QUESTION_OPTION : offers

    SUBJECT ||--o{ EXAM : groups
    TEACHER ||--o{ EXAM : creates
    EXAM ||--o{ EXAM_QUESTION : includes
    QUESTION ||--o{ EXAM_QUESTION : referenced
    EXAM ||--o{ EXAM_CLASS : assigned_to
    CLASS ||--o{ EXAM_CLASS : receives

    EXAM ||--o{ EXAM_ATTEMPT : produces
    STUDENT ||--o{ EXAM_ATTEMPT : starts
    EXAM_ATTEMPT ||--o{ ANSWER : stores
    ANSWER ||--o{ ANSWER_OPTION : selects

    EXAM_ATTEMPT ||--o{ GRADING_RESULT : graded_by_question
    USER ||--o{ NOTIFICATION : receives
    USER ||--o{ ACTIVITY_LOG : traces
```

## 7. Key Sequence - Authentication

```mermaid
sequenceDiagram
    participant U as User
    participant FE as Frontend
    participant API as AuthController
    participant AS as AuthService
    participant UR as UserRepository
    participant SR as UserSessionRepository

    U->>FE: Submit username/password
    FE->>API: POST /api/auth/login
    API->>AS: LoginAsync(...)
    AS->>UR: GetByUsername
    UR-->>AS: User + roles
    AS->>AS: Verify password hash
    AS->>SR: Create refresh token session
    AS-->>API: AccessToken + RefreshToken
    API-->>FE: 200 login response
    FE->>FE: Save token in localStorage
    FE->>FE: Attach token via axios interceptor
```

## 8. Key Sequence - Exam Taking Lifecycle

```mermaid
sequenceDiagram
    participant S as Student
    participant FE as Frontend
    participant AC as ExamAttemptsController
    participant EAS as ExamAttemptService
    participant AAS as AnswerService
    participant DB as PostgreSQL

    S->>FE: Start exam
    FE->>AC: POST /api/exam-attempts/start
    AC->>EAS: StartAttemptAsync(examId, studentId)
    EAS->>DB: Validate exam status/window/enrollment
    EAS->>DB: Create ExamAttempt(IN_PROGRESS)
    EAS-->>AC: Attempt created
    AC-->>FE: attemptId

    loop During exam
        FE->>AC: POST /api/exam-attempts/{id}/answers
        AC->>AAS: SubmitAnswerAsync
        AAS->>DB: Upsert Answer + AnswerOptions
        AAS-->>AC: Answer saved
        AC-->>FE: Success
    end

    S->>FE: Submit attempt
    FE->>AC: POST /api/exam-attempts/{id}/submit
    AC->>EAS: SubmitAttemptAsync
    EAS->>DB: Update status SUBMITTED
    EAS->>DB: Auto-grade objective questions
    EAS-->>AC: Submission result + late penalty info
    AC-->>FE: Submission completed
```

## 9. Key Sequence - Grading and Publication

```mermaid
sequenceDiagram
    participant T as Teacher
    participant FE as Frontend
    participant GC as GradingController
    participant GS as GradingService
    participant DB as PostgreSQL
    participant NS as NotificationService

    T->>FE: Open grading screen
    FE->>GC: GET /api/grading/attempts/{attemptId}/view
    GC->>GS: GetAttemptGradingViewAsync
    GS->>DB: Load attempt + answers + gradingResults
    GS-->>GC: Aggregated grading view
    GC-->>FE: Render question-by-question grading

    T->>FE: Manual grade subjective question
    FE->>GC: PUT /api/grading/attempts/{attemptId}/questions/{questionId}
    GC->>GS: ManualGradeQuestionAsync
    GS->>DB: Upsert GradingResult

    T->>FE: Finalize and publish
    FE->>GC: POST /api/grading/attempts/{attemptId}/publish
    GC->>GS: PublishResultAsync
    GS->>DB: Mark IsResultPublished=true
    GS->>NS: Create student notification
    GC-->>FE: Published
```

## 10. Key Sequence - Question Import (Current: Excel)

```mermaid
sequenceDiagram
    participant Admin as Admin/Teacher
    participant FE as Frontend or Swagger
    participant API as ImportController
    participant IS as ImportService
  participant XP as ExcelParserService
    participant DB as PostgreSQL

  Admin->>FE: Upload Excel file
    FE->>API: POST /api/import/questions
    API->>IS: ImportQuestionsAsync(stream, userId)
  IS->>XP: ParseExcelAsync<ImportQuestionRow>
    IS->>DB: Create Question + Options
    IS-->>API: ImportResult(success/fail per row)
    API-->>FE: Import completed summary
```

Note:

- PdfImportService and IPdfImportService exist in codebase and are registered in DI.
- Current ImportController endpoints validate only .xlsx/.xls and route to ImportService (Excel flow).
- PDF parsing can be documented as an extension path, not as active API flow yet.

## 11. Cross-Cutting Design Decisions

- Authentication and authorization:
  - JWT bearer auth
  - Role checks at controller endpoint level
- Reliability:
  - Unique index on Answer(ExamAttemptId, QuestionId) to prevent duplicate submissions
  - RowVersion on ExamAttempt for optimistic concurrency
- Security hardening:
  - Security headers
  - API rate limiting (auth and general profiles)
- Observability:
  - Activity logs for key actions (start exam, submit, grade publish)

## 12. Risks and Design Recommendations

Current risks observed from implementation review:

- Service placement drift: business logic concentrated in Infrastructure can reduce architectural clarity.
- In-memory/local token handling in SPA can be vulnerable to XSS if frontend hardening is weak.
- Redis appears underused compared to provisioned topology.

Recommendations:

1. Move domain-heavy use case orchestration toward Application layer services.
2. Add centralized authorization policy handlers for repeated ownership checks.
3. Introduce standardized idempotency strategy for critical write endpoints.
4. Expand event-driven notifications/audit through background queue if traffic grows.
5. Add architecture decision records (ADR) for major choices (auth model, grading strategy, import parser constraints).

## 13. Suggested Documentation Set for Next Iteration

- API contract handbook per bounded context (Auth, Exam, Attempt, Grading, Import)
- Non-functional requirements (performance SLO, reliability objectives, backup/restore)
- Threat model and security controls matrix
- Data retention and exam evidence policy
- Deployment runbook (local, staging, production)

---

Document version: 1.0  
Last reviewed: 2026-03-14
