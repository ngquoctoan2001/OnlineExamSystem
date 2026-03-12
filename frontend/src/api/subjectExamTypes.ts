import apiClient from './client'
import type { ApiResponse, SubjectExamTypeResponse, CreateSubjectExamTypeRequest, UpdateSubjectExamTypeRequest } from '../types/api'

export const subjectExamTypesApi = {
  getBySubject: (subjectId: number) =>
    apiClient.get<ApiResponse<SubjectExamTypeResponse[]>>(`/subject-exam-types/subject/${subjectId}`),

  getById: (id: number) =>
    apiClient.get<ApiResponse<SubjectExamTypeResponse>>(`/subject-exam-types/${id}`),

  create: (data: CreateSubjectExamTypeRequest) =>
    apiClient.post<ApiResponse<SubjectExamTypeResponse>>('/subject-exam-types', data),

  update: (id: number, data: UpdateSubjectExamTypeRequest) =>
    apiClient.put<ApiResponse<SubjectExamTypeResponse>>(`/subject-exam-types/${id}`, data),

  delete: (id: number) =>
    apiClient.delete<ApiResponse<object>>(`/subject-exam-types/${id}`),
}
