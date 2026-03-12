import { useState, useEffect, useCallback, useRef } from 'react'
import { studentsApi } from '../api/students'
import { classesApi } from '../api/classes'
import { usersApi } from '../api/users'
import type { StudentResponse, CreateStudentRequest, ClassResponse } from '../types/api'

interface FormData extends CreateStudentRequest {}

const emptyForm: FormData = {
  username: '', email: '', password: '', fullName: '', studentCode: '', rollNumber: ''
}

export default function StudentsPage() {
  const [students, setStudents] = useState<StudentResponse[]>([])
  const [total, setTotal] = useState(0)
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [loading, setLoading] = useState(true)
  const [modal, setModal] = useState<'create' | 'edit' | null>(null)
  const [form, setForm] = useState<FormData>(emptyForm)
  const [editId, setEditId] = useState<number | null>(null)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const [deleteConfirm, setDeleteConfirm] = useState<number | null>(null)
  const [filterStatus, setFilterStatus] = useState<'' | 'active' | 'inactive'>('')
  const [filterClassId, setFilterClassId] = useState(0)
  const [allClasses, setAllClasses] = useState<ClassResponse[]>([])
  const [importResult, setImportResult] = useState<string | null>(null)
  const [createdAccount, setCreatedAccount] = useState<{ username: string; password: string } | null>(null)
  const [selectedClassId, setSelectedClassId] = useState<number>(0)
  const fileInputRef = useRef<HTMLInputElement>(null)
  const pageSize = 20

  useEffect(() => {
    classesApi.getAll(1, 200).then(r => setAllClasses(r.data.data?.classes || [])).catch(() => {})
  }, [])

  const fetchStudents = useCallback(async () => {
    setLoading(true)
    try {
      if (filterClassId) {
        const res = await classesApi.getStudents(filterClassId)
        const data = res.data.data || []
        const rawList = Array.isArray(data) ? data : []
        // Enrich with full student data
        const enriched = await Promise.all(rawList.map(async (cs: { studentId: number; username: string; fullName: string; studentCode: string; rollNumber: string; enrolledAt: string }) => {
          try {
            const detail = await studentsApi.getById(cs.studentId)
            const s = detail.data.data
            if (s) return s
          } catch { /* fallback */ }
          return {
            id: cs.studentId, userId: 0, username: cs.username, email: '', fullName: cs.fullName,
            studentCode: cs.studentCode, rollNumber: cs.rollNumber, isActive: true, createdAt: cs.enrolledAt
          } as StudentResponse
        }))
        setStudents(search.trim() ? enriched.filter(s => s.fullName.toLowerCase().includes(search.toLowerCase()) || s.studentCode.toLowerCase().includes(search.toLowerCase())) : enriched)
        setTotal(enriched.length)
      } else if (search.trim()) {
        const res = await studentsApi.search(search)
        const data = res.data.data || []
        setStudents(Array.isArray(data) ? data : [])
        setTotal(Array.isArray(data) ? data.length : 0)
      } else {
        const res = await studentsApi.getAll(page, pageSize)
        const d = res.data.data
        setStudents(d?.students || [])
        setTotal(d?.totalCount || 0)
      }
    } catch { setStudents([]); setTotal(0) }
    finally { setLoading(false) }
  }, [page, search, filterClassId])

  useEffect(() => { fetchStudents() }, [fetchStudents])

  const openCreate = () => { setForm(emptyForm); setEditId(null); setSelectedClassId(0); setError(''); setModal('create') }
  const openEdit = (s: StudentResponse) => {
    setForm({ username: s.username, email: s.email, password: '', fullName: s.fullName, studentCode: s.studentCode, rollNumber: s.rollNumber })
    setEditId(s.id); setSelectedClassId(filterClassId || 0); setError(''); setModal('edit')
  }

  const handleSave = async () => {
    if (!form.username || !form.email || !form.fullName || !form.studentCode) {
      setError('Vui lòng điền đầy đủ thông tin bắt buộc'); return
    }
    setSaving(true); setError('')
    try {
      let studentId: number | null = null
      if (modal === 'create') {
        if (!form.password) { setError('Vui lòng nhập mật khẩu'); setSaving(false); return }
        const res = await studentsApi.create(form)
        studentId = res.data.data?.id || null
        setCreatedAccount({ username: form.username, password: form.password })
      } else if (editId) {
        await studentsApi.update(editId, form)
        studentId = editId
      }
      // Assign to class if selected
      if (selectedClassId && studentId) {
        try { await classesApi.addStudent(selectedClassId, studentId) } catch { /* best-effort */ }
      }
      setModal(null); fetchStudents()
    } catch (e: unknown) {
      const err = e as { response?: { data?: { message?: string } } }
      setError(err.response?.data?.message || 'Lỗi lưu học sinh')
    } finally { setSaving(false) }
  }

  const handleDelete = async (id: number) => {
    try { await studentsApi.delete(id); fetchStudents() }
    catch { alert('Không thể xóa học sinh này') }
    finally { setDeleteConfirm(null) }
  }

  const [resetPwdTarget, setResetPwdTarget] = useState<{ userId: number; name: string } | null>(null)
  const [resetPwdValue, setResetPwdValue] = useState('')
  const handleResetPassword = async () => {
    if (!resetPwdTarget || resetPwdValue.length < 6) { alert('Mật khẩu mới phải có ít nhất 6 ký tự'); return }
    try {
      await usersApi.resetPassword(resetPwdTarget.userId, resetPwdValue)
      alert('Reset mật khẩu thành công!')
      setResetPwdTarget(null); setResetPwdValue('')
    } catch { alert('Không thể reset mật khẩu') }
  }

  const handleExport = async () => {
    try {
      const res = await studentsApi.exportFile()
      const url = window.URL.createObjectURL(new Blob([res.data]))
      const a = document.createElement('a')
      a.href = url; a.download = 'students.xlsx'; a.click()
      window.URL.revokeObjectURL(url)
    } catch { alert('Lỗi khi xuất file') }
  }

  const handleImport = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    if (!file) return
    try {
      const res = await studentsApi.importFile(file)
      const d = res.data.data as Record<string, unknown> | undefined
      setImportResult(`Import hoàn tất: ${d?.successCount ?? 0} thành công, ${d?.failedCount ?? 0} lỗi`)
      fetchStudents()
    } catch {
      setImportResult('Lỗi khi import file')
    }
    if (fileInputRef.current) fileInputRef.current.value = ''
  }

  const filteredStudents = students.filter(s => {
    if (filterStatus === 'active' && !s.isActive) return false
    if (filterStatus === 'inactive' && s.isActive) return false
    return true
  })

  const totalPages = Math.ceil(total / pageSize)

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 20 }}>
        <div>
          <h2 style={{ marginBottom: 2 }}>Danh sách Học sinh</h2>
          <p style={{ fontSize: 13 }}>{total} học sinh trong hệ thống</p>
        </div>
        <div style={{ display: 'flex', gap: 8 }}>
          <input type="file" ref={fileInputRef} accept=".xlsx,.xls" style={{ display: 'none' }} onChange={handleImport} />
          <button className="btn btn-secondary" onClick={() => fileInputRef.current?.click()} title="Import từ Excel">
            <span className="material-icons" style={{ fontSize: 18 }}>upload_file</span>
            Import
          </button>
          <button className="btn btn-secondary" onClick={handleExport} title="Xuất ra Excel">
            <span className="material-icons" style={{ fontSize: 18 }}>download</span>
            Export
          </button>
          <button className="btn btn-primary" onClick={openCreate}>
            <span className="material-icons" style={{ fontSize: 18 }}>person_add</span>
            Thêm học sinh
          </button>
        </div>
      </div>

      {importResult && (
        <div className="alert alert-info" style={{ marginBottom: 16, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <span>{importResult}</span>
          <button className="btn btn-icon" onClick={() => setImportResult(null)}><span className="material-icons" style={{ fontSize: 18 }}>close</span></button>
        </div>
      )}

      <div className="card">
        <div className="search-bar" style={{ display: 'flex', gap: 12, alignItems: 'center', flexWrap: 'wrap' }}>
          <div className="input-group search-input" style={{ flex: 1, minWidth: 200 }}>
            <span className="material-icons input-icon">search</span>
            <input
              className="form-control"
              placeholder="Tìm học sinh..."
              value={search}
              onChange={e => { setSearch(e.target.value); setPage(1) }}
            />
          </div>
          <select className="form-control" style={{ width: 150 }} value={filterStatus} onChange={e => setFilterStatus(e.target.value as '' | 'active' | 'inactive')}>
            <option value="">Tất cả trạng thái</option>
            <option value="active">Hoạt động</option>
            <option value="inactive">Tạm khóa</option>
          </select>
          <select className="form-control" style={{ width: 160 }} value={filterClassId} onChange={e => { setFilterClassId(Number(e.target.value)); setPage(1) }}>
            <option value={0}>Tất cả lớp</option>
            {allClasses.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
          </select>
        </div>

        {loading ? (
          <div className="loading-center"><div className="spinner" /></div>
        ) : filteredStudents.length === 0 ? (
          <div className="empty-state">
            <span className="material-icons">person_search</span>
            <p>Không tìm thấy học sinh nào</p>
          </div>
        ) : (
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>#</th>
                  <th>Họ tên</th>
                  <th>Mã học sinh</th>
                  <th>Email</th>
                  <th>Số thứ tự</th>
                  <th>Trạng thái</th>
                  <th>Ngày tạo</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                {filteredStudents.map((s, idx) => (
                  <tr key={s.id}>
                    <td style={{ color: 'var(--text-muted)' }}>{(page - 1) * pageSize + idx + 1}</td>
                    <td>
                      <div style={{ fontWeight: 500 }}>{s.fullName}</div>
                      <div style={{ fontSize: 12, color: 'var(--text-muted)' }}>{s.username}</div>
                    </td>
                    <td><span className="badge badge-blue">{s.studentCode}</span></td>
                    <td>{s.email}</td>
                    <td>{s.rollNumber || '—'}</td>
                    <td>
                      {s.isActive
                        ? <span className="badge badge-green">Hoạt động</span>
                        : <span className="badge badge-red">Tạm khóa</span>}
                    </td>
                    <td style={{ color: 'var(--text-muted)', fontSize: 12 }}>
                      {new Date(s.createdAt).toLocaleDateString('vi-VN')}
                    </td>
                    <td>
                      <div className="actions">
                        <button className="btn-icon btn" title="Sửa" onClick={() => openEdit(s)}>
                          <span className="material-icons" style={{ fontSize: 18 }}>edit</span>
                        </button>
                        <button className="btn-icon btn" title="Reset mật khẩu" onClick={() => setResetPwdTarget({ userId: s.userId, name: s.fullName })} style={{ color: 'var(--warning, #f59e0b)' }}>
                          <span className="material-icons" style={{ fontSize: 18 }}>lock_reset</span>
                        </button>
                        <button className="btn-icon btn" title="Xóa" onClick={() => setDeleteConfirm(s.id)} style={{ color: 'var(--danger)' }}>
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
            {Array.from({ length: Math.min(totalPages, 7) }, (_, i) => {
              const pg = i + 1
              return (
                <button key={pg} className={`page-btn${page === pg ? ' active' : ''}`} onClick={() => setPage(pg)}>
                  {pg}
                </button>
              )
            })}
            <button className="page-btn" onClick={() => setPage(p => Math.min(totalPages, p + 1))} disabled={page === totalPages}>
              <span className="material-icons" style={{ fontSize: 16 }}>chevron_right</span>
            </button>
          </div>
        )}
      </div>

      {/* Create/Edit Modal */}
      {modal && (
        <div className="modal-overlay" onClick={e => e.target === e.currentTarget && setModal(null)}>
          <div className="modal">
            <div className="modal-header">
              <h3>{modal === 'create' ? 'Thêm học sinh mới' : 'Cập nhật học sinh'}</h3>
              <button className="btn btn-icon" onClick={() => setModal(null)}>
                <span className="material-icons">close</span>
              </button>
            </div>
            <div className="modal-body">
              {error && <div className="alert alert-error"><span className="material-icons" style={{ fontSize: 18 }}>error</span>{error}</div>}
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '0 16px' }}>
                <div className="form-group">
                  <label className="form-label">Họ và tên *</label>
                  <input className="form-control" value={form.fullName} onChange={e => setForm(f => ({ ...f, fullName: e.target.value }))} placeholder="Nguyễn Văn A" />
                </div>
                <div className="form-group">
                  <label className="form-label">Tên đăng nhập *</label>
                  <input className="form-control" value={form.username} onChange={e => setForm(f => ({ ...f, username: e.target.value }))} placeholder="nguyenvana" disabled={modal === 'edit'} />
                </div>
                <div className="form-group">
                  <label className="form-label">Email *</label>
                  <input className="form-control" type="email" value={form.email} onChange={e => setForm(f => ({ ...f, email: e.target.value }))} placeholder="email@example.com" />
                </div>
                <div className="form-group">
                  <label className="form-label">Mã học sinh *</label>
                  <input className="form-control" value={form.studentCode} onChange={e => setForm(f => ({ ...f, studentCode: e.target.value }))} placeholder="HS001" />
                </div>
                <div className="form-group">
                  <label className="form-label">Số thứ tự</label>
                  <input className="form-control" value={form.rollNumber || ''} onChange={e => setForm(f => ({ ...f, rollNumber: e.target.value }))} placeholder="01" />
                </div>
                {modal === 'create' && (
                  <div className="form-group">
                    <label className="form-label">Mật khẩu *</label>
                    <input className="form-control" type="password" value={form.password} onChange={e => setForm(f => ({ ...f, password: e.target.value }))} placeholder="Nhập mật khẩu" />
                  </div>
                )}
                <div className="form-group">
                  <label className="form-label">Lớp học</label>
                  <select className="form-control" value={selectedClassId} onChange={e => setSelectedClassId(Number(e.target.value))}>
                    <option value={0}>-- Chọn lớp --</option>
                    {allClasses.map(c => <option key={c.id} value={c.id}>{c.name} ({c.code})</option>)}
                  </select>
                </div>
              </div>
            </div>
            <div className="modal-footer">
              <button className="btn btn-secondary" onClick={() => setModal(null)}>Hủy</button>
              <button className="btn btn-primary" onClick={handleSave} disabled={saving}>
                {saving ? <><div className="spinner" style={{ width: 16, height: 16, borderWidth: 2 }} /> Đang lưu...</> : 'Lưu'}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Delete Confirm */}
      {deleteConfirm !== null && (
        <div className="modal-overlay">
          <div className="modal" style={{ maxWidth: 380 }}>
            <div className="modal-header">
              <h3>Xác nhận xóa</h3>
              <button className="btn btn-icon" onClick={() => setDeleteConfirm(null)}>
                <span className="material-icons">close</span>
              </button>
            </div>
            <div className="modal-body">
              <p>Bạn có chắc muốn xóa học sinh này? Hành động này không thể hoàn tác.</p>
            </div>
            <div className="modal-footer">
              <button className="btn btn-secondary" onClick={() => setDeleteConfirm(null)}>Hủy</button>
              <button className="btn btn-danger" onClick={() => handleDelete(deleteConfirm)}>Xóa</button>
            </div>
          </div>
        </div>
      )}

      {/* Reset Password Modal */}
      {resetPwdTarget && (
        <div className="modal-overlay" onClick={() => setResetPwdTarget(null)}>
          <div className="modal" style={{ maxWidth: 400 }} onClick={e => e.stopPropagation()}>
            <div className="modal-header">
              <h3>Reset mật khẩu</h3>
              <button className="btn btn-icon" onClick={() => setResetPwdTarget(null)}><span className="material-icons">close</span></button>
            </div>
            <div className="modal-body">
              <p style={{ marginBottom: 12, fontSize: 13 }}>Reset mật khẩu cho: <strong>{resetPwdTarget.name}</strong></p>
              <div className="form-group">
                <label className="form-label">Mật khẩu mới</label>
                <input type="password" className="form-control" value={resetPwdValue} onChange={e => setResetPwdValue(e.target.value)} minLength={6} placeholder="Nhập mật khẩu mới (tối thiểu 6 ký tự)" />
              </div>
            </div>
            <div className="modal-footer">
              <button className="btn btn-secondary" onClick={() => setResetPwdTarget(null)}>Hủy</button>
              <button className="btn btn-primary" onClick={handleResetPassword}>Reset</button>
            </div>
          </div>
        </div>
      )}

      {createdAccount && (
        <div className="modal-overlay">
          <div className="modal" style={{ maxWidth: 420 }}>
            <div className="modal-header">
              <h3>Thông tin tài khoản học sinh</h3>
              <button className="btn btn-icon" onClick={() => setCreatedAccount(null)}><span className="material-icons">close</span></button>
            </div>
            <div className="modal-body">
              <div style={{ background: 'var(--bg-secondary, #f5f5f5)', borderRadius: 8, padding: 16 }}>
                <p style={{ marginBottom: 8 }}><strong>Tên đăng nhập:</strong> {createdAccount.username}</p>
                <p style={{ marginBottom: 0 }}><strong>Mật khẩu:</strong> {createdAccount.password}</p>
              </div>
              <p style={{ marginTop: 12, fontSize: 13, color: 'var(--text-muted)' }}>Vui lòng lưu lại thông tin tài khoản này. Mật khẩu sẽ không thể xem lại sau khi đóng.</p>
            </div>
            <div className="modal-footer">
              <button className="btn btn-primary" onClick={() => setCreatedAccount(null)}>Đã hiểu</button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
