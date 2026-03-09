Thiết kế này theo chuẩn production-grade, đảm bảo:

chuẩn hóa dữ liệu

dễ mở rộng

hỗ trợ nhiều loại câu hỏi

autosave

chấm tự luận

canvas drawing

thống kê điểm

log hệ thống

Database đề xuất: PostgreSQL

1️⃣ USERS & AUTH (7 bảng)
users
CREATE TABLE users (
    id BIGSERIAL PRIMARY KEY,
    username VARCHAR(50) UNIQUE,
    password_hash TEXT NOT NULL,
    role VARCHAR(20) NOT NULL, -- ADMIN, TEACHER, STUDENT
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT NOW()
);
user_sessions
CREATE TABLE user_sessions (
    id BIGSERIAL PRIMARY KEY,
    user_id BIGINT REFERENCES users(id),
    refresh_token TEXT,
    expires_at TIMESTAMP,
    created_at TIMESTAMP DEFAULT NOW()
);
user_login_logs
CREATE TABLE user_login_logs (
    id BIGSERIAL PRIMARY KEY,
    user_id BIGINT REFERENCES users(id),
    ip_address VARCHAR(50),
    device_info TEXT,
    login_time TIMESTAMP DEFAULT NOW()
);
permissions
CREATE TABLE permissions (
    id BIGSERIAL PRIMARY KEY,
    name VARCHAR(100)
);
roles
CREATE TABLE roles (
    id BIGSERIAL PRIMARY KEY,
    name VARCHAR(50)
);
role_permissions
CREATE TABLE role_permissions (
    role_id BIGINT REFERENCES roles(id),
    permission_id BIGINT REFERENCES permissions(id),
    PRIMARY KEY(role_id, permission_id)
);
user_roles
CREATE TABLE user_roles (
    user_id BIGINT REFERENCES users(id),
    role_id BIGINT REFERENCES roles(id),
    PRIMARY KEY(user_id, role_id)
);
2️⃣ SCHOOL STRUCTURE (7 bảng)
schools
CREATE TABLE schools (
    id BIGSERIAL PRIMARY KEY,
    name VARCHAR(255),
    address TEXT
);
subjects
CREATE TABLE subjects (
    id BIGSERIAL PRIMARY KEY,
    name VARCHAR(100)
);
classes
CREATE TABLE classes (
    id BIGSERIAL PRIMARY KEY,
    name VARCHAR(50),
    grade INT,
    homeroom_teacher_id BIGINT
);
teachers
CREATE TABLE teachers (
    id BIGSERIAL PRIMARY KEY,
    user_id BIGINT REFERENCES users(id),
    teacher_code VARCHAR(50) UNIQUE,
    full_name VARCHAR(255),
    subject_id BIGINT REFERENCES subjects(id),
    position VARCHAR(100)
);
students
CREATE TABLE students (
    id BIGSERIAL PRIMARY KEY,
    user_id BIGINT REFERENCES users(id),
    student_code VARCHAR(50) UNIQUE,
    full_name VARCHAR(255),
    class_id BIGINT REFERENCES classes(id)
);
class_students
CREATE TABLE class_students (
    class_id BIGINT REFERENCES classes(id),
    student_id BIGINT REFERENCES students(id),
    PRIMARY KEY(class_id, student_id)
);
class_subject_teachers
CREATE TABLE class_subject_teachers (
    id BIGSERIAL PRIMARY KEY,
    class_id BIGINT REFERENCES classes(id),
    subject_id BIGINT REFERENCES subjects(id),
    teacher_id BIGINT REFERENCES teachers(id)
);
3️⃣ EXAM MANAGEMENT (8 bảng)
exams
CREATE TABLE exams (
    id BIGSERIAL PRIMARY KEY,
    title VARCHAR(255),
    subject_id BIGINT REFERENCES subjects(id),
    teacher_id BIGINT REFERENCES teachers(id),
    duration_minutes INT,
    start_time TIMESTAMP,
    end_time TIMESTAMP,
    status VARCHAR(20),
    created_at TIMESTAMP DEFAULT NOW()
);
exam_classes
CREATE TABLE exam_classes (
    exam_id BIGINT REFERENCES exams(id),
    class_id BIGINT REFERENCES classes(id),
    PRIMARY KEY(exam_id, class_id)
);
exam_settings
CREATE TABLE exam_settings (
    exam_id BIGINT PRIMARY KEY REFERENCES exams(id),
    shuffle_questions BOOLEAN,
    shuffle_answers BOOLEAN,
    show_result_immediately BOOLEAN,
    allow_review BOOLEAN
);
exam_attempts
CREATE TABLE exam_attempts (
    id BIGSERIAL PRIMARY KEY,
    exam_id BIGINT REFERENCES exams(id),
    student_id BIGINT REFERENCES students(id),
    start_time TIMESTAMP,
    end_time TIMESTAMP,
    status VARCHAR(20),
    total_score NUMERIC(5,2)
);
4️⃣ QUESTION BANK (10 bảng)
question_types
CREATE TABLE question_types (
    id BIGSERIAL PRIMARY KEY,
    name VARCHAR(50)
);
questions
CREATE TABLE questions (
    id BIGSERIAL PRIMARY KEY,
    question_type_id BIGINT REFERENCES question_types(id),
    subject_id BIGINT REFERENCES subjects(id),
    content TEXT,
    difficulty_level INT,
    created_by BIGINT REFERENCES teachers(id)
);
question_options
CREATE TABLE question_options (
    id BIGSERIAL PRIMARY KEY,
    question_id BIGINT REFERENCES questions(id),
    option_label VARCHAR(10),
    content TEXT,
    is_correct BOOLEAN
);
question_tags
CREATE TABLE question_tags (
    id BIGSERIAL PRIMARY KEY,
    name VARCHAR(100)
);
question_tag_map
CREATE TABLE question_tag_map (
    question_id BIGINT REFERENCES questions(id),
    tag_id BIGINT REFERENCES question_tags(id),
    PRIMARY KEY(question_id, tag_id)
);
exam_questions
CREATE TABLE exam_questions (
    id BIGSERIAL PRIMARY KEY,
    exam_id BIGINT REFERENCES exams(id),
    question_id BIGINT REFERENCES questions(id),
    order_index INT,
    max_score NUMERIC(5,2)
);
5️⃣ STUDENT ANSWERS (8 bảng)
answers
CREATE TABLE answers (
    id BIGSERIAL PRIMARY KEY,
    attempt_id BIGINT REFERENCES exam_attempts(id),
    question_id BIGINT REFERENCES questions(id),
    answer_text TEXT,
    created_at TIMESTAMP DEFAULT NOW()
);
answer_options
CREATE TABLE answer_options (
    answer_id BIGINT REFERENCES answers(id),
    option_id BIGINT REFERENCES question_options(id),
    PRIMARY KEY(answer_id, option_id)
);
answer_canvas
CREATE TABLE answer_canvas (
    id BIGSERIAL PRIMARY KEY,
    answer_id BIGINT REFERENCES answers(id),
    image_url TEXT,
    json_data JSONB
);
autosave_answers
CREATE TABLE autosave_answers (
    id BIGSERIAL PRIMARY KEY,
    attempt_id BIGINT REFERENCES exam_attempts(id),
    question_id BIGINT,
    data JSONB,
    saved_at TIMESTAMP
);
6️⃣ GRADING SYSTEM (5 bảng)
grading_results
CREATE TABLE grading_results (
    id BIGSERIAL PRIMARY KEY,
    attempt_id BIGINT REFERENCES exam_attempts(id),
    question_id BIGINT REFERENCES questions(id),
    score NUMERIC(5,2),
    graded_by BIGINT REFERENCES teachers(id),
    graded_at TIMESTAMP
);
grading_annotations
CREATE TABLE grading_annotations (
    id BIGSERIAL PRIMARY KEY,
    grading_result_id BIGINT REFERENCES grading_results(id),
    annotation_data JSONB
);
grading_comments
CREATE TABLE grading_comments (
    id BIGSERIAL PRIMARY KEY,
    grading_result_id BIGINT REFERENCES grading_results(id),
    comment TEXT
);
7️⃣ RESULTS & STATISTICS (5 bảng)
exam_results
CREATE TABLE exam_results (
    id BIGSERIAL PRIMARY KEY,
    attempt_id BIGINT REFERENCES exam_attempts(id),
    total_score NUMERIC(5,2),
    graded_at TIMESTAMP
);
class_exam_results
CREATE TABLE class_exam_results (
    id BIGSERIAL PRIMARY KEY,
    exam_id BIGINT REFERENCES exams(id),
    class_id BIGINT REFERENCES classes(id),
    average_score NUMERIC(5,2)
);
exam_statistics
CREATE TABLE exam_statistics (
    exam_id BIGINT PRIMARY KEY REFERENCES exams(id),
    avg_score NUMERIC(5,2),
    max_score NUMERIC(5,2),
    min_score NUMERIC(5,2)
);
8️⃣ FILE STORAGE (3 bảng)
files
CREATE TABLE files (
    id BIGSERIAL PRIMARY KEY,
    file_url TEXT,
    file_type VARCHAR(50),
    uploaded_by BIGINT REFERENCES users(id),
    created_at TIMESTAMP DEFAULT NOW()
);
exam_files
CREATE TABLE exam_files (
    exam_id BIGINT REFERENCES exams(id),
    file_id BIGINT REFERENCES files(id),
    PRIMARY KEY(exam_id, file_id)
);
9️⃣ NOTIFICATION SYSTEM (3 bảng)
notifications
CREATE TABLE notifications (
    id BIGSERIAL PRIMARY KEY,
    title VARCHAR(255),
    message TEXT,
    created_at TIMESTAMP
);
notification_users
CREATE TABLE notification_users (
    notification_id BIGINT REFERENCES notifications(id),
    user_id BIGINT REFERENCES users(id),
    is_read BOOLEAN DEFAULT FALSE,
    PRIMARY KEY(notification_id, user_id)
);
🔟 SYSTEM LOGS (2 bảng)
activity_logs
CREATE TABLE activity_logs (
    id BIGSERIAL PRIMARY KEY,
    user_id BIGINT REFERENCES users(id),
    action VARCHAR(255),
    metadata JSONB,
    created_at TIMESTAMP
);
system_settings
CREATE TABLE system_settings (
    id BIGSERIAL PRIMARY KEY,
    key VARCHAR(100),
    value TEXT
);
📊 Tổng số bảng
Module	Tables
Auth	7
School	7
Exam	8
Question	6
Answers	4
Grading	3
Results	3
Files	2
Notification	2
Logs	2

✅ Tổng ~44–50 bảng

🔥 Kiến trúc dữ liệu này hỗ trợ:

✔ thi trắc nghiệm
✔ thi tự luận
✔ thi vẽ canvas
✔ autosave
✔ OCR import đề
✔ chấm bài giáo viên
✔ thống kê điểm
✔ ranking lớp