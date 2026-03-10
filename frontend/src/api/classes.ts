import apiClient from './client'
import type { ApiResponse, ClassResponse, CreateClassRequest } from '../types/api'

interface ClassListResponse { classes: ClassResponse[]; totalCount: number; page: number; pageSize: number }

export const classesApi = {
  getAll: (page = 1, pageSize = 20) =>
    apiClient.get<ApiResponse<ClassListResponse>>(`/classes?page=${page}&pageSize=${pageSize}`),

  getById: (id: number) =>
    apiClient.get<ApiResponse<ClassResponse>>(`/classes/${id}`),

  search: (term: string) =>
    apiClient.get<ApiResponse<ClassResponse[]>>(`/classes/search/${encodeURIComponent(term)}`),

  create: (data: CreateClassRequest) =>
    apiClient.post<ApiResponse<ClassResponse>>('/classes', data),

  update: (id: number, data: Partial<CreateClassRequest>) =>
    apiClient.put<ApiResponse<ClassResponse>>(`/classes/${id}`, data),

  delete: (id: number) =>
    apiClient.delete<ApiResponse<object>>(`/classes/${id}`),

  getStudents: (id: number) =>
    apiClient.get(`/classes/${id}/students`),

  addStudent: (classId: number, studentId: number) =>
    apiClient.post(`/classes/${classId}/students`, { studentId }),

  removeStudent: (classId: number, studentId: number) =>
    apiClient.delete(`/classes/${classId}/students/${studentId}`),
}
