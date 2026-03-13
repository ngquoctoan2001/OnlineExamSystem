import { useState, useEffect, useCallback } from 'react'
import { usersApi } from '../api/users'
import type { UserDto, CreateUserRequest, RoleDto } from '../types/api'

const roleLabel: Record<string, string> = {
  ADMIN: 'Quản trị viên',
  TEACHER: 'Giáo viên',
  STUDENT: 'Học sinh',
}

export default function UsersPage() {
  const [users, setUsers] = useState<UserDto[]>([])
  const [total, setTotal] = useState(0)
  const [page, setPage] = useState(1)
  const [loading, setLoading] = useState(true)
  const [roles, setRoles] = useState<RoleDto[]>([])
  const [modal, setModal] = useState<'create' | 'edit' | null>(null)
  const [editUser, setEditUser] = useState<UserDto | null>(null)
  const [form, setForm] = useState<CreateUserRequest>({ username: '', email: '', password: '', fullName: '', roles: [] })
  const [editForm, setEditForm] = useState({ email: '', fullName: '', isActive: true, roles: [] as string[] })
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const [deleteConfirm, setDeleteConfirm] = useState<number | null>(null)
  const [resetPwdTarget, setResetPwdTarget] = useState<UserDto | null>(null)
  const [resetPwdValue, setResetPwdValue] = useState('')
  const [searchText, setSearchText] = useState('')
  const pageSize = 20

  const fetchData = useCallback(async () => {
    setLoading(true)
    try {
      const res = await usersApi.getAll(page, pageSize)
      const data = res.data.data || []
      setUsers(data)
      // Parse total from message "Retrieved X users out of Y total"
      const match = res.data.message?.match(/out of (\d+) total/)
      setTotal(match ? parseInt(match[1]) : data.length)
    } catch { setUsers([]); setTotal(0) }
    finally { setLoading(false) }
  }, [page])

  useEffect(() => { fetchData() }, [fetchData])
  useEffect(() => {
    usersApi.getRoles().then(r => setRoles(r.data.data || [])).catch(() => {})
  }, [])

  const filtered = searchText.trim()
    ? users.filter(u =>
        u.username.toLowerCase().includes(searchText.toLowerCase()) ||
        u.fullName.toLowerCase().includes(searchText.toLowerCase()) ||
        u.email.toLowerCase().includes(searchText.toLowerCase())
      )
    : users

  const openCreate = () => {
    setForm({ username: '', email: '', password: '', fullName: '', roles: [] })
    setError('')
    setModal('create')
  }

  const openEdit = (u: UserDto) => {
    setEditUser(u)
    setEditForm({ email: u.email, fullName: u.fullName, isActive: u.isActive, roles: [...u.roles] })
    setError('')
    setModal('edit')
  }

  const handleCreate = async () => {
    if (!form.username.trim() || !form.email.trim() || !form.password.trim() || !form.fullName.trim()) {
      setError('Vui lòng điền đầy đủ thông tin'); return
    }
    if (form.roles.length === 0) { setError('Vui lòng chọn ít nhất 1 vai trò'); return }
    setSaving(true); setError('')
    try {
      await usersApi.create(form)
      setModal(null); fetchData()
    } catch (e: unknown) {
      const err = e as { response?: { data?: { message?: string } } }
      setError(err.response?.data?.message || 'Lỗi tạo tài khoản')
    } finally { setSaving(false) }
  }

  const handleUpdate = async () => {
    if (!editUser) return
    if (!editForm.email.trim() || !editForm.fullName.trim()) { setError('Vui lòng điền đầy đủ thông tin'); return }
    if (editForm.roles.length === 0) { setError('Vui lòng chọn ít nhất 1 vai trò'); return }
    setSaving(true); setError('')
    try {
      await usersApi.update(editUser.id, editForm)
      setModal(null); setEditUser(null); fetchData()
    } catch (e: unknown) {
      const err = e as { response?: { data?: { message?: string } } }
      setError(err.response?.data?.message || 'Lỗi cập nhật')
    } finally { setSaving(false) }
  }

  const handleDelete = async (id: number) => {
    try { await usersApi.delete(id); fetchData() }
    catch { alert('Không thể xóa tài khoản này') }
    finally { setDeleteConfirm(null) }
  }

  const handleToggleActive = async (u: UserDto) => {
    try {
      await usersApi.toggleActive(u.id, !u.isActive)
      fetchData()
    } catch { alert('Không thể thay đổi trạng thái') }
  }

  const handleResetPwd = async () => {
    if (!resetPwdTarget || resetPwdValue.length < 6) {
      alert('Mật khẩu phải ít nhất 6 ký tự'); return
    }
    try {
      await usersApi.resetPassword(resetPwdTarget.id, resetPwdValue)
      alert('Đặt lại mật khẩu thành công')
      setResetPwdTarget(null); setResetPwdValue('')
    } catch { alert('Lỗi đặt lại mật khẩu') }
  }

  const toggleRole = (roleName: string, formRoles: string[], setter: (roles: string[]) => void) => {
    if (formRoles.includes(roleName)) {
      setter(formRoles.filter(r => r !== roleName))
    } else {
      setter([...formRoles, roleName])
    }
  }

  const totalPages = Math.ceil(total / pageSize)

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 20 }}>
        <div>
          <h2 style={{ marginBottom: 2 }}>Quản lý Tài khoản</h2>
          <p style={{ fontSize: 13 }}>{total} tài khoản trong hệ thống</p>
        </div>
        <button className="btn btn-primary" onClick={openCreate}>
          <span className="material-icons" style={{ fontSize: 18 }}>person_add</span>
          Thêm tài khoản
        </button>
      </div>

      <div className="card">
        <div className="search-bar">
          <div className="input-group search-input">
            <span className="material-icons input-icon">search</span>
            <input
              className="form-control"
              placeholder="Tìm theo tên, username, email..."
              value={searchText}
              onChange={e => setSearchText(e.target.value)}
            />
          </div>
        </div>

        {loading ? (
          <div className="loading-center"><div className="spinner" /></div>
        ) : filtered.length === 0 ? (
          <div className="empty-state">
            <span className="material-icons">people</span>
            <p>Chưa có tài khoản nào</p>
          </div>
        ) : (
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>#</th>
                  <th>Username</th>
                  <th>Họ tên</th>
                  <th>Email</th>
                  <th>Vai trò</th>
                  <th>Trạng thái</th>
                  <th>Ngày tạo</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                {filtered.map((u, idx) => (
                  <tr key={u.id}>
                    <td style={{ color: 'var(--text-muted)' }}>{(page - 1) * pageSize + idx + 1}</td>
                    <td style={{ fontWeight: 500 }}>{u.username}</td>
                    <td>{u.fullName}</td>
                    <td style={{ color: 'var(--text-muted)', fontSize: 13 }}>{u.email}</td>
                    <td>
                      <div style={{ display: 'flex', gap: 4, flexWrap: 'wrap' }}>
                        {u.roles.map(r => (
                          <span key={r} className={`badge ${r === 'ADMIN' ? 'badge-red' : r === 'TEACHER' ? 'badge-purple' : 'badge-blue'}`}>
                            {roleLabel[r] || r}
                          </span>
                        ))}
                      </div>
                    </td>
                    <td>
                      <span
                        className={`badge ${u.isActive ? 'badge-green' : 'badge-gray'}`}
                        style={{ cursor: 'pointer' }}
                        onClick={() => handleToggleActive(u)}
                        title="Bấm để thay đổi"
                      >
                        {u.isActive ? 'Hoạt động' : 'Đã khóa'}
                      </span>
                    </td>
                    <td style={{ color: 'var(--text-muted)', fontSize: 12 }}>
                      {new Date(u.createdAt).toLocaleDateString('vi-VN')}
                    </td>
                    <td>
                      <div className="actions">
                        <button className="btn-icon btn" title="Sửa" onClick={() => openEdit(u)} style={{ color: 'var(--primary)' }}>
                          <span className="material-icons" style={{ fontSize: 18 }}>edit</span>
                        </button>
                        <button className="btn-icon btn" title="Đặt lại mật khẩu" onClick={() => { setResetPwdTarget(u); setResetPwdValue('') }}>
                          <span className="material-icons" style={{ fontSize: 18 }}>lock_reset</span>
                        </button>
                        <button className="btn-icon btn" title="Xóa" onClick={() => setDeleteConfirm(u.id)} style={{ color: 'var(--danger)' }}>
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

      {/* Create Modal */}
      {modal === 'create' && (
        <div className="modal-overlay" onClick={e => e.target === e.currentTarget && setModal(null)}>
          <div className="modal" style={{ maxWidth: 520 }}>
            <div className="modal-header">
              <h3>Thêm tài khoản mới</h3>
              <button className="btn btn-icon" onClick={() => setModal(null)}><span className="material-icons">close</span></button>
            </div>
            <div className="modal-body">
              {error && <div className="alert alert-error"><span className="material-icons" style={{ fontSize: 18 }}>error</span>{error}</div>}
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '0 16px' }}>
                <div className="form-group">
                  <label className="form-label">Username *</label>
                  <input className="form-control" value={form.username} onChange={e => setForm(f => ({ ...f, username: e.target.value }))} placeholder="Tên đăng nhập" />
                </div>
                <div className="form-group">
                  <label className="form-label">Mật khẩu *</label>
                  <input className="form-control" type="password" value={form.password} onChange={e => setForm(f => ({ ...f, password: e.target.value }))} placeholder="Mật khẩu" />
                </div>
                <div className="form-group" style={{ gridColumn: 'span 2' }}>
                  <label className="form-label">Họ tên *</label>
                  <input className="form-control" value={form.fullName} onChange={e => setForm(f => ({ ...f, fullName: e.target.value }))} placeholder="Họ và tên" />
                </div>
                <div className="form-group" style={{ gridColumn: 'span 2' }}>
                  <label className="form-label">Email *</label>
                  <input className="form-control" type="email" value={form.email} onChange={e => setForm(f => ({ ...f, email: e.target.value }))} placeholder="Email" />
                </div>
              </div>
              <div className="form-group">
                <label className="form-label">Vai trò *</label>
                <div style={{ display: 'flex', gap: 12, flexWrap: 'wrap' }}>
                  {roles.map(r => (
                    <label key={r.id} style={{ display: 'flex', alignItems: 'center', gap: 6, cursor: 'pointer', fontSize: 13 }}>
                      <input
                        type="checkbox"
                        checked={form.roles.includes(r.name)}
                        onChange={() => toggleRole(r.name, form.roles, (roles) => setForm(f => ({ ...f, roles })))}
                        style={{ accentColor: 'var(--primary)' }}
                      />
                      {roleLabel[r.name] || r.name}
                    </label>
                  ))}
                </div>
              </div>
            </div>
            <div className="modal-footer">
              <button className="btn btn-secondary" onClick={() => setModal(null)}>Hủy</button>
              <button className="btn btn-primary" onClick={handleCreate} disabled={saving}>
                {saving ? 'Đang lưu...' : 'Tạo tài khoản'}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Edit Modal */}
      {modal === 'edit' && editUser && (
        <div className="modal-overlay" onClick={e => e.target === e.currentTarget && setModal(null)}>
          <div className="modal" style={{ maxWidth: 520 }}>
            <div className="modal-header">
              <h3>Chỉnh sửa tài khoản</h3>
              <button className="btn btn-icon" onClick={() => setModal(null)}><span className="material-icons">close</span></button>
            </div>
            <div className="modal-body">
              {error && <div className="alert alert-error"><span className="material-icons" style={{ fontSize: 18 }}>error</span>{error}</div>}
              <div className="form-group">
                <label className="form-label">Username</label>
                <input className="form-control" value={editUser.username} disabled style={{ background: 'var(--surface-alt)' }} />
              </div>
              <div className="form-group">
                <label className="form-label">Họ tên *</label>
                <input className="form-control" value={editForm.fullName} onChange={e => setEditForm(f => ({ ...f, fullName: e.target.value }))} />
              </div>
              <div className="form-group">
                <label className="form-label">Email *</label>
                <input className="form-control" type="email" value={editForm.email} onChange={e => setEditForm(f => ({ ...f, email: e.target.value }))} />
              </div>
              <div className="form-group">
                <label className="form-label">Trạng thái</label>
                <label style={{ display: 'flex', alignItems: 'center', gap: 8, cursor: 'pointer', fontSize: 13 }}>
                  <input
                    type="checkbox"
                    checked={editForm.isActive}
                    onChange={e => setEditForm(f => ({ ...f, isActive: e.target.checked }))}
                    style={{ accentColor: 'var(--primary)' }}
                  />
                  Tài khoản hoạt động
                </label>
              </div>
              <div className="form-group">
                <label className="form-label">Vai trò *</label>
                <div style={{ display: 'flex', gap: 12, flexWrap: 'wrap' }}>
                  {roles.map(r => (
                    <label key={r.id} style={{ display: 'flex', alignItems: 'center', gap: 6, cursor: 'pointer', fontSize: 13 }}>
                      <input
                        type="checkbox"
                        checked={editForm.roles.includes(r.name)}
                        onChange={() => toggleRole(r.name, editForm.roles, (roles) => setEditForm(f => ({ ...f, roles })))}
                        style={{ accentColor: 'var(--primary)' }}
                      />
                      {roleLabel[r.name] || r.name}
                    </label>
                  ))}
                </div>
              </div>
            </div>
            <div className="modal-footer">
              <button className="btn btn-secondary" onClick={() => setModal(null)}>Hủy</button>
              <button className="btn btn-primary" onClick={handleUpdate} disabled={saving}>
                {saving ? 'Đang lưu...' : 'Cập nhật'}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Reset Password Modal */}
      {resetPwdTarget && (
        <div className="modal-overlay" onClick={e => e.target === e.currentTarget && setResetPwdTarget(null)} style={{ position: 'fixed', top: 0, left: 0, right: 0, bottom: 0, zIndex: 1000 }}>
          <div className="modal" style={{ maxWidth: 400, position: 'relative' }}>
            <div className="modal-header">
              <h3>Đặt lại mật khẩu</h3>
              <button className="btn btn-icon" onClick={() => setResetPwdTarget(null)}><span className="material-icons">close</span></button>
            </div>
            <div className="modal-body">
              <p style={{ fontSize: 13, marginBottom: 12 }}>
                Đặt mật khẩu mới cho <strong>{resetPwdTarget.fullName}</strong> ({resetPwdTarget.username})
              </p>
              <div className="form-group">
                <label className="form-label">Mật khẩu mới *</label>
                <input
                  className="form-control"
                  type="password"
                  value={resetPwdValue}
                  onChange={e => setResetPwdValue(e.target.value)}
                  placeholder="Ít nhất 6 ký tự"
                  autoFocus
                />
              </div>
            </div>
            <div className="modal-footer">
              <button className="btn btn-secondary" onClick={() => setResetPwdTarget(null)}>Hủy</button>
              <button className="btn btn-primary" onClick={handleResetPwd} disabled={saving}>
                {saving ? 'Đang lưu...' : 'Đặt lại'}
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
              <button className="btn btn-icon" onClick={() => setDeleteConfirm(null)}><span className="material-icons">close</span></button>
            </div>
            <div className="modal-body"><p>Bạn có chắc muốn xóa tài khoản này? Thao tác không thể hoàn tác.</p></div>
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
