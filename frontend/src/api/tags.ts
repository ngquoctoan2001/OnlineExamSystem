import apiClient from './client'
import type { ApiResponse, TagResponse, CreateTagRequest } from '../types/api'

export const tagsApi = {
  getAll: (search?: string) =>
    apiClient.get<ApiResponse<TagResponse[]>>('/tags', { params: search ? { search } : undefined }),

  getById: (id: number) =>
    apiClient.get<ApiResponse<TagResponse>>(`/tags/${id}`),

  create: (data: CreateTagRequest) =>
    apiClient.post<ApiResponse<TagResponse>>('/tags', data),

  update: (id: number, data: CreateTagRequest) =>
    apiClient.put<ApiResponse<TagResponse>>(`/tags/${id}`, data),

  delete: (id: number) =>
    apiClient.delete<ApiResponse<object>>(`/tags/${id}`),

  // Question-Tag assignments
  getQuestionTags: (questionId: number) =>
    apiClient.get<ApiResponse<TagResponse[]>>(`/questions/${questionId}/tags`),

  assignTag: (questionId: number, tagId: number) =>
    apiClient.post<ApiResponse<object>>(`/questions/${questionId}/tags/${tagId}`),

  removeTag: (questionId: number, tagId: number) =>
    apiClient.delete<ApiResponse<object>>(`/questions/${questionId}/tags/${tagId}`),
}
