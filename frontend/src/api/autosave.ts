import apiClient from './client'
import type { ApiResponse, AutosaveRequest, AutosaveResponse, AnswerResponse } from '../types/api'

export const autosaveApi = {
  /** Save autosave data for multiple exam answers */
  save: (data: AutosaveRequest) =>
    apiClient.post<ApiResponse<AutosaveResponse>>('/autosave', data),

  /** Retrieve autosaved answers for an exam attempt */
  get: (attemptId: number) =>
    apiClient.get<ApiResponse<AnswerResponse[]>>(`/autosave/${attemptId}`),

  /** Clear autosave data for an attempt */
  delete: (attemptId: number) =>
    apiClient.delete<ApiResponse<object>>(`/autosave/${attemptId}`),
}
