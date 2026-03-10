import { useState, useEffect, useCallback } from 'react'
import { questionsApi } from '../api/questions'
import { subjectsApi } from '../api/subjects'
import type { QuestionResponse, QuestionDetailResponse, SubjectResponse, QuestionTypeResponse, CreateQuestionRequest } from '../types/api'

const diffLabel: Record<string, { cls: string; text: string }> = {
  EASY:   { cls: 'diff-easy',   text: 'Dễ' },
  MEDIUM: { cls: 'diff-medium', text: 'Trung bình' },
  HARD:   { cls: 'diff-hard',   text: 'Khó' },
}

interface FormState {
  subjectId: number
  questionTypeId: number
  content: string
  difficulty: string
  options: { label: string; content: string; isCorrect: boolean; orderIndex: number }[]
}

const defaultOptions = () => [
  { label: 'A', content: '', isCorrect: false, orderIndex: 0 },
  { label: 'B', content: '', isCorrect: false, orderIndex: 1 },
  { label: 'C', content: '', isCorrect: false, orderIndex: 2 },
  { label: 'D', content: '', isCorrect: false, orderIndex: 3 },
]

const emptyForm = (): FormState => ({
  subjectId: 0, questionTypeId: 1, content: '', difficulty: 'MEDIUM', options: defaultOptions()
})

