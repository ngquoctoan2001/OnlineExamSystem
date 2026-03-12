import apiClient from './client'
import type {
  ApiResponse,
  LoginRequest,
  LoginResponse,
  RegisterAuthRequest,
  RefreshTokenRequest,
  ChangePasswordRequest,
  ResetPasswordWithTokenRequest,
  UserDto,
} from '../types/api'

export const authApi = {
  login: (data: LoginRequest) =>
    apiClient.post<ApiResponse<LoginResponse>>('/auth/login', data),

  register: (data: RegisterAuthRequest) =>
    apiClient.post<ApiResponse<object>>('/auth/register', data),

  refreshToken: (data: RefreshTokenRequest) =>
    apiClient.post<ApiResponse<LoginResponse>>('/auth/refresh-token', data),

  logout: () =>
    apiClient.post<ApiResponse<object>>('/auth/logout'),

  me: () =>
    apiClient.get<ApiResponse<UserDto>>('/auth/me'),

  changePassword: (data: ChangePasswordRequest) =>
    apiClient.post<ApiResponse<object>>('/auth/change-password', data),

  forgotPassword: (email: string) =>
    apiClient.post<ApiResponse<object>>('/auth/forgot-password', { email }),

  resetPassword: (data: ResetPasswordWithTokenRequest) =>
    apiClient.post<ApiResponse<object>>('/auth/reset-password', data),

  verifySession: () =>
    apiClient.post<ApiResponse<UserDto>>('/auth/verify-session'),
}
