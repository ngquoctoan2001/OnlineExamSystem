import { useState, useEffect, useCallback } from 'react'
import { subjectsApi } from '../api/subjects'
import { subjectExamTypesApi } from '../api/subjectExamTypes'
import type { SubjectResponse, CreateSubjectRequest, SubjectExamTypeResponse, CreateSubjectExamTypeRequest, UpdateSubjectExamTypeRequest } from '../types/api'

const emptyForm: CreateSubjectRequest = { name: '', code: '', description: '' }

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

  // Exam type config state
  const [examTypeSubject, setExamTypeSubject] = useState<SubjectResponse | null>(null)
  const [examTypes, setExamTypes] = useState<SubjectExamTypeResponse[]>([])
  const [examTypesLoading, setExamTypesLoading] = useState(false)
  const [examTypeForm, setExamTypeForm] = useState<Partial<CreateSubjectExamTypeRequest>>({})
  const [examTypeEditId, setExamTypeEditId] = useState<number | null>(null)
  const [examTypeSaving, setExamTypeSaving] = useState(false)
  const [examTypeError, setExamTypeError] = useState('')

  const fetchSubjects = useCallback(async () => {
    setLoading(true)
    try {
      const res = await subjectsApi.getAll(page, pageSize)
      const d = res.data.data
      setSubjects(d?.subjects || [])
      setTotal(d?.totalCount || 0)
    } catch { setSubjects([]); setTotal(0) }
    finally { setLoading(false) }
  }, [page])

  useEffect(() => { fetchSubjects() }, [fetchSubjects])

  const openCreate = () => { setForm(emptyForm); setEditId(null); setError(''); setModal('create') }
  const openEdit = (s: SubjectResponse) => {
    setForm({ name: s.name, code: s.code, description: s.description || '' })
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

  // Exam type config functions
  const openExamTypeConfig = async (subject: SubjectResponse) => {
    setExamTypeSubject(subject)
    setExamTypeForm({})
    setExamTypeEditId(null)
    setExamTypeError('')
    setExamTypesLoading(true)
    try {
      const res = await subjectExamTypesApi.getBySubject(subject.id)
      setExamTypes(res.data.data || [])
    } catch { setExamTypes([]) }
    finally { setExamTypesLoading(false) }
  }

  const handleExamTypeSave = async () => {
    if (!examTypeSubject || !examTypeForm.name) { setExamTypeError('Vui lòng nhập tên loại bài kiểm tra'); return }
    setExamTypeSaving(true); setExamTypeError('')
    try {
      if (examTypeEditId) {
        const updateData: UpdateSubjectExamTypeRequest = {
          name: examTypeForm.name,
          coefficient: examTypeForm.coefficient,
          requiredCount: examTypeForm.requiredCount,
          sortOrder: examTypeForm.sortOrder,
        }
        await subjectExamTypesApi.update(examTypeEditId, updateData)
      } else {
        await subjectExamTypesApi.create({
          subjectId: examTypeSubject.id,
          name: examTypeForm.name,
          coefficient: examTypeForm.coefficient || 1,
          requiredCount: examTypeForm.requiredCount || 1,
          sortOrder: examTypeForm.sortOrder || examTypes.length,
        })
      }
      setExamTypeForm({}); setExamTypeEditId(null)
      const res = await subjectExamTypesApi.getBySubject(examTypeSubject.id)
      setExamTypes(res.data.data || [])
    } catch (e: unknown) {
      const err = e as { response?: { data?: { message?: string } } }
      setExamTypeError(err.response?.data?.message || 'Lỗi lưu loại bài kiểm tra')
    } finally { setExamTypeSaving(false) }
  }

  const startEditExamType = (et: SubjectExamTypeResponse) => {
    setExamTypeEditId(et.id)
    setExamTypeForm({ name: et.name, coefficient: et.coefficient, requiredCount: et.requiredCount, sortOrder: et.sortOrder })
    setExamTypeError('')
  }

  const handleDeleteExamType = async (id: number) => {
    if (!examTypeSubject) return
    try {
      await subjectExamTypesApi.delete(id)
      const res = await subjectExamTypesApi.getBySubject(examTypeSubject.id)
      setExamTypes(res.data.data || [])
    } catch { alert('Không thể xóa loại bài kiểm tra này') }
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
                  <span><span className="material-icons" style={{ fontSize: 13, verticalAlign: 'middle' }}>quiz</span> {s.questionCount} câu hỏi</span>
                  <span><span className="material-icons" style={{ fontSize: 13, verticalAlign: 'middle' }}>assignment</span> {s.examCount} kỳ thi</span>
                </div>
              </div>
              <div className="actions" style={{ flexDirection: 'column' }}>
                <button className="btn-icon btn" title="Cấu hình loại bài KT" onClick={() => openExamTypeConfig(s)}>
                  <span className="material-icons" style={{ fontSize: 18 }}>tune</span>
                </button>
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

      {examTypeSubject && (
        <div className="modal-overlay" onClick={e => e.target === e.currentTarget && setExamTypeSubject(null)}>
          <div className="modal" style={{ maxWidth: 700 }}>
            <div className="modal-header">
              <h3>Cấu hình loại bài kiểm tra - {examTypeSubject.name}</h3>
              <button className="btn btn-icon" onClick={() => setExamTypeSubject(null)}><span className="material-icons">close</span></button>
            </div>
            <div className="modal-body">
              {examTypeError && <div className="alert alert-error" style={{ marginBottom: 12 }}><span className="material-icons" style={{ fontSize: 18 }}>error</span>{examTypeError}</div>}
              
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 100px 80px 80px auto', gap: 8, alignItems: 'end', marginBottom: 16 }}>
                <div className="form-group" style={{ marginBottom: 0 }}>
                  <label className="form-label" style={{ fontSize: 12 }}>Tên loại *</label>
                  <input className="form-control" placeholder="KT 15 phút" value={examTypeForm.name || ''} onChange={e => setExamTypeForm(f => ({ ...f, name: e.target.value }))} />
                </div>
                <div className="form-group" style={{ marginBottom: 0 }}>
                  <label className="form-label" style={{ fontSize: 12 }}>Hệ số</label>
                  <input type="number" className="form-control" step="0.1" min="0.1" max="10" value={examTypeForm.coefficient ?? 1} onChange={e => setExamTypeForm(f => ({ ...f, coefficient: parseFloat(e.target.value) || 1 }))} />
                </div>
                <div className="form-group" style={{ marginBottom: 0 }}>
                  <label className="form-label" style={{ fontSize: 12 }}>Số cột</label>
                  <input type="number" className="form-control" min="0" max="50" value={examTypeForm.requiredCount ?? 1} onChange={e => setExamTypeForm(f => ({ ...f, requiredCount: parseInt(e.target.value) || 1 }))} />
                </div>
                <div className="form-group" style={{ marginBottom: 0 }}>
                  <label className="form-label" style={{ fontSize: 12 }}>Thứ tự</label>
                  <input type="number" className="form-control" min="0" value={examTypeForm.sortOrder ?? examTypes.length} onChange={e => setExamTypeForm(f => ({ ...f, sortOrder: parseInt(e.target.value) || 0 }))} />
                </div>
                <div style={{ display: 'flex', gap: 4 }}>
                  <button className="btn btn-primary btn-sm" onClick={handleExamTypeSave} disabled={examTypeSaving} style={{ whiteSpace: 'nowrap' }}>
                    {examTypeSaving ? '...' : examTypeEditId ? 'Cập nhật' : 'Thêm'}
                  </button>
                  {examTypeEditId && (
                    <button className="btn btn-secondary btn-sm" onClick={() => { setExamTypeEditId(null); setExamTypeForm({}) }}>Hủy</button>
                  )}
                </div>
              </div>

              {examTypesLoading ? <p>Đang tải...</p> : examTypes.length === 0 ? (
                <div style={{ textAlign: 'center', padding: 20, color: 'var(--text-muted)' }}>
                  <span className="material-icons" style={{ fontSize: 40, display: 'block', marginBottom: 8 }}>rule</span>
                  <p>Chưa có loại bài kiểm tra nào. Hãy thêm mới.</p>
                </div>
              ) : (
                <table className="table">
                  <thead>
                    <tr>
                      <th>Thứ tự</th>
                      <th>Tên loại</th>
                      <th>Hệ số</th>
                      <th>Số cột yêu cầu</th>
                      <th style={{ width: 80 }}>Thao tác</th>
                    </tr>
                  </thead>
                  <tbody>
                    {examTypes.sort((a, b) => a.sortOrder - b.sortOrder).map(et => (
                      <tr key={et.id} style={examTypeEditId === et.id ? { background: 'var(--primary-light)' } : {}}>
                        <td>{et.sortOrder}</td>
                        <td><strong>{et.name}</strong></td>
                        <td><span className="badge badge-blue">x{et.coefficient}</span></td>
                        <td>{et.requiredCount}</td>
                        <td>
                          <div style={{ display: 'flex', gap: 4 }}>
                            <button className="btn-icon btn" title="Sửa" onClick={() => startEditExamType(et)}>
                              <span className="material-icons" style={{ fontSize: 16 }}>edit</span>
                            </button>
                            <button className="btn-icon btn" title="Xóa" onClick={() => handleDeleteExamType(et.id)} style={{ color: 'var(--danger)' }}>
                              <span className="material-icons" style={{ fontSize: 16 }}>delete</span>
                            </button>
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              )}
              
              <div style={{ marginTop: 12, padding: 12, background: 'var(--surface-alt)', borderRadius: 8, fontSize: 13 }}>
                <strong>Hướng dẫn:</strong> Cấu hình các loại bài kiểm tra cho môn học. Ví dụ: KT 15 phút (hệ số 1), KT 45 phút (hệ số 2), Giữa kỳ (hệ số 2), Cuối kỳ (hệ số 3).
                <br />Số cột yêu cầu là số bài kiểm tra tối thiểu cho mỗi loại. Khi tạo bài thi, giáo viên sẽ chọn loại bài kiểm tra tương ứng.
              </div>
            </div>
            <div className="modal-footer">
              <button className="btn btn-secondary" onClick={() => setExamTypeSubject(null)}>Đóng</button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
