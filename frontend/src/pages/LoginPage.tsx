import { useState, FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '../contexts/AuthContext'
import './LoginPage.css'

export default function LoginPage() {
  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)
  const { login } = useAuth()
  const navigate = useNavigate()

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    if (!username.trim() || !password.trim()) {
      setError('Vui lòng nhập tên đăng nhập và mật khẩu')
      return
    }
    setError('')
    setLoading(true)
    const result = await login(username.trim(), password)
    setLoading(false)
    if (result.success) {
      navigate('/')
    } else {
      setError(result.message)
    }
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

        <h2 className="login-title">Đăng nhập</h2>
        <p className="login-subtitle">Chào mừng bạn trở lại</p>

        {error && (
          <div className="alert alert-error">
            <span className="material-icons" style={{ fontSize: 18 }}>error_outline</span>
            {error}
          </div>
        )}

        <form onSubmit={handleSubmit} className="login-form">
          <div className="form-group">
            <label className="form-label">Tên đăng nhập</label>
            <div className="input-group">
              <span className="material-icons input-icon">person</span>
              <input
                className="form-control"
                type="text"
                placeholder="Nhập tên đăng nhập"
                value={username}
                onChange={e => setUsername(e.target.value)}
                autoComplete="username"
                autoFocus
              />
            </div>
          </div>

          <div className="form-group">
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <label className="form-label">Mật khẩu</label>
              <a href="#" className="login-forgot" onClick={e => e.preventDefault()}>
                Quên mật khẩu?
              </a>
            </div>
            <div className="input-group">
              <span className="material-icons input-icon">lock</span>
              <input
                className="form-control"
                type={showPassword ? 'text' : 'password'}
                placeholder="Nhập mật khẩu"
                value={password}
                onChange={e => setPassword(e.target.value)}
                autoComplete="current-password"
              />
              <span
                className="material-icons input-icon-r"
                onClick={() => setShowPassword(!showPassword)}
              >
                {showPassword ? 'visibility_off' : 'visibility'}
              </span>
            </div>
          </div>

          <button
            type="submit"
            className="btn btn-primary btn-lg"
            style={{ width: '100%', marginTop: 8 }}
            disabled={loading}
          >
            {loading ? (
              <><div className="spinner" style={{ width: 18, height: 18, borderWidth: 2 }} /> Đang đăng nhập...</>
            ) : (
              <><span className="material-icons" style={{ fontSize: 20 }}>arrow_forward</span> Đăng nhập</>
            )}
          </button>
        </form>

        <p className="login-contact">
          Bạn chưa có tài khoản? <a href="#">Liên hệ quản trị viên</a>
        </p>

        <div className="login-footer">
          <span><span className="material-icons" style={{ fontSize: 14 }}>help</span> Trợ giúp</span>
          <span><span className="material-icons" style={{ fontSize: 14 }}>language</span> Tiếng Việt</span>
          <span><span className="material-icons" style={{ fontSize: 14 }}>security</span> Bảo mật</span>
        </div>
        <div className="login-powered">
          Powered by Antigravity OS • © {new Date().getFullYear()}
        </div>
      </div>
    </div>
  )
}
