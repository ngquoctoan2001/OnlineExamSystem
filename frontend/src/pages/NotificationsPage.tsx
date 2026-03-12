import { useState, useEffect, useCallback } from 'react'
import { useAuth } from '../contexts/AuthContext'
import { notificationsApi } from '../api/notifications'
import { classesApi } from '../api/classes'
import type { ClassResponse, NotificationResponse } from '../types/api'

export default function NotificationsPage() {
  const { user } = useAuth()
  const role = user?.role?.toUpperCase() || ''
  const isAdmin = role === 'ADMIN'
  const isTeacher = role === 'TEACHER'
  const canSend = isAdmin || isTeacher

  const [notifications, setNotifications] = useState<NotificationResponse[]>([])
  const [loading, setLoading] = useState(true)
  const [page, setPage] = useState(1)
  const [total, setTotal] = useState(0)
  const pageSize = 20

  // Send notification modal
  const [showSend, setShowSend] = useState(false)
  const [sendForm, setSendForm] = useState({ title: '', content: '', classId: 0 })
  const [classes, setClasses] = useState<ClassResponse[]>([])
  const [sending, setSending] = useState(false)
  const [sendMsg, setSendMsg] = useState('')
  const [sendError, setSendError] = useState('')

  const fetchNotifications = useCallback(async () => {
    setLoading(true)
    try {
      const res = await notificationsApi.getUserNotifications({ page, pageSize })
      const data = res.data?.data
      if (data && !Array.isArray(data) && (data as any)?.items) {
        setNotifications((data as any).items)
        setTotal((data as any).totalCount || (data as any).items.length)
      } else if (Array.isArray(data)) {
        setNotifications(data)
        setTotal(data.length)
      } else {
        setNotifications([])
        setTotal(0)
      }
    } catch { setNotifications([]); setTotal(0) }
    finally { setLoading(false) }
  }, [page])

  useEffect(() => { fetchNotifications() }, [fetchNotifications])

  const markAsRead = async (id: number) => {
    try {
      await notificationsApi.markAsRead(id)
      setNotifications(prev => prev.map(n => n.id === id ? { ...n, isRead: true } : n))
    } catch { /* ignore */ }
  }

  const markAllRead = async () => {
    try {
      await notificationsApi.markAllAsRead()
      setNotifications(prev => prev.map(n => ({ ...n, isRead: true })))
    } catch { /* ignore */ }
  }

  const deleteNotification = async (id: number) => {
    try {
      await notificationsApi.delete(id)
      fetchNotifications()
    } catch { /* ignore */ }
  }

  const openSendModal = async () => {
    setShowSend(true)
    setSendForm({ title: '', content: '', classId: 0 })
    setSendMsg('')
    setSendError('')
    try {
      const res = await classesApi.getAll(1, 200)
      setClasses(res.data.data?.classes || [])
    } catch { setClasses([]) }
  }

  const handleSend = async () => {
    if (!sendForm.title.trim() || !sendForm.content.trim()) {
      setSendError('Vui lòng nhập tiêu đề và nội dung')
      return
    }
    setSending(true); setSendError(''); setSendMsg('')
    try {
      if (sendForm.classId > 0) {
        await notificationsApi.sendToClass({
          classId: sendForm.classId,
          type: 'GENERAL',
          title: sendForm.title,
          message: sendForm.content,
        })
      } else {
        await notificationsApi.create({
          userId: 0,
          type: 'SYSTEM',
          title: sendForm.title,
          message: sendForm.content,
        })
      }
      setSendMsg('Gửi thông báo thành công!')
      setTimeout(() => { setShowSend(false); fetchNotifications() }, 1200)
    } catch (e: any) {
      setSendError(e.response?.data?.message || 'Lỗi gửi thông báo')
    } finally { setSending(false) }
  }

  const totalPages = Math.ceil(total / pageSize)
  const unreadCount = notifications.filter(n => !n.isRead).length

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 20 }}>
        <div>
          <h2 style={{ marginBottom: 2 }}>Thông báo</h2>
          <p style={{ fontSize: 13 }}>{total} thông báo{unreadCount > 0 ? ` • ${unreadCount} chưa đọc` : ''}</p>
        </div>
        <div style={{ display: 'flex', gap: 8 }}>
          {unreadCount > 0 && (
            <button className="btn" onClick={markAllRead}>
              <span className="material-icons" style={{ fontSize: 16 }}>done_all</span> Đánh dấu tất cả đã đọc
            </button>
          )}
          {canSend && (
            <button className="btn btn-primary" onClick={openSendModal}>
              <span className="material-icons" style={{ fontSize: 16 }}>send</span> Gửi thông báo
            </button>
          )}
        </div>
      </div>

      <div className="card">
        {loading ? (
          <div className="loading-center"><div className="spinner" /></div>
        ) : notifications.length === 0 ? (
          <div className="empty-state">
            <span className="material-icons">notifications_none</span>
            <p>Chưa có thông báo nào</p>
          </div>
        ) : (
          <div>
            {notifications.map(n => (
              <div
                key={n.id}
                style={{
                  padding: '16px 20px',
                  borderBottom: '1px solid var(--border)',
                  background: n.isRead ? 'transparent' : 'rgba(19,127,236,0.03)',
                  display: 'flex',
                  justifyContent: 'space-between',
                  alignItems: 'flex-start',
                  gap: 16,
                }}
              >
                <div style={{ flex: 1 }}>
                  <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                    <span className="material-icons" style={{ fontSize: 18, color: n.isRead ? 'var(--text-muted)' : 'var(--primary)' }}>
                      {n.isRead ? 'notifications_none' : 'notifications_active'}
                    </span>
                    <strong style={{ fontSize: 14 }}>{n.title}</strong>
                    {!n.isRead && <span className="badge badge-blue" style={{ fontSize: 10, padding: '1px 6px' }}>Mới</span>}
                  </div>
                  <p style={{ margin: '4px 0 0 26px', fontSize: 13, color: 'var(--text-secondary)' }}>
                    {n.message}
                  </p>
                  <div style={{ marginLeft: 26, marginTop: 4, fontSize: 11, color: 'var(--text-muted)' }}>
                    {new Date(n.createdAt).toLocaleString('vi-VN')}
                  </div>
                </div>
                <div style={{ display: 'flex', gap: 4 }}>
                  {!n.isRead && (
                    <button className="btn-icon btn" title="Đánh dấu đã đọc" onClick={() => markAsRead(n.id)}>
                      <span className="material-icons" style={{ fontSize: 18 }}>check</span>
                    </button>
                  )}
                  <button className="btn-icon btn" title="Xóa" onClick={() => deleteNotification(n.id)} style={{ color: 'var(--danger)' }}>
                    <span className="material-icons" style={{ fontSize: 18 }}>delete</span>
                  </button>
                </div>
              </div>
            ))}
          </div>
        )}

        {totalPages > 1 && (
          <div className="pagination">
            <button className="page-btn" onClick={() => setPage(p => Math.max(1, p - 1))} disabled={page === 1}>
              <span className="material-icons" style={{ fontSize: 16 }}>chevron_left</span>
            </button>
            {Array.from({ length: Math.min(totalPages, 7) }, (_, i) => (
              <button key={i + 1} className={`page-btn${page === i + 1 ? ' active' : ''}`} onClick={() => setPage(i + 1)}>{i + 1}</button>
            ))}
            <button className="page-btn" onClick={() => setPage(p => Math.min(totalPages, p + 1))} disabled={page === totalPages}>
              <span className="material-icons" style={{ fontSize: 16 }}>chevron_right</span>
            </button>
          </div>
        )}
      </div>

      {/* Send Notification Modal */}
      {showSend && (
        <div className="modal-overlay" onClick={e => e.target === e.currentTarget && setShowSend(false)}>
          <div className="modal" style={{ maxWidth: 500 }}>
            <div className="modal-header">
              <h3>Gửi thông báo</h3>
              <button className="btn btn-icon" onClick={() => setShowSend(false)}><span className="material-icons">close</span></button>
            </div>
            <div className="modal-body">
              {sendMsg && <div className="alert" style={{ background: '#d4edda', color: '#155724', border: '1px solid #c3e6cb', borderRadius: 8, padding: '8px 12px', marginBottom: 12, fontSize: 13 }}>{sendMsg}</div>}
              {sendError && <div className="alert alert-error" style={{ marginBottom: 12 }}><span className="material-icons" style={{ fontSize: 18 }}>error</span>{sendError}</div>}
              <div className="form-group">
                <label className="form-label">Tiêu đề *</label>
                <input className="form-control" value={sendForm.title} onChange={e => setSendForm(f => ({ ...f, title: e.target.value }))} placeholder="Nhập tiêu đề thông báo" />
              </div>
              <div className="form-group">
                <label className="form-label">Nội dung *</label>
                <textarea className="form-control" rows={4} value={sendForm.content} onChange={e => setSendForm(f => ({ ...f, content: e.target.value }))} placeholder="Nhập nội dung thông báo" />
              </div>
              <div className="form-group">
                <label className="form-label">Gửi cho lớp (tùy chọn)</label>
                <select className="form-control" value={sendForm.classId} onChange={e => setSendForm(f => ({ ...f, classId: Number(e.target.value) }))}>
                  <option value={0}>-- Tất cả --</option>
                  {classes.map(c => <option key={c.id} value={c.id}>{c.name} ({c.code})</option>)}
                </select>
              </div>
            </div>
            <div className="modal-footer">
              <button className="btn" onClick={() => setShowSend(false)}>Hủy</button>
              <button className="btn btn-primary" onClick={handleSend} disabled={sending}>
                {sending ? 'Đang gửi...' : 'Gửi thông báo'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
