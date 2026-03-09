1. Quy mô thực tế của hệ thống

Ví dụ trường THPT bình thường:

Thành phần	Số lượng
Giáo viên	60 – 120
Học sinh	800 – 2000
Lớp	25 – 50
Môn học	12 – 15

Số bài kiểm tra mỗi năm:

~ 300 – 1000 bài

Số lượt làm bài:

~ 10.000 – 30.000 attempts

👉 Quy mô này 1 server chạy dư sức.

2. Kiến trúc backend phù hợp nhất

Không cần microservice.

Dùng:

Modular Monolith

Kiến trúc:

Frontend (React / Next)
        │
        │
    ASP.NET API
        │
   PostgreSQL / SQL Server
        │
    File Storage

Đơn giản – dễ bảo trì – dễ code.

3. Kiến trúc Backend Modules

Chỉ cần khoảng 10 module chính.

Backend
│
├── Auth
├── Teacher
├── Student
├── Class
├── Subject
├── Exam
├── Question
├── Attempt
├── Grading
└── Report
4. Auth Module

Quản lý đăng nhập.

User chỉ có 3 loại:

Admin
Teacher
Student

Database:

Users
Roles
5. Teacher Module

Quản lý giáo viên.

Chức năng:

tạo giáo viên
sửa giáo viên
xóa giáo viên
xem lớp đang dạy
6. Student Module

Quản lý học sinh.

Chức năng:

thêm học sinh
import excel
xem danh sách lớp
7. Class Module

Quản lý lớp.

Ví dụ:

10A1
10A2
11A1

Quan hệ:

Class
  ├─ Students
  └─ Teachers
8. Subject Module

Danh sách môn học.

Toán
Văn
Anh
Lý
Hóa
Sinh
9. Exam Module

Đây là module quan trọng nhất.

Exam có thông tin:

Title
Subject
Teacher
Duration
StartTime
EndTime
Classes
10. Question Module

Quản lý câu hỏi.

Các loại câu:

MCQ
TrueFalse
ShortAnswer
Essay
Drawing
11. Attempt Module

Xử lý khi học sinh làm bài.

Flow:

Start exam
→ create attempt
→ answer question
→ submit exam
12. Grading Module

Chấm bài.

Có 2 kiểu:

Auto grading
MCQ
True/False
Short answer
Manual grading
Essay
Drawing
13. Report Module

Báo cáo.

Ví dụ:

điểm trung bình lớp
phổ điểm
ranking
14. Kiến trúc code trong backend

Dùng Clean Architecture.

Structure:

src
│
├── API
│   ├── Controllers
│
├── Application
│   ├── Services
│   ├── DTO
│
├── Domain
│   ├── Entities
│
├── Infrastructure
│   ├── EFCore
│   ├── Repositories
15. Database (đơn giản hơn)

Chỉ cần khoảng 20–25 bảng.

Ví dụ:

Users
Roles

Teachers
Students

Classes
ClassStudents

Subjects
TeachingAssignments

Exams
ExamClasses

Questions
QuestionOptions

ExamAttempts
AttemptAnswers

ManualGradings

Notifications
Files
16. File Storage

Cần lưu:

bài vẽ học sinh
ảnh đề thi
file import

Giải pháp:

MinIO

hoặc

Local storage
17. Chịu tải khi thi cùng lúc

Giả sử:

400 học sinh thi cùng lúc

Server:

4 CPU
8GB RAM

chạy:

ASP.NET + PostgreSQL

vẫn rất ổn.

18. Server triển khai

Chỉ cần:

1 VPS

Cấu hình:

4 CPU
8GB RAM
80GB SSD

Chi phí:

~300k / tháng
19. Docker Setup

Services:

api
postgres
redis
minio

Redis dùng để:

session
cache
20. Kiến trúc production cuối cùng
Internet
   │
Cloudflare
   │
Nginx
   │
ASP.NET API
   │
PostgreSQL
   │
MinIO
21. Điểm quan trọng nhất của hệ thống này

Phần khó nhất không phải backend.

Mà là:

Exam Player (UI làm bài)

Phải xử lý:

autosave
timer
canvas drawing
question navigation
anti cheat
22. Tổng độ lớn hệ thống

Nếu làm chuẩn:

Backend
~25 tables
~80 API endpoints

Frontend:

~30 pages
23. Lời khuyên thực tế

Với hệ thống này Cốt nên:

Modular Monolith
ASP.NET
PostgreSQL
React

đừng làm microservice.

mô tả tính năng:
1. Các vai trò trong hệ thống

Hệ thống chỉ cần 3 loại tài khoản:

1. Admin trường

Quản lý toàn bộ hệ thống.

2. Giáo viên

tạo bài kiểm tra

chấm bài

xem điểm học sinh

3. Học sinh

làm bài kiểm tra

xem điểm

xem lịch kiểm tra

2. Module quản lý tài khoản
2.1 Đăng nhập

Người dùng đăng nhập bằng:

username
password

Sau khi đăng nhập hệ thống sẽ chuyển tới dashboard tương ứng với vai trò.

2.2 Đổi mật khẩu

Người dùng có thể:

đổi mật khẩu
2.3 Reset mật khẩu

Admin có thể:

reset password cho giáo viên hoặc học sinh
3. Module quản lý giáo viên

Admin có thể:

Thêm giáo viên

Nhập thông tin:

Mã giáo viên
Họ tên
Bộ môn
Chức vụ
Số điện thoại
Email
Chỉnh sửa giáo viên

Admin có thể cập nhật:

bộ môn
chức vụ
thông tin liên hệ
Import danh sách giáo viên

