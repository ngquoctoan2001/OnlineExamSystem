# School Exam System API Specification

Version: 1.0  
Base URL:

/api/v1

Authentication: JWT Bearer Token

Roles:
- SUPER_ADMIN
- SCHOOL_ADMIN
- TEACHER
- STUDENT

---

# 1. Authentication APIs

## Login

POST /auth/login

Body

{
  "username": "string",
  "password": "string"
}

Response

{
  "accessToken": "jwt",
  "refreshToken": "jwt"
}

---

## Logout

POST /auth/logout

---

## Refresh Token

POST /auth/refresh-token

---

## Get Current User

GET /auth/me

---

## Change Password

POST /auth/change-password

---

## Forgot Password

POST /auth/forgot-password

---

## Reset Password

POST /auth/reset-password

---

## Verify Session

POST /auth/verify-session

---

# 2. Teacher APIs

## Get Teachers

GET /teachers

Query

?page=1
&limit=20

---

## Get Teacher Detail

GET /teachers/{teacherId}

---

## Create Teacher

POST /teachers

Body

{
  "teacherCode": "GV001",
  "fullName": "Nguyen Van A",
  "subjectId": 1
}

---

## Update Teacher

PUT /teachers/{teacherId}

---

## Delete Teacher

DELETE /teachers/{teacherId}

---

## Teacher Classes

GET /teachers/{teacherId}/classes

---

## Teacher Subjects

GET /teachers/{teacherId}/subjects

---

# 3. Student APIs

## Get Students

GET /students

---

## Get Student Detail

GET /students/{studentId}

---

## Create Student

POST /students

---

## Update Student

PUT /students/{studentId}

---

## Delete Student

DELETE /students/{studentId}

---

## Student Scores

GET /students/{studentId}/scores

---

## Student Exams

GET /students/{studentId}/exams

---

# 4. Class APIs

## Get Classes

GET /classes

---

## Get Class Detail

GET /classes/{classId}

---

## Create Class

POST /classes

---

## Update Class

PUT /classes/{classId}

---

## Delete Class

DELETE /classes/{classId}

---

## Class Students

GET /classes/{classId}/students

---

## Class Teachers

GET /classes/{classId}/teachers

---

## Assign Teacher to Class

POST /classes/{classId}/assign-teacher

---

## Assign Students

POST /classes/{classId}/assign-students

---

# 5. Subject APIs

## Get Subjects

GET /subjects

---

## Create Subject

POST /subjects

---

## Update Subject

PUT /subjects/{subjectId}

---

## Delete Subject

DELETE /subjects/{subjectId}

---

# 6. Exam APIs

## Get Exams

GET /exams

---

## Get Exam Detail

GET /exams/{examId}

---

## Create Exam

POST /exams

Body

{
  "title": "Kiem tra 15 phut",
  "subjectId": 1,
  "durationMinutes": 15,
  "startTime": "2026-03-20T08:00:00"
}

---

## Update Exam

PUT /exams/{examId}

---

## Delete Exam

DELETE /exams/{examId}

---

## Publish Exam

POST /exams/{examId}/publish

---

## Unpublish Exam

POST /exams/{examId}/unpublish

---

## Start Exam

POST /exams/{examId}/start

---

## Stop Exam

POST /exams/{examId}/stop

---

## Duplicate Exam

POST /exams/{examId}/duplicate

---

## Exam Preview

GET /exams/{examId}/preview

---

## Assign Class to Exam

POST /exams/{examId}/assign-class

---

# 7. Question APIs

## Get Questions

GET /questions

---

## Get Question Detail

GET /questions/{questionId}

---

## Create Question

POST /questions

---

## Update Question

PUT /questions/{questionId}

---

## Delete Question

DELETE /questions/{questionId}

---

## Add Question Options

POST /questions/{questionId}/options

---

## Update Question Option

PUT /questions/options/{optionId}

---

## Delete Question Option

DELETE /questions/options/{optionId}

---

## Import Questions from PDF

POST /questions/import/pdf

---

## Import Questions from Word

POST /questions/import/docx

---

## Import Questions from Latex

POST /questions/import/latex

---

## Import Questions from Excel

POST /questions/import/excel

---

# 8. Exam Question APIs

## Get Exam Questions

GET /exams/{examId}/questions

---

## Add Question to Exam

POST /exams/{examId}/questions

---

## Update Exam Question

PUT /exams/{examId}/questions/{questionId}

---

## Remove Question from Exam

DELETE /exams/{examId}/questions/{questionId}

---

## Reorder Questions

POST /exams/{examId}/reorder-questions

---

# 9. Exam Player APIs

## Start Exam Attempt

POST /exam-attempts/start

Body

{
  "examId": 1
}

Response

{
  "attemptId": 1001
}

---

## Get Exam Questions for Student

GET /exam-attempts/{attemptId}/questions

---

## Save Answer

POST /exam-attempts/{attemptId}/answers

Body

{
  "questionId": 10,
  "answer": "B"
}

---

## Save Canvas Answer

POST /exam-attempts/{attemptId}/canvas

---

## Flag Question

POST /exam-attempts/{attemptId}/flag-question

---

## Unflag Question

POST /exam-attempts/{attemptId}/unflag-question

---

## Resume Exam

GET /exam-attempts/{attemptId}/resume

---

## Submit Exam

POST /exam-attempts/{attemptId}/submit

---

# 10. Autosave APIs

## Save Autosave

POST /autosave

---

## Get Autosave

GET /autosave/{attemptId}

---

## Delete Autosave

DELETE /autosave/{attemptId}

---

# 11. Grading APIs

## Get Attempts

GET /grading/exams/{examId}/attempts

---

## Get Attempt Detail

GET /grading/attempts/{attemptId}

---

## Grade Question

POST /grading/attempts/{attemptId}/score

Body

{
  "questionId": 10,
  "score": 2
}

---

## Add Annotation

POST /grading/attempts/{attemptId}/annotation

---

## Finalize Grading

POST /grading/attempts/{attemptId}/finalize

---

# 12. Score APIs

## Student Scores

GET /scores/student/{studentId}

---

## Class Scores

GET /scores/class/{classId}

---

## Exam Scores

GET /scores/exam/{examId}

---

## Exam Ranking

GET /scores/exam/{examId}/ranking

---

## Exam Statistics

GET /scores/exam/{examId}/statistics

---

# 13. Notification APIs

## Get Notifications

GET /notifications

---

## Create Notification

POST /notifications

---

## Mark As Read

PUT /notifications/{notificationId}/read

---

## Delete Notification

DELETE /notifications/{notificationId}

---

## Send To Class

POST /notifications/send-class

---

# 14. File Upload APIs

## Upload Image

POST /upload/image

---

## Upload PDF

POST /upload/pdf

---

## Upload Exam Document

POST /upload/exam-doc

---

## Upload Canvas Drawing

POST /upload/canvas

---

## Get File

GET /files/{fileId}

---

# 15. Admin APIs

## System Stats

GET /admin/system-stats

---

## System Logs

GET /admin/logs

---

## Backup Database

POST /admin/backup

---

## Restore Database

POST /admin/restore

---

## Health Check

GET /admin/health
