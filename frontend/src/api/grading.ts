import apiClient from './client'
import type {
  ApiResponse,
  GradingResultResponse,
  AttemptGradingViewResponse,
  PendingGradingAttemptResponse,
  ManualGradeRequest,
  BatchGradeRequest,
  PublishResultResponse,
  GradeQuestionRequest,
  AddAnnotationRequest,
  ExamAttemptListResponse,
} from '../types/api'

export const gradingApi = {
  /** Auto-grade objective questions in an attempt */
  autoGrade: (attemptId: number) =>
    apiClient.post<ApiResponse<GradingResultResponse[]>>(`/grading/auto-grade/${attemptId}`),

  /** Get exam attempt for grading review */
  getGradingView: (attemptId: number) =>
    apiClient.get<ApiResponse<AttemptGradingViewResponse>>(`/grading/attempts/${attemptId}/view`),

  /** Get all attempts pending manual grading for exam */
  getPending: (examId: number) =>
    apiClient.get<ApiResponse<PendingGradingAttemptResponse[]>>(`/grading/exams/${examId}/pending`),

  /** Manually grade a specific question */
  manualGrade: (attemptId: number, questionId: number, data: ManualGradeRequest) =>
    apiClient.put<ApiResponse<GradingResultResponse>>(`/grading/attempts/${attemptId}/questions/${questionId}`, data),

  /** Batch grade multiple questions in an attempt */
  batchGrade: (attemptId: number, data: BatchGradeRequest) =>
    apiClient.put<ApiResponse<GradingResultResponse[]>>(`/grading/attempts/${attemptId}/batch-grade`, data),

  /** Mark attempt as completely graded */
  markAsGraded: (attemptId: number) =>
    apiClient.post<ApiResponse<object>>(`/grading/attempts/${attemptId}/mark-graded`),

  /** Publish grades for student */
  publish: (attemptId: number) =>
    apiClient.post<ApiResponse<PublishResultResponse>>(`/grading/attempts/${attemptId}/publish`),

  /** Get published result visible to student */
  getStudentResult: (attemptId: number) =>
    apiClient.get<ApiResponse<AttemptGradingViewResponse>>(`/grading/attempts/${attemptId}/result`),

  /** Get all attempts for exam with pagination */
  getExamAttempts: (examId: number, page = 1, pageSize = 20) =>
    apiClient.get<ApiResponse<ExamAttemptListResponse>>(`/grading/exams/${examId}/attempts?page=${page}&pageSize=${pageSize}`),

  /** Get attempt detail for grading */
  getAttemptDetail: (attemptId: number) =>
    apiClient.get<ApiResponse<AttemptGradingViewResponse>>(`/grading/attempts/${attemptId}`),

  /** Grade specific question with score */
  gradeQuestion: (attemptId: number, data: GradeQuestionRequest) =>
    apiClient.post<ApiResponse<GradingResultResponse>>(`/grading/attempts/${attemptId}/score`, data),

  /** Add annotation/comment to question */
  addAnnotation: (attemptId: number, data: AddAnnotationRequest) =>
    apiClient.post<ApiResponse<object>>(`/grading/attempts/${attemptId}/annotation`, data),

  /** Finalize grading for attempt */
  finalizeGrading: (attemptId: number) =>
    apiClient.post<ApiResponse<object>>(`/grading/attempts/${attemptId}/finalize`),
}
