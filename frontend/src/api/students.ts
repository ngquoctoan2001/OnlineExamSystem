import apiClient from './client'
import type {
  ApiResponse,
  StudentResponse,
  CreateStudentRequest,
  StudentListResponse,
  ExamAttemptResponse,
  StudentPerformanceResponse,
} from '../types/api'

export const studentsApi = {
  getAll: (page = 1, pageSize = 20) =>
    apiClient.get<ApiResponse<StudentListResponse>>(`/students?page=${page}&pageSize=${pageSize}`),

  getMe: () =>
    apiClient.get<ApiResponse<StudentResponse>>('/students/me'),

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

  getStudentClasses: (id: number) =>
    apiClient.get(`/students/${id}/classes`),

  getStudentScores: (id: number) =>
    apiClient.get<ApiResponse<StudentPerformanceResponse>>(`/students/${id}/scores`),

  getStudentExams: (id: number) =>
    apiClient.get<ApiResponse<ExamAttemptResponse[]>>(`/students/${id}/exams`),

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
