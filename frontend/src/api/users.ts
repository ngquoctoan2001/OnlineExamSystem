import apiClient from './client'
import type { ApiResponse, UserDto, CreateUserRequest, UpdateUserRequest, RoleDto } from '../types/api'

export const usersApi = {
  getAll: (pageNumber = 1, pageSize = 20) =>
    apiClient.get<ApiResponse<UserDto[]>>(`/users?pageNumber=${pageNumber}&pageSize=${pageSize}`),

  getById: (id: number) =>
    apiClient.get<ApiResponse<UserDto>>(`/users/${id}`),

  create: (data: CreateUserRequest) =>
    apiClient.post<ApiResponse<UserDto>>('/users', data),

  update: (id: number, data: UpdateUserRequest) =>
    apiClient.put<ApiResponse<UserDto>>(`/users/${id}`, data),

  delete: (id: number) =>
    apiClient.delete<ApiResponse<object>>(`/users/${id}`),

  resetPassword: (id: number, newPassword: string) =>
    apiClient.post<ApiResponse<object>>(`/users/${id}/reset-password`, { newPassword }),

  toggleActive: (id: number, isActive: boolean) =>
    apiClient.patch<ApiResponse<UserDto>>(`/users/${id}/active?isActive=${isActive}`),

  getRoles: () =>
    apiClient.get<ApiResponse<RoleDto[]>>('/users/roles/list'),
}
