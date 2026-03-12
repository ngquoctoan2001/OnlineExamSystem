import React, { createContext, useContext, useState, useEffect, useCallback } from 'react'
import { authApi } from '../api/auth'
import { studentsApi } from '../api/students'

interface User {
  id: number
  username: string
  fullName: string
  role: string
  studentId?: number
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

  // Fetch studentId for STUDENT role
  const fetchStudentId = useCallback(async (u: User) => {
    if (u.role?.toUpperCase() !== 'STUDENT' || u.studentId) return u
    try {
      const res = await studentsApi.getMe()
      if (res.data?.data?.id) {
        const updated = { ...u, studentId: res.data.data.id }
        localStorage.setItem('user', JSON.stringify(updated))
        return updated
      }
    } catch { /* ignore */ }
    return u
  }, [])

  // Restore session on mount – also re-decode the JWT so that
  // Vietnamese names that were previously garbled by plain atob() are fixed.
  useEffect(() => {
    const stored = localStorage.getItem('user')
    const token = localStorage.getItem('accessToken')
    if (stored && token) {
      try {
        let parsed = JSON.parse(stored)
        // Re-decode fullName from token to fix any prior UTF-8 garbling
        try {
          const payload = JSON.parse(new TextDecoder().decode(Uint8Array.from(atob(token.split('.')[1]), c => c.charCodeAt(0))))
          const freshName = payload.fullname || payload.name || payload.fullName
          if (freshName && freshName !== parsed.fullName) {
            parsed = { ...parsed, fullName: freshName }
            localStorage.setItem('user', JSON.stringify(parsed))
          }
        } catch { /* ignore decode error */ }
        setUser(parsed)
        // Refresh studentId if needed
        if (parsed.role?.toUpperCase() === 'STUDENT' && !parsed.studentId) {
          fetchStudentId(parsed).then(u => setUser(u))
        }
        // Verify session is still valid on the server
        authApi.verifySession().catch(() => {
          // Session invalid – log out
          logout()
        })
      } catch {
        logout()
      }
    }
    setLoading(false)
  }, [logout, fetchStudentId])

  const login = async (username: string, password: string) => {
    try {
      const res = await authApi.login({ username, password })
      if (res.data.success && res.data.data) {
        const { accessToken, refreshToken } = res.data.data
        localStorage.setItem('accessToken', accessToken)
        localStorage.setItem('refreshToken', refreshToken)

        // Decode basic info from token payload
        try {
          const payload = JSON.parse(new TextDecoder().decode(Uint8Array.from(atob(accessToken.split('.')[1]), c => c.charCodeAt(0))))
          let u: User = {
            id: Number(payload.sub || payload.userId || payload.nameid || 0),
            username: payload.unique_name || payload.username || username,
            fullName: payload.fullname || payload.name || payload.fullName || username,
            role: payload.role || payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] || 'USER',
          }
          localStorage.setItem('user', JSON.stringify(u))
          setUser(u)
          // Fetch studentId in background for student users
          if (u.role?.toUpperCase() === 'STUDENT') {
            fetchStudentId(u).then(updated => setUser(updated))
          }
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
