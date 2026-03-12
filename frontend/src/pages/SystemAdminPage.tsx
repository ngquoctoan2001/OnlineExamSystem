import { useState, useEffect, useCallback } from 'react'
import { adminApi } from '../api/admin'

interface SystemStats {
  totalUsers: number
  totalTeachers: number
  totalStudents: number
  totalClasses: number
  totalExams: number
  totalQuestions: number
  activeExams: number
  totalAttempts: number
}

interface HealthStatus {
  status: string
  database: string
  timestamp: string
  version: string
}

export default function SystemAdminPage() {
  const [stats, setStats] = useState<SystemStats | null>(null)
  const [health, setHealth] = useState<HealthStatus | null>(null)
  const [loading, setLoading] = useState(true)
  const [backupLoading, setBackupLoading] = useState(false)
  const [backupMsg, setBackupMsg] = useState('')
  const [restoreId, setRestoreId] = useState('')
  const [restoreLoading, setRestoreLoading] = useState(false)
  const [restoreMsg, setRestoreMsg] = useState('')

  const fetchData = useCallback(async () => {
    setLoading(true)
    try {
      const [statsRes, healthRes] = await Promise.all([
        adminApi.getSystemStats(),
        adminApi.healthCheck(),
      ])
      setStats(statsRes.data?.data || null)
      setHealth(healthRes.data?.data || null)
    } catch { /* ignore */ }
    finally { setLoading(false) }
  }, [])

  useEffect(() => { fetchData() }, [fetchData])

  const handleBackup = async () => {
    setBackupLoading(true); setBackupMsg('')
    try {
      const res = await adminApi.backupDatabase()
      setBackupMsg(`Backup khởi tạo thành công! ID: ${res.data?.data?.backupId || 'N/A'}`)
    } catch (e: any) {
      setBackupMsg(`Lỗi: ${e.response?.data?.message || 'Không thể tạo backup'}`)
    } finally { setBackupLoading(false) }
  }

  const handleRestore = async () => {
    if (!restoreId.trim()) { setRestoreMsg('Vui lòng nhập Backup ID'); return }
    if (!confirm('Bạn có chắc chắn muốn restore database? Thao tác này không thể hoàn tác.')) return
    setRestoreLoading(true); setRestoreMsg('')
    try {
      const res = await adminApi.restoreDatabase({ backupId: restoreId.trim() })
      setRestoreMsg(res.data?.message || 'Restore đã được khởi tạo')
    } catch (e: any) {
      setRestoreMsg(`Lỗi: ${e.response?.data?.message || 'Không thể restore'}`)
    } finally { setRestoreLoading(false) }
  }

  if (loading) return <div className="loading-center"><div className="spinner" /></div>

  return (
    <div>
      <div style={{ marginBottom: 20 }}>
        <h2 style={{ marginBottom: 2 }}>Quản trị Hệ thống</h2>
        <p style={{ fontSize: 13 }}>Giám sát và quản lý hệ thống</p>
      </div>

      {/* Health Status */}
      <div className="card" style={{ marginBottom: 20 }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
          <h3 style={{ margin: 0 }}>
            <span className="material-icons" style={{ fontSize: 20, verticalAlign: 'middle', marginRight: 8 }}>monitor_heart</span>
            Tình trạng hệ thống
          </h3>
          <button className="btn" onClick={fetchData}><span className="material-icons" style={{ fontSize: 16 }}>refresh</span> Làm mới</button>
        </div>
        {health && (
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: 16 }}>
            <div style={{ padding: 16, background: 'var(--bg)', borderRadius: 8 }}>
              <div style={{ fontSize: 12, color: 'var(--text-muted)', marginBottom: 4 }}>Trạng thái</div>
              <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                <span style={{ width: 10, height: 10, borderRadius: '50%', background: health.status === 'Healthy' ? '#22c55e' : '#ef4444' }} />
                <strong>{health.status === 'Healthy' ? 'Hoạt động tốt' : 'Có vấn đề'}</strong>
              </div>
            </div>
            <div style={{ padding: 16, background: 'var(--bg)', borderRadius: 8 }}>
              <div style={{ fontSize: 12, color: 'var(--text-muted)', marginBottom: 4 }}>Database</div>
              <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                <span style={{ width: 10, height: 10, borderRadius: '50%', background: health.database === 'Connected' ? '#22c55e' : '#ef4444' }} />
                <strong>{health.database === 'Connected' ? 'Đã kết nối' : 'Mất kết nối'}</strong>
              </div>
            </div>
            <div style={{ padding: 16, background: 'var(--bg)', borderRadius: 8 }}>
              <div style={{ fontSize: 12, color: 'var(--text-muted)', marginBottom: 4 }}>Phiên bản</div>
              <strong>{health.version}</strong>
            </div>
            <div style={{ padding: 16, background: 'var(--bg)', borderRadius: 8 }}>
              <div style={{ fontSize: 12, color: 'var(--text-muted)', marginBottom: 4 }}>Cập nhật lúc</div>
              <strong style={{ fontSize: 13 }}>{new Date(health.timestamp).toLocaleString('vi-VN')}</strong>
            </div>
          </div>
        )}
      </div>

      {/* System Stats */}
      {stats && (
        <div className="card" style={{ marginBottom: 20 }}>
          <h3 style={{ margin: '0 0 16px' }}>
            <span className="material-icons" style={{ fontSize: 20, verticalAlign: 'middle', marginRight: 8 }}>analytics</span>
            Thống kê hệ thống
          </h3>
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: 16 }}>
            {[
              { label: 'Tổng tài khoản', value: stats.totalUsers, icon: 'people', color: '#137fec' },
              { label: 'Giáo viên', value: stats.totalTeachers, icon: 'supervisor_account', color: '#8b5cf6' },
              { label: 'Học sinh', value: stats.totalStudents, icon: 'person', color: '#06b6d4' },
              { label: 'Lớp học', value: stats.totalClasses, icon: 'groups', color: '#f59e0b' },
              { label: 'Tổng bài thi', value: stats.totalExams, icon: 'edit_calendar', color: '#10b981' },
              { label: 'Đang thi', value: stats.activeExams, icon: 'play_circle', color: '#ef4444' },
              { label: 'Câu hỏi', value: stats.totalQuestions, icon: 'quiz', color: '#ec4899' },
              { label: 'Lượt thi', value: stats.totalAttempts, icon: 'assignment', color: '#6366f1' },
            ].map(item => (
              <div key={item.label} style={{ padding: 16, background: 'var(--bg)', borderRadius: 8, display: 'flex', alignItems: 'center', gap: 12 }}>
                <div style={{ width: 40, height: 40, borderRadius: 8, background: item.color + '15', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
                  <span className="material-icons" style={{ color: item.color, fontSize: 20 }}>{item.icon}</span>
                </div>
                <div>
                  <div style={{ fontSize: 22, fontWeight: 700 }}>{item.value.toLocaleString('vi-VN')}</div>
                  <div style={{ fontSize: 12, color: 'var(--text-muted)' }}>{item.label}</div>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Backup & Restore */}
      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 20 }}>
        <div className="card">
          <h3 style={{ margin: '0 0 16px' }}>
            <span className="material-icons" style={{ fontSize: 20, verticalAlign: 'middle', marginRight: 8 }}>backup</span>
            Sao lưu Database
          </h3>
          <p style={{ fontSize: 13, color: 'var(--text-secondary)', marginBottom: 16 }}>
            Tạo bản sao lưu database hệ thống. Quá trình này có thể mất vài phút.
          </p>
          {backupMsg && (
            <div style={{ padding: '8px 12px', borderRadius: 8, marginBottom: 12, fontSize: 13, background: backupMsg.startsWith('Lỗi') ? '#fee2e2' : '#d4edda', color: backupMsg.startsWith('Lỗi') ? '#991b1b' : '#155724' }}>
              {backupMsg}
            </div>
          )}
          <button className="btn btn-primary" onClick={handleBackup} disabled={backupLoading}>
            <span className="material-icons" style={{ fontSize: 16 }}>cloud_upload</span>
            {backupLoading ? 'Đang tạo backup...' : 'Tạo Backup'}
          </button>
        </div>

        <div className="card">
          <h3 style={{ margin: '0 0 16px' }}>
            <span className="material-icons" style={{ fontSize: 20, verticalAlign: 'middle', marginRight: 8 }}>restore</span>
            Khôi phục Database
          </h3>
          <p style={{ fontSize: 13, color: 'var(--text-secondary)', marginBottom: 16 }}>
            Khôi phục database từ bản sao lưu. <strong style={{ color: 'var(--danger)' }}>Cảnh báo: Dữ liệu hiện tại sẽ bị ghi đè.</strong>
          </p>
          {restoreMsg && (
            <div style={{ padding: '8px 12px', borderRadius: 8, marginBottom: 12, fontSize: 13, background: restoreMsg.startsWith('Lỗi') ? '#fee2e2' : '#d4edda', color: restoreMsg.startsWith('Lỗi') ? '#991b1b' : '#155724' }}>
              {restoreMsg}
            </div>
          )}
          <div style={{ display: 'flex', gap: 8 }}>
            <input className="form-control" placeholder="Nhập Backup ID" value={restoreId} onChange={e => setRestoreId(e.target.value)} style={{ flex: 1 }} />
            <button className="btn" onClick={handleRestore} disabled={restoreLoading} style={{ color: 'var(--danger)', borderColor: 'var(--danger)' }}>
              <span className="material-icons" style={{ fontSize: 16 }}>settings_backup_restore</span>
              {restoreLoading ? 'Đang khôi phục...' : 'Khôi phục'}
            </button>
          </div>
        </div>
      </div>
    </div>
  )
}
