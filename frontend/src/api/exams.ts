import apiClient from './client'
import type { ApiResponse, ExamResponse, ExamListResponse, CreateExamRequest, ExamSettingsResponse, ConfigureExamSettingsRequest, ExamQuestionsListResponse, ReorderExamQuestionsRequest } from '../types/api'

export const examsApi = {
  getAll: (page = 1, pageSize = 20) =>
    apiClient.get<ApiResponse<ExamListResponse>>(`/exams?page=${page}&pageSize=${pageSize}`),

  getById: (id: number) =>
    apiClient.get<ApiResponse<ExamResponse>>(`/exams/${id}`),

  search: (term: string) =>
    apiClient.get<ApiResponse<ExamResponse[]>>(`/exams/search/${encodeURIComponent(term)}`),

  create: (data: CreateExamRequest) =>
    apiClient.post<ApiResponse<ExamResponse>>('/exams', data),

  update: (id: number, data: Partial<CreateExamRequest>) =>
    apiClient.put<ApiResponse<ExamResponse>>(`/exams/${id}`, data),

  delete: (id: number) =>
    apiClient.delete<ApiResponse<object>>(`/exams/${id}`),

  getBySubject: (subjectId: number) =>
    apiClient.get<ApiResponse<ExamResponse[]>>(`/exams/subject/${subjectId}`),

  getByClass: (classId: number) =>
    apiClient.get<ApiResponse<ExamResponse[]>>(`/exams/class/${classId}`),

  getClasses: (examId: number) =>
    apiClient.get(`/exams/${examId}/classes`),

  assignClass: (examId: number, classId: number) =>
    apiClient.post(`/exams/${examId}/classes`, classId, { headers: { 'Content-Type': 'application/json' } }),

  removeClass: (examId: number, classId: number) =>
    apiClient.delete(`/exams/${examId}/classes/${classId}`),

  // Exam Settings
  getSettings: (examId: number) =>
    apiClient.get<ApiResponse<ExamSettingsResponse>>(`/exams/${examId}/settings`),

  configureSettings: (examId: number, data: ConfigureExamSettingsRequest) =>
    apiClient.post<ApiResponse<ExamSettingsResponse>>(`/exams/${examId}/settings`, data),

  // Activate / Close (dedicated endpoints)
  activate: (examId: number) =>
    apiClient.post<ApiResponse<object>>(`/exams/${examId}/activate`),

  close: (examId: number) =>
    apiClient.post<ApiResponse<object>>(`/exams/${examId}/close`),

  // Exam Questions
  getQuestions: (examId: number) =>
    apiClient.get<ApiResponse<ExamQuestionsListResponse>>(`/exams/${examId}/questions`),

  addQuestion: (examId: number, questionId: number, questionOrder: number, maxScore = 1) =>
    apiClient.post(`/exams/${examId}/questions`, { questionId, questionOrder, maxScore }),

  removeQuestion: (examId: number, questionId: number) =>
    apiClient.delete(`/exams/${examId}/questions/${questionId}`),

  reorderQuestions: (examId: number, data: ReorderExamQuestionsRequest) =>
    apiClient.post(`/exams/${examId}/questions/reorder`, data),

  updateQuestionMaxScore: (examId: number, examQuestionId: number, maxScore: number) =>
    apiClient.post(`/exams/${examId}/questions/${examQuestionId}/max-score`, { maxScore }),

  // Teacher filter
  getByTeacher: (teacherId: number) =>
    apiClient.get<ApiResponse<ExamResponse[]>>(`/exams/teacher/${teacherId}`),

  // Student-specific endpoints
  getAvailableForStudent: (studentId: number) =>
    apiClient.get<ApiResponse<ExamResponse[]>>(`/exams/student/${studentId}/available`),

  getUpcomingForStudent: (studentId: number) =>
    apiClient.get<ApiResponse<ExamResponse[]>>(`/exams/student/${studentId}/upcoming`),

  getStudentAttempts: (studentId: number) =>
    apiClient.get(`/exam-attempts/student/${studentId}`),
}
