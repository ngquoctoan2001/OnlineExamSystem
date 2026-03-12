import apiClient from './client'
import type { ApiResponse, QuestionListResponse, QuestionDetailResponse, CreateQuestionRequest, UpdateQuestionRequest, QuestionResponse, QuestionTypeResponse, QuestionOptionResponse, CreateQuestionOptionRequest } from '../types/api'

export const questionsApi = {
  getAll: (page = 1, pageSize = 20, tagId?: number) =>
    apiClient.get<ApiResponse<QuestionListResponse>>(`/questions?page=${page}&pageSize=${pageSize}${tagId ? `&tagId=${tagId}` : ''}`),

  getById: (id: number) =>
    apiClient.get<ApiResponse<QuestionDetailResponse>>(`/questions/${id}`),

  getBySubject: (subjectId: number) =>
    apiClient.get<ApiResponse<QuestionResponse[]>>(`/questions/subject/${subjectId}`),

  getByDifficulty: (difficulty: string) =>
    apiClient.get<ApiResponse<QuestionResponse[]>>(`/questions/difficulty/${encodeURIComponent(difficulty)}`),

  search: (term: string) =>
    apiClient.get<ApiResponse<QuestionResponse[]>>(`/questions/search?term=${encodeURIComponent(term)}`),

  create: (data: CreateQuestionRequest) =>
    apiClient.post<ApiResponse<QuestionDetailResponse>>('/questions', data),

  update: (id: number, data: UpdateQuestionRequest) =>
    apiClient.put<ApiResponse<QuestionDetailResponse>>(`/questions/${id}`, data),

  delete: (id: number) =>
    apiClient.delete<ApiResponse<object>>(`/questions/${id}`),

  publish: (id: number) =>
    apiClient.post<ApiResponse<object>>(`/questions/${id}/publish`),

  unpublish: (id: number) =>
    apiClient.post<ApiResponse<object>>(`/questions/${id}/unpublish`),

  getTypes: () =>
    apiClient.get<ApiResponse<QuestionTypeResponse[]>>('/question-types'),

  // Options CRUD
  getOptions: (questionId: number) =>
    apiClient.get<ApiResponse<QuestionOptionResponse[]>>(`/questions/${questionId}/options`),

  addOption: (questionId: number, data: CreateQuestionOptionRequest) =>
    apiClient.post<ApiResponse<QuestionOptionResponse>>(`/questions/${questionId}/options`, data),

  updateOption: (questionId: number, optionId: number, data: CreateQuestionOptionRequest) =>
    apiClient.put<ApiResponse<QuestionOptionResponse>>(`/questions/${questionId}/options/${optionId}`, data),

  deleteOption: (questionId: number, optionId: number) =>
    apiClient.delete<ApiResponse<object>>(`/questions/${questionId}/options/${optionId}`),

  // Options by optionId directly
  updateOptionById: (optionId: number, data: CreateQuestionOptionRequest) =>
    apiClient.put<ApiResponse<QuestionOptionResponse>>(`/questions/options/${optionId}`, data),

  deleteOptionById: (optionId: number) =>
    apiClient.delete<ApiResponse<object>>(`/questions/options/${optionId}`),

  // Published questions
  getPublished: () =>
    apiClient.get<ApiResponse<QuestionResponse[]>>('/questions/published'),

  getByType: (questionTypeId: number) =>
    apiClient.get<ApiResponse<QuestionResponse[]>>(`/questions/type/${questionTypeId}`),

  // Import
  importFile: (file: File) => {
    const formData = new FormData()
    formData.append('file', file)
    return apiClient.post<ApiResponse<object>>('/import/questions', formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    })
  },

  importPdf: (file: File) => {
    const formData = new FormData()
    formData.append('file', file)
    return apiClient.post<ApiResponse<object>>('/questions/import/pdf', formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    })
  },

  importDocx: (file: File) => {
    const formData = new FormData()
    formData.append('file', file)
    return apiClient.post<ApiResponse<object>>('/questions/import/docx', formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    })
  },

  importLatex: (file: File) => {
    const formData = new FormData()
    formData.append('file', file)
    return apiClient.post<ApiResponse<object>>('/questions/import/latex', formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    })
  },

  importExcel: (file: File) => {
    const formData = new FormData()
    formData.append('file', file)
    return apiClient.post<ApiResponse<object>>('/questions/import/excel', formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    })
  },
}
