import apiClient from './client'
import type { ApiResponse, QuestionListResponse, QuestionDetailResponse, CreateQuestionRequest, QuestionResponse, QuestionTypeResponse } from '../types/api'

export const questionsApi = {
  getAll: (page = 1, pageSize = 20) =>
    apiClient.get<ApiResponse<QuestionListResponse>>(`/questions?page=${page}&pageSize=${pageSize}`),

  getById: (id: number) =>
    apiClient.get<ApiResponse<QuestionDetailResponse>>(`/questions/${id}`),

  getBySubject: (subjectId: number) =>
    apiClient.get<ApiResponse<QuestionResponse[]>>(`/questions/subject/${subjectId}`),

  search: (term: string) =>
    apiClient.get<ApiResponse<QuestionResponse[]>>(`/questions/search/${encodeURIComponent(term)}`),

  create: (data: CreateQuestionRequest) =>
    apiClient.post<ApiResponse<QuestionDetailResponse>>('/questions', data),

  update: (id: number, data: Partial<CreateQuestionRequest>) =>
    apiClient.put<ApiResponse<QuestionDetailResponse>>(`/questions/${id}`, data),

  delete: (id: number) =>
    apiClient.delete<ApiResponse<object>>(`/questions/${id}`),

  publish: (id: number) =>
    apiClient.post<ApiResponse<object>>(`/questions/${id}/publish`),

  getTypes: () =>
    apiClient.get<ApiResponse<QuestionTypeResponse[]>>('/question-types'),
}
