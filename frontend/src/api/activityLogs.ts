import apiClient from './client'
import type { ApiResponse, ActivityLogPagedResponse } from '../types/api'

export const activityLogsApi = {
  /** Get activity logs with filtering */
  getLogs: (params?: { page?: number; pageSize?: number; action?: string; userId?: number; from?: string; to?: string }) => {
    const query = new URLSearchParams()
    if (params?.page) query.set('page', String(params.page))
    if (params?.pageSize) query.set('pageSize', String(params.pageSize))
    if (params?.action) query.set('action', params.action)
    if (params?.userId) query.set('userId', String(params.userId))
    if (params?.from) query.set('from', params.from)
    if (params?.to) query.set('to', params.to)
    return apiClient.get<ApiResponse<ActivityLogPagedResponse>>(`/logs?${query.toString()}`)
  },
}
