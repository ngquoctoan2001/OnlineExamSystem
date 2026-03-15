# ERD (Entity Relationship Design)

## 1. Purpose

This ERD document captures the core logical relationships used by Online Exam System.
It is grouped by business domains for delivery and review.

## 2. Core Authentication and Identity ERD

```mermaid
erDiagram
    USER ||--o{ USER_ROLE : has
    ROLE ||--o{ USER_ROLE : assigned_to
    ROLE ||--o{ ROLE_PERMISSION : includes
    PERMISSION ||--o{ ROLE_PERMISSION : granted_by

    USER ||--o{ USER_SESSION : owns
    USER ||--o{ USER_LOGIN_LOG : writes

    USER {
      long id PK
      string username
      string email
      string password_hash
      bool is_active
    }

    ROLE {
      long id PK
      string name
    }

    PERMISSION {
      long id PK
      string name
    }
```

## 3. School and Academic Structure ERD

```mermaid
erDiagram
    SCHOOL ||--o{ CLASS : has
    USER ||--|| TEACHER : profile
    USER ||--|| STUDENT : profile

    CLASS ||--o{ CLASS_TEACHER : mapping
    TEACHER ||--o{ CLASS_TEACHER : mapping
    SUBJECT ||--o{ CLASS_TEACHER : taught_in

    CLASS ||--o{ CLASS_STUDENT : contains
    STUDENT ||--o{ CLASS_STUDENT : enrolled

    SUBJECT ||--o{ SUBJECT_EXAM_TYPE : defines
```

## 4. Exam Authoring and Assignment ERD

```mermaid
erDiagram
    SUBJECT ||--o{ EXAM : groups
    TEACHER ||--o{ EXAM : creates
    SUBJECT_EXAM_TYPE ||--o{ EXAM : categorizes

    EXAM ||--|| EXAM_SETTING : configured_by

    EXAM ||--o{ EXAM_CLASS : assigned_to
    CLASS ||--o{ EXAM_CLASS : receives

    EXAM ||--o{ EXAM_QUESTION : includes
    QUESTION ||--o{ EXAM_QUESTION : referenced

    EXAM {
      long id PK
      long subject_id FK
      long created_by FK
      string title
      int duration_minutes
      string status
    }

    EXAM_SETTING {
      long id PK
      long exam_id FK
      bool shuffle_questions
      bool allow_late_submission
      int grace_period_minutes
      decimal late_penalty_percent
    }
```

## 5. Question Bank ERD

```mermaid
erDiagram
    SUBJECT ||--o{ QUESTION : contains
    QUESTION_TYPE ||--o{ QUESTION : classifies
    QUESTION ||--o{ QUESTION_OPTION : options

    QUESTION ||--o{ QUESTION_TAG : mapped
    TAG ||--o{ QUESTION_TAG : mapped

    QUESTION {
      long id PK
      long subject_id FK
      long question_type_id FK
      string content
      string difficulty
      bool is_published
    }

    QUESTION_OPTION {
      long id PK
      long question_id FK
      string label
      bool is_correct
      int order_index
    }
```

## 6. Attempt, Answer, and Grading ERD

```mermaid
erDiagram
    EXAM ||--o{ EXAM_ATTEMPT : produces
    STUDENT ||--o{ EXAM_ATTEMPT : starts

    EXAM_ATTEMPT ||--o{ ANSWER : has
    ANSWER ||--o{ ANSWER_OPTION : selects

    EXAM_ATTEMPT ||--o{ GRADING_RESULT : receives
    EXAM_ATTEMPT ||--o{ EXAM_VIOLATION : logs

    EXAM_ATTEMPT {
      long id PK
      long exam_id FK
      long student_id FK
      string status
      decimal score
      bool is_result_published
      bytes row_version
    }

    ANSWER {
      long id PK
      long exam_attempt_id FK
      long question_id FK
      string text_content
      string essay_content
      string canvas_image
    }

    GRADING_RESULT {
      long id PK
      long exam_attempt_id FK
      long question_id FK
      decimal score
      long graded_by
      datetime graded_at
    }
```

## 7. Notifications, Logs, and Analytics ERD

```mermaid
erDiagram
    USER ||--o{ NOTIFICATION : receives
    USER ||--o{ ACTIVITY_LOG : triggers
    EXAM ||--o{ EXAM_STATISTIC : aggregates

    NOTIFICATION {
      long id PK
      long user_id FK
      string type
      string title
      bool is_read
      datetime created_at
    }

    ACTIVITY_LOG {
      long id PK
      long user_id FK
      string action
      string entity_type
      long entity_id
      datetime occurred_at
    }
```

## 8. Integrity and Key Constraints

- Composite keys:
  - UserRole(UserId, RoleId)
  - RolePermission(RoleId, PermissionId)
  - ExamClass(ExamId, ClassId)
  - ClassStudent(ClassId, StudentId)
  - QuestionTag(QuestionId, TagId)
  - AnswerOption(AnswerId, OptionId)
- Uniqueness:
  - Users.username
  - Users.email
  - UserSessions.refresh_token
  - Students.student_code
  - Teachers.employee_id
  - Answers(exam_attempt_id, question_id)
- Concurrency:
  - ExamAttempt.row_version
