import apiClient from './client'
import type { ApiResponse, LoginRequest, LoginResponse } from '../types/api'

export const authApi = {
  login: (data: LoginRequest) =>
    apiClient.post<ApiResponse<LoginResponse>>('/auth/login', data),

  logout: () =>
    apiClient.post('/auth/logout'),

  me: () =>
    apiClient.get<ApiResponse<{ id: number; username: string; fullName: string; role: string }>>('/auth/me'),
}
