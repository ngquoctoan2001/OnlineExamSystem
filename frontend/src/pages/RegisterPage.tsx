import { useState, FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { authApi } from '../api/auth'
import type { RegisterAuthRequest } from '../types/api'
import './LoginPage.css'

export default function RegisterPage() {
  const navigate = useNavigate()
  const [form, setForm] = useState<RegisterAuthRequest>({
    username: '',
    email: '',
    fullName: '',
    password: '',
    confirmPassword: '',
    role: 'Student',
  })
  const [showPassword, setShowPassword] = useState(false)
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)
  const [success, setSuccess] = useState(false)

  const update = (field: keyof RegisterAuthRequest, value: string) =>
    setForm(prev => ({ ...prev, [field]: value }))

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    setError('')

    if (!form.username.trim() || !form.email.trim() || !form.fullName.trim() || !form.password || !form.confirmPassword) {
      setError('Vui lòng điền đầy đủ thông tin')
      return
    }
    if (form.password.length < 6) {
      setError('Mật khẩu phải có ít nhất 6 ký tự')
      return
    }
    if (form.password !== form.confirmPassword) {
      setError('Mật khẩu xác nhận không khớp')
      return
    }

    setLoading(true)
    try {
      const res = await authApi.register(form)
      if (res.data.success) {
        setSuccess(true)
      } else {
        setError(res.data.message || 'Đăng ký thất bại')
      }
    } catch (err: unknown) {
      const e = err as { response?: { data?: { message?: string } } }
      setError(e.response?.data?.message || 'Có lỗi xảy ra, vui lòng thử lại')
    } finally {
      setLoading(false)
    }
  }

  if (success) {
    return (
      <div className="login-outer">
        <div className="login-card" style={{ textAlign: 'center' }}>
          <span className="material-icons" style={{ fontSize: 56, color: '#22c55e', marginBottom: 12 }}>check_circle</span>
          <h2 style={{ marginBottom: 8 }}>Đăng ký thành công!</h2>
          <p style={{ fontSize: 13, color: 'var(--text-secondary)', marginBottom: 20 }}>
            Tài khoản của bạn đã được tạo. Vui lòng đăng nhập để tiếp tục.
          </p>
          <button className="btn btn-primary" style={{ width: '100%' }} onClick={() => navigate('/login')}>
            <span className="material-icons" style={{ fontSize: 20 }}>login</span> Đăng nhập ngay
          </button>
        </div>
      </div>
    )
  }

  return (
    <div className="login-outer">
      <div className="login-card">
        {/* Brand */}
        <div className="login-brand">
          <div className="login-brand-icon">
            <span className="material-icons">rocket_launch</span>
          </div>
          <div>
            <div className="login-brand-name">Antigravity</div>
            <div className="login-brand-sub">Hệ Thống Thi Trực Tuyến</div>
          </div>
        </div>

        <div className="login-divider" />

        <h2 className="login-title">Đăng ký tài khoản</h2>
        <p className="login-subtitle">Tạo tài khoản mới để sử dụng hệ thống</p>

        {error && (
          <div className="alert alert-error">
            <span className="material-icons" style={{ fontSize: 18 }}>error_outline</span>
            {error}
          </div>
        )}

        <form onSubmit={handleSubmit} className="login-form">
          <div className="form-group">
            <label className="form-label">Họ và tên</label>
            <div className="input-group">
              <span className="material-icons input-icon">badge</span>
              <input
                className="form-control"
                type="text"
                placeholder="Nhập họ và tên"
                value={form.fullName}
                onChange={e => update('fullName', e.target.value)}
                autoFocus
              />
            </div>
          </div>

          <div className="form-group">
            <label className="form-label">Tên đăng nhập</label>
            <div className="input-group">
              <span className="material-icons input-icon">person</span>
              <input
                className="form-control"
                type="text"
                placeholder="Nhập tên đăng nhập"
                value={form.username}
                onChange={e => update('username', e.target.value)}
                autoComplete="username"
              />
            </div>
          </div>

          <div className="form-group">
            <label className="form-label">Email</label>
            <div className="input-group">
              <span className="material-icons input-icon">email</span>
              <input
                className="form-control"
                type="email"
                placeholder="Nhập email"
                value={form.email}
                onChange={e => update('email', e.target.value)}
                autoComplete="email"
              />
            </div>
          </div>

          <div className="form-group">
            <label className="form-label">Vai trò</label>
            <div className="input-group">
              <span className="material-icons input-icon">school</span>
              <select
                className="form-control"
                value={form.role}
                onChange={e => update('role', e.target.value)}
              >
                <option value="Student">Học sinh</option>
                <option value="Teacher">Giáo viên</option>
              </select>
            </div>
          </div>

          <div className="form-group">
            <label className="form-label">Mật khẩu</label>
            <div className="input-group">
              <span className="material-icons input-icon">lock</span>
              <input
                className="form-control"
                type={showPassword ? 'text' : 'password'}
                placeholder="Nhập mật khẩu (tối thiểu 6 ký tự)"
                value={form.password}
                onChange={e => update('password', e.target.value)}
                autoComplete="new-password"
              />
              <span className="material-icons input-icon-r" onClick={() => setShowPassword(!showPassword)}>
                {showPassword ? 'visibility_off' : 'visibility'}
              </span>
            </div>
          </div>

          <div className="form-group">
            <label className="form-label">Xác nhận mật khẩu</label>
            <div className="input-group">
              <span className="material-icons input-icon">lock</span>
              <input
                className="form-control"
                type={showPassword ? 'text' : 'password'}
                placeholder="Nhập lại mật khẩu"
                value={form.confirmPassword}
                onChange={e => update('confirmPassword', e.target.value)}
                autoComplete="new-password"
              />
            </div>
          </div>

          <button
            type="submit"
            className="btn btn-primary btn-lg"
            style={{ width: '100%', marginTop: 8 }}
            disabled={loading}
          >
            {loading ? (
              <><div className="spinner" style={{ width: 18, height: 18, borderWidth: 2 }} /> Đang đăng ký...</>
            ) : (
              <><span className="material-icons" style={{ fontSize: 20 }}>person_add</span> Đăng ký</>
            )}
          </button>
        </form>

        <p className="login-contact">
          Đã có tài khoản? <Link to="/login">Đăng nhập</Link>
        </p>

        <div className="login-powered">
          Powered by Antigravity OS • © {new Date().getFullYear()}
        </div>
      </div>
    </div>
  )
}
