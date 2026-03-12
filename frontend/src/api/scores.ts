import apiClient from './client'
import type {
  ApiResponse,
  ScoreResponse,
  ExamRankingResponse,
  StudentPerformanceResponse,
  ExamStatisticResponse,
} from '../types/api'

export const scoresApi = {
  /** Get scores/performance for a student */
  getStudentScores: (studentId: number) =>
    apiClient.get<ApiResponse<StudentPerformanceResponse>>(`/scores/student/${studentId}`),

  /** Get scores for all students in a class (requires examId) */
  getClassScores: (classId: number, examId?: number) =>
    apiClient.get<ApiResponse<ScoreResponse[]>>(`/scores/class/${classId}${examId ? `?examId=${examId}` : ''}`),

  /** Get scores of all students in an exam */
  getExamScores: (examId: number) =>
    apiClient.get<ApiResponse<ScoreResponse[]>>(`/scores/exam/${examId}`),

  /** Get ranking/leaderboard for exam */
  getExamRanking: (examId: number) =>
    apiClient.get<ApiResponse<ExamRankingResponse>>(`/scores/exam/${examId}/ranking`),

  /** Get exam statistics */
  getExamStatistics: (examId: number) =>
    apiClient.get<ApiResponse<ExamStatisticResponse>>(`/scores/exam/${examId}/statistics`),
}
