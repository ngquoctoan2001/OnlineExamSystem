import apiClient from './client'
import type { ApiResponse, StudentFullGradebookResponse, StudentSubjectGradebookResponse, ClassSubjectGradebookResponse } from '../types/api'

export const gradebookApi = {
  getStudentGradebook: (studentId: number) =>
    apiClient.get<ApiResponse<StudentFullGradebookResponse>>(`/gradebook/student/${studentId}`),

  getStudentSubjectGradebook: (studentId: number, subjectId: number) =>
    apiClient.get<ApiResponse<StudentSubjectGradebookResponse>>(`/gradebook/student/${studentId}/subject/${subjectId}`),

  getClassSubjectGradebook: (classId: number, subjectId: number) =>
    apiClient.get<ApiResponse<ClassSubjectGradebookResponse>>(`/gradebook/class/${classId}/subject/${subjectId}`),
}
