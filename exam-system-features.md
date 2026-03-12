# Online Exam System – Feature Specification

Hệ thống Web App phục vụ **thi kiểm tra online cho 1 trường học**.

Các role trong hệ thống:

- System Admin
- School Admin
- Teacher
- Student

---

# 1. User Management

## 1.1 Authentication

Chức năng:

- Login bằng mã giáo viên
- Login bằng mã học sinh
- Login admin
- Logout
- Refresh token
- Change password
- Reset password

Security:

- JWT Authentication
- Role-based access control

---

## 1.2 Role System

Roles:

| Role | Description |
|-----|-------------|
System Admin | quản lý hệ thống |
School Admin | quản lý dữ liệu trường |
Teacher | tạo và chấm bài |
Student | làm bài thi |

---

## 1.3 Teacher Profile

Thông tin:

- teacher_id
- full_name
- subject
- position
- homeroom_class
- school

Chức năng:

- xem lớp đang dạy
- xem học sinh lớp
- xem điểm học sinh

---

## 1.4 Student Profile

Thông tin:

- student_id
- full_name
- class
- school
- homeroom_teacher

Chức năng:

- xem lịch thi
- xem điểm
- xem giáo viên bộ môn

---

# 2. School Structure Management

## 2.1 Class Management

Chức năng:

- tạo lớp
- sửa lớp
- xoá lớp
- gán giáo viên chủ nhiệm

Thông tin lớp:

- class_id
- class_name
- grade
- homeroom_teacher_id

---

## 2.2 Student Management

Chức năng:

- import học sinh từ Excel
- thêm học sinh
- sửa học sinh
- xoá học sinh
- chuyển lớp

---

## 2.3 Teacher Management

Chức năng:

- thêm giáo viên
- sửa giáo viên
- xoá giáo viên
- phân công giáo viên bộ môn

---

## 2.4 Subject Management

Ví dụ:

- Toán
- Văn
- Anh
- Lý
- Hoá
- Sinh
- Sử
- Địa
- Tin
- GDCD

Chức năng:

- thêm môn
- sửa môn
- xoá môn

---

## 2.5 Teacher-Class Assignment

Phân công giáo viên dạy lớp.

Ví dụ:

Class 9A

Toán -> Teacher A  
Văn -> Teacher B  
Anh -> Teacher C  

Chỉ giáo viên được phân công mới tạo bài kiểm tra cho lớp đó.

---

# 3. Question Bank

## 3.1 Question Types

Hệ thống hỗ trợ nhiều loại câu hỏi:

- Multiple Choice (A B C D)
- True / False
- Short Answer
- Essay
- Drawing Answer (Canvas)

---

## 3.2 Create Question

Thông tin câu hỏi:

- question_id
- subject
- difficulty
- content
- question_type

---

## 3.3 Answer Options

Đối với trắc nghiệm:

- option A
- option B
- option C
- option D

Correct answer:

- A / B / C / D

---

## 3.4 Question Tagging

Có thể gắn:

- subject
- difficulty
- tags

Ví dụ:

Toán  
Hình học  
Cấp độ khó 3

---

## 3.5 Import Questions

Hệ thống hỗ trợ import từ:

- PDF
- Word
- Latex
- Excel

Pipeline:
OCR → Parse → Create Question

---

## 3.6 Question Preview

Giáo viên có thể xem:

- format hiển thị
- đáp án
- preview đề

---

# 4. Exam Management

## 4.1 Create Exam

Form tạo bài kiểm tra:

- exam_name
- subject
- class
- duration
- start_time

---

## 4.2 Add Questions

Có thể:

- chọn từ question bank
- import đề
- nhập thủ công

---

## 4.3 Question Ordering

Chức năng:

- reorder câu hỏi
- shuffle câu
- shuffle đáp án

---

## 4.4 Exam Settings

Options:

- shuffle questions
- shuffle answers
- show result immediately
- allow review

---

## 4.5 Assign Exam to Classes

Ví dụ:

Exam: Toán 15 phút

Classes:

- 9A
- 9B

---

## 4.6 Exam Preview

Hiển thị:

- dạng web
- dạng in A4

---

# 5. Exam Player

Module quan trọng nhất của hệ thống.

---

## 5.1 Exam Interface

Bao gồm:

- câu hỏi
- navigation
- timer
- submit button

---

## 5.2 Question Navigation

Ví dụ:
1 2 3 4 5 6 7 8

Trạng thái:

- unanswered
- answered
- flagged

---

## 5.3 Timer

Hiển thị:

- thời gian còn lại

Hệ thống:

- cảnh báo 5 phút
- auto submit khi hết giờ

---

## 5.4 Autosave

Hệ thống tự lưu khi:

- chọn đáp án
- chuyển câu
- mỗi vài giây

Mục đích:

- tránh mất bài
- hỗ trợ mất mạng

---

## 5.5 Multiple Choice Answer

Học sinh chọn:

- A
- B
- C
- D

---

## 5.6 Short Answer

Học sinh nhập:
text input

---

## 5.7 Essay Answer

Học sinh nhập:
rich text editor

---

## 5.8 Drawing Answer

Canvas support:

Tools:

- pen
- color
- eraser
- undo

Ứng dụng:

- Toán
- Hình học
- Vật lý

---

## 5.9 Resume Exam

Nếu mất mạng:

- resume attempt
- tiếp tục làm bài

---

# 6. Grading System

## 6.1 Auto Grading

Đối với trắc nghiệm:
system auto grade

---

## 6.2 Essay Grading

Giáo viên:

- xem bài học sinh
- nhập điểm

---

## 6.3 Annotation

Giáo viên có thể:

- vẽ lên bài
- highlight lỗi
- ghi chú

---

## 6.4 Final Score

Sau khi chấm:
publish score

---

# 7. Score System

## 7.1 Student Score

Học sinh xem:

- điểm
- bài làm
- đáp án đúng

---

## 7.2 Class Score

Giáo viên xem:

- danh sách học sinh
- điểm từng bài

---

## 7.3 Ranking

Có thể hiển thị:

- top học sinh
- bảng xếp hạng

---

## 7.4 Statistics

Thống kê:

- điểm trung bình
- điểm cao nhất
- điểm thấp nhất

---

# 8. Dashboard

## 8.1 Teacher Dashboard

Hiển thị:

- bài kiểm tra đã tạo
- bài cần chấm
- lớp đang dạy

---

## 8.2 Student Dashboard

Hiển thị:

- bài kiểm tra sắp tới
- điểm gần đây
- thông báo

---

# 9. Notification System

Hệ thống gửi thông báo:

- thông báo bài kiểm tra
- thông báo điểm
- thông báo hệ thống

Có thể gửi cho:

- lớp
- giáo viên
- học sinh

---

# 10. File Storage

Lưu trữ:

- đề thi
- file import
- canvas drawing
- image

Storage:

- S3
- MinIO

---

# 11. Anti-cheat

Hệ thống phát hiện:

- chuyển tab
- thoát fullscreen
- mất kết nối

Log lại toàn bộ sự kiện.

---

# 12. System Admin

Admin có thể:

- xem log
- backup database
- restore database
- monitor system health

---

# Summary

Modules:

1. User Management
2. School Management
3. Question Bank
4. Exam Management
5. Exam Player
6. Grading System
7. Score System
8. Dashboard
9. Notification System
10. File Storage
11. Anti-cheat
12. System Admin

Tổng khoảng **80+ features**.