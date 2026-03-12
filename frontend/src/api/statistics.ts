import apiClient from './client'
import type { ApiResponse, ExamStatisticResponse, ScoreDistributionResponse, StudentPerformanceResponse, ClassResultsResponse } from '../types/api'

export const statisticsApi = {
  // Exam statistics
  calculateExamStats: (examId: number) =>
    apiClient.post<ApiResponse<ExamStatisticResponse>>(`/statistics/exams/${examId}/calculate`),

  getExamStats: (examId: number) =>
    apiClient.get<ApiResponse<ExamStatisticResponse>>(`/statistics/exams/${examId}`),

  getScoreDistribution: (examId: number) =>
    apiClient.get<ApiResponse<ScoreDistributionResponse>>(`/statistics/exams/${examId}/distribution`),

  // Student performance
  getStudentPerformance: (studentId: number) =>
    apiClient.get<ApiResponse<StudentPerformanceResponse>>(`/statistics/students/${studentId}/performance`),

  // Class results
  getClassResults: (classId: number, examId: number) =>
    apiClient.get<ApiResponse<ClassResultsResponse>>(`/statistics/classes/${classId}/results`, { params: { examId } }),

  exportClassResults: (classId: number, examId: number) =>
    apiClient.get(`/statistics/classes/${classId}/results/export`, { params: { examId }, responseType: 'blob' }),
}
