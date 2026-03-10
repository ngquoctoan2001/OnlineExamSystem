import { useLocation } from 'react-router-dom'
import { useAuth } from '../contexts/AuthContext'

const pageTitles: Record<string, string> = {
  '/':          'Tổng quan',
  '/teachers':  'Quản lý Giáo viên',
  '/students':  'Quản lý Học sinh',
  '/classes':   'Quản lý Lớp học',
  '/questions': 'Ngân hàng Câu hỏi',
  '/exams':     'Quản lý Kỳ thi',
  '/results':   'Kết quả & Thống kê',
}

export default function Header() {
  const { pathname } = useLocation()
  const { user } = useAuth()

  const base = '/' + pathname.split('/')[1]
  const title = pageTitles[base] || 'Antigravity'

  return (
    <header className="app-header">
      <span className="header-title">{title}</span>
      <div className="header-actions">
        <button className="notif-btn">
          <span className="material-icons">notifications</span>
          <span className="notif-badge" />
        </button>
        <div style={{ display: 'flex', alignItems: 'center', gap: 8, fontSize: 13 }}>
          <span style={{ color: 'var(--text-secondary)' }}>{user?.fullName || user?.username}</span>
        </div>
      </div>
    </header>
  )
}
