import React, { createContext, useContext, useState, useEffect, useCallback } from 'react'
import { authApi } from '../api/auth'

interface User {
  id: number
  username: string
  fullName: string
  role: string
}

interface AuthContextType {
  user: User | null
  isAuthenticated: boolean
  loading: boolean
  login: (username: string, password: string) => Promise<{ success: boolean; message: string }>
  logout: () => void
}

const AuthContext = createContext<AuthContextType | null>(null)

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<User | null>(null)
  const [loading, setLoading] = useState(true)

  const logout = useCallback(() => {
    localStorage.removeItem('accessToken')
    localStorage.removeItem('refreshToken')
    localStorage.removeItem('user')
    setUser(null)
  }, [])

  // Restore session on mount
  useEffect(() => {
    const stored = localStorage.getItem('user')
    const token = localStorage.getItem('accessToken')
    if (stored && token) {
      try {
        setUser(JSON.parse(stored))
      } catch {
        logout()
      }
    }
    setLoading(false)
  }, [logout])

  const login = async (username: string, password: string) => {
    try {
      const res = await authApi.login({ username, password })
      if (res.data.success && res.data.data) {
        const { accessToken, refreshToken } = res.data.data
        localStorage.setItem('accessToken', accessToken)
        localStorage.setItem('refreshToken', refreshToken)

        // Decode basic info from token payload
        try {
          const payload = JSON.parse(atob(accessToken.split('.')[1]))
          const u: User = {
            id: Number(payload.sub || payload.userId || payload.nameid || 0),
            username: payload.unique_name || payload.username || username,
            fullName: payload.fullname || payload.name || payload.fullName || username,
            role: payload.role || payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] || 'USER',
          }
          localStorage.setItem('user', JSON.stringify(u))
          setUser(u)
        } catch {
          // If decode fails, use minimal info
          const u: User = { id: 0, username, fullName: username, role: 'USER' }
          localStorage.setItem('user', JSON.stringify(u))
          setUser(u)
        }

        return { success: true, message: res.data.message }
      }
      return { success: false, message: res.data.message || 'Đăng nhập thất bại' }
    } catch (err: unknown) {
      const e = err as { response?: { data?: { message?: string } } }
      return { success: false, message: e.response?.data?.message || 'Lỗi kết nối máy chủ' }
    }
  }

  return (
    <AuthContext.Provider value={{ user, isAuthenticated: !!user, loading, login, logout }}>
      {children}
    </AuthContext.Provider>
  )
}

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used inside AuthProvider')
  return ctx
}
