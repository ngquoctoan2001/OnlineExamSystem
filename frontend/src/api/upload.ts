import apiClient from './client'
import type { ApiResponse, FileUploadResponse } from '../types/api'

export const uploadApi = {
  /** Upload an image file */
  uploadImage: (file: File) => {
    const formData = new FormData()
    formData.append('file', file)
    return apiClient.post<ApiResponse<FileUploadResponse>>('/upload/image', formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    })
  },

  /** Upload a PDF file */
  uploadPdf: (file: File) => {
    const formData = new FormData()
    formData.append('file', file)
    return apiClient.post<ApiResponse<FileUploadResponse>>('/upload/pdf', formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    })
  },

  /** Upload an exam document */
  uploadExamDoc: (file: File) => {
    const formData = new FormData()
    formData.append('file', file)
    return apiClient.post<ApiResponse<FileUploadResponse>>('/upload/exam-doc', formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    })
  },

  /** Upload a canvas drawing */
  uploadCanvas: (file: File) => {
    const formData = new FormData()
    formData.append('file', file)
    return apiClient.post<ApiResponse<FileUploadResponse>>('/upload/canvas', formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    })
  },
}
