# Database Schema Documentation

## Overview
Online Exam System uses PostgreSQL with 25 tables organized into 7 groups for efficient data management.

## 1. Authentication & User Management (7 tables)

### users
Stores all user accounts in the system.

```sql
CREATE TABLE users (
    id BIGSERIAL PRIMARY KEY,
    username VARCHAR(50) UNIQUE NOT NULL,
    password_hash TEXT NOT NULL,
    email VARCHAR(255) UNIQUE NOT NULL,
    full_name VARCHAR(255),
    role VARCHAR(20) NOT NULL,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP,
    last_login_at TIMESTAMP
);
```

### user_sessions
JWT session tokens and refresh tokens

```sql
CREATE TABLE user_sessions (
    id BIGSERIAL PRIMARY KEY,
    user_id BIGINT REFERENCES users(id) ON DELETE CASCADE,
    refresh_token TEXT NOT NULL,
    expires_at TIMESTAMP NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

### user_login_logs
Audit trail for user logins

```sql
CREATE TABLE user_login_logs (
    id BIGSERIAL PRIMARY KEY,
    user_id BIGINT REFERENCES users(id) ON DELETE CASCADE,
    ip_address VARCHAR(50),
    device_info TEXT,
    login_time TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

### roles
Role definitions (Admin, Teacher, Student)

```sql
CREATE TABLE roles (
    id BIGSERIAL PRIMARY KEY,
    name VARCHAR(50) UNIQUE NOT NULL,
    description TEXT
);
```

### permissions
Permission definitions

```sql
CREATE TABLE permissions (
    id BIGSERIAL PRIMARY KEY,
    name VARCHAR(100) UNIQUE NOT NULL,
    description TEXT
);
```

### role_permissions
Junction table for role-permission relationship

```sql
CREATE TABLE role_permissions (
    role_id BIGINT REFERENCES roles(id) ON DELETE CASCADE,
    permission_id BIGINT REFERENCES permissions(id) ON DELETE CASCADE,
    PRIMARY KEY (role_id, permission_id)
);
```

### user_roles
Junction table for user-role relationship

```sql
CREATE TABLE user_roles (
    user_id BIGINT REFERENCES users(id) ON DELETE CASCADE,
    role_id BIGINT REFERENCES roles(id) ON DELETE CASCADE,
    PRIMARY KEY (user_id, role_id)
);
```

## 2. School Structure (5 tables)

### schools
School information

```sql
CREATE TABLE schools (
    id BIGSERIAL PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    address TEXT,
    phone VARCHAR(20),
    email VARCHAR(255),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

### subjects
Subjects offered in the school

```sql
CREATE TABLE subjects (
    id BIGSERIAL PRIMARY KEY,
    school_id BIGINT REFERENCES schools(id) ON DELETE CASCADE,
    name VARCHAR(100) NOT NULL,
    code VARCHAR(50),
    description TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

### classes
Classes in the school

```sql
CREATE TABLE classes (
    id BIGSERIAL PRIMARY KEY,
    school_id BIGINT REFERENCES schools(id) ON DELETE CASCADE,
    name VARCHAR(50) NOT NULL,
    grade INT NOT NULL,
    homeroom_teacher_id BIGINT,
    student_count INT DEFAULT 0,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

### teachers
Teacher information

```sql
CREATE TABLE teachers (
    id BIGSERIAL PRIMARY KEY,
    user_id BIGINT REFERENCES users(id) ON DELETE CASCADE,
    school_id BIGINT REFERENCES schools(id) ON DELETE CASCADE,
    teacher_code VARCHAR(50) UNIQUE NOT NULL,
    full_name VARCHAR(255),
    subject_id BIGINT REFERENCES subjects(id),
    position VARCHAR(100),
    degree VARCHAR(100),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

### students
Student information

```sql
CREATE TABLE students (
    id BIGSERIAL PRIMARY KEY,
    user_id BIGINT REFERENCES users(id) ON DELETE CASCADE,
    school_id BIGINT REFERENCES schools(id) ON DELETE CASCADE,
    class_id BIGINT REFERENCES classes(id) ON DELETE CASCADE,
    student_code VARCHAR(50) UNIQUE NOT NULL,
    full_name VARCHAR(255),
    date_of_birth DATE,
    gender VARCHAR(10),
    phone VARCHAR(20),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

## 3. Exam Management (5 tables)

### exams
Exam/Test configuration

```sql
CREATE TABLE exams (
    id BIGSERIAL PRIMARY KEY,
    teacher_id BIGINT REFERENCES teachers(id),
    subject_id BIGINT REFERENCES subjects(id),
    title VARCHAR(500) NOT NULL,
    description TEXT,
    duration_minutes INT NOT NULL,
    passing_score INT,
    total_questions INT,
    total_score DECIMAL(5,2),
    exam_type VARCHAR(50),
    status VARCHAR(50) DEFAULT 'DRAFT',
    start_time TIMESTAMP,
    end_time TIMESTAMP,
    shuffle_questions BOOLEAN DEFAULT FALSE,
    show_answer_after BOOLEAN DEFAULT FALSE,
    allow_review BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP
);
```

### exam_classes
Which classes take which exams

```sql
CREATE TABLE exam_classes (
    exam_id BIGINT REFERENCES exams(id) ON DELETE CASCADE,
    class_id BIGINT REFERENCES classes(id) ON DELETE CASCADE,
    PRIMARY KEY (exam_id, class_id)
);
```

### exam_settings
Exam-specific settings

```sql
CREATE TABLE exam_settings (
    id BIGSERIAL PRIMARY KEY,
    exam_id BIGINT REFERENCES exams(id) ON DELETE CASCADE,
    setting_key VARCHAR(100),
    setting_value TEXT
);
```

### exam_attempts
Student's exam attempts

```sql
CREATE TABLE exam_attempts (
    id BIGSERIAL PRIMARY KEY,
    exam_id BIGINT REFERENCES exams(id),
    student_id BIGINT REFERENCES students(id),
    status VARCHAR(50) DEFAULT 'IN_PROGRESS',
    start_time TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    end_time TIMESTAMP,
    score DECIMAL(5,2),
    is_passed BOOLEAN,
    time_spent_seconds INT
);
```

### exam_questions
Which questions in which exams

```sql
CREATE TABLE exam_questions (
    id BIGSERIAL PRIMARY KEY,
    exam_id BIGINT REFERENCES exams(id) ON DELETE CASCADE,
    question_id BIGINT REFERENCES questions(id),
    display_order INT,
    score_weight DECIMAL(5,2)
);
```

## 4. Question Management (4 tables)

### question_types
Types of questions

```sql
CREATE TABLE question_types (
    id BIGSERIAL PRIMARY KEY,
    name VARCHAR(100),
    description TEXT,
    allow_partial_score BOOLEAN DEFAULT FALSE
);
```

### questions
Question database

```sql
CREATE TABLE questions (
    id BIGSERIAL PRIMARY KEY,
    subject_id BIGINT REFERENCES subjects(id),
    question_type_id BIGINT REFERENCES question_types(id),
    created_by_teacher_id BIGINT REFERENCES teachers(id),
    content TEXT NOT NULL,
    explanation TEXT,
    score DECIMAL(5,2),
    difficulty_level INT,
    is_published BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP
);
```

### question_options
Options for multiple choice/true false questions

```sql
CREATE TABLE question_options (
    id BIGSERIAL PRIMARY KEY,
    question_id BIGINT REFERENCES questions(id) ON DELETE CASCADE,
    content TEXT NOT NULL,
    is_correct BOOLEAN DEFAULT FALSE,
    display_order INT
);
```

### question_tags
Tags for categorizing questions

```sql
CREATE TABLE question_tags (
    id BIGSERIAL PRIMARY KEY,
    question_id BIGINT REFERENCES questions(id) ON DELETE CASCADE,
    tag VARCHAR(100)
);
```

## 5. Answer Management (4 tables)

### answers
Student's answer to a question

```sql
CREATE TABLE answers (
    id BIGSERIAL PRIMARY KEY,
    exam_attempt_id BIGINT REFERENCES exam_attempts(id) ON DELETE CASCADE,
    question_id BIGINT REFERENCES questions(id),
    student_id BIGINT REFERENCES students(id),
    text_answer TEXT,
    answer_score DECIMAL(5,2),
    status VARCHAR(50) DEFAULT 'UNANSWERED',
    answered_at TIMESTAMP,
    time_spent_seconds INT
);
```

### answer_options
Selected options for multiple choice

```sql
CREATE TABLE answer_options (
    id BIGSERIAL PRIMARY KEY,
    answer_id BIGINT REFERENCES answers(id) ON DELETE CASCADE,
    question_option_id BIGINT REFERENCES question_options(id)
);
```

### answer_canvas
Canvas drawing for answers

```sql
CREATE TABLE answer_canvas (
    id BIGSERIAL PRIMARY KEY,
    answer_id BIGINT REFERENCES answers(id) ON DELETE CASCADE,
    drawing_data BYTEA,
    mime_type VARCHAR(100),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

### autosave_answers
Draft answers for auto-save

```sql
CREATE TABLE autosave_answers (
    id BIGSERIAL PRIMARY KEY,
    exam_attempt_id BIGINT REFERENCES exam_attempts(id) ON DELETE CASCADE,
    question_id BIGINT REFERENCES questions(id),
    text_answer TEXT,
    selected_options TEXT,
    saved_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

## 6. Grading System (3 tables)

### grading_results
Final grading results

```sql
CREATE TABLE grading_results (
    id BIGSERIAL PRIMARY KEY,
    exam_attempt_id BIGINT REFERENCES exam_attempts(id),
    student_id BIGINT REFERENCES students(id),
    total_score DECIMAL(5,2),
    max_score DECIMAL(5,2),
    percentage_score DECIMAL(5,2),
    is_passed BOOLEAN,
    status VARCHAR(50) DEFAULT 'NOT_GRADED',
    graded_at TIMESTAMP,
    graded_by_teacher_id BIGINT REFERENCES teachers(id)
);
```

### grading_annotations
Comments and marks on answers

```sql
CREATE TABLE grading_annotations (
    id BIGSERIAL PRIMARY KEY,
    grading_result_id BIGINT REFERENCES grading_results(id) ON DELETE CASCADE,
    answer_id BIGINT REFERENCES answers(id),
    content TEXT,
    start_offset INT,
    end_offset INT,
    annotation_type VARCHAR(50),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

### grading_comments
Comments on grading

```sql
CREATE TABLE grading_comments (
    id BIGSERIAL PRIMARY KEY,
    grading_result_id BIGINT REFERENCES grading_results(id) ON DELETE CASCADE,
    created_by_teacher_id BIGINT REFERENCES teachers(id),
    content TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

## 7. Statistics & Storage (3 tables)

### exam_statistics
Statistics for exams

```sql
CREATE TABLE exam_statistics (
    id BIGSERIAL PRIMARY KEY,
    exam_id BIGINT REFERENCES exams(id),
    total_attempts INT,
    completed_attempts INT,
    average_score DECIMAL(5,2),
    highest_score DECIMAL(5,2),
    lowest_score DECIMAL(5,2),
    passed_count INT,
    failed_count INT,
    last_updated TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

### files
File storage records

```sql
CREATE TABLE files (
    id BIGSERIAL PRIMARY KEY,
    file_name VARCHAR(500),
    file_extension VARCHAR(20),
    mime_type VARCHAR(100),
    file_size BIGINT,
    storage_path TEXT,
    storage_type VARCHAR(50),
    file_hash VARCHAR(256),
    uploaded_by_user_id BIGINT REFERENCES users(id),
    file_category VARCHAR(50),
    uploaded_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    is_deleted BOOLEAN DEFAULT FALSE,
    deleted_at TIMESTAMP
);
```

### notifications
System notifications

```sql
CREATE TABLE notifications (
    id BIGSERIAL PRIMARY KEY,
    user_id BIGINT REFERENCES users(id),
    title VARCHAR(500),
    content TEXT,
    type VARCHAR(50),
    status VARCHAR(50) DEFAULT 'UNREAD',
    related_entity_type VARCHAR(100),
    related_entity_id BIGINT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    read_at TIMESTAMP
);
```

## Indexes

Important indexes for performance:

```sql
CREATE INDEX idx_users_username ON users(username);
CREATE INDEX idx_users_email ON users(email);
CREATE INDEX idx_teachers_teacher_code ON teachers(teacher_code);
CREATE INDEX idx_students_student_code ON students(student_code);
CREATE INDEX idx_exam_attempts_exam_id ON exam_attempts(exam_id);
CREATE INDEX idx_exam_attempts_student_id ON exam_attempts(student_id);
CREATE INDEX idx_answers_exam_attempt_id ON answers(exam_attempt_id);
CREATE INDEX idx_answers_question_id ON answers(question_id);
CREATE INDEX idx_questions_subject_id ON questions(subject_id);
CREATE INDEX idx_exams_status ON exams(status);
CREATE INDEX idx_user_login_logs_user_id ON user_login_logs(user_id);
CREATE INDEX idx_notifications_user_id ON notifications(user_id);
```

## Relationships

```
Entity Relationship Diagram:

users (1) ──┬─ (many) user_sessions
            ├─ (many) user_login_logs
            ├─ (many) user_roles
            ├─ (1)   teacher
            ├─ (1)   student
            └─ (many) files

teachers (1) ──┬─ (many) exams
               ├─ (many) grading_results
               ├─ (many) grading_comments
               └─ (many) questions

students (1) ──┬─ (many) exam_attempts
               └─ (many) answers

exams (1) ──┬─ (many) exam_classes
            ├─ (many) exam_attempts
            ├─ (many) exam_questions
            ├─ (many) exam_settings
            └─ (1) exam_statistics

questions (1) ──┬─ (many) question_options
                ├─ (many) question_tags
                └─ (many) exam_questions
```

## Cascade Delete Rules

- Delete school → Auto delete classes, teachers, students, subjects
- Delete exam → Auto delete exam_classes, exam_attempts, exam_questions, exam_settings
- Delete exam_attempt → Auto delete answers, autosave_answers
- Delete answer → Auto delete answer_options, answer_canvas
- Delete question → Auto delete question_options, question_tags, exam_questions
