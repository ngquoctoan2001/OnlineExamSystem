import apiClient from './client'
import type { ApiResponse, SubmitAnswerRequest, AnswerResponse } from '../types/api'

export const answersApi = {
  /** Submit answer to a question in exam attempt */
  submit: (attemptId: number, data: SubmitAnswerRequest) =>
    apiClient.post<ApiResponse<AnswerResponse>>(`/attempts/${attemptId}/answers`, data),

  /** Update answer for a specific question */
  update: (attemptId: number, questionId: number, data: SubmitAnswerRequest) =>
    apiClient.put<ApiResponse<AnswerResponse>>(`/attempts/${attemptId}/answers/${questionId}`, data),

  /** Get answer for a specific question */
  getAnswer: (attemptId: number, questionId: number) =>
    apiClient.get<ApiResponse<AnswerResponse>>(`/attempts/${attemptId}/answers/${questionId}`),

  /** Auto-save answer without final submission */
  autoSave: (attemptId: number, data: SubmitAnswerRequest) =>
    apiClient.post<ApiResponse<AnswerResponse>>(`/attempts/${attemptId}/answers/autosave`, data),
}
