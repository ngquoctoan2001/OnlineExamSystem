import apiClient from './client'

export const filesApi = {
  /** Download/retrieve a file by ID */
  getFile: (fileId: string) =>
    apiClient.get(`/files/${encodeURIComponent(fileId)}`, { responseType: 'blob' }),

  /** Get file URL for embedding */
  getFileUrl: (fileId: string) => `/api/files/${encodeURIComponent(fileId)}`,
}
