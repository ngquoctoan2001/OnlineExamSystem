import apiClient from './client'
import type {
  ApiResponse,
  TeacherResponse,
  CreateTeacherRequest,
  TeacherListResponse,
  SubjectAssignmentResponse,
} from '../types/api'

export const teachersApi = {
  getAll: (page = 1, pageSize = 20) =>
    apiClient.get<ApiResponse<TeacherListResponse>>(`/teachers?page=${page}&pageSize=${pageSize}`),

  getMe: () =>
    apiClient.get<ApiResponse<TeacherResponse>>('/teachers/me'),

  getById: (id: number) =>
    apiClient.get<ApiResponse<TeacherResponse>>(`/teachers/${id}`),

  search: (term: string) =>
    apiClient.get<ApiResponse<TeacherResponse[]>>(`/teachers/search/${encodeURIComponent(term)}`),

  create: (data: CreateTeacherRequest) =>
    apiClient.post<ApiResponse<TeacherResponse>>('/teachers', data),

  update: (id: number, data: Partial<CreateTeacherRequest>) =>
    apiClient.put<ApiResponse<TeacherResponse>>(`/teachers/${id}`, data),

  delete: (id: number) =>
    apiClient.delete<ApiResponse<object>>(`/teachers/${id}`),

  getTeacherClasses: (id: number) =>
    apiClient.get(`/teachers/${id}/classes`),

  getTeacherSubjects: (id: number) =>
    apiClient.get<ApiResponse<SubjectAssignmentResponse[]>>(`/teachers/${id}/subjects`),

  importFile: (file: File) => {
    const formData = new FormData()
    formData.append('file', file)
    return apiClient.post<ApiResponse<object>>('/import/teachers', formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    })
  },

  exportFile: () =>
    apiClient.get('/teachers/export', { responseType: 'blob' }),
}
