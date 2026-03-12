import apiClient from './client'
import type {
  ApiResponse,
  TeachingAssignmentResponse,
  TeachingAssignmentListResponse,
  TeacherAssignmentResponse,
  SubjectAssignmentResponse,
  CreateTeachingAssignmentRequest,
  UpdateTeachingAssignmentRequest,
} from '../types/api'

export const teachingAssignmentsApi = {
  /** Get all teaching assignments with pagination */
  getAll: (page = 1, pageSize = 20) =>
    apiClient.get<ApiResponse<TeachingAssignmentListResponse>>(`/teachingassignments?page=${page}&pageSize=${pageSize}`),

  /** Get teaching assignment by ID */
  getById: (id: number) =>
    apiClient.get<ApiResponse<TeachingAssignmentResponse>>(`/teachingassignments/${id}`),

  /** Get all assignments for a class */
  getByClass: (classId: number) =>
    apiClient.get<ApiResponse<TeacherAssignmentResponse[]>>(`/teachingassignments/class/${classId}`),

  /** Get all assignments for a teacher */
  getByTeacher: (teacherId: number) =>
    apiClient.get<ApiResponse<SubjectAssignmentResponse[]>>(`/teachingassignments/teacher/${teacherId}`),

  /** Create new teaching assignment */
  create: (data: CreateTeachingAssignmentRequest) =>
    apiClient.post<ApiResponse<TeachingAssignmentResponse>>('/teachingassignments', data),

  /** Update teaching assignment */
  update: (id: number, data: UpdateTeachingAssignmentRequest) =>
    apiClient.put<ApiResponse<TeachingAssignmentResponse>>(`/teachingassignments/${id}`, data),

  /** Delete teaching assignment */
  delete: (id: number) =>
    apiClient.delete<ApiResponse<object>>(`/teachingassignments/${id}`),
}
