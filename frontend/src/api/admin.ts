import apiClient from './client'
import type {
  ApiResponse,
  SystemStatsResponse,
  ActivityLogPagedResponse,
  BackupResponse,
  RestoreRequest,
  HealthCheckResponse,
} from '../types/api'

export const adminApi = {
  /** Get system-wide statistics */
  getSystemStats: () =>
    apiClient.get<ApiResponse<SystemStatsResponse>>('/admin/system-stats'),

  /** Get system activity logs with pagination and filters */
  getLogs: (params?: { page?: number; pageSize?: number; action?: string; userId?: number; from?: string; to?: string }) => {
    const query = new URLSearchParams()
    if (params?.page) query.set('page', String(params.page))
    if (params?.pageSize) query.set('pageSize', String(params.pageSize))
    if (params?.action) query.set('action', params.action)
    if (params?.userId) query.set('userId', String(params.userId))
    if (params?.from) query.set('from', params.from)
    if (params?.to) query.set('to', params.to)
    return apiClient.get<ApiResponse<ActivityLogPagedResponse>>(`/admin/logs?${query.toString()}`)
  },

  /** Initiate database backup */
  backupDatabase: () =>
    apiClient.post<ApiResponse<BackupResponse>>('/admin/backup'),

  /** Restore database from backup */
  restoreDatabase: (data: RestoreRequest) =>
    apiClient.post<ApiResponse<object>>('/admin/restore', data),

  /** Check system and database health */
  healthCheck: () =>
    apiClient.get<ApiResponse<HealthCheckResponse>>('/admin/health'),
}
