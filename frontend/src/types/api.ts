// Shared types for API responses

export interface ApiResponse<T> {
  success: boolean
  message: string
  data?: T
}

export interface PagedResponse<T> {
  items: T[]
  totalCount: number
  page: number
  pageSize: number
}

// Auth
export interface LoginRequest {
  username: string
  password: string
}

export interface LoginResponse {
  accessToken: string
  refreshToken: string
}

export interface RegisterRequest {
  username: string
  email: string
  password: string
  fullName: string
  role: string
}

// User
export interface UserResponse {
  id: number
  username: string
  email: string
  fullName: string
  role: string
  isActive: boolean
  createdAt: string
}

// Student
export interface StudentResponse {
  id: number
  userId: number
  username: string
  email: string
  fullName: string
  studentCode: string
  rollNumber: string
  isActive: boolean
  createdAt: string
}

export interface CreateStudentRequest {
  username: string
  email: string
  password: string
  fullName: string
  studentCode: string
  rollNumber?: string
}

// Teacher
export interface TeacherResponse {
  id: number
  userId: number
  username: string
  email: string
  fullName: string
  employeeCode: string
  department: string
  isActive: boolean
  createdAt: string
}

export interface CreateTeacherRequest {
  username: string
  email: string
  password: string
  fullName: string
  employeeCode: string
  department?: string
}

// Class
export interface ClassResponse {
  id: number
  name: string
  code: string
  grade: number
  homeroomTeacherId?: number
  homeroomTeacherName?: string
  studentCount: number
  teacherCount: number
}

export interface CreateClassRequest {
  name: string
  code: string
  grade: number
  homeroomTeacherId?: number
}

// Subject
export interface SubjectResponse {
  id: number
  name: string
  code: string
  description?: string
  credits: number
  isActive: boolean
  createdAt: string
}

export interface CreateSubjectRequest {
  name: string
  code: string
  description?: string
  credits: number
}

// Question
export interface QuestionResponse {
  id: number
  subjectId: number
  subjectName?: string
  questionTypeId: number
  questionTypeName?: string
  content: string
  difficulty: 'EASY' | 'MEDIUM' | 'HARD'
  isPublished: boolean
  createdAt: string
  optionCount: number
}

export interface QuestionDetailResponse extends QuestionResponse {
  options: QuestionOptionResponse[]
}

export interface QuestionOptionResponse {
  id: number
  questionId: number
  label: string
  content: string
  isCorrect: boolean
  orderIndex: number
}

export interface CreateQuestionRequest {
  subjectId: number
  questionTypeId: number
  content: string
  difficulty: string
  options: CreateQuestionOptionRequest[]
}

export interface CreateQuestionOptionRequest {
  label: string
  content: string
  isCorrect: boolean
  orderIndex: number
}

export interface QuestionListResponse {
  items: QuestionResponse[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

// Exam
export interface ExamResponse {
  id: number
  title: string
  subjectId: number
  subjectName: string
  createdBy: number
  durationMinutes: number
  startTime: string
  endTime: string
  description: string
  status: 'DRAFT' | 'ACTIVE' | 'CLOSED'
  createdAt: string
}

export interface ExamListResponse {
  items: ExamResponse[]
  totalCount: number
  page: number
  pageSize: number
}

export interface CreateExamRequest {
  title: string
  subjectId: number
  createdBy: number
  durationMinutes: number
  startTime: string
  endTime: string
  description?: string
}

// Exam Attempt
export interface ExamAttemptResponse {
  id: number
  examId: number
  examTitle: string
  studentId: number
  studentName: string
  startedAt: string
  finishedAt?: string
  score?: number
  isPassed?: boolean
  status: string
}

// Statistics
export interface DashboardStats {
  totalTeachers: number
  totalStudents: number
  totalExams: number
  activeExams: number
  totalQuestions: number
  totalClasses: number
  completionRate: number
  recentActivity: ActivityItem[]
  examTrend: TrendPoint[]
}

export interface ActivityItem {
  type: string
  description: string
  time: string
}

export interface TrendPoint {
  day: string
  count: number
}

// QuestionType
export interface QuestionTypeResponse {
  id: number
  name: string
  code: string
  description?: string
}

// Notification
export interface NotificationResponse {
  id: number
  userId: number
  title: string
  content: string
  isRead: boolean
  createdAt: string
}
