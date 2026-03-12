import { useState, useEffect, useCallback, useRef } from 'react'
import { useLocation } from 'react-router-dom'
import { useAuth } from '../contexts/AuthContext'
import { notificationsApi } from '../api/notifications'
import { authApi } from '../api/auth'

const pageTitles: Record<string, string> = {
  '/':          'Tổng quan',
  '/teachers':  'Quản lý Giáo viên',
  '/students':  'Quản lý Học sinh',
  '/classes':   'Quản lý Lớp học',
  '/subjects':  'Quản lý Môn học',
  '/questions': 'Ngân hàng Câu hỏi',
  '/exams':     'Quản lý Kỳ thi',
  '/results':   'Kết quả & Thống kê',
  '/grading':   'Chấm bài',
  '/activity-logs': 'Nhật ký hoạt động',
  '/notifications': 'Thông báo',
  '/system-admin':  'Quản trị Hệ thống',
  '/profile':       'Hồ sơ cá nhân',
}

const studentPageTitles: Record<string, string> = {
  '/':          'Tổng quan',
  '/exams':     'Phòng thi',
  '/results':   'Lịch sử bài thi',
  '/review':    'Xem lại bài thi',
  '/notifications': 'Thông báo',
  '/profile':       'Hồ sơ cá nhân',
}

interface Notification {
  id: number
  title: string
  message: string
  isRead: boolean
  createdAt: string
}

