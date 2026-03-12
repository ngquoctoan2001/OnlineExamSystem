import axios from 'axios'

const API_BASE = '/api'

export const apiClient = axios.create({
  baseURL: API_BASE,
  headers: { 'Content-Type': 'application/json' },
})

// Attach JWT token on every request
apiClient.interceptors.request.use((config) => {
  const token = localStorage.getItem('accessToken')
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

// Handle 401 and 429 responses
apiClient.interceptors.response.use(
  (res) => res,
  async (error) => {
    const config = error.config

    // Handle 429 – retry after delay (max 2 retries)
    if (error.response?.status === 429 && (!config._retryCount || config._retryCount < 2)) {
      config._retryCount = (config._retryCount || 0) + 1
      const retryAfter = error.response.headers['retry-after']
      const delay = retryAfter ? Math.min(parseInt(retryAfter, 10) * 1000, 30000) : 5000
      await new Promise(resolve => setTimeout(resolve, delay))
      return apiClient(config)
    }

    // Handle 401 – clear token and redirect to login (skip if already on login page)
    if (error.response?.status === 401 && !window.location.pathname.includes('/login')) {
      localStorage.removeItem('accessToken')
      localStorage.removeItem('refreshToken')
      localStorage.removeItem('user')
      window.location.href = '/login'
    }

    return Promise.reject(error)
  }
)

export default apiClient
