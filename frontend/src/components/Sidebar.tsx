import { NavLink, useNavigate } from 'react-router-dom'
import { useAuth } from '../contexts/AuthContext'

interface NavItem {
  to: string
  icon: string
  label: string
  roles?: string[]
}

const navItems: NavItem[] = [
  { to: '/',         icon: 'dashboard',         label: 'Tổng quan' },
  { to: '/teachers', icon: 'supervisor_account', label: 'Giáo viên', roles: ['ADMIN'] },
  { to: '/students', icon: 'person',             label: 'Học sinh',  roles: ['ADMIN', 'TEACHER'] },
  { to: '/classes',  icon: 'groups',             label: 'Lớp học',   roles: ['ADMIN', 'TEACHER'] },
  { to: '/questions',icon: 'database',           label: 'Ngân hàng câu hỏi', roles: ['ADMIN', 'TEACHER'] },
  { to: '/exams',    icon: 'edit_calendar',      label: 'Kỳ thi' },
  { to: '/results',  icon: 'assessment',         label: 'Kết quả' },
]

export default function Sidebar() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()

  const handleLogout = () => {
    logout()
    navigate('/login')
  }

  const role = user?.role?.toUpperCase() || 'USER'
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
        {visibleItems.map(item => (
          <NavLink
            key={item.to}
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
            <div className="user-role">{role}</div>
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