Admin có thể:

import Excel danh sách giáo viên
4. Module quản lý học sinh

Admin có thể:

Thêm học sinh

Thông tin học sinh:

Mã học sinh
Họ tên
Lớp
Số điện thoại
Email
Import danh sách học sinh

Admin import bằng:

Excel

Ví dụ file:

MaHS | HoTen | Lop
5. Module quản lý lớp học

Admin có thể:

Tạo lớp

Ví dụ:

10A1
10A2
11A1

Thông tin lớp:

Tên lớp
Khối
Giáo viên chủ nhiệm
Gán học sinh vào lớp

Admin có thể:

thêm học sinh vào lớp
xóa học sinh khỏi lớp
6. Module phân công giáo viên bộ môn

Admin gán:

Giáo viên A → dạy Toán lớp 10A1
Giáo viên B → dạy Văn lớp 10A1

Chỉ giáo viên được phân công mới tạo bài kiểm tra cho lớp đó.

7. Module tạo bài kiểm tra

Giáo viên có thể tạo bài kiểm tra.

Thông tin bài kiểm tra:

Tên bài kiểm tra
Môn học
Thời gian làm bài
Ngày giờ bắt đầu
Ngày giờ kết thúc
Lớp tham gia

Ví dụ:

Kiểm tra 15 phút – Chương 1
Môn: Toán
Thời gian: 15 phút
Bắt đầu: 08:00
Lớp: 10A1
8. Module quản lý câu hỏi

Giáo viên có thể thêm câu hỏi vào bài kiểm tra.

Các loại câu hỏi
1. Trắc nghiệm

Ví dụ:

1. Thủ đô Việt Nam là?

A. Huế
B. Hà Nội
C. Đà Nẵng
D. Hải Phòng

Giáo viên chọn:

đáp án đúng
2. Đúng / Sai

Ví dụ:

Trái đất quay quanh mặt trời
True / False
3. Trả lời ngắn

Ví dụ:

2 + 2 = ?

Học sinh nhập:

4
4. Tự luận

Ví dụ:

Phân tích bài thơ Tây Tiến

Học sinh nhập:

text dài
5. Vẽ / giải toán

Học sinh có thể:

viết tay
vẽ
ghi công thức

Canvas có:

bút
tẩy
undo
đổi màu
9. Module import đề kiểm tra

Giáo viên có thể import đề từ:

Word
PDF
Excel

Hệ thống sẽ:

parse câu hỏi
tạo câu hỏi tự động
10. Module xem đề dạng A4

Giáo viên có thể xem:

đề thi dạng in

Giống đề giấy.

Có thể:

in PDF
11. Module làm bài kiểm tra

Học sinh vào hệ thống và thấy:

Danh sách bài kiểm tra

Ví dụ:

Kiểm tra 15 phút – Toán
Bắt đầu: 08:00
Bắt đầu làm bài

Học sinh bấm:

Start Exam

Hệ thống sẽ:

bắt đầu timer
12. Module giao diện làm bài

Trong khi làm bài:

Học sinh thấy:

Danh sách câu hỏi
Thời gian còn lại
Thanh điều hướng câu hỏi

Ví dụ:

Câu 1
Câu 2
Câu 3
13. Auto save bài làm

Hệ thống tự động:

lưu bài làm mỗi vài giây

để tránh mất dữ liệu.

14. Nộp bài

Học sinh có thể:

submit bài

Hoặc khi hết giờ:

auto submit
15. Chấm bài

Có 2 dạng chấm.

Chấm tự động

Áp dụng cho:

trắc nghiệm
đúng sai
trả lời ngắn

Hệ thống tự tính điểm.

Chấm tự luận

Giáo viên vào phần:

Chấm bài

Hệ thống hiển thị:

Câu hỏi
Bài làm học sinh

Giáo viên có thể:

vẽ lên bài
ghi nhận xét
nhập điểm
16. Xem kết quả

Sau khi chấm xong:

Học sinh có thể xem:

điểm
đáp án
nhận xét giáo viên
17. Thống kê điểm

Giáo viên có thể xem:

điểm trung bình
phổ điểm
bảng điểm lớp

Ví dụ:

Điểm TB: 7.2
Điểm cao nhất: 10
Điểm thấp nhất: 3
18. Xuất báo cáo

Có thể xuất:

Excel

Bao gồm:

danh sách học sinh
điểm
xếp hạng
19. Lịch kiểm tra

Học sinh có thể xem:

lịch kiểm tra

Ví dụ:

08:00 Toán
10:00 Anh
20. Thông báo

Hệ thống gửi thông báo:

Cho học sinh:

Sắp đến giờ kiểm tra

Cho giáo viên:

Có bài cần chấm
21. Nhật ký hoạt động

Admin có thể xem:

ai tạo bài kiểm tra
ai sửa bài kiểm tra
ai chấm bài
22. Tính năng chống gian lận (cơ bản)

Có thể phát hiện:

đổi tab
rời khỏi trang

Hệ thống ghi log.

23. Các tính năng nâng cao (tùy chọn)

Sau này có thể thêm:

Random đề
shuffle câu hỏi
shuffle đáp án
Ngân hàng câu hỏi

Giáo viên lưu câu hỏi để dùng lại.

AI chấm tự luận

AI gợi ý điểm cho giáo viên.

Kết luận

Hệ thống cho 1 trường duy nhất sẽ có khoảng:

10 module

và khoảng:

20 – 25 bảng database

Đây là quy mô rất hợp lý để 1 dev xây dựng trong 3–4 tháng.