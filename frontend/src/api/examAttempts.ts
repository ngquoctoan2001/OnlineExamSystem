import apiClient from './client'
import type {
  ApiResponse,
  StartExamAttemptRequest,
  ExamAttemptResponse,
  ExamAttemptDetailResponse,
  ExamAttemptListResponse,
  SubmitExamAttemptResponse,
  AttemptQuestionResponse,
  LogViolationRequest,
  ViolationResponse,
  SubmitAnswerRequest,
  AnswerResponse,
  SaveCanvasRequest,
  FlagQuestionRequest,
} from '../types/api'

export const examAttemptsApi = {
  /** Start a new exam attempt */
  start: (data: StartExamAttemptRequest) =>
    apiClient.post<ApiResponse<ExamAttemptResponse>>('/exam-attempts/start', data),

  /** Get exam attempt by ID */
  getById: (id: number) =>
    apiClient.get<ApiResponse<ExamAttemptResponse>>(`/exam-attempts/${id}`),

  /** Get detailed exam attempt (includes answers) */
  getDetail: (id: number) =>
    apiClient.get<ApiResponse<ExamAttemptDetailResponse>>(`/exam-attempts/${id}/detail`),

  /** Get all attempts with pagination */
  getAll: (page = 1, pageSize = 20) =>
    apiClient.get<ApiResponse<ExamAttemptListResponse>>(`/exam-attempts?page=${page}&pageSize=${pageSize}`),

  /** Get all attempts by a student */
  getStudentAttempts: (studentId: number) =>
    apiClient.get<ApiResponse<ExamAttemptResponse[]>>(`/exam-attempts/student/${studentId}`),

  /** Get all attempts for a specific exam */
  getExamAttempts: (examId: number, page = 1, pageSize = 20) =>
    apiClient.get<ApiResponse<ExamAttemptListResponse>>(`/exam-attempts/exam/${examId}?page=${page}&pageSize=${pageSize}`),

  /** Get current in-progress attempt for student in exam */
  getCurrentAttempt: (studentId: number, examId: number) =>
    apiClient.get<ApiResponse<ExamAttemptResponse>>(`/exam-attempts/student/${studentId}/exam/${examId}/current`),

  /** Submit completed exam attempt */
  submit: (id: number) =>
    apiClient.post<ApiResponse<SubmitExamAttemptResponse>>(`/exam-attempts/${id}/submit`),

  /** Get all questions in an exam attempt */
  getQuestions: (id: number) =>
    apiClient.get<ApiResponse<AttemptQuestionResponse[]>>(`/exam-attempts/${id}/questions`),

  /** Log exam violation / proctoring issue */
  logViolation: (id: number, data: LogViolationRequest) =>
    apiClient.post<ApiResponse<ViolationResponse>>(`/exam-attempts/${id}/violations`, data),

  /** Save answer for a question */
  saveAnswer: (id: number, data: SubmitAnswerRequest) =>
    apiClient.post<ApiResponse<AnswerResponse>>(`/exam-attempts/${id}/answers`, data),

  /** Save canvas drawing as answer */
  saveCanvas: (id: number, data: SaveCanvasRequest) =>
    apiClient.post<ApiResponse<AnswerResponse>>(`/exam-attempts/${id}/canvas`, data),

  /** Flag a question for later review */
  flagQuestion: (id: number, data: FlagQuestionRequest) =>
    apiClient.post<ApiResponse<object>>(`/exam-attempts/${id}/flag-question`, data),

  /** Remove flag from question */
  unflagQuestion: (id: number, data: FlagQuestionRequest) =>
    apiClient.post<ApiResponse<object>>(`/exam-attempts/${id}/unflag-question`, data),

  /** Resume an in-progress exam attempt */
  resumeExam: (id: number) =>
    apiClient.get<ApiResponse<ExamAttemptDetailResponse>>(`/exam-attempts/${id}/resume`),
}
