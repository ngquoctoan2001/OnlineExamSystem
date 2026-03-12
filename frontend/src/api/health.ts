import apiClient from './client'

export const healthApi = {
  /** Check system health and database connection */
  getHealth: () =>
    apiClient.get('/health'),

  /** Get system status and environment info */
  getStatus: () =>
    apiClient.get('/health/status'),
}
