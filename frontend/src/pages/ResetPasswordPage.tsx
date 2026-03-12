import { useState, useEffect, FormEvent } from 'react'
import { Link, useSearchParams, useNavigate } from 'react-router-dom'
import { authApi } from '../api/auth'
import './LoginPage.css'

export default function ResetPasswordPage() {
  const [searchParams] = useSearchParams()
  const navigate = useNavigate()
  const token = searchParams.get('token') || ''

  const [newPassword, setNewPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)
  const [success, setSuccess] = useState(false)

  useEffect(() => {
    if (!token) {
      setError('Link đặt lại mật khẩu không hợp lệ hoặc đã hết hạn.')
    }
  }, [token])

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    setError('')

    if (!newPassword || !confirmPassword) {
      setError('Vui lòng nhập mật khẩu mới')
      return
    }
    if (newPassword.length < 6) {
      setError('Mật khẩu phải có ít nhất 6 ký tự')
      return
    }
    if (newPassword !== confirmPassword) {
      setError('Mật khẩu xác nhận không khớp')
      return
    }

    setLoading(true)
    try {
      const res = await authApi.resetPassword({ token, newPassword, confirmPassword })
      if (res.data.success) {
        setSuccess(true)
      } else {
        setError(res.data.message || 'Đặt lại mật khẩu thất bại')
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
          <h2 style={{ marginBottom: 8 }}>Đặt lại mật khẩu thành công!</h2>
          <p style={{ fontSize: 13, color: 'var(--text-secondary)', marginBottom: 20 }}>
            Mật khẩu của bạn đã được cập nhật. Vui lòng đăng nhập với mật khẩu mới.
          </p>
          <button className="btn btn-primary" style={{ width: '100%' }} onClick={() => navigate('/login')}>
            <span className="material-icons" style={{ fontSize: 20 }}>login</span> Đăng nhập
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

        <h2 className="login-title">Đặt lại mật khẩu</h2>
        <p className="login-subtitle">Nhập mật khẩu mới cho tài khoản của bạn</p>

        {error && (
          <div className="alert alert-error">
            <span className="material-icons" style={{ fontSize: 18 }}>error_outline</span>
            {error}
          </div>
        )}

        {token ? (
          <form onSubmit={handleSubmit} className="login-form">
            <div className="form-group">
              <label className="form-label">Mật khẩu mới</label>
              <div className="input-group">
                <span className="material-icons input-icon">lock</span>
                <input
                  className="form-control"
                  type={showPassword ? 'text' : 'password'}
                  placeholder="Nhập mật khẩu mới (tối thiểu 6 ký tự)"
                  value={newPassword}
                  onChange={e => setNewPassword(e.target.value)}
                  autoComplete="new-password"
                  autoFocus
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
                  placeholder="Nhập lại mật khẩu mới"
                  value={confirmPassword}
                  onChange={e => setConfirmPassword(e.target.value)}
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
                <><div className="spinner" style={{ width: 18, height: 18, borderWidth: 2 }} /> Đang xử lý...</>
              ) : (
                <><span className="material-icons" style={{ fontSize: 20 }}>vpn_key</span> Đặt lại mật khẩu</>
              )}
            </button>
          </form>
        ) : (
          <p style={{ fontSize: 13, color: 'var(--text-secondary)', textAlign: 'center', marginTop: 12 }}>
            Vui lòng sử dụng link đặt lại mật khẩu được gửi qua email.
          </p>
        )}

        <p className="login-contact">
          <Link to="/login">← Quay lại đăng nhập</Link>
        </p>

        <div className="login-powered">
          Powered by Antigravity OS • © {new Date().getFullYear()}
        </div>
      </div>
    </div>
  )
}
