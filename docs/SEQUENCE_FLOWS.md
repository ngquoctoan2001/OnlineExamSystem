# Sequence Flows

## 1. Authentication Flow

```mermaid
sequenceDiagram
    participant U as User
    participant FE as Frontend
    participant AC as AuthController
    participant AS as AuthService
    participant UR as UserRepository
    participant SR as UserSessionRepository

    U->>FE: Enter username and password
    FE->>AC: POST /api/auth/login
    AC->>AS: LoginAsync(credentials, ip, userAgent)
    AS->>UR: GetByUsernameAsync
    UR-->>AS: User
    AS->>AS: Verify password hash
    AS->>SR: Create refresh token session
    AS-->>AC: AccessToken + RefreshToken
    AC-->>FE: 200 Success
    FE->>FE: Persist token for API calls
```

## 2. Start Exam Attempt

```mermaid
sequenceDiagram
    participant S as Student
    participant FE as Frontend
    participant EAC as ExamAttemptsController
    participant EAS as ExamAttemptService
    participant DB as PostgreSQL

    S->>FE: Start exam
    FE->>EAC: POST /api/exam-attempts/start
    EAC->>EAS: StartAttemptAsync(examId, studentId)
    EAS->>DB: Validate exam status and time window
    EAS->>DB: Validate student enrollment in assigned class
    EAS->>DB: Check retake and attempt limits
    EAS->>DB: Create ExamAttempt(IN_PROGRESS)
    EAS-->>EAC: Attempt response
    EAC-->>FE: 200 Success
```

## 3. Submit or Update Answer

```mermaid
sequenceDiagram
    participant S as Student
    participant FE as Frontend
    participant EAC as ExamAttemptsController
    participant AAS as AnswerService
    participant DB as PostgreSQL

    S->>FE: Submit answer for question
    FE->>EAC: POST /api/exam-attempts/{id}/answers
    EAC->>AAS: SubmitAnswerAsync(attemptId, request)
    AAS->>DB: Validate attempt status/time
    AAS->>DB: Check existing answer
    alt Existing answer found
        AAS->>DB: Update answer and selected options
    else No existing answer
        AAS->>DB: Insert answer and selected options
    end
    AAS-->>EAC: Answer response
    EAC-->>FE: 200 Success
```

## 4. Submit Attempt and Auto-Grade

```mermaid
sequenceDiagram
    participant S as Student
    participant FE as Frontend
    participant EAC as ExamAttemptsController
    participant EAS as ExamAttemptService
    participant DB as PostgreSQL

    S->>FE: Submit attempt
    FE->>EAC: POST /api/exam-attempts/{id}/submit
    EAC->>EAS: SubmitAttemptAsync(attemptId)
    EAS->>DB: Validate submission timing (late/grace rules)
    EAS->>DB: Set status SUBMITTED and end time
    EAS->>DB: Auto-grade objective questions
    EAS->>DB: Apply late penalty if needed
    EAS-->>EAC: Submission result
    EAC-->>FE: 200 Success
```

## 5. Manual Grading and Publish Result

```mermaid
sequenceDiagram
    participant T as Teacher
    participant FE as Frontend
    participant GC as GradingController
    participant GS as GradingService
    participant DB as PostgreSQL
    participant NS as NotificationService

    T->>FE: Open attempt grading view
    FE->>GC: GET /api/grading/attempts/{attemptId}/view
    GC->>GS: GetAttemptGradingViewAsync
    GS->>DB: Load attempt, answers, grading results
    GS-->>GC: Grading view
    GC-->>FE: Render details

    T->>FE: Submit manual score
    FE->>GC: PUT /api/grading/attempts/{attemptId}/questions/{questionId}
    GC->>GS: ManualGradeQuestionAsync
    GS->>DB: Upsert grading result

    T->>FE: Publish result
    FE->>GC: POST /api/grading/attempts/{attemptId}/publish
    GC->>GS: PublishResultAsync
    GS->>DB: Set IsResultPublished = true
    GS->>NS: Create notification for student
    GC-->>FE: Publish success
```

## 6. Bulk Import Questions (Current Active Path)

```mermaid
sequenceDiagram
    participant A as Admin or Teacher
    participant FE as Frontend or Swagger
    participant IC as ImportController
    participant IS as ImportService
    participant XP as ExcelParserService
    participant DB as PostgreSQL

    A->>FE: Upload .xlsx or .xls
    FE->>IC: POST /api/import/questions
    IC->>IS: ImportQuestionsAsync(stream, userId)
    IS->>XP: ParseExcelAsync<ImportQuestionRow>
    XP-->>IS: Parsed rows
    IS->>DB: Validate and insert Question + QuestionOptions
    IS-->>IC: Import summary with row errors
    IC-->>FE: Import result
```

## 7. Notes

- PdfImportService exists in codebase and can be used for a future PDF import endpoint.
- Current controller-level import endpoints enforce Excel extensions.
