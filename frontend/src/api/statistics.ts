import apiClient from './client'

export const statisticsApi = {
  getDashboard: () =>
    apiClient.get('/statistics/dashboard'),

  getExamStats: (examId: number) =>
    apiClient.get(`/statistics/exam/${examId}`),

  getStudentStats: (studentId: number) =>
    apiClient.get(`/statistics/student/${studentId}`),

  getOverview: () =>
    apiClient.get('/statistics/overview'),
}
