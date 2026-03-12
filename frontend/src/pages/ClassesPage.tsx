import { useState, useEffect, useCallback } from 'react'
import { useNavigate } from 'react-router-dom'
import { classesApi } from '../api/classes'
import { teachersApi } from '../api/teachers'
import type { ClassResponse, CreateClassRequest, TeacherResponse } from '../types/api'

const emptyForm: CreateClassRequest = {
  name: '', code: '', grade: 10, homeroomTeacherId: undefined
}

export default function ClassesPage() {
  const navigate = useNavigate()
  const [classes, setClasses] = useState<ClassResponse[]>([])
  const [total, setTotal] = useState(0)
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [filterGrade, setFilterGrade] = useState(0)
  const [loading, setLoading] = useState(true)
  const [modal, setModal] = useState<'create' | 'edit' | null>(null)
  const [form, setForm] = useState<CreateClassRequest>(emptyForm)
  const [editId, setEditId] = useState<number | null>(null)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const [deleteConfirm, setDeleteConfirm] = useState<number | null>(null)
  const [teachers, setTeachers] = useState<TeacherResponse[]>([])
  const pageSize = 20

  useEffect(() => {
    teachersApi.getAll(1, 200).then(r => setTeachers(r.data.data?.teachers || [])).catch(() => {})
  }, [])

  const fetchClasses = useCallback(async () => {
    setLoading(true)
    try {
      if (search.trim()) {
        const res = await classesApi.search(search)
        const data = res.data.data || []
        setClasses(Array.isArray(data) ? data : [])
        setTotal(Array.isArray(data) ? data.length : 0)
      } else {
        const res = await classesApi.getAll(page, pageSize)
        const d = res.data.data
        setClasses(d?.classes || [])
        setTotal(d?.totalCount || 0)
      }
    } catch { setClasses([]); setTotal(0) }
    finally { setLoading(false) }
  }, [page, search])

  useEffect(() => { fetchClasses() }, [fetchClasses])

  const openCreate = () => { setForm(emptyForm); setEditId(null); setError(''); setModal('create') }
  const openEdit = (c: ClassResponse) => {
    setForm({ name: c.name, code: c.code, grade: c.grade, homeroomTeacherId: c.homeroomTeacherId || undefined })
    setEditId(c.id); setError(''); setModal('edit')
  }

  const handleSave = async () => {
    if (!form.name) {
      setError('Vui lòng điền tên lớp'); return
    }
    // Auto-generate code from name if empty
    if (!form.code) {
      form.code = form.name.replace(/\s+/g, '').toUpperCase()
    }
    setSaving(true); setError('')
    try {
      if (modal === 'create') await classesApi.create(form)
      else if (editId) await classesApi.update(editId, form)
      setModal(null); fetchClasses()
    } catch (e: unknown) {
      const err = e as { response?: { data?: { message?: string } } }
      setError(err.response?.data?.message || 'Lỗi lưu lớp học')
    } finally { setSaving(false) }
  }

  const handleDelete = async (id: number) => {
    try { await classesApi.delete(id); fetchClasses() }
    catch { alert('Không thể xóa lớp này') }
    finally { setDeleteConfirm(null) }
  }

  const filteredClasses = filterGrade ? classes.filter(c => c.grade === filterGrade) : classes
  const totalPages = Math.ceil(total / pageSize)

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 20 }}>
        <div>
          <h2 style={{ marginBottom: 2 }}>Quản lý Lớp học</h2>
          <p style={{ fontSize: 13 }}>{total} lớp học trong hệ thống</p>
        </div>
        <button className="btn btn-primary" onClick={openCreate}>
          <span className="material-icons" style={{ fontSize: 18 }}>add</span>
          Thêm lớp học
        </button>
      </div>

      <div className="card">
        <div className="search-bar">
          <div className="input-group search-input">
            <span className="material-icons input-icon">search</span>
            <input
              className="form-control"
              placeholder="Tìm lớp học..."
              value={search}
              onChange={e => { setSearch(e.target.value); setPage(1) }}
            />
          </div>
          <select className="form-control" style={{ minWidth: 160, width: 'auto' }} value={filterGrade} onChange={e => setFilterGrade(Number(e.target.value))}>
            <option value={0}>Tất cả khối</option>
            <option value={10}>Khối 10</option>
            <option value={11}>Khối 11</option>
            <option value={12}>Khối 12</option>
          </select>
        </div>

        {loading ? (
          <div className="loading-center"><div className="spinner" /></div>
        ) : filteredClasses.length === 0 ? (
          <div className="empty-state">
            <span className="material-icons">groups</span>
            <p>Chưa có lớp học nào</p>
          </div>
        ) : (
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>#</th>
                  <th>Tên lớp</th>
                  <th>Khối</th>
                  <th>GVCN</th>
                  <th>Sĩ số</th>
                  <th>GV bộ môn</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                {filteredClasses.map((c, idx) => (
                  <tr key={c.id}>
                    <td style={{ color: 'var(--text-muted)' }}>{(page - 1) * pageSize + idx + 1}</td>
                    <td><div style={{ fontWeight: 500 }}>{c.name}</div></td>
                    <td>{c.grade}</td>
                    <td>{c.homeroomTeacherName || <span style={{ color: 'var(--text-muted)' }}>Chưa phân công</span>}</td>
                    <td>
                      <span style={{ display: 'flex', alignItems: 'center', gap: 4 }}>
                        <span className="material-icons" style={{ fontSize: 16, color: 'var(--text-muted)' }}>people</span>
                        {c.studentCount || 0}
                      </span>
                    </td>
                    <td>{c.teacherCount || 0}</td>
                    <td>
                      <div className="actions">
                        <button className="btn-icon btn" title="Chi tiết" onClick={() => navigate(`/classes/${c.id}`)}>
                          <span className="material-icons" style={{ fontSize: 18 }}>visibility</span>
                        </button>
                        <button className="btn-icon btn" title="Sửa" onClick={() => openEdit(c)}>
                          <span className="material-icons" style={{ fontSize: 18 }}>edit</span>
                        </button>
                        <button className="btn-icon btn" title="Xóa" onClick={() => setDeleteConfirm(c.id)} style={{ color: 'var(--danger)' }}>
                          <span className="material-icons" style={{ fontSize: 18 }}>delete</span>
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
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

      {modal && (
        <div className="modal-overlay" onClick={e => e.target === e.currentTarget && setModal(null)}>
          <div className="modal">
            <div className="modal-header">
              <h3>{modal === 'create' ? 'Thêm lớp học mới' : 'Cập nhật lớp học'}</h3>
              <button className="btn btn-icon" onClick={() => setModal(null)}><span className="material-icons">close</span></button>
            </div>
            <div className="modal-body">
              {error && <div className="alert alert-error"><span className="material-icons" style={{ fontSize: 18 }}>error</span>{error}</div>}
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '0 16px' }}>
                <div className="form-group">
                  <label className="form-label">Tên lớp *</label>
                  <input className="form-control" value={form.name} onChange={e => setForm(f => ({ ...f, name: e.target.value }))} placeholder="Lớp 10A1" />
                </div>

                <div className="form-group">
                  <label className="form-label">Khối *</label>
                  <select className="form-control" value={form.grade} onChange={e => setForm(f => ({ ...f, grade: Number(e.target.value) }))}>
                    <option value={10}>Khối 10</option>
                    <option value={11}>Khối 11</option>
                    <option value={12}>Khối 12</option>
                  </select>
                </div>
                <div className="form-group">
                  <label className="form-label">Giáo viên chủ nhiệm</label>
                  <select className="form-control" value={form.homeroomTeacherId || ''} onChange={e => setForm(f => ({ ...f, homeroomTeacherId: e.target.value ? Number(e.target.value) : undefined }))}>
                    <option value="">-- Chọn GVCN --</option>
                    {teachers.map(t => (
                      <option key={t.id} value={t.id}>{t.fullName} ({t.employeeId})</option>
                    ))}
                  </select>
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
            <div className="modal-body"><p>Bạn có chắc muốn xóa lớp học này?</p></div>
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
