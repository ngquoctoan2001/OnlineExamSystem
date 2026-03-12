import apiClient from './client'
import type {
  ApiResponse,
  NotificationResponse,
  CreateNotificationRequest,
  SendNotificationToClassRequest,
} from '../types/api'

export const notificationsApi = {
  /** Get notifications for a user with optional filters */
  getUserNotifications: (params?: { userId?: number; unreadOnly?: boolean; page?: number; pageSize?: number }) => {
    const q = new URLSearchParams()
    if (params?.userId) q.set('userId', String(params.userId))
    if (params?.unreadOnly != null) q.set('unreadOnly', String(params.unreadOnly))
    if (params?.page) q.set('page', String(params.page))
    if (params?.pageSize) q.set('pageSize', String(params.pageSize))
    return apiClient.get<ApiResponse<NotificationResponse[]>>(`/notifications?${q.toString()}`)
  },

  /** Create a new notification */
  create: (data: CreateNotificationRequest) =>
    apiClient.post<ApiResponse<NotificationResponse>>('/notifications', data),

  /** Mark a notification as read */
  markAsRead: (id: number) =>
    apiClient.put<ApiResponse<object>>(`/notifications/${id}/read`),

  /** Mark all notifications as read for a user */
  markAllAsRead: (userId?: number) =>
    apiClient.put<ApiResponse<object>>(`/notifications/read-all${userId ? `?userId=${userId}` : ''}`),

  /** Get count of unread notifications */
  getUnreadCount: (userId: number) =>
    apiClient.get<ApiResponse<number>>(`/notifications/unread-count?userId=${userId}`),

  /** Delete a notification */
  delete: (id: number) =>
    apiClient.delete<ApiResponse<object>>(`/notifications/${id}`),

  /** Send notification to all students in a class */
  sendToClass: (data: SendNotificationToClassRequest) =>
    apiClient.post<ApiResponse<object>>('/notifications/send-class', data),
}