export default function Header({ onMenuClick }: { onMenuClick?: () => void }) {
  const { pathname } = useLocation()
  const { user } = useAuth()

  const base = '/' + pathname.split('/')[1]
  const isStudent = user?.role?.toUpperCase() === 'STUDENT'
  const title = (isStudent ? studentPageTitles[base] : undefined) || pageTitles[base] || 'Antigravity'

  // ── Notification state ──
  const [notifications, setNotifications] = useState<Notification[]>([])
  const [unreadCount, setUnreadCount] = useState(0)
  const [showNotif, setShowNotif] = useState(false)
  const notifRef = useRef<HTMLDivElement>(null)

  // ── Change Password state ──
  const [showPwdModal, setShowPwdModal] = useState(false)
  const [pwdForm, setPwdForm] = useState({ currentPassword: '', newPassword: '', confirmNewPassword: '' })
  const [pwdMsg, setPwdMsg] = useState('')
  const [pwdLoading, setPwdLoading] = useState(false)

  const fetchNotifications = useCallback(async () => {
    try {
      const res = await notificationsApi.getUserNotifications({ pageSize: 10 })
      const data = res.data?.data
      const items = Array.isArray(data) ? data : (data as any)?.items || []
      setNotifications(items.slice(0, 10))
      if (user?.id) {
        const countRes = await notificationsApi.getUnreadCount(user.id)
        setUnreadCount(countRes.data?.data || 0)
      }
    } catch { /* ignore */ }
  }, [user?.id])

  useEffect(() => {
    fetchNotifications()
    const interval = setInterval(fetchNotifications, 30000)
    return () => clearInterval(interval)
  }, [fetchNotifications])

  // Close dropdown on outside click
  useEffect(() => {
    const handler = (e: MouseEvent) => {
      if (notifRef.current && !notifRef.current.contains(e.target as Node)) setShowNotif(false)
    }
    document.addEventListener('mousedown', handler)
    return () => document.removeEventListener('mousedown', handler)
  }, [])

  const markAsRead = async (id: number) => {
    try {
      await notificationsApi.markAsRead(id)
      setNotifications(prev => prev.map(n => n.id === id ? { ...n, isRead: true } : n))
      setUnreadCount(prev => Math.max(0, prev - 1))
    } catch { /* ignore */ }
  }

  const markAllRead = async () => {
    try {
      await notificationsApi.markAllAsRead()
      setNotifications(prev => prev.map(n => ({ ...n, isRead: true })))
      setUnreadCount(0)
    } catch { /* ignore */ }
  }

  const handleChangePassword = async (e: React.FormEvent) => {
    e.preventDefault()
    setPwdMsg('')
    if (pwdForm.newPassword !== pwdForm.confirmNewPassword) {
      setPwdMsg('Mật khẩu mới không khớp')
      return
    }
    if (pwdForm.newPassword.length < 6) {
      setPwdMsg('Mật khẩu mới phải có ít nhất 6 ký tự')
      return
    }
    setPwdLoading(true)
    try {
      await authApi.changePassword(pwdForm)
      setPwdMsg('Đổi mật khẩu thành công!')
      setTimeout(() => { setShowPwdModal(false); setPwdForm({ currentPassword: '', newPassword: '', confirmNewPassword: '' }); setPwdMsg('') }, 1500)
    } catch (err: any) {
      setPwdMsg(err.response?.data?.message || 'Đổi mật khẩu thất bại')
    } finally { setPwdLoading(false) }
  }

  return (
    <>
      <header className="app-header">
        <button className="menu-toggle" onClick={onMenuClick}>
          <span className="material-icons">menu</span>
        </button>
        <span className="header-title">{title}</span>
        <div className="header-actions">
          {/* Notification Bell */}
          <div ref={notifRef} style={{ position: 'relative' }}>
            <button className="notif-btn" onClick={() => setShowNotif(!showNotif)}>
              <span className="material-icons">notifications</span>
              {unreadCount > 0 && <span className="notif-badge">{unreadCount > 9 ? '9+' : unreadCount}</span>}
            </button>
            {showNotif && (
              <div className="notif-dropdown">
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '12px 16px', borderBottom: '1px solid var(--border)' }}>
                  <strong style={{ fontSize: 14 }}>Thông báo</strong>
                  {unreadCount > 0 && (
                    <button onClick={markAllRead} style={{ fontSize: 12, color: 'var(--primary)', background: 'none', border: 'none', cursor: 'pointer' }}>
                      Đánh dấu tất cả đã đọc
                    </button>
                  )}
                </div>
                {notifications.length === 0 ? (
                  <div style={{ padding: 24, textAlign: 'center', color: 'var(--text-muted)', fontSize: 13 }}>
                    Không có thông báo
                  </div>
                ) : notifications.map(n => (
                  <div
                    key={n.id}
                    onClick={() => !n.isRead && markAsRead(n.id)}
                    style={{
                      padding: '10px 16px', borderBottom: '1px solid var(--border)',
                      background: n.isRead ? 'transparent' : 'rgba(19,127,236,0.04)',
                      cursor: n.isRead ? 'default' : 'pointer'
                    }}
                  >
                    <div style={{ fontWeight: n.isRead ? 400 : 600, fontSize: 13 }}>{n.title}</div>
                    <div style={{ fontSize: 12, color: 'var(--text-muted)', marginTop: 2 }}>{n.message}</div>
                    <div style={{ fontSize: 11, color: 'var(--text-muted)', marginTop: 4 }}>
                      {new Date(n.createdAt).toLocaleString('vi-VN')}
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>

          {/* Change Password Button */}
          <button
            onClick={() => setShowPwdModal(true)}
            style={{ background: 'none', border: 'none', cursor: 'pointer', display: 'flex', alignItems: 'center', color: 'var(--text-secondary)' }}
            title="Đổi mật khẩu"
          >
            <span className="material-icons" style={{ fontSize: 20 }}>lock</span>
          </button>

          <div style={{ display: 'flex', alignItems: 'center', gap: 8, fontSize: 13 }}>
            <span style={{ color: 'var(--text-secondary)' }}>{user?.fullName || user?.username}</span>
          </div>
        </div>
      </header>

      {/* Change Password Modal */}
      {showPwdModal && (
        <div className="modal-overlay" onClick={() => setShowPwdModal(false)}>
          <div className="modal" onClick={e => e.stopPropagation()} style={{ maxWidth: 400 }}>
            <div className="modal-header">
              <h3>Đổi mật khẩu</h3>
              <button className="btn-icon" onClick={() => setShowPwdModal(false)}>
                <span className="material-icons">close</span>
              </button>
            </div>
            <div className="modal-body">
              <form onSubmit={handleChangePassword}>
                <div className="form-group">
                  <label className="form-label">Mật khẩu hiện tại</label>
                  <input
                    type="password"
                    className="form-control"
                    value={pwdForm.currentPassword}
                    onChange={e => setPwdForm(p => ({ ...p, currentPassword: e.target.value }))}
                    required
                  />
                </div>
                <div className="form-group">
                  <label className="form-label">Mật khẩu mới</label>
                  <input
                    type="password"
                    className="form-control"
                    value={pwdForm.newPassword}
                    onChange={e => setPwdForm(p => ({ ...p, newPassword: e.target.value }))}
                    required
                    minLength={6}
                  />
                </div>
                <div className="form-group">
                  <label className="form-label">Xác nhận mật khẩu mới</label>
                  <input
                    type="password"
                    className="form-control"
                    value={pwdForm.confirmNewPassword}
                    onChange={e => setPwdForm(p => ({ ...p, confirmNewPassword: e.target.value }))}
                    required
                  />
                </div>
                {pwdMsg && (
                  <div style={{ padding: '8px 12px', borderRadius: 'var(--radius)', marginBottom: 12, fontSize: 13,
                    background: pwdMsg.includes('thành công') ? 'rgba(34,197,94,0.1)' : 'rgba(239,68,68,0.1)',
                    color: pwdMsg.includes('thành công') ? 'var(--success)' : 'var(--danger)'
                  }}>{pwdMsg}</div>
                )}
                <div style={{ display: 'flex', gap: 8, justifyContent: 'flex-end' }}>
                  <button type="button" className="btn btn-secondary" onClick={() => setShowPwdModal(false)}>Hủy</button>
                  <button type="submit" className="btn btn-primary" disabled={pwdLoading}>
                    {pwdLoading ? 'Đang xử lý...' : 'Đổi mật khẩu'}
                  </button>
                </div>
              </form>
            </div>
          </div>
        </div>
      )}
    </>
  )
}