export default function QuestionsPage() {
  const [questions, setQuestions] = useState<QuestionResponse[]>([])
  const [total, setTotal] = useState(0)
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [filterSubject, setFilterSubject] = useState(0)
  const [filterDifficulty, setFilterDifficulty] = useState('')
  const [loading, setLoading] = useState(true)
  const [subjects, setSubjects] = useState<SubjectResponse[]>([])
  const [qTypes, setQTypes] = useState<QuestionTypeResponse[]>([])
  const [modal, setModal] = useState<'create' | 'view' | null>(null)
  const [form, setForm] = useState<FormState>(emptyForm())
  const [viewDetail, setViewDetail] = useState<QuestionDetailResponse | null>(null)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const [deleteConfirm, setDeleteConfirm] = useState<number | null>(null)
  const pageSize = 20

  const fetchData = useCallback(async () => {
    setLoading(true)
    try {
      if (search.trim()) {
        const res = await questionsApi.search(search)
        const data = res.data.data || []
        setQuestions(Array.isArray(data) ? data : [])
        setTotal(Array.isArray(data) ? data.length : 0)
      } else if (filterSubject) {
        const res = await questionsApi.getBySubject(filterSubject)
        const data = res.data.data || []
        let list = Array.isArray(data) ? data : []
        if (filterDifficulty) list = list.filter(q => q.difficulty === filterDifficulty)
        setQuestions(list); setTotal(list.length)
      } else {
        const res = await questionsApi.getAll(page, pageSize)
        const d = res.data.data
        let list = d?.items || []
        if (filterDifficulty) list = list.filter(q => q.difficulty === filterDifficulty)
        setQuestions(list); setTotal(d?.totalCount || 0)
      }
    } catch { setQuestions([]); setTotal(0) }
    finally { setLoading(false) }
  }, [page, search, filterSubject, filterDifficulty])

  useEffect(() => { fetchData() }, [fetchData])

  useEffect(() => {
    subjectsApi.getAll(1, 100).then(r => setSubjects(r.data.data?.subjects || [])).catch(() => {})
    questionsApi.getTypes().then(r => setQTypes(r.data.data || [])).catch(() => {})
  }, [])

  const openCreate = () => { setForm(emptyForm()); setError(''); setModal('create') }
  const openView = async (q: QuestionResponse) => {
    try {
      const res = await questionsApi.getById(q.id)
      setViewDetail(res.data.data || null)
      setModal('view')
    } catch { setViewDetail(null) }
  }

  const setOptionField = (i: number, field: string, value: string | boolean) => {
    setForm(f => {
      const opts = [...f.options]
      opts[i] = { ...opts[i], [field]: value }
      // Only 1 correct answer for single-choice
      if (field === 'isCorrect' && value === true) {
        opts.forEach((o, j) => { if (j !== i) opts[j] = { ...o, isCorrect: false } })
      }
      return { ...f, options: opts }
    })
  }

  const handleSave = async () => {
    if (!form.subjectId || !form.content.trim()) { setError('Vui lòng chọn môn học và nhập nội dung câu hỏi'); return }
    const filledOpts = form.options.filter(o => o.content.trim())
    if (filledOpts.length < 2) { setError('Vui lòng nhập ít nhất 2 lựa chọn'); return }
    if (!filledOpts.some(o => o.isCorrect)) { setError('Vui lòng chọn ít nhất 1 đáp án đúng'); return }
    setSaving(true); setError('')
    try {
      const payload: CreateQuestionRequest = {
        subjectId: form.subjectId,
        questionTypeId: form.questionTypeId || 1,
        content: form.content,
        difficulty: form.difficulty,
        options: filledOpts,
      }
      await questionsApi.create(payload)
      setModal(null); fetchData()
    } catch (e: unknown) {
      const err = e as { response?: { data?: { message?: string } } }
      setError(err.response?.data?.message || 'Lỗi tạo câu hỏi')
    } finally { setSaving(false) }
  }

  const handleDelete = async (id: number) => {
    try { await questionsApi.delete(id); fetchData() }
    catch { alert('Không thể xóa câu hỏi này') }
    finally { setDeleteConfirm(null) }
  }

  const handlePublish = async (id: number) => {
    try { await questionsApi.publish(id); fetchData() }
    catch { alert('Không thể xuất bản câu hỏi') }
  }

  const totalPages = Math.ceil(total / pageSize)

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 20 }}>
        <div>
          <h2 style={{ marginBottom: 2 }}>Ngân hàng Câu hỏi</h2>
          <p style={{ fontSize: 13 }}>{total} câu hỏi trong hệ thống</p>
        </div>
        <button className="btn btn-primary" onClick={openCreate}>
          <span className="material-icons" style={{ fontSize: 18 }}>add</span>
          Thêm câu hỏi
        </button>
      </div>

      <div className="card">
        <div className="search-bar">
          <div className="input-group search-input">
            <span className="material-icons input-icon">search</span>
            <input
              className="form-control"
              placeholder="Tìm câu hỏi..."
              value={search}
              onChange={e => { setSearch(e.target.value); setPage(1) }}
            />
          </div>
          <select
            className="form-control"
            style={{ width: 180 }}
            value={filterSubject}
            onChange={e => { setFilterSubject(Number(e.target.value)); setPage(1) }}
          >
            <option value={0}>Tất cả môn học</option>
            {subjects.map(s => <option key={s.id} value={s.id}>{s.name}</option>)}
          </select>
          <select
            className="form-control"
            style={{ width: 140 }}
            value={filterDifficulty}
            onChange={e => { setFilterDifficulty(e.target.value); setPage(1) }}
          >
            <option value="">Tất cả độ khó</option>
            <option value="EASY">Dễ</option>
            <option value="MEDIUM">Trung bình</option>
            <option value="HARD">Khó</option>
          </select>
        </div>

        {loading ? (
          <div className="loading-center"><div className="spinner" /></div>
        ) : questions.length === 0 ? (
          <div className="empty-state">
            <span className="material-icons">database</span>
            <p>Chưa có câu hỏi nào</p>
          </div>
        ) : (
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>#</th>
                  <th>Nội dung câu hỏi</th>
                  <th>Môn học</th>
                  <th>Độ khó</th>
                  <th>Lựa chọn</th>
                  <th>Trạng thái</th>
                  <th>Ngày tạo</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                {questions.map((q, idx) => {
                  const diff = diffLabel[q.difficulty] || { cls: 'badge-gray', text: q.difficulty }
                  return (
                    <tr key={q.id}>
                      <td style={{ color: 'var(--text-muted)' }}>{(page - 1) * pageSize + idx + 1}</td>
                      <td style={{ maxWidth: 320 }}>
                        <div className="truncate" style={{ fontWeight: 500, cursor: 'pointer' }} onClick={() => openView(q)}>
                          {q.content}
                        </div>
                      </td>
                      <td>{q.subjectName || '—'}</td>
                      <td><span className={`badge ${diff.cls}`}>{diff.text}</span></td>
                      <td>{q.optionCount} lựa chọn</td>
                      <td>
                        {q.isPublished
                          ? <span className="badge badge-green">Đã xuất bản</span>
                          : <span className="badge badge-yellow">Nháp</span>}
                      </td>
                      <td style={{ color: 'var(--text-muted)', fontSize: 12 }}>
                        {new Date(q.createdAt).toLocaleDateString('vi-VN')}
                      </td>
                      <td>
                        <div className="actions">
                          <button className="btn-icon btn" title="Xem" onClick={() => openView(q)}>
                            <span className="material-icons" style={{ fontSize: 18 }}>visibility</span>
                          </button>
                          {!q.isPublished && (
                            <button className="btn-icon btn" title="Xuất bản" onClick={() => handlePublish(q.id)} style={{ color: 'var(--success)' }}>
                              <span className="material-icons" style={{ fontSize: 18 }}>publish</span>
                            </button>
                          )}
                          <button className="btn-icon btn" title="Xóa" onClick={() => setDeleteConfirm(q.id)} style={{ color: 'var(--danger)' }}>
                            <span className="material-icons" style={{ fontSize: 18 }}>delete</span>
                          </button>
                        </div>
                      </td>
                    </tr>
                  )
                })}
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
          <div className="modal" style={{ maxWidth: 640 }}>
            <div className="modal-header">
              <h3>Thêm câu hỏi mới</h3>
              <button className="btn btn-icon" onClick={() => setModal(null)}><span className="material-icons">close</span></button>
            </div>
            <div className="modal-body">
              {error && <div className="alert alert-error"><span className="material-icons" style={{ fontSize: 18 }}>error</span>{error}</div>}
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '0 16px' }}>
                <div className="form-group">
                  <label className="form-label">Môn học *</label>
                  <select className="form-control" value={form.subjectId} onChange={e => setForm(f => ({ ...f, subjectId: Number(e.target.value) }))}>
                    <option value={0}>Chọn môn học</option>
                    {subjects.map(s => <option key={s.id} value={s.id}>{s.name}</option>)}
                  </select>
                </div>
                <div className="form-group">
                  <label className="form-label">Độ khó</label>
                  <select className="form-control" value={form.difficulty} onChange={e => setForm(f => ({ ...f, difficulty: e.target.value }))}>
                    <option value="EASY">Dễ</option>
                    <option value="MEDIUM">Trung bình</option>
                    <option value="HARD">Khó</option>
                  </select>
                </div>
                <div className="form-group" style={{ gridColumn: 'span 2' }}>
                  <label className="form-label">Nội dung câu hỏi *</label>
                  <textarea
                    className="form-control"
                    rows={3}
                    value={form.content}
                    onChange={e => setForm(f => ({ ...f, content: e.target.value }))}
                    placeholder="Nhập nội dung câu hỏi..."
                    style={{ resize: 'vertical' }}
                  />
                </div>
              </div>
              <div style={{ marginTop: 4 }}>
                <div style={{ fontSize: 13, fontWeight: 600, marginBottom: 8 }}>Các lựa chọn (chọn đáp án đúng)</div>
                {form.options.map((opt, i) => (
                  <div key={i} style={{ display: 'flex', gap: 10, alignItems: 'center', marginBottom: 10 }}>
                    <input
                      type="radio"
                      name="correctAnswer"
                      checked={opt.isCorrect}
                      onChange={() => setOptionField(i, 'isCorrect', true)}
                      style={{ width: 16, height: 16, accentColor: 'var(--primary)', flexShrink: 0 }}
                    />
                    <div style={{
                      width: 28, height: 28, borderRadius: 6,
                      background: opt.isCorrect ? 'var(--primary)' : 'var(--surface-alt)',
                      color: opt.isCorrect ? '#fff' : 'var(--text)',
                      display: 'flex', alignItems: 'center', justifyContent: 'center',
                      fontWeight: 600, fontSize: 13, flexShrink: 0
                    }}>
                      {opt.label}
                    </div>
                    <input
                      className="form-control"
                      style={{ flex: 1 }}
                      value={opt.content}
                      onChange={e => setOptionField(i, 'content', e.target.value)}
                      placeholder={`Lựa chọn ${opt.label}`}
                    />
                  </div>
                ))}
              </div>
            </div>
            <div className="modal-footer">
              <button className="btn btn-secondary" onClick={() => setModal(null)}>Hủy</button>
              <button className="btn btn-primary" onClick={handleSave} disabled={saving}>
                {saving ? 'Đang lưu...' : 'Tạo câu hỏi'}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* View Modal */}
      {modal === 'view' && viewDetail && (
        <div className="modal-overlay" onClick={e => e.target === e.currentTarget && setModal(null)}>
          <div className="modal" style={{ maxWidth: 580 }}>
            <div className="modal-header">
              <h3>Chi tiết câu hỏi</h3>
              <button className="btn btn-icon" onClick={() => setModal(null)}><span className="material-icons">close</span></button>
            </div>
            <div className="modal-body">
              <div style={{ display: 'flex', gap: 8, marginBottom: 12 }}>
                <span className="badge badge-blue">{viewDetail.subjectName}</span>
                <span className={`badge ${diffLabel[viewDetail.difficulty]?.cls || 'badge-gray'}`}>
                  {diffLabel[viewDetail.difficulty]?.text || viewDetail.difficulty}
                </span>
                {viewDetail.isPublished
                  ? <span className="badge badge-green">Đã xuất bản</span>
                  : <span className="badge badge-yellow">Nháp</span>}
              </div>
              <div style={{ background: 'var(--surface-alt)', borderRadius: 8, padding: 16, marginBottom: 16, fontSize: 14, lineHeight: 1.6 }}>
                {viewDetail.content}
              </div>
              <div>
                {viewDetail.options.sort((a, b) => a.orderIndex - b.orderIndex).map(opt => (
                  <div key={opt.id} style={{
                    display: 'flex', alignItems: 'center', gap: 10, padding: '10px 14px',
                    marginBottom: 8, borderRadius: 8,
                    background: opt.isCorrect ? '#dcfce7' : 'var(--surface-alt)',
                    border: `1px solid ${opt.isCorrect ? '#86efac' : 'var(--border)'}`,
                  }}>
                    <div style={{
                      width: 28, height: 28, borderRadius: 6, flexShrink: 0,
                      background: opt.isCorrect ? '#16a34a' : 'var(--border)',
                      color: opt.isCorrect ? '#fff' : 'var(--text)',
                      display: 'flex', alignItems: 'center', justifyContent: 'center',
                      fontWeight: 600, fontSize: 13
                    }}>
                      {opt.label}
                    </div>
                    <span style={{ flex: 1, fontSize: 13 }}>{opt.content}</span>
                    {opt.isCorrect && <span className="material-icons" style={{ color: '#16a34a' }}>check_circle</span>}
                  </div>
                ))}
              </div>
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
            <div className="modal-body"><p>Bạn có chắc muốn xóa câu hỏi này?</p></div>
            <div className="modal-footer">
              <button className="btn btn-secondary" onClick={() => setDeleteConfirm(null)}>Hủy</button>
              <button className="btn btn-danger" onClick={() => handleDelete(deleteConfirm)}>Xóa</button>
            </div>
          </div>
        </div>
      )}

      {/* unused qTypes supression */}
      {false && qTypes.length}
    </div>
  )
}
