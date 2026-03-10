import apiClient from './client'
import type { ApiResponse, StudentResponse, CreateStudentRequest } from '../types/api'

interface StudentListResponse { students: StudentResponse[]; totalCount: number; page: number; pageSize: number }

export const studentsApi = {
  getAll: (page = 1, pageSize = 20) =>
    apiClient.get<ApiResponse<StudentListResponse>>(`/students?page=${page}&pageSize=${pageSize}`),

  getById: (id: number) =>
    apiClient.get<ApiResponse<StudentResponse>>(`/students/${id}`),

  search: (term: string) =>
    apiClient.get<ApiResponse<StudentResponse[]>>(`/students/search/${encodeURIComponent(term)}`),

  create: (data: CreateStudentRequest) =>
    apiClient.post<ApiResponse<StudentResponse>>('/students', data),

  update: (id: number, data: Partial<CreateStudentRequest>) =>
    apiClient.put<ApiResponse<StudentResponse>>(`/students/${id}`, data),

  delete: (id: number) =>
    apiClient.delete<ApiResponse<object>>(`/students/${id}`),

  importFile: (file: File) => {
    const formData = new FormData()
    formData.append('file', file)
    return apiClient.post<ApiResponse<object>>('/import/students', formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    })
  },

  exportFile: () =>
    apiClient.get('/students/export', { responseType: 'blob' }),
}
