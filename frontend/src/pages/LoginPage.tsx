import { useState, FormEvent } from 'react'
import { useNavigate, Link } from 'react-router-dom'
import { useAuth } from '../contexts/AuthContext'
import { authApi } from '../api/auth'
import './LoginPage.css'

export default function LoginPage() {
  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)
  const { login } = useAuth()
  const navigate = useNavigate()

  // Forgot password state
  const [showForgot, setShowForgot] = useState(false)
  const [forgotUsername, setForgotUsername] = useState('')
  const [forgotMsg, setForgotMsg] = useState('')
  const [forgotError, setForgotError] = useState('')
  const [forgotLoading, setForgotLoading] = useState(false)

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
              <a href="#" className="login-forgot" onClick={e => { e.preventDefault(); setShowForgot(true); setForgotMsg(''); setForgotError(''); setForgotUsername('') }}>
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
          Bạn chưa có tài khoản? <Link to="/register">Đăng ký ngay</Link>
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

      {/* Forgot Password Modal */}
      {showForgot && (
        <div className="login-modal-overlay" onClick={() => setShowForgot(false)}>
          <div className="login-modal" onClick={e => e.stopPropagation()}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
              <h3 style={{ margin: 0 }}>Quên mật khẩu</h3>
              <button onClick={() => setShowForgot(false)} style={{ background: 'none', border: 'none', cursor: 'pointer', fontSize: 20 }}>✕</button>
            </div>
            <p style={{ fontSize: 13, color: '#666', marginBottom: 16 }}>
              Nhập tên đăng nhập của bạn. Hệ thống sẽ gửi yêu cầu đặt lại mật khẩu tới quản trị viên.
            </p>
            {forgotMsg && <div className="alert alert-success" style={{ marginBottom: 12 }}><span className="material-icons" style={{ fontSize: 18 }}>check_circle</span> {forgotMsg}</div>}
            {forgotError && <div className="alert alert-error" style={{ marginBottom: 12 }}><span className="material-icons" style={{ fontSize: 18 }}>error_outline</span> {forgotError}</div>}
            <form onSubmit={async (e: FormEvent) => {
              e.preventDefault()
              if (!forgotUsername.trim()) { setForgotError('Vui lòng nhập tên đăng nhập'); return }
              setForgotLoading(true); setForgotError(''); setForgotMsg('')
              try {
                await authApi.forgotPassword(forgotUsername.trim())
                setForgotMsg('Yêu cầu đặt lại mật khẩu đã được gửi. Vui lòng liên hệ quản trị viên để nhận mật khẩu mới.')
              } catch (err: any) {
                setForgotError(err.response?.data?.message || 'Có lỗi xảy ra, vui lòng thử lại')
              } finally { setForgotLoading(false) }
            }}>
              <div className="form-group">
                <label className="form-label">Tên đăng nhập</label>
                <div className="input-group">
                  <span className="material-icons input-icon">person</span>
                  <input className="form-control" type="text" placeholder="Nhập tên đăng nhập" value={forgotUsername} onChange={e => setForgotUsername(e.target.value)} autoFocus />
                </div>
              </div>
              <button type="submit" className="btn btn-primary" style={{ width: '100%' }} disabled={forgotLoading}>
                {forgotLoading ? 'Đang gửi...' : 'Gửi yêu cầu'}
              </button>
            </form>
          </div>
        </div>
      )}
    </div>
  )
}
