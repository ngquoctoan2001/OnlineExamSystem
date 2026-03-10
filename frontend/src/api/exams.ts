import apiClient from './client'
import type { ApiResponse, ExamResponse, ExamListResponse, CreateExamRequest } from '../types/api'

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

  changeStatus: (id: number, status: string) =>
    apiClient.post<ApiResponse<object>>(`/exams/${id}/status`, { status }),

  getBySubject: (subjectId: number) =>
    apiClient.get<ApiResponse<ExamResponse[]>>(`/exams/subject/${subjectId}`),

  getActiveExams: () =>
    apiClient.get<ApiResponse<ExamResponse[]>>('/exams/active'),
}
