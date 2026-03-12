import { useState, useEffect, useCallback, useRef } from 'react'
import { teachersApi } from '../api/teachers'
import { subjectsApi } from '../api/subjects'
import { classesApi } from '../api/classes'
import { usersApi } from '../api/users'
import { teachingAssignmentsApi } from '../api/teachingAssignments'
import type { TeacherResponse, CreateTeacherRequest, SubjectResponse, ClassResponse } from '../types/api'

interface TeachingAssignment { id: number; classId: number; className: string; subjectId: number; subjectName: string; subjectCode: string; academicYear: string; semester: number }

interface FormData extends CreateTeacherRequest {}

const emptyForm: FormData = {
  username: '', email: '', password: '', fullName: '', employeeId: '', department: ''
}

export default function TeachersPage() {
  const [teachers, setTeachers] = useState<TeacherResponse[]>([])
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
  const [subjects, setSubjects] = useState<SubjectResponse[]>([])
  const [filterDept, setFilterDept] = useState('')
  const [filterStatus, setFilterStatus] = useState<'' | 'active' | 'inactive'>('')
  const [importResult, setImportResult] = useState<string | null>(null)
  const [createdAccount, setCreatedAccount] = useState<{ username: string; password: string } | null>(null)
  const [classHomeroomMap, setClassHomeroomMap] = useState<Record<number, string[]>>({})
  const [allClasses, setAllClasses] = useState<ClassResponse[]>([])
  const [detailTeacher, setDetailTeacher] = useState<TeacherResponse | null>(null)
  const [teacherAssignments, setTeacherAssignments] = useState<TeachingAssignment[]>([])
  const [assignForm, setAssignForm] = useState({ classId: 0, subjectId: 0, academicYear: new Date().getFullYear() + '-' + (new Date().getFullYear() + 1), semester: 1 })
  const [assignError, setAssignError] = useState('')
  const [selectedHomeroomClassId, setSelectedHomeroomClassId] = useState<number>(0)
  const fileInputRef = useRef<HTMLInputElement>(null)
  const pageSize = 20

  useEffect(() => {
    subjectsApi.getAll(1, 50).then(res => {
      setSubjects(res.data.data?.subjects || [])
    }).catch(() => {})
    classesApi.getAll(1, 500).then(res => {
      setAllClasses(res.data.data?.classes || [])
    }).catch(() => {})
  }, [])

  const fetchTeachers = useCallback(async () => {
    setLoading(true)
    try {
      if (search.trim()) {
        const res = await teachersApi.search(search)
        const data = res.data.data || []
        setTeachers(Array.isArray(data) ? data : [])
        setTotal(Array.isArray(data) ? data.length : 0)
      } else {
        const res = await teachersApi.getAll(page, pageSize)
        const d = res.data.data
        setTeachers(d?.teachers || [])
        setTotal(d?.totalCount || 0)
      }
    } catch { setTeachers([]); setTotal(0) }
    finally { setLoading(false) }
  }, [page, search])

  useEffect(() => { fetchTeachers() }, [fetchTeachers])

  useEffect(() => {
    classesApi.getAll(1, 500).then(res => {
      const list = res.data.data?.classes || []
      const map: Record<number, string[]> = {}
      list.forEach((c: ClassResponse) => {
        if (!c.homeroomTeacherId) return
        if (!map[c.homeroomTeacherId]) map[c.homeroomTeacherId] = []
        map[c.homeroomTeacherId].push(c.name)
      })
      setClassHomeroomMap(map)
    }).catch(() => setClassHomeroomMap({}))
  }, [teachers])

  const filteredTeachers = teachers.filter(t => {
    if (filterDept && t.department !== filterDept) return false
    if (filterStatus === 'active' && !t.isActive) return false
    if (filterStatus === 'inactive' && t.isActive) return false
    return true
  })

  const openCreate = () => { setForm(emptyForm); setEditId(null); setSelectedHomeroomClassId(0); setError(''); setModal('create') }
  const openEdit = (t: TeacherResponse) => {
    setForm({ username: t.username, email: t.email, password: '', fullName: t.fullName, employeeId: t.employeeId, department: t.department })
    const homeroomClass = allClasses.find(c => c.homeroomTeacherId === t.id)
    setEditId(t.id); setSelectedHomeroomClassId(homeroomClass?.id || 0); setError(''); setModal('edit')
  }

  const openDetail = async (t: TeacherResponse) => {
    setDetailTeacher(t)
    setAssignError('')
    setAssignForm({ classId: 0, subjectId: 0, academicYear: new Date().getFullYear() + '-' + (new Date().getFullYear() + 1), semester: 1 })
    try {
      const res = await teachingAssignmentsApi.getByTeacher(t.id)
      setTeacherAssignments(res.data.data || [])
    } catch { setTeacherAssignments([]) }
  }

  const handleAddAssignment = async () => {
    if (!detailTeacher || !assignForm.classId || !assignForm.subjectId) {
      setAssignError('Vui lòng chọn lớp và môn học'); return
    }
    try {
      await teachingAssignmentsApi.create({ ...assignForm, teacherId: detailTeacher.id })
      const res = await teachingAssignmentsApi.getByTeacher(detailTeacher.id)
      setTeacherAssignments(res.data.data || [])
      setAssignForm(f => ({ ...f, classId: 0, subjectId: 0 }))
      setAssignError('')
    } catch (e: unknown) {
      const err = e as { response?: { data?: { message?: string } } }
      setAssignError(err.response?.data?.message || 'Lỗi thêm phân công')
    }
  }

  const handleDeleteAssignment = async (id: number) => {
    if (!detailTeacher) return
    try {
      await teachingAssignmentsApi.delete(id)
      const res = await teachingAssignmentsApi.getByTeacher(detailTeacher.id)
      setTeacherAssignments(res.data.data || [])
    } catch { /* ignore */ }
  }

  const handleSave = async () => {
    if (!form.username || !form.email || !form.fullName || !form.employeeId) {
      setError('Vui lòng điền đầy đủ thông tin bắt buộc'); return
    }
    setSaving(true); setError('')
    try {
      if (modal === 'create') {
        if (!form.password) { setError('Vui lòng nhập mật khẩu'); setSaving(false); return }
        const res = await teachersApi.create(form)
        const newId = res.data.data?.id
        // Set homeroom if selected
        if (selectedHomeroomClassId && newId) {
          try { await classesApi.update(selectedHomeroomClassId, { homeroomTeacherId: newId } as Partial<import('../types/api').CreateClassRequest> & { homeroomTeacherId: number }) } catch { /* best-effort */ }
        }
        setCreatedAccount({ username: form.username, password: form.password })
      } else if (editId) {
        await teachersApi.update(editId, form)
        // Update homeroom class
        if (selectedHomeroomClassId) {
          try { await classesApi.update(selectedHomeroomClassId, { homeroomTeacherId: editId } as Partial<import('../types/api').CreateClassRequest> & { homeroomTeacherId: number }) } catch { /* best-effort */ }
        }
      }
      setModal(null); fetchTeachers()
    } catch (e: unknown) {
      const err = e as { response?: { data?: { message?: string } } }
      setError(err.response?.data?.message || 'Lỗi lưu giáo viên')
    } finally { setSaving(false) }
  }

  const handleDelete = async (id: number) => {
    try { await teachersApi.delete(id); fetchTeachers() }
    catch { alert('Không thể xóa giáo viên này') }
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
      const res = await teachersApi.exportFile()
      const url = window.URL.createObjectURL(new Blob([res.data]))
      const a = document.createElement('a')
      a.href = url; a.download = 'teachers.xlsx'; a.click()
      window.URL.revokeObjectURL(url)
    } catch { alert('Lỗi khi xuất file') }
  }

  const handleImport = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    if (!file) return
    try {
      const res = await teachersApi.importFile(file)
      const d = res.data.data as Record<string, unknown> | undefined
      setImportResult(`Import hoàn tất: ${d?.successCount ?? 0} thành công, ${d?.failedCount ?? 0} lỗi`)
      fetchTeachers()
    } catch {
      setImportResult('Lỗi khi import file')
    }
    if (fileInputRef.current) fileInputRef.current.value = ''
  }

  const totalPages = Math.ceil(total / pageSize)
  const deptList = [...new Set(teachers.map(t => t.department).filter(Boolean))]

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 20 }}>
        <div>
          <h2 style={{ marginBottom: 2 }}>Danh sách Giáo viên</h2>
          <p style={{ fontSize: 13 }}>{total} giáo viên trong hệ thống</p>
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
            Thêm giáo viên
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
              placeholder="Tìm giáo viên..."
              value={search}
              onChange={e => { setSearch(e.target.value); setPage(1) }}
            />
          </div>
          <select className="form-control" style={{ minWidth: 200, width: 'auto' }} value={filterDept} onChange={e => setFilterDept(e.target.value)}>
            <option value="">Tất cả bộ môn</option>
            {deptList.map(d => <option key={d} value={d}>{d}</option>)}
          </select>
          <select className="form-control" style={{ minWidth: 170, width: 'auto' }} value={filterStatus} onChange={e => setFilterStatus(e.target.value as '' | 'active' | 'inactive')}>
            <option value="">Tất cả trạng thái</option>
            <option value="active">Hoạt động</option>
            <option value="inactive">Tạm khóa</option>
          </select>
        </div>

        {loading ? (
          <div className="loading-center"><div className="spinner" /></div>
        ) : filteredTeachers.length === 0 ? (
          <div className="empty-state">
            <span className="material-icons">manage_accounts</span>
            <p>Chưa có giáo viên nào</p>
          </div>
        ) : (
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>#</th>
                  <th>Họ tên</th>
                  <th>Mã GV</th>
                  <th>Email</th>
                  <th>Bộ môn</th>
                  <th>Chủ nhiệm</th>
                  <th>Trạng thái</th>
                  <th>Ngày tạo</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                {filteredTeachers.map((t, idx) => (
                  <tr key={t.id}>
                    <td style={{ color: 'var(--text-muted)' }}>{(page - 1) * pageSize + idx + 1}</td>
                    <td>
                      <div style={{ fontWeight: 500 }}>{t.fullName}</div>
                      <div style={{ fontSize: 12, color: 'var(--text-muted)' }}>{t.username}</div>
                    </td>
                    <td><span className="badge badge-blue">{t.employeeId}</span></td>
                    <td>{t.email}</td>
                    <td>{t.department || '—'}</td>
                    <td>
                      {(classHomeroomMap[t.id] && classHomeroomMap[t.id].length > 0)
                        ? classHomeroomMap[t.id].join(', ')
                        : '—'}
                    </td>
                    <td>
                      {t.isActive
                        ? <span className="badge badge-green">Hoạt động</span>
                        : <span className="badge badge-red">Tạm khóa</span>}
                    </td>
                    <td style={{ color: 'var(--text-muted)', fontSize: 12 }}>
                      {new Date(t.createdAt).toLocaleDateString('vi-VN')}
                    </td>
                    <td>
                      <div className="actions">
                        <button className="btn-icon btn" title="Phân công" onClick={() => openDetail(t)}>
                          <span className="material-icons" style={{ fontSize: 18 }}>assignment</span>
                        </button>
                        <button className="btn-icon btn" title="Sửa" onClick={() => openEdit(t)}>
                          <span className="material-icons" style={{ fontSize: 18 }}>edit</span>
                        </button>
                        <button className="btn-icon btn" title="Reset mật khẩu" onClick={() => setResetPwdTarget({ userId: t.userId, name: t.fullName })} style={{ color: 'var(--warning, #f59e0b)' }}>
                          <span className="material-icons" style={{ fontSize: 18 }}>lock_reset</span>
                        </button>
                        <button className="btn-icon btn" title="Xóa" onClick={() => setDeleteConfirm(t.id)} style={{ color: 'var(--danger)' }}>
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
              <h3>{modal === 'create' ? 'Thêm giáo viên mới' : 'Cập nhật giáo viên'}</h3>
              <button className="btn btn-icon" onClick={() => setModal(null)}>
                <span className="material-icons">close</span>
              </button>
            </div>
            <div className="modal-body">
              {error && <div className="alert alert-error"><span className="material-icons" style={{ fontSize: 18 }}>error</span>{error}</div>}
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '0 16px' }}>
                <div className="form-group">
                  <label className="form-label">Họ và tên *</label>
                  <input className="form-control" value={form.fullName} onChange={e => setForm(f => ({ ...f, fullName: e.target.value }))} />
                </div>
                <div className="form-group">
                  <label className="form-label">Tên đăng nhập *</label>
                  <input className="form-control" value={form.username} onChange={e => setForm(f => ({ ...f, username: e.target.value }))} disabled={modal === 'edit'} />
                </div>
                <div className="form-group">
                  <label className="form-label">Email *</label>
                  <input className="form-control" type="email" value={form.email} onChange={e => setForm(f => ({ ...f, email: e.target.value }))} />
                </div>
                <div className="form-group">
                  <label className="form-label">Mã giáo viên *</label>
                  <input className="form-control" value={form.employeeId} onChange={e => setForm(f => ({ ...f, employeeId: e.target.value }))} />
                </div>
                <div className="form-group">
                  <label className="form-label">Bộ môn</label>
                  <select className="form-control" value={form.department || ''} onChange={e => setForm(f => ({ ...f, department: e.target.value }))}>
                    <option value="">-- Chọn bộ môn --</option>
                    {subjects.map(s => (
                      <option key={s.id} value={s.name}>{s.name}</option>
                    ))}
                  </select>
                </div>
                {modal === 'create' && (
                  <div className="form-group">
                    <label className="form-label">Mật khẩu *</label>
                    <input className="form-control" type="password" value={form.password} onChange={e => setForm(f => ({ ...f, password: e.target.value }))} />
                  </div>
                )}
                <div className="form-group">
                  <label className="form-label">Chủ nhiệm lớp</label>
                  <select className="form-control" value={selectedHomeroomClassId} onChange={e => setSelectedHomeroomClassId(Number(e.target.value))}>
                    <option value={0}>-- Không chọn --</option>
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

      {deleteConfirm !== null && (
        <div className="modal-overlay">
          <div className="modal" style={{ maxWidth: 380 }}>
            <div className="modal-header">
              <h3>Xác nhận xóa</h3>
              <button className="btn btn-icon" onClick={() => setDeleteConfirm(null)}><span className="material-icons">close</span></button>
            </div>
            <div className="modal-body"><p>Bạn có chắc muốn xóa giáo viên này?</p></div>
            <div className="modal-footer">
              <button className="btn btn-secondary" onClick={() => setDeleteConfirm(null)}>Hủy</button>
              <button className="btn btn-danger" onClick={() => handleDelete(deleteConfirm)}>Xóa</button>
            </div>
          </div>
        </div>
      )}

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
              <h3>Thông tin tài khoản giáo viên</h3>
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

      {/* Teaching Assignment Detail Modal */}
      {detailTeacher && (
        <div className="modal-overlay" onClick={e => e.target === e.currentTarget && setDetailTeacher(null)}>
          <div className="modal" style={{ maxWidth: 680 }}>
            <div className="modal-header">
              <h3>Phân công giảng dạy — {detailTeacher.fullName}</h3>
              <button className="btn btn-icon" onClick={() => setDetailTeacher(null)}><span className="material-icons">close</span></button>
            </div>
            <div className="modal-body">
              {teacherAssignments.length > 0 ? (
                <div className="table-wrap" style={{ marginBottom: 16 }}>
                  <table>
                    <thead>
                      <tr><th>Lớp</th><th>Môn học</th><th>Năm học</th><th>HK</th><th></th></tr>
                    </thead>
                    <tbody>
                      {teacherAssignments.map(a => (
                        <tr key={a.id}>
                          <td>{a.className}</td>
                          <td>{a.subjectName}</td>
                          <td>{a.academicYear}</td>
                          <td>{a.semester}</td>
                          <td>
                            <button className="btn-icon btn" title="Xóa" onClick={() => handleDeleteAssignment(a.id)} style={{ color: 'var(--danger)' }}>
                              <span className="material-icons" style={{ fontSize: 18 }}>delete</span>
                            </button>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              ) : (
                <p style={{ color: 'var(--text-muted)', marginBottom: 16 }}>Chưa có phân công giảng dạy nào.</p>
              )}
              <div style={{ borderTop: '1px solid var(--border)', paddingTop: 16 }}>
                <div style={{ fontSize: 13, fontWeight: 600, marginBottom: 8 }}>Thêm phân công mới</div>
                {assignError && <div className="alert alert-error" style={{ marginBottom: 8 }}><span className="material-icons" style={{ fontSize: 18 }}>error</span>{assignError}</div>}
                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '0 12px' }}>
                  <div className="form-group">
                    <label className="form-label">Lớp *</label>
                    <select className="form-control" value={assignForm.classId} onChange={e => setAssignForm(f => ({ ...f, classId: Number(e.target.value) }))}>
                      <option value={0}>-- Chọn lớp --</option>
                      {allClasses.map(c => <option key={c.id} value={c.id}>{c.name} ({c.code})</option>)}
                    </select>
                  </div>
                  <div className="form-group">
                    <label className="form-label">Môn học *</label>
                    <select className="form-control" value={assignForm.subjectId} onChange={e => setAssignForm(f => ({ ...f, subjectId: Number(e.target.value) }))}>
                      <option value={0}>-- Chọn môn --</option>
                      {subjects.map(s => <option key={s.id} value={s.id}>{s.name}</option>)}
                    </select>
                  </div>
                  <div className="form-group">
                    <label className="form-label">Năm học</label>
                    <input className="form-control" value={assignForm.academicYear} onChange={e => setAssignForm(f => ({ ...f, academicYear: e.target.value }))} placeholder="2024-2025" />
                  </div>
                  <div className="form-group">
                    <label className="form-label">Học kỳ</label>
                    <select className="form-control" value={assignForm.semester} onChange={e => setAssignForm(f => ({ ...f, semester: Number(e.target.value) }))}>
                      <option value={1}>Học kỳ 1</option>
                      <option value={2}>Học kỳ 2</option>
                    </select>
                  </div>
                </div>
                <button className="btn btn-primary" onClick={handleAddAssignment} style={{ marginTop: 4 }}>
                  <span className="material-icons" style={{ fontSize: 18 }}>add</span> Thêm phân công
                </button>
              </div>
            </div>
            <div className="modal-footer">
              <button className="btn btn-secondary" onClick={() => setDetailTeacher(null)}>Đóng</button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
