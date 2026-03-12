import { useState, useEffect, FormEvent } from 'react'
import { useAuth } from '../contexts/AuthContext'
import { authApi } from '../api/auth'
import { studentsApi } from '../api/students'
import { teachingAssignmentsApi } from '../api/teachingAssignments'
import { teachersApi } from '../api/teachers'

interface TeacherProfile {
  id: number
  fullName: string
  employeeId: string
  department: string
  email: string
  username: string
}

interface StudentProfile {
  id: number
  fullName: string
  studentCode: string
  rollNumber: string
  email: string
  username: string
}

interface ClassInfo {
  id: number
  name: string
  code: string
  grade: number
  homeroomTeacherName: string
}

interface TeachingAssignment {
  id: number
  className: string
  subjectName: string
  academicYear: string
  semester: number
}

export default function ProfilePage() {
  const { user } = useAuth()
  const role = user?.role?.toUpperCase() || ''
  const isStudent = role === 'STUDENT'
  const isTeacher = role === 'TEACHER'

  const [profile, setProfile] = useState<any>(null)
  const [classes, setClasses] = useState<ClassInfo[]>([])
  const [assignments, setAssignments] = useState<TeachingAssignment[]>([])
  const [loading, setLoading] = useState(true)

  // Change password state
  const [showChangePwd, setShowChangePwd] = useState(false)
  const [pwdForm, setPwdForm] = useState({ currentPassword: '', newPassword: '', confirmNewPassword: '' })
  const [pwdLoading, setPwdLoading] = useState(false)
  const [pwdError, setPwdError] = useState('')
  const [pwdSuccess, setPwdSuccess] = useState('')
  const [showPwdFields, setShowPwdFields] = useState(false)

  const openChangePwd = () => {
    setPwdForm({ currentPassword: '', newPassword: '', confirmNewPassword: '' })
    setPwdError('')
    setPwdSuccess('')
    setShowChangePwd(true)
  }

  const handleChangePassword = async (e: FormEvent) => {
    e.preventDefault()
    setPwdError('')
    setPwdSuccess('')

    if (!pwdForm.currentPassword || !pwdForm.newPassword || !pwdForm.confirmNewPassword) {
      setPwdError('Vui lòng điền đầy đủ thông tin')
      return
    }
    if (pwdForm.newPassword.length < 6) {
      setPwdError('Mật khẩu mới phải có ít nhất 6 ký tự')
      return
    }
    if (pwdForm.newPassword !== pwdForm.confirmNewPassword) {
      setPwdError('Mật khẩu xác nhận không khớp')
      return
    }

    setPwdLoading(true)
    try {
      const res = await authApi.changePassword(pwdForm)
      if (res.data.success) {
        setPwdSuccess('Đổi mật khẩu thành công!')
        setPwdForm({ currentPassword: '', newPassword: '', confirmNewPassword: '' })
      } else {
        setPwdError(res.data.message || 'Đổi mật khẩu thất bại')
      }
    } catch (err: unknown) {
      const e = err as { response?: { data?: { message?: string } } }
      setPwdError(e.response?.data?.message || 'Có lỗi xảy ra, vui lòng thử lại')
    } finally {
      setPwdLoading(false)
    }
  }

  useEffect(() => {
    const fetchProfile = async () => {
      setLoading(true)
      try {
        if (isStudent) {
          const res = await studentsApi.getMe()
          setProfile(res.data?.data)
        } else if (isTeacher) {
          const res = await teachersApi.getMe()
          setProfile(res.data?.data)
          if (res.data?.data?.id) {
            const assignRes = await teachingAssignmentsApi.getByTeacher(res.data.data.id).catch(() => ({ data: { data: [] } }))
            const aList = assignRes.data?.data || []
            setAssignments(Array.isArray(aList) ? aList : [])

            const classRes = await teachersApi.getTeacherClasses(res.data.data.id).catch(() => ({ data: { data: [] } }))
            const cList = classRes.data?.data?.classes || classRes.data?.data || []
            setClasses(Array.isArray(cList) ? cList : [])
          }
        }
      } catch { /* ignore */ }
      finally { setLoading(false) }
    }
    fetchProfile()
  }, [isStudent, isTeacher])

  if (loading) return <div className="loading-center"><div className="spinner" /></div>

  return (
    <div>
      <div style={{ marginBottom: 20 }}>
        <h2 style={{ marginBottom: 2 }}>Hồ sơ cá nhân</h2>
        <p style={{ fontSize: 13 }}>Thông tin tài khoản của bạn</p>
      </div>

      {/* Profile Card */}
      <div className="card" style={{ marginBottom: 20 }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 20 }}>
          <div style={{
            width: 72, height: 72, borderRadius: '50%', background: 'var(--primary)',
            display: 'flex', alignItems: 'center', justifyContent: 'center',
            color: '#fff', fontSize: 28, fontWeight: 700
          }}>
            {(profile?.fullName || user?.fullName || 'U').split(' ').map((w: string) => w[0]).slice(-2).join('').toUpperCase()}
          </div>
          <div>
            <h2 style={{ margin: 0 }}>{profile?.fullName || user?.fullName}</h2>
            <div style={{ fontSize: 13, color: 'var(--text-secondary)', marginTop: 4, display: 'flex', gap: 12 }}>
              <span className="badge badge-blue">{isStudent ? 'Học sinh' : isTeacher ? 'Giáo viên' : 'Quản trị viên'}</span>
              {isStudent && profile?.studentCode && <span>Mã HS: <strong>{profile.studentCode}</strong></span>}
              {isTeacher && profile?.employeeId && <span>Mã GV: <strong>{profile.employeeId}</strong></span>}
            </div>
          </div>
        </div>
      </div>

      {/* Info Grid */}
      <div className="profile-info-grid">
        <div className="card">
          <h3 style={{ margin: '0 0 16px' }}>
            <span className="material-icons" style={{ fontSize: 20, verticalAlign: 'middle', marginRight: 8 }}>info</span>
            Thông tin cơ bản
          </h3>
          <div style={{ display: 'grid', gap: 12 }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', padding: '8px 0', borderBottom: '1px solid var(--border)' }}>
              <span style={{ color: 'var(--text-muted)', fontSize: 13 }}>Tên đăng nhập</span>
              <strong style={{ fontSize: 13 }}>{profile?.username || user?.username}</strong>
            </div>
            <div style={{ display: 'flex', justifyContent: 'space-between', padding: '8px 0', borderBottom: '1px solid var(--border)' }}>
              <span style={{ color: 'var(--text-muted)', fontSize: 13 }}>Họ và tên</span>
              <strong style={{ fontSize: 13 }}>{profile?.fullName || user?.fullName}</strong>
            </div>
            <div style={{ display: 'flex', justifyContent: 'space-between', padding: '8px 0', borderBottom: '1px solid var(--border)' }}>
              <span style={{ color: 'var(--text-muted)', fontSize: 13 }}>Email</span>
              <strong style={{ fontSize: 13 }}>{profile?.email || '-'}</strong>
            </div>
            {isTeacher && profile?.department && (
              <div style={{ display: 'flex', justifyContent: 'space-between', padding: '8px 0', borderBottom: '1px solid var(--border)' }}>
                <span style={{ color: 'var(--text-muted)', fontSize: 13 }}>Bộ môn</span>
                <strong style={{ fontSize: 13 }}>{profile.department}</strong>
              </div>
            )}
            {isStudent && profile?.rollNumber && (
              <div style={{ display: 'flex', justifyContent: 'space-between', padding: '8px 0', borderBottom: '1px solid var(--border)' }}>
                <span style={{ color: 'var(--text-muted)', fontSize: 13 }}>Số thứ tự</span>
                <strong style={{ fontSize: 13 }}>{profile.rollNumber}</strong>
              </div>
            )}
          </div>
        </div>

        <div className="card">
          <h3 style={{ margin: '0 0 16px' }}>
            <span className="material-icons" style={{ fontSize: 20, verticalAlign: 'middle', marginRight: 8 }}>security</span>
            Bảo mật
          </h3>
          <p style={{ fontSize: 13, color: 'var(--text-secondary)', marginBottom: 12 }}>
            Quản lý mật khẩu và bảo mật tài khoản
          </p>
          <button className="btn btn-primary" onClick={openChangePwd}>
            <span className="material-icons" style={{ fontSize: 18, verticalAlign: 'middle', marginRight: 6 }}>vpn_key</span>
            Đổi mật khẩu
          </button>
          <div style={{ padding: 12, background: 'var(--bg)', borderRadius: 8, fontSize: 13, marginTop: 12 }}>
            <span className="material-icons" style={{ fontSize: 16, verticalAlign: 'middle', marginRight: 8, color: '#22c55e' }}>verified_user</span>
            Tài khoản đang hoạt động
          </div>
        </div>
      </div>

      {/* Teacher-specific: Teaching Assignments */}
      {isTeacher && assignments.length > 0 && (
        <div className="card" style={{ marginBottom: 20 }}>
          <h3 style={{ margin: '0 0 16px' }}>
            <span className="material-icons" style={{ fontSize: 20, verticalAlign: 'middle', marginRight: 8 }}>school</span>
            Phân công giảng dạy ({assignments.length})
          </h3>
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>#</th>
                  <th>Lớp</th>
                  <th>Môn học</th>
                  <th>Năm học</th>
                  <th>Học kỳ</th>
                </tr>
              </thead>
              <tbody>
                {assignments.map((a, idx) => (
                  <tr key={a.id}>
                    <td style={{ color: 'var(--text-muted)' }}>{idx + 1}</td>
                    <td style={{ fontWeight: 500 }}>{a.className}</td>
                    <td><span className="badge badge-green">{a.subjectName}</span></td>
                    <td>{a.academicYear}</td>
                    <td>HK{a.semester}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* Teacher-specific: Classes */}
      {isTeacher && classes.length > 0 && (
        <div className="card">
          <h3 style={{ margin: '0 0 16px' }}>
            <span className="material-icons" style={{ fontSize: 20, verticalAlign: 'middle', marginRight: 8 }}>groups</span>
            Lớp đang dạy ({classes.length})
          </h3>
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(200px, 1fr))', gap: 12 }}>
            {classes.map((c: any) => (
              <div key={c.id || c.classId} style={{ padding: 16, background: 'var(--bg)', borderRadius: 8, textAlign: 'center' }}>
                <div style={{ fontSize: 16, fontWeight: 600 }}>{c.name || c.className}</div>
                <div style={{ fontSize: 12, color: 'var(--text-muted)', marginTop: 4 }}>{c.code || ''} • Khối {c.grade}</div>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Change Password Modal */}
      {showChangePwd && (
        <div style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.4)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 1000 }}
          onClick={() => setShowChangePwd(false)}>
          <div style={{ background: 'var(--surface)', borderRadius: 12, padding: '24px 28px', width: '100%', maxWidth: 400, boxShadow: '0 8px 32px rgba(0,0,0,0.2)' }}
            onClick={e => e.stopPropagation()}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
              <h3 style={{ margin: 0 }}>Đổi mật khẩu</h3>
              <button onClick={() => setShowChangePwd(false)} style={{ background: 'none', border: 'none', cursor: 'pointer', fontSize: 20 }}>✕</button>
            </div>

            {pwdSuccess && (
              <div className="alert alert-success" style={{ marginBottom: 12 }}>
                <span className="material-icons" style={{ fontSize: 18 }}>check_circle</span> {pwdSuccess}
              </div>
            )}
            {pwdError && (
              <div className="alert alert-error" style={{ marginBottom: 12 }}>
                <span className="material-icons" style={{ fontSize: 18 }}>error_outline</span> {pwdError}
              </div>
            )}

            <form onSubmit={handleChangePassword}>
              <div className="form-group" style={{ marginBottom: 12 }}>
                <label className="form-label">Mật khẩu hiện tại</label>
                <div className="input-group">
                  <span className="material-icons input-icon">lock</span>
                  <input
                    className="form-control"
                    type={showPwdFields ? 'text' : 'password'}
                    placeholder="Nhập mật khẩu hiện tại"
                    value={pwdForm.currentPassword}
                    onChange={e => setPwdForm(p => ({ ...p, currentPassword: e.target.value }))}
                    autoFocus
                  />
                </div>
              </div>
              <div className="form-group" style={{ marginBottom: 12 }}>
                <label className="form-label">Mật khẩu mới</label>
                <div className="input-group">
                  <span className="material-icons input-icon">vpn_key</span>
                  <input
                    className="form-control"
                    type={showPwdFields ? 'text' : 'password'}
                    placeholder="Nhập mật khẩu mới (tối thiểu 6 ký tự)"
                    value={pwdForm.newPassword}
                    onChange={e => setPwdForm(p => ({ ...p, newPassword: e.target.value }))}
                  />
                  <span className="material-icons input-icon-r" onClick={() => setShowPwdFields(!showPwdFields)}>
                    {showPwdFields ? 'visibility_off' : 'visibility'}
                  </span>
                </div>
              </div>
              <div className="form-group" style={{ marginBottom: 16 }}>
                <label className="form-label">Xác nhận mật khẩu mới</label>
                <div className="input-group">
                  <span className="material-icons input-icon">vpn_key</span>
                  <input
                    className="form-control"
                    type={showPwdFields ? 'text' : 'password'}
                    placeholder="Nhập lại mật khẩu mới"
                    value={pwdForm.confirmNewPassword}
                    onChange={e => setPwdForm(p => ({ ...p, confirmNewPassword: e.target.value }))}
                  />
                </div>
              </div>
              <button type="submit" className="btn btn-primary" style={{ width: '100%' }} disabled={pwdLoading}>
                {pwdLoading ? 'Đang xử lý...' : 'Đổi mật khẩu'}
              </button>
            </form>
          </div>
        </div>
      )}
    </div>
  )
}
