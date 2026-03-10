import { useState, useEffect, useCallback } from 'react'
import { subjectsApi } from '../api/subjects'
import type { SubjectResponse, CreateSubjectRequest } from '../types/api'

const emptyForm: CreateSubjectRequest = { name: '', code: '', description: '', credits: 3 }

export default function SubjectsPage() {
  const [subjects, setSubjects] = useState<SubjectResponse[]>([])
  const [total, setTotal] = useState(0)
  const [page, setPage] = useState(1)
  const [loading, setLoading] = useState(true)
  const [modal, setModal] = useState<'create' | 'edit' | null>(null)
  const [form, setForm] = useState<CreateSubjectRequest>(emptyForm)
  const [editId, setEditId] = useState<number | null>(null)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const [deleteConfirm, setDeleteConfirm] = useState<number | null>(null)
  const pageSize = 20

  const fetchSubjects = useCallback(async () => {
    setLoading(true)
    try {
      const res = await subjectsApi.getAll(page, pageSize)
      const d = res.data.data
      setSubjects(d?.items || [])
      setTotal(d?.totalCount || 0)
    } catch { setSubjects([]); setTotal(0) }
    finally { setLoading(false) }
  }, [page])

  useEffect(() => { fetchSubjects() }, [fetchSubjects])

  const openCreate = () => { setForm(emptyForm); setEditId(null); setError(''); setModal('create') }
  const openEdit = (s: SubjectResponse) => {
    setForm({ name: s.name, code: s.code, description: s.description || '', credits: s.credits })
    setEditId(s.id); setError(''); setModal('edit')
  }

  const handleSave = async () => {
    if (!form.name || !form.code) { setError('Vui lòng điền tên và mã môn học'); return }
    setSaving(true); setError('')
    try {
      if (modal === 'create') await subjectsApi.create(form)
      else if (editId) await subjectsApi.update(editId, form)
      setModal(null); fetchSubjects()
    } catch (e: unknown) {
      const err = e as { response?: { data?: { message?: string } } }
      setError(err.response?.data?.message || 'Lỗi lưu môn học')
    } finally { setSaving(false) }
  }

  const handleDelete = async (id: number) => {
    try { await subjectsApi.delete(id); fetchSubjects() }
    catch { alert('Không thể xóa môn học này') }
    finally { setDeleteConfirm(null) }
  }

  const totalPages = Math.ceil(total / pageSize)

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 20 }}>
        <div>
          <h2 style={{ marginBottom: 2 }}>Quản lý Môn học</h2>
          <p style={{ fontSize: 13 }}>{total} môn học trong hệ thống</p>
        </div>
        <button className="btn btn-primary" onClick={openCreate}>
          <span className="material-icons" style={{ fontSize: 18 }}>add</span>
          Thêm môn học
        </button>
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(280px, 1fr))', gap: 16, marginBottom: 24 }}>
        {loading ? (
          Array.from({ length: 6 }).map((_, i) => (
            <div key={i} className="card" style={{ height: 120 }}>
              <div style={{ background: 'var(--surface-alt)', height: '100%', borderRadius: 8 }} />
            </div>
          ))
        ) : subjects.map(s => (
          <div key={s.id} className="card" style={{ position: 'relative' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
              <div style={{ flex: 1 }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 6 }}>
                  <div style={{ width: 36, height: 36, borderRadius: 8, background: 'var(--primary-light)', color: 'var(--primary)', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
                    <span className="material-icons" style={{ fontSize: 18 }}>library_books</span>
                  </div>
                  <span className="badge badge-blue">{s.code}</span>
                </div>
                <h4 style={{ marginBottom: 4 }}>{s.name}</h4>
                {s.description && <p style={{ fontSize: 12, marginBottom: 4 }}>{s.description}</p>}
                <div style={{ fontSize: 12, color: 'var(--text-muted)', display: 'flex', gap: 12 }}>
                  <span><span className="material-icons" style={{ fontSize: 13, verticalAlign: 'middle' }}>stars</span> {s.credits} tín chỉ</span>
                  <span>{s.isActive ? <span className="badge badge-green" style={{ padding: '1px 8px' }}>Hoạt động</span> : <span className="badge badge-gray" style={{ padding: '1px 8px' }}>Tạm dừng</span>}</span>
                </div>
              </div>
              <div className="actions" style={{ flexDirection: 'column' }}>
                <button className="btn-icon btn" title="Sửa" onClick={() => openEdit(s)}>
                  <span className="material-icons" style={{ fontSize: 18 }}>edit</span>
                </button>
                <button className="btn-icon btn" title="Xóa" onClick={() => setDeleteConfirm(s.id)} style={{ color: 'var(--danger)' }}>
                  <span className="material-icons" style={{ fontSize: 18 }}>delete</span>
                </button>
              </div>
            </div>
          </div>
        ))}
      </div>

      {subjects.length === 0 && !loading && (
        <div className="empty-state">
          <span className="material-icons">library_books</span>
          <p>Chưa có môn học nào</p>
          <button className="btn btn-primary btn-sm" onClick={openCreate}>Thêm môn học</button>
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

      {modal && (
        <div className="modal-overlay" onClick={e => e.target === e.currentTarget && setModal(null)}>
          <div className="modal">
            <div className="modal-header">
              <h3>{modal === 'create' ? 'Thêm môn học mới' : 'Cập nhật môn học'}</h3>
              <button className="btn btn-icon" onClick={() => setModal(null)}><span className="material-icons">close</span></button>
            </div>
            <div className="modal-body">
              {error && <div className="alert alert-error"><span className="material-icons" style={{ fontSize: 18 }}>error</span>{error}</div>}
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '0 16px' }}>
                <div className="form-group">
                  <label className="form-label">Tên môn học *</label>
                  <input className="form-control" value={form.name} onChange={e => setForm(f => ({ ...f, name: e.target.value }))} placeholder="Toán học" />
                </div>
                <div className="form-group">
                  <label className="form-label">Mã môn *</label>
                  <input className="form-control" value={form.code} onChange={e => setForm(f => ({ ...f, code: e.target.value }))} placeholder="MATH101" />
                </div>
                <div className="form-group">
                  <label className="form-label">Số tín chỉ</label>
                  <input className="form-control" type="number" min={1} max={10} value={form.credits} onChange={e => setForm(f => ({ ...f, credits: Number(e.target.value) }))} />
                </div>
                <div className="form-group" style={{ gridColumn: 'span 2' }}>
                  <label className="form-label">Mô tả</label>
                  <input className="form-control" value={form.description || ''} onChange={e => setForm(f => ({ ...f, description: e.target.value }))} placeholder="Mô tả môn học" />
                </div>
              </div>
            </div>
            <div className="modal-footer">
              <button className="btn btn-secondary" onClick={() => setModal(null)}>Hủy</button>
              <button className="btn btn-primary" onClick={handleSave} disabled={saving}>
                {saving ? 'Đang lưu...' : 'Lưu'}
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
            <div className="modal-body"><p>Bạn có chắc muốn xóa môn học này?</p></div>
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
