import { NavLink, useNavigate } from 'react-router-dom'
import { useAuth } from '../contexts/AuthContext'

interface NavItem {
  to: string
  icon: string
  label: string
  roles?: string[]
}

const navItems: NavItem[] = [
  { to: '/',         icon: 'dashboard',         label: 'Tổng quan', roles: ['ADMIN', 'TEACHER', 'STUDENT'] },
  { to: '/teachers', icon: 'supervisor_account', label: 'Giáo viên', roles: ['ADMIN'] },
  { to: '/students', icon: 'person',             label: 'Học sinh',  roles: ['ADMIN', 'TEACHER'] },
  { to: '/classes',  icon: 'groups',             label: 'Lớp học',   roles: ['ADMIN', 'TEACHER'] },
  { to: '/subjects', icon: 'menu_book',          label: 'Môn học',   roles: ['ADMIN'] },
  { to: '/questions',icon: 'quiz',              label: 'Câu hỏi', roles: ['ADMIN', 'TEACHER'] },
  { to: '/tags',     icon: 'label',             label: 'Tags',    roles: ['ADMIN', 'TEACHER'] },
  { to: '/users',    icon: 'manage_accounts',   label: 'Tài khoản', roles: ['ADMIN'] },
  { to: '/exams',    icon: 'school',             label: 'Phòng thi',  roles: ['STUDENT'] },
  { to: '/exams',    icon: 'edit_calendar',      label: 'Kỳ thi',    roles: ['ADMIN', 'TEACHER'] },
  { to: '/results',  icon: 'assessment',         label: 'Kết quả',   roles: ['ADMIN', 'TEACHER'] },
  { to: '/grading',  icon: 'rate_review',        label: 'Chấm bài',  roles: ['ADMIN', 'TEACHER'] },
  { to: '/gradebook', icon: 'grade',              label: 'Bảng điểm', roles: ['ADMIN', 'TEACHER', 'STUDENT'] },
  { to: '/reports',   icon: 'analytics',          label: 'Báo cáo',   roles: ['ADMIN', 'TEACHER'] },
  { to: '/notifications', icon: 'notifications', label: 'Thông báo', roles: ['ADMIN', 'TEACHER', 'STUDENT'] },
  { to: '/activity-logs', icon: 'history',      label: 'Nhật ký',   roles: ['ADMIN'] },
  { to: '/system-admin',  icon: 'admin_panel_settings', label: 'Quản trị hệ thống', roles: ['ADMIN'] },
  { to: '/results',  icon: 'history',            label: 'Lịch sử bài thi', roles: ['STUDENT'] },
  { to: '/profile',  icon: 'account_circle',     label: 'Hồ sơ cá nhân', roles: ['TEACHER', 'STUDENT'] },
]

export default function Sidebar() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()

  const handleLogout = () => {
    logout()
    navigate('/login')
  }

  const role = user?.role?.toUpperCase() || 'USER'
  const roleLabel: Record<string, string> = { ADMIN: 'Quản trị viên', TEACHER: 'Giáo viên', STUDENT: 'Học sinh' }
  const visibleItems = navItems.filter(item => !item.roles || item.roles.includes(role))

  const initials = user?.fullName
    ? user.fullName.split(' ').map(w => w[0]).slice(-2).join('').toUpperCase()
    : (user?.username?.[0] || 'A').toUpperCase()

  return (
    <nav className="sidebar">
      <a className="sidebar-brand" href="/">
        <div className="brand-icon">
          <span className="material-icons">rocket_launch</span>
        </div>
        <div className="brand-text">
          <span className="brand-name">Antigravity</span>
          <span className="brand-sub">Online Exam</span>
        </div>
      </a>

      <div className="sidebar-nav">
        <div className="nav-section">Chính</div>
        {visibleItems.map((item, idx) => (
          <NavLink
            key={item.to + '-' + idx}
            to={item.to}
            end={item.to === '/'}
            className={({ isActive }) => `nav-item${isActive ? ' active' : ''}`}
          >
            <span className="material-icons">{item.icon}</span>
            {item.label}
          </NavLink>
        ))}
      </div>

      <div className="sidebar-footer">
        <div className="user-info">
          <div className="user-avatar">{initials}</div>
          <div className="user-details">
            <div className="user-name">{user?.fullName || user?.username}</div>
            <div className="user-role">{roleLabel[role] || role}</div>
          </div>
        </div>
        <button className="nav-item" onClick={handleLogout} style={{ marginTop: 4 }}>
          <span className="material-icons">logout</span>
          Đăng xuất
        </button>
      </div>
    </nav>
  )
}
