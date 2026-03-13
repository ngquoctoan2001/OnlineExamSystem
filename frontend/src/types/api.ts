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
  employeeId: string
  department: string
  isActive: boolean
  createdAt: string
}

export interface CreateTeacherRequest {
  username: string
  email: string
  password: string
  fullName: string
  employeeId: string
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
  questionCount: number
  examCount: number
}

export interface CreateSubjectRequest {
  name: string
  code: string
  description?: string
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
  tags: TagResponse[]
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
  tagIds?: number[]
}

export interface CreateQuestionOptionRequest {
  label: string
  content: string
  isCorrect: boolean
  orderIndex: number
}

export interface UpdateQuestionRequest {
  content: string
  difficulty: string
  isPublished: boolean
  options: CreateQuestionOptionRequest[]
  tagIds?: number[]
}

export interface QuestionListResponse {
  items: QuestionResponse[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

// Subject Exam Type
export interface SubjectExamTypeResponse {
  id: number
  subjectId: number
  subjectName: string
  name: string
  coefficient: number
  requiredCount: number
  sortOrder: number
}

export interface CreateSubjectExamTypeRequest {
  subjectId: number
  name: string
  coefficient: number
  requiredCount: number
  sortOrder: number
}

export interface UpdateSubjectExamTypeRequest {
  name?: string
  coefficient?: number
  requiredCount?: number
  sortOrder?: number
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
  subjectExamTypeId?: number
  subjectExamTypeName?: string
  subjectExamTypeCoefficient?: number
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
  subjectExamTypeId?: number | null
}

// Exam Settings
export interface ConfigureExamSettingsRequest {
  shuffleQuestions: boolean
  shuffleAnswers: boolean
  showResultImmediately: boolean
  allowReview: boolean
}

export interface ExamSettingsResponse {
  id: number
  examId: number
  shuffleQuestions: boolean
  shuffleAnswers: boolean
  showResultImmediately: boolean
  allowReview: boolean
  createdAt: string
  updatedAt: string
}

// Exam Questions
export interface ExamQuestionResponse {
  id: number
  examId: number
  questionId: number
  questionContent: string
  questionDifficulty: string
  questionOrder: number
  maxScore: number
  optionCount: number
  addedAt: string
}

export interface ExamQuestionsListResponse {
  examId: number
  examTitle: string
  questions: ExamQuestionResponse[]
  totalQuestions: number
  totalScore: number
}

export interface ReorderExamQuestionsRequest {
  questions: { examQuestionId: number; newOrder: number }[]
}

// Exam Attempt
export interface ExamAttemptResponse {
  id: number
  examId: number
  examTitle: string
  studentId: number
  studentName: string
  studentCode: string
  startTime: string
  endTime?: string
  score?: number
  totalPoints?: number
  isPassed?: boolean
  status: string
  totalQuestions: number
  answeredQuestions: number
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

// Exam Statistics
export interface ExamStatisticResponse {
  examId: number
  examTitle: string
  totalAttempts: number
  passCount: number
  failCount: number
  passRate: number
  averageScore: number
  maxScore: number
  minScore: number
  calculatedAt: string
}

export interface ScoreDistributionResponse {
  examId: number
  buckets: ScoreBucket[]
}

export interface ScoreBucket {
  label: string
  min: number
  max: number
  count: number
}

export interface StudentPerformanceResponse {
  studentId: number
  studentName: string
  totalAttempts: number
  averageScore: number
  attempts: StudentAttemptSummary[]
}

export interface StudentAttemptSummary {
  attemptId: number
  studentName: string
  examId: number
  examTitle: string
  subjectName: string
  score: number | null
  status: string
  startTime: string
  endTime: string | null
}

export interface ClassResultsResponse {
  classId: number
  className: string
  examId: number
  examTitle: string
  totalStudents: number
  attemptedCount: number
  averageScore: number
  studentResults: StudentAttemptSummary[]
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
  type: string
  title: string
  message: string
  isRead: boolean
  relatedEntityId?: number
  relatedEntityType?: string
  createdAt: string
  readAt?: string
}

export interface CreateNotificationRequest {
  userId: number
  type: string
  title: string
  message: string
  relatedEntityId?: number
  relatedEntityType?: string
}

export interface SendNotificationToClassRequest {
  classId: number
  type: string
  title: string
  message: string
  relatedEntityId?: number
  relatedEntityType?: string
}

// Tag
export interface TagResponse {
  id: number
  name: string
  description?: string
  createdAt: string
}

export interface CreateTagRequest {
  name: string
  description?: string
}

// User Management
export interface UserDto {
  id: number
  username: string
  email: string
  fullName: string
  isActive: boolean
  roles: string[]
  createdAt: string
  updatedAt: string
}

export interface CreateUserRequest {
  username: string
  email: string
  password: string
  fullName: string
  roles: string[]
}

export interface UpdateUserRequest {
  email: string
  fullName: string
  isActive: boolean
  roles: string[]
}

export interface RoleDto {
  id: number
  name: string
  description: string
}

// Gradebook
export interface GradebookEntryResponse {
  examId: number
  examTitle: string
  subjectExamTypeId?: number
  subjectExamTypeName?: string
  coefficient: number
  score?: number
  totalPoints?: number
  scoreOn10?: number
  status: string
  completedAt?: string
}

export interface StudentSubjectGradebookResponse {
  studentId: number
  studentName: string
  studentCode: string
  subjectId: number
  subjectName: string
  entries: GradebookEntryResponse[]
  weightedAverage?: number
}

export interface StudentFullGradebookResponse {
  studentId: number
  studentName: string
  studentCode: string
  subjects: SubjectGradeSummary[]
  overallAverage?: number
}

export interface SubjectGradeSummary {
  subjectId: number
  subjectName: string
  entries: GradebookEntryResponse[]
  weightedAverage?: number
}

export interface ClassSubjectGradebookResponse {
  classId: number
  className: string
  subjectId: number
  subjectName: string
  students: StudentSubjectGradebookResponse[]
  examTypes: SubjectExamTypeResponse[]
}

// ──────────────────────────────────────
// Exam Attempt
// ──────────────────────────────────────
export interface StartExamAttemptRequest {
  examId: number
  studentId: number
}

export interface ExamAttemptDetailResponse {
  id: number
  examId: number
  examTitle?: string
  studentId: number
  studentName?: string
  status: string
  startTime: string
  endTime?: string
  score?: number
  answers: AttemptAnswerResponse[]
}

export interface ExamAttemptListResponse {
  items: ExamAttemptResponse[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export interface SubmitExamAttemptResponse {
  attemptId: number
  status: string
  submittedAt: string
  message: string
}

export interface AttemptAnswerResponse {
  answerId: number
  questionId: number
  textContent?: string
  essayContent?: string
  canvasImage?: string
}

export interface AttemptQuestionResponse {
  questionId: number
  content: string
  questionType: string
  orderIndex: number
  points: number
  options: AttemptQuestionOptionResponse[]
  isAnswered: boolean
  currentAnswer?: AnswerResponse
}

export interface AttemptQuestionOptionResponse {
  id: number
  content: string
  orderIndex: number
}

// ──────────────────────────────────────
// Violation
// ──────────────────────────────────────
export interface LogViolationRequest {
  violationType: string
  description?: string
}

export interface ViolationResponse {
  id: number
  examAttemptId: number
  violationType: string
  description?: string
  occurredAt: string
}

// ──────────────────────────────────────
// Answers
// ──────────────────────────────────────
export interface SubmitAnswerRequest {
  questionId: number
  selectedOptionIds?: number[]
  textContent?: string
  essayContent?: string
  canvasImage?: string
}

export interface AnswerResponse {
  id: number
  examAttemptId: number
  questionId: number
  selectedOptionIds: number[]
  textContent?: string
  essayContent?: string
  canvasImage?: string
  answeredAt?: string
}

export interface SaveCanvasRequest {
  questionId: number
  canvasData: string
}

export interface FlagQuestionRequest {
  questionId: number
}

// ──────────────────────────────────────
// Autosave
// ──────────────────────────────────────
export interface AutosaveAnswerItem {
  questionId: number
  selectedOptionIds?: number[]
  textContent?: string
  essayContent?: string
}

export interface AutosaveRequest {
  attemptId: number
  answers: AutosaveAnswerItem[]
}

export interface AutosaveResponse {
  attemptId: number
  savedCount: number
  savedAt: string
}

// ──────────────────────────────────────
// Grading
// ──────────────────────────────────────
export interface GradingResultResponse {
  id: number
  examAttemptId: number
  questionId: number
  questionContent: string
  questionType: string
  points: number
  score: number
  comment?: string
  annotations?: string
  gradedBy?: number
  gradedAt?: string
  isAutoGraded: boolean
}

export interface ManualGradeRequest {
  score: number
  comment?: string
  annotations?: string
}

export interface BatchGradeItem {
  questionId: number
  score: number
  comment?: string
  annotations?: string
}

export interface BatchGradeRequest {
  grades: BatchGradeItem[]
}

export interface QuestionOptionGradeInfo {
  id: number
  content: string
  isCorrect: boolean
  wasSelected: boolean
}

export interface QuestionGradingItem {
  questionId: number
  content: string
  questionType: string
  points: number
  selectedOptionIds: number[]
  textContent?: string
  essayContent?: string
  canvasImage?: string
  options: QuestionOptionGradeInfo[]
  gradingResult?: GradingResultResponse
}

export interface AttemptGradingViewResponse {
  attemptId: number
  studentId: number
  studentName: string
  examTitle: string
  status: string
  totalScore?: number
  questions: QuestionGradingItem[]
}

export interface PendingGradingAttemptResponse {
  attemptId: number
  studentId: number
  studentName: string
  submittedAt: string
  hasUngraded: boolean
}

export interface PublishResultResponse {
  attemptId: number
  totalScore?: number
  status: string
  published: boolean
}

export interface GradeQuestionRequest {
  questionId: number
  score: number
  comment?: string
}

export interface AddAnnotationRequest {
  questionId: number
  content: string
  type?: string
}

// ──────────────────────────────────────
// Scores
// ──────────────────────────────────────
export interface ScoreResponse {
  attemptId: number
  examId: number
  examTitle: string
  studentId: number
  studentName: string
  score?: number
  status: string
  startTime: string
  endTime?: string
}

export interface RankingEntry {
  rank: number
  studentId: number
  studentName: string
  score?: number
  submittedAt?: string
}

export interface ExamRankingResponse {
  examId: number
  examTitle: string
  rankings: RankingEntry[]
}

// ──────────────────────────────────────
// Admin & System
// ──────────────────────────────────────
export interface SystemStatsResponse {
  totalUsers: number
  totalTeachers: number
  totalStudents: number
  totalClasses: number
  totalExams: number
  totalQuestions: number
  activeExams: number
  totalAttempts: number
}

export interface BackupResponse {
  backupId: string
  status: string
  createdAt: string
}

export interface RestoreRequest {
  backupId: string
}

export interface HealthCheckResponse {
  status: string
  database: string
  timestamp: string
  version: string
}

export interface HealthStatusResponse {
  status: string
  timestamp: string
  environment: string
  version: string
}

// ──────────────────────────────────────
// Activity Logs
// ──────────────────────────────────────
export interface ActivityLogResponse {
  id: number
  userId?: number
  action: string
  entityType?: string
  entityId?: number
  detail?: string
  ipAddress?: string
  occurredAt: string
}

export interface ActivityLogPagedResponse {
  logs: ActivityLogResponse[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
}

// ──────────────────────────────────────
// Teaching Assignments
// ──────────────────────────────────────
export interface TeachingAssignmentResponse {
  id: number
  classId: number
  className: string
  teacherId: number
  teacherName: string
  subjectId: number
  subjectName: string
  subjectCode: string
  academicYear: string
  semester: number
  assignedDate: string
}

export interface TeachingAssignmentListResponse {
  items: TeachingAssignmentResponse[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
}

export interface TeacherAssignmentResponse {
  id: number
  teacherId: number
  teacherName: string
  subjectId: number
  subjectName: string
  subjectCode: string
  academicYear: string
  semester: number
  assignedDate: string
}

export interface SubjectAssignmentResponse {
  id: number
  classId: number
  className: string
  subjectId: number
  subjectName: string
  subjectCode: string
  academicYear: string
  semester: number
  assignedDate: string
}

export interface CreateTeachingAssignmentRequest {
  classId: number
  teacherId: number
  subjectId: number
  academicYear: string
  semester: number
}

export interface UpdateTeachingAssignmentRequest {
  subjectId: number
  academicYear: string
  semester: number
}

// ──────────────────────────────────────
// Class extensions
// ──────────────────────────────────────
export interface ClassListResponse {
  totalCount: number
  pageSize: number
  currentPage: number
  totalPages: number
  classes: ClassResponse[]
}

export interface ClassStudentResponse {
  studentId: number
  username: string
  fullName: string
  studentCode: string
  rollNumber: string
  enrolledAt: string
}

export interface AssignTeacherToClassRequest {
  teacherId: number
  subjectId: number
}

export interface AssignStudentsToClassRequest {
  studentIds: number[]
}

// ──────────────────────────────────────
// File Upload
// ──────────────────────────────────────
export interface FileUploadResponse {
  fileId: string
  fileName: string
  fileSize: number
  contentType: string
  url: string
}

// ──────────────────────────────────────
// Auth (extended)
// ──────────────────────────────────────
export interface RegisterAuthRequest {
  username: string
  email: string
  fullName: string
  password: string
  confirmPassword: string
  role?: string
}

export interface RefreshTokenRequest {
  refreshToken: string
}

export interface ChangePasswordRequest {
  currentPassword: string
  newPassword: string
  confirmNewPassword: string
}

export interface ResetPasswordWithTokenRequest {
  token: string
  newPassword: string
  confirmPassword: string
}

export interface UserDto {
  id: number
  username: string
  email: string
  fullName: string
  role: string
  isActive: boolean
}

// ──────────────────────────────────────
// Student & Teacher list responses
// ──────────────────────────────────────
export interface StudentListResponse {
  totalCount: number
  pageSize: number
  currentPage: number
  totalPages: number
  students: StudentResponse[]
}

export interface TeacherListResponse {
  totalCount: number
  pageSize: number
  currentPage: number
  totalPages: number
  teachers: TeacherResponse[]
}
