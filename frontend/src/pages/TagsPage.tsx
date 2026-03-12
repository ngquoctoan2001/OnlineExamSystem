import { useState, useEffect, useCallback } from 'react'
import { tagsApi } from '../api/tags'
import type { TagResponse, CreateTagRequest } from '../types/api'

export default function TagsPage() {
  const [tags, setTags] = useState<TagResponse[]>([])
  const [loading, setLoading] = useState(true)
  const [search, setSearch] = useState('')
  const [modal, setModal] = useState<'create' | 'edit' | null>(null)
  const [editId, setEditId] = useState<number | null>(null)
  const [form, setForm] = useState<CreateTagRequest>({ name: '', description: '' })
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const [deleteConfirm, setDeleteConfirm] = useState<number | null>(null)

  const fetchData = useCallback(async () => {
    setLoading(true)
    try {
      const res = await tagsApi.getAll(search.trim() || undefined)
      setTags(res.data.data || [])
    } catch { setTags([]) }
    finally { setLoading(false) }
  }, [search])

  useEffect(() => { fetchData() }, [fetchData])

  const openCreate = () => {
    setForm({ name: '', description: '' })
    setEditId(null)
    setError('')
    setModal('create')
  }

  const openEdit = (tag: TagResponse) => {
    setForm({ name: tag.name, description: tag.description || '' })
    setEditId(tag.id)
    setError('')
    setModal('edit')
  }

  const handleSave = async () => {
    if (!form.name.trim()) { setError('Vui lòng nhập tên tag'); return }
    setSaving(true); setError('')
    try {
      if (modal === 'edit' && editId) {
        await tagsApi.update(editId, form)
      } else {
        await tagsApi.create(form)
      }
      setModal(null); fetchData()
    } catch (e: unknown) {
      const err = e as { response?: { data?: { message?: string } } }
      setError(err.response?.data?.message || 'Lỗi lưu tag')
    } finally { setSaving(false) }
  }

  const handleDelete = async (id: number) => {
    try { await tagsApi.delete(id); fetchData() }
    catch { alert('Không thể xóa tag này') }
    finally { setDeleteConfirm(null) }
  }

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 20 }}>
        <div>
          <h2 style={{ marginBottom: 2 }}>Quản lý Tags</h2>
          <p style={{ fontSize: 13 }}>{tags.length} tag trong hệ thống</p>
        </div>
        <button className="btn btn-primary" onClick={openCreate}>
          <span className="material-icons" style={{ fontSize: 18 }}>add</span>
          Thêm tag
        </button>
      </div>

      <div className="card">
        <div className="search-bar">
          <div className="input-group search-input">
            <span className="material-icons input-icon">search</span>
            <input
              className="form-control"
              placeholder="Tìm tag..."
              value={search}
              onChange={e => setSearch(e.target.value)}
            />
          </div>
        </div>

        {loading ? (
          <div className="loading-center"><div className="spinner" /></div>
        ) : tags.length === 0 ? (
          <div className="empty-state">
            <span className="material-icons">label</span>
            <p>Chưa có tag nào</p>
          </div>
        ) : (
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>#</th>
                  <th>Tên tag</th>
                  <th>Mô tả</th>
                  <th>Ngày tạo</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                {tags.map((tag, idx) => (
                  <tr key={tag.id}>
                    <td style={{ color: 'var(--text-muted)' }}>{idx + 1}</td>
                    <td>
                      <span className="badge badge-blue" style={{ cursor: 'pointer' }} onClick={() => openEdit(tag)}>
                        {tag.name}
                      </span>
                    </td>
                    <td style={{ color: 'var(--text-muted)', maxWidth: 400 }}>
                      <div className="truncate">{tag.description || '—'}</div>
                    </td>
                    <td style={{ color: 'var(--text-muted)', fontSize: 12 }}>
                      {new Date(tag.createdAt).toLocaleDateString('vi-VN')}
                    </td>
                    <td>
                      <div className="actions">
                        <button className="btn-icon btn" title="Sửa" onClick={() => openEdit(tag)} style={{ color: 'var(--primary)' }}>
                          <span className="material-icons" style={{ fontSize: 18 }}>edit</span>
                        </button>
                        <button className="btn-icon btn" title="Xóa" onClick={() => setDeleteConfirm(tag.id)} style={{ color: 'var(--danger)' }}>
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
      </div>

      {/* Create / Edit Modal */}
      {modal && (
        <div className="modal-overlay" onClick={e => e.target === e.currentTarget && setModal(null)}>
          <div className="modal" style={{ maxWidth: 460 }}>
            <div className="modal-header">
              <h3>{modal === 'edit' ? 'Chỉnh sửa tag' : 'Thêm tag mới'}</h3>
              <button className="btn btn-icon" onClick={() => setModal(null)}><span className="material-icons">close</span></button>
            </div>
            <div className="modal-body">
              {error && <div className="alert alert-error"><span className="material-icons" style={{ fontSize: 18 }}>error</span>{error}</div>}
              <div className="form-group">
                <label className="form-label">Tên tag *</label>
                <input
                  className="form-control"
                  value={form.name}
                  onChange={e => setForm(f => ({ ...f, name: e.target.value }))}
                  placeholder="Nhập tên tag..."
                />
              </div>
              <div className="form-group">
                <label className="form-label">Mô tả</label>
                <textarea
                  className="form-control"
                  rows={3}
                  value={form.description || ''}
                  onChange={e => setForm(f => ({ ...f, description: e.target.value }))}
                  placeholder="Mô tả tag (tùy chọn)..."
                  style={{ resize: 'vertical' }}
                />
              </div>
            </div>
            <div className="modal-footer">
              <button className="btn btn-secondary" onClick={() => setModal(null)}>Hủy</button>
              <button className="btn btn-primary" onClick={handleSave} disabled={saving}>
                {saving ? 'Đang lưu...' : modal === 'edit' ? 'Cập nhật' : 'Tạo tag'}
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
            <div className="modal-body"><p>Bạn có chắc muốn xóa tag này? Các câu hỏi đã gán tag sẽ bị gỡ liên kết.</p></div>
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
