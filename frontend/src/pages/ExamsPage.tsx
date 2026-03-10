import { useState, useEffect, useCallback } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { examsApi } from '../api/exams'
import { subjectsApi } from '../api/subjects'
import type { ExamResponse, CreateExamRequest, SubjectResponse } from '../types/api'
import { useAuth } from '../contexts/AuthContext'

const emptyForm = (): CreateExamRequest => ({
  title: '',
  subjectId: 0,
  createdBy: 0,
  durationMinutes: 60,
  startTime: '',
  endTime: '',
  description: '',
})

const statusBadge = (status: string) => {
  if (status === 'ACTIVE') return <span className="badge badge-green">Đang hoạt động</span>
  if (status === 'DRAFT')  return <span className="badge badge-gray">Nháp</span>
  return <span className="badge badge-red">Đã đóng</span>
}

export default function ExamsPage() {
  const { user } = useAuth()
  const navigate = useNavigate()
  const [exams, setExams] = useState<ExamResponse[]>([])
  const [total, setTotal] = useState(0)
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [loading, setLoading] = useState(true)
  const [subjects, setSubjects] = useState<SubjectResponse[]>([])
  const [modal, setModal] = useState<'create' | 'edit' | null>(null)
  const [form, setForm] = useState<CreateExamRequest>(emptyForm())
  const [editId, setEditId] = useState<number | null>(null)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const [deleteConfirm, setDeleteConfirm] = useState<number | null>(null)
  const [viewMode, setViewMode] = useState<'card' | 'list'>('card')
  const pageSize = 20

  const fetchExams = useCallback(async () => {
    setLoading(true)
    try {
      if (search.trim()) {
        const res = await examsApi.search(search)
        const data = res.data.data || []
        setExams(Array.isArray(data) ? data : [])
        setTotal(Array.isArray(data) ? data.length : 0)
      } else {
        const res = await examsApi.getAll(page, pageSize)
        const d = res.data.data
        setExams(d?.items || [])
        setTotal(d?.totalCount || 0)
      }
    } catch { setExams([]); setTotal(0) }
    finally { setLoading(false) }
  }, [page, search])

  useEffect(() => { fetchExams() }, [fetchExams])
  useEffect(() => {
    subjectsApi.getAll(1, 100).then(r => setSubjects(r.data.data?.items || [])).catch(() => {})
  }, [])

  const openCreate = () => {
    setForm({ ...emptyForm(), createdBy: user?.id || 0 })
    setEditId(null); setError(''); setModal('create')
  }

  const openEdit = (e: ExamResponse) => {
    const toInputDt = (d: string) => d ? new Date(d).toISOString().slice(0, 16) : ''
    setForm({
      title: e.title, subjectId: e.subjectId, createdBy: e.createdBy,
      durationMinutes: e.durationMinutes, description: e.description,
      startTime: toInputDt(e.startTime), endTime: toInputDt(e.endTime),
    })
    setEditId(e.id); setError(''); setModal('edit')
  }

  const handleSave = async () => {
    if (!form.title || !form.subjectId || !form.startTime || !form.endTime) {
      setError('Vui lòng điền đầy đủ thông tin bắt buộc'); return
    }
    setSaving(true); setError('')
    try {
      if (modal === 'create') await examsApi.create({ ...form, startTime: new Date(form.startTime).toISOString(), endTime: new Date(form.endTime).toISOString() })
      else if (editId) await examsApi.update(editId, { ...form, startTime: new Date(form.startTime).toISOString(), endTime: new Date(form.endTime).toISOString() })
      setModal(null); fetchExams()
    } catch (e: unknown) {
      const err = e as { response?: { data?: { message?: string } } }
      setError(err.response?.data?.message || 'Lỗi lưu kỳ thi')
    } finally { setSaving(false) }
  }

  const handleDelete = async (id: number) => {
    try { await examsApi.delete(id); fetchExams() }
    catch { alert('Không thể xóa kỳ thi này') }
    finally { setDeleteConfirm(null) }
  }

  const handleChangeStatus = async (id: number, status: string) => {
    try { await examsApi.changeStatus(id, status); fetchExams() }
    catch { alert('Không thể thay đổi trạng thái kỳ thi') }
  }

  const totalPages = Math.ceil(total / pageSize)

  const fmtDate = (d: string) => d ? new Date(d).toLocaleString('vi-VN', { dateStyle: 'short', timeStyle: 'short' }) : '—'

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 20 }}>
        <div>
          <h2 style={{ marginBottom: 2 }}>Quản lý Kỳ thi</h2>
          <p style={{ fontSize: 13 }}>{total} kỳ thi trong hệ thống</p>
        </div>
        <div style={{ display: 'flex', gap: 8 }}>
          <button
            className={`btn ${viewMode === 'card' ? 'btn-primary' : 'btn-secondary'}`}
            onClick={() => setViewMode('card')}
          >
            <span className="material-icons" style={{ fontSize: 18 }}>grid_view</span>
          </button>
          <button
            className={`btn ${viewMode === 'list' ? 'btn-primary' : 'btn-secondary'}`}
            onClick={() => setViewMode('list')}
          >
            <span className="material-icons" style={{ fontSize: 18 }}>list</span>
          </button>
          <button className="btn btn-primary" onClick={openCreate}>
            <span className="material-icons" style={{ fontSize: 18 }}>add</span>
            Tạo kỳ thi
          </button>
        </div>
      </div>

      <div className="search-bar" style={{ marginBottom: 16 }}>
        <div className="input-group search-input">
          <span className="material-icons input-icon">search</span>
          <input
            className="form-control"
            placeholder="Tìm kỳ thi..."
            value={search}
            onChange={e => { setSearch(e.target.value); setPage(1) }}
          />
        </div>
      </div>

      {loading ? (
        <div className="loading-center"><div className="spinner" /></div>
      ) : exams.length === 0 ? (
        <div className="empty-state">
          <span className="material-icons">edit_calendar</span>
          <p>Chưa có kỳ thi nào</p>
          <button className="btn btn-primary btn-sm" onClick={openCreate}>Tạo kỳ thi đầu tiên</button>
        </div>
      ) : viewMode === 'card' ? (
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(300px, 1fr))', gap: 16 }}>
          {exams.map(exam => (
            <div key={exam.id} className="card" style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
                <div style={{ flex: 1 }}>
                  <Link to={`/exams/${exam.id}`} style={{ fontWeight: 600, fontSize: 15, color: 'var(--text)', textDecoration: 'none' }}>
                    {exam.title}
                  </Link>
                  <div style={{ fontSize: 12, color: 'var(--text-muted)', marginTop: 2 }}>{exam.subjectName}</div>
                </div>
                {statusBadge(exam.status)}
              </div>
              <div style={{ display: 'flex', gap: 16, fontSize: 12, color: 'var(--text-secondary)' }}>
                <span style={{ display: 'flex', alignItems: 'center', gap: 4 }}>
                  <span className="material-icons" style={{ fontSize: 14 }}>schedule</span>
                  {exam.durationMinutes} phút
                </span>
                <span style={{ display: 'flex', alignItems: 'center', gap: 4 }}>
                  <span className="material-icons" style={{ fontSize: 14 }}>event</span>
                  {fmtDate(exam.startTime)}
                </span>
              </div>
              {exam.description && <p style={{ fontSize: 12, margin: 0 }}>{exam.description}</p>}
              <div style={{ display: 'flex', gap: 6, marginTop: 'auto' }}>
                <Link to={`/exams/${exam.id}`} className="btn btn-secondary btn-sm" style={{ flex: 1, justifyContent: 'center' }}>
                  <span className="material-icons" style={{ fontSize: 16 }}>visibility</span>
                  Xem
                </Link>
                <button className="btn btn-secondary btn-sm" onClick={() => openEdit(exam)}>
                  <span className="material-icons" style={{ fontSize: 16 }}>edit</span>
                </button>
                {exam.status === 'DRAFT' && (
                  <button className="btn btn-success btn-sm" onClick={() => handleChangeStatus(exam.id, 'ACTIVE')}>
                    <span className="material-icons" style={{ fontSize: 16 }}>play_arrow</span>
                  </button>
                )}
                {exam.status === 'ACTIVE' && (
                  <>
                    <button className="btn btn-primary btn-sm" onClick={() => navigate(`/exam-player/${exam.id}`)} style={{ flex: 1, justifyContent: 'center' }}>
                      <span className="material-icons" style={{ fontSize: 16 }}>play_circle</span>
                      Bắt đầu thi
                    </button>
                    <button className="btn btn-secondary btn-sm" onClick={() => handleChangeStatus(exam.id, 'CLOSED')} style={{ color: 'var(--danger)' }}>
                      <span className="material-icons" style={{ fontSize: 16 }}>stop</span>
                    </button>
                  </>
                )}
                <button className="btn btn-secondary btn-sm" onClick={() => setDeleteConfirm(exam.id)} style={{ color: 'var(--danger)' }}>
                  <span className="material-icons" style={{ fontSize: 16 }}>delete</span>
                </button>
              </div>
            </div>
          ))}
        </div>
      ) : (
        <div className="card">
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>#</th>
                  <th>Tên kỳ thi</th>
                  <th>Môn học</th>
                  <th>Thời lượng</th>
                  <th>Bắt đầu</th>
                  <th>Kết thúc</th>
                  <th>Trạng thái</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                {exams.map((exam, idx) => (
                  <tr key={exam.id}>
                    <td style={{ color: 'var(--text-muted)' }}>{(page - 1) * pageSize + idx + 1}</td>
                    <td>
                      <Link to={`/exams/${exam.id}`} style={{ fontWeight: 500, color: 'var(--primary)', textDecoration: 'none' }}>
                        {exam.title}
                      </Link>
                    </td>
                    <td>{exam.subjectName || '—'}</td>
                    <td>{exam.durationMinutes} phút</td>
                    <td style={{ fontSize: 12 }}>{fmtDate(exam.startTime)}</td>
                    <td style={{ fontSize: 12 }}>{fmtDate(exam.endTime)}</td>
                    <td>{statusBadge(exam.status)}</td>
                    <td>
                      <div className="actions">
                        <button className="btn-icon btn" title="Sửa" onClick={() => openEdit(exam)}>
                          <span className="material-icons" style={{ fontSize: 18 }}>edit</span>
                        </button>
                        {exam.status === 'ACTIVE' && (
                          <button className="btn-icon btn" title="Bắt đầu thi" onClick={() => navigate(`/exam-player/${exam.id}`)} style={{ color: 'var(--primary)' }}>
                            <span className="material-icons" style={{ fontSize: 18 }}>play_circle</span>
                          </button>
                        )}
                        <button className="btn-icon btn" title="Xóa" onClick={() => setDeleteConfirm(exam.id)} style={{ color: 'var(--danger)' }}>
                          <span className="material-icons" style={{ fontSize: 18 }}>delete</span>
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {totalPages > 1 && (
        <div className="pagination" style={{ marginTop: 20 }}>
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

      {modal && (
        <div className="modal-overlay" onClick={e => e.target === e.currentTarget && setModal(null)}>
          <div className="modal" style={{ maxWidth: 560 }}>
            <div className="modal-header">
              <h3>{modal === 'create' ? 'Tạo kỳ thi mới' : 'Cập nhật kỳ thi'}</h3>
              <button className="btn btn-icon" onClick={() => setModal(null)}><span className="material-icons">close</span></button>
            </div>
            <div className="modal-body">
              {error && <div className="alert alert-error"><span className="material-icons" style={{ fontSize: 18 }}>error</span>{error}</div>}
              <div className="form-group">
                <label className="form-label">Tên kỳ thi *</label>
                <input className="form-control" value={form.title} onChange={e => setForm(f => ({ ...f, title: e.target.value }))} placeholder="Ví dụ: Kiểm tra giữa kỳ Toán 12" />
              </div>
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '0 16px' }}>
                <div className="form-group">
                  <label className="form-label">Môn học *</label>
                  <select className="form-control" value={form.subjectId} onChange={e => setForm(f => ({ ...f, subjectId: Number(e.target.value) }))}>
                    <option value={0}>Chọn môn học</option>
                    {subjects.map(s => <option key={s.id} value={s.id}>{s.name}</option>)}
                  </select>
                </div>
                <div className="form-group">
                  <label className="form-label">Thời lượng (phút) *</label>
                  <input className="form-control" type="number" min={1} max={600} value={form.durationMinutes} onChange={e => setForm(f => ({ ...f, durationMinutes: Number(e.target.value) }))} />
                </div>
                <div className="form-group">
                  <label className="form-label">Thời gian bắt đầu *</label>
                  <input className="form-control" type="datetime-local" value={form.startTime} onChange={e => setForm(f => ({ ...f, startTime: e.target.value }))} />
                </div>
                <div className="form-group">
                  <label className="form-label">Thời gian kết thúc *</label>
                  <input className="form-control" type="datetime-local" value={form.endTime} onChange={e => setForm(f => ({ ...f, endTime: e.target.value }))} />
                </div>
              </div>
              <div className="form-group">
                <label className="form-label">Mô tả</label>
                <textarea className="form-control" rows={3} value={form.description || ''} onChange={e => setForm(f => ({ ...f, description: e.target.value }))} placeholder="Mô tả kỳ thi (tùy chọn)" style={{ resize: 'vertical' }} />
              </div>
            </div>
            <div className="modal-footer">
              <button className="btn btn-secondary" onClick={() => setModal(null)}>Hủy</button>
              <button className="btn btn-primary" onClick={handleSave} disabled={saving}>
                {saving ? 'Đang lưu...' : modal === 'create' ? 'Tạo kỳ thi' : 'Cập nhật'}
              </button>
            </div>
          </div>
        </div>
      )}

      {deleteConfirm !== null && (
        <div className="modal-overlay">
          <div className="modal" style={{ maxWidth: 380 }}>
            <div className="modal-header">
              <h3>Xác nhận xóa</h3>
              <button className="btn btn-icon" onClick={() => setDeleteConfirm(null)}><span className="material-icons">close</span></button>
            </div>
            <div className="modal-body"><p>Bạn có chắc muốn xóa kỳ thi này? Tất cả dữ liệu liên quan sẽ bị xóa.</p></div>
            <div className="modal-footer">
              <button className="btn btn-secondary" onClick={() => setDeleteConfirm(null)}>Hủy</button>
              <button className="btn btn-danger" onClick={() => handleDelete(deleteConfirm)}>Xóa</button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
