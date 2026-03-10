import apiClient from './client'
import type { ApiResponse, SubjectResponse, CreateSubjectRequest } from '../types/api'

interface SubjectListResponse { subjects: SubjectResponse[]; totalCount: number; page: number; pageSize: number }

export const subjectsApi = {
  getAll: (page = 1, pageSize = 50) =>
    apiClient.get<ApiResponse<SubjectListResponse>>(`/subjects?page=${page}&pageSize=${pageSize}`),

  getById: (id: number) =>
    apiClient.get<ApiResponse<SubjectResponse>>(`/subjects/${id}`),

  create: (data: CreateSubjectRequest) =>
    apiClient.post<ApiResponse<SubjectResponse>>('/subjects', data),

  update: (id: number, data: Partial<CreateSubjectRequest>) =>
    apiClient.put<ApiResponse<SubjectResponse>>(`/subjects/${id}`, data),

  delete: (id: number) =>
    apiClient.delete<ApiResponse<object>>(`/subjects/${id}`),
}
