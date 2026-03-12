import apiClient from './client'
import type {
  ApiResponse,
  ClassResponse,
  ClassListResponse,
  CreateClassRequest,
  ClassStudentResponse,
  TeacherAssignmentResponse,
  AssignTeacherToClassRequest,
  AssignStudentsToClassRequest,
} from '../types/api'

export const classesApi = {
  getAll: (page = 1, pageSize = 20) =>
    apiClient.get<ApiResponse<ClassListResponse>>(`/classes?page=${page}&pageSize=${pageSize}`),

  getById: (id: number) =>
    apiClient.get<ApiResponse<ClassResponse>>(`/classes/${id}`),

  getBySchool: (schoolId: number, page = 1, pageSize = 20) =>
    apiClient.get<ApiResponse<ClassListResponse>>(`/classes/school/${schoolId}?page=${page}&pageSize=${pageSize}`),

  getByGrade: (grade: number, page = 1, pageSize = 20) =>
    apiClient.get<ApiResponse<ClassListResponse>>(`/classes/grade/${grade}?page=${page}&pageSize=${pageSize}`),

  search: (term: string) =>
    apiClient.get<ApiResponse<ClassResponse[]>>(`/classes/search/${encodeURIComponent(term)}`),

  create: (data: CreateClassRequest) =>
    apiClient.post<ApiResponse<ClassResponse>>('/classes', data),

  update: (id: number, data: Partial<CreateClassRequest>) =>
    apiClient.put<ApiResponse<ClassResponse>>(`/classes/${id}`, data),

  delete: (id: number) =>
    apiClient.delete<ApiResponse<object>>(`/classes/${id}`),

  getStudents: (id: number) =>
    apiClient.get<ApiResponse<ClassStudentResponse[]>>(`/classes/${id}/students`),

  getClassTeachers: (id: number) =>
    apiClient.get<ApiResponse<TeacherAssignmentResponse[]>>(`/classes/${id}/teachers`),

  addStudent: (classId: number, studentId: number) =>
    apiClient.post<ApiResponse<object>>(`/classes/${classId}/students/${studentId}`, {}),

  removeStudent: (classId: number, studentId: number) =>
    apiClient.delete<ApiResponse<object>>(`/classes/${classId}/students/${studentId}`),

  assignTeacher: (classId: number, data: AssignTeacherToClassRequest) =>
    apiClient.post<ApiResponse<object>>(`/classes/${classId}/assign-teacher`, data),

  assignStudents: (classId: number, data: AssignStudentsToClassRequest) =>
    apiClient.post<ApiResponse<object>>(`/classes/${classId}/assign-students`, data),
}
