import { useState, useEffect, useCallback, useRef } from 'react'
import { questionsApi } from '../api/questions'
import { subjectsApi } from '../api/subjects'
import { tagsApi } from '../api/tags'
import type { QuestionResponse, QuestionDetailResponse, SubjectResponse, QuestionTypeResponse, CreateQuestionRequest, UpdateQuestionRequest, TagResponse } from '../types/api'

const diffLabel: Record<string, { cls: string; text: string }> = {
  EASY:   { cls: 'diff-easy',   text: 'Dễ' },
  MEDIUM: { cls: 'diff-medium', text: 'Trung bình' },
  HARD:   { cls: 'diff-hard',   text: 'Khó' },
}

const typeLabel: Record<string, string> = {
  MCQ: 'Trắc nghiệm',
  TRUE_FALSE: 'Đúng/Sai',
  SHORT_ANSWER: 'Tự luận ngắn',
  ESSAY: 'Tự luận dài',
  DRAWING: 'Vẽ hình',
}

interface FormState {
  subjectId: number
  questionTypeId: number
  content: string
  difficulty: string
  correctAnswer: string
  options: { label: string; content: string; isCorrect: boolean; orderIndex: number }[]
}

const defaultOptions = () => [
  { label: 'A', content: '', isCorrect: false, orderIndex: 0 },
  { label: 'B', content: '', isCorrect: false, orderIndex: 1 },
  { label: 'C', content: '', isCorrect: false, orderIndex: 2 },
  { label: 'D', content: '', isCorrect: false, orderIndex: 3 },
]

const trueFalseOptions = () => [
  { label: 'T', content: 'Đúng', isCorrect: false, orderIndex: 0 },
  { label: 'F', content: 'Sai', isCorrect: false, orderIndex: 1 },
]

const emptyForm = (): FormState => ({
  subjectId: 0, questionTypeId: 1, content: '', difficulty: 'MEDIUM', correctAnswer: '', options: defaultOptions()
})

export default function QuestionsPage() {
  const [questions, setQuestions] = useState<QuestionResponse[]>([])
  const [total, setTotal] = useState(0)
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [filterSubject, setFilterSubject] = useState(0)
  const [filterDifficulty, setFilterDifficulty] = useState('')
  const [filterType, setFilterType] = useState(0)
  const [filterPublished, setFilterPublished] = useState<'' | 'true' | 'false'>('')
  const [loading, setLoading] = useState(true)
  const [subjects, setSubjects] = useState<SubjectResponse[]>([])
  const [qTypes, setQTypes] = useState<QuestionTypeResponse[]>([])
  const [modal, setModal] = useState<'create' | 'edit' | 'view' | null>(null)
  const [editId, setEditId] = useState<number | null>(null)
  const [form, setForm] = useState<FormState>(emptyForm())
  const [viewDetail, setViewDetail] = useState<QuestionDetailResponse | null>(null)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const [deleteConfirm, setDeleteConfirm] = useState<number | null>(null)
  const [importing, setImporting] = useState(false)
  const [importResult, setImportResult] = useState<{ success: boolean; message: string } | null>(null)
  const [importFormat, setImportFormat] = useState<'auto' | 'pdf' | 'docx' | 'latex' | 'excel'>('auto')
  const [showImportMenu, setShowImportMenu] = useState(false)
  const importRef = useRef<HTMLInputElement>(null)
  const [allTags, setAllTags] = useState<TagResponse[]>([])
  const [filterTag, setFilterTag] = useState(0)
  const [viewTags, setViewTags] = useState<TagResponse[]>([])
  const [tagAdding, setTagAdding] = useState(false)
  const pageSize = 20

  const fetchData = useCallback(async () => {
    setLoading(true)
    try {
      let list: QuestionResponse[] = []
      let cnt = 0

      if (search.trim()) {
        const res = await questionsApi.search(search)
        const data = res.data.data || []
        list = Array.isArray(data) ? data : []
        cnt = list.length
      } else if (filterPublished === 'true') {
        const res = await questionsApi.getPublished()
        list = Array.isArray(res.data.data) ? res.data.data : []
        cnt = list.length
      } else if (filterType) {
        const res = await questionsApi.getByType(filterType)
        list = Array.isArray(res.data.data) ? res.data.data : []
        cnt = list.length
      } else if (filterTag) {
        const res = await questionsApi.getAll(1, 500, filterTag)
        const d = res.data.data
        list = d?.items || []
        cnt = list.length
      } else if (filterSubject) {
        const res = await questionsApi.getBySubject(filterSubject)
        const data = res.data.data || []
        list = Array.isArray(data) ? data : []
        cnt = list.length
      } else {
        const res = await questionsApi.getAll(page, pageSize)
        const d = res.data.data
        list = d?.items || []
        cnt = d?.totalCount || 0
      }

      // Apply client-side filters
      if (filterDifficulty) list = list.filter(q => q.difficulty === filterDifficulty)
      if (filterSubject && !search.trim() && filterPublished !== 'true' && !filterType) {
        // already filtered by subject API
      } else if (filterSubject) {
        list = list.filter(q => q.subjectId === filterSubject)
      }
      if (filterPublished === 'false') list = list.filter(q => !q.isPublished)
      if (filterType && !search.trim() && filterPublished !== 'true') {
        // already filtered by type API
      }

      setQuestions(list)
      setTotal(filterDifficulty || (filterPublished === 'false') ? list.length : cnt)
    } catch { setQuestions([]); setTotal(0) }
    finally { setLoading(false) }
  }, [page, search, filterSubject, filterDifficulty, filterTag, filterType, filterPublished])

  useEffect(() => { fetchData() }, [fetchData])

  useEffect(() => {
    subjectsApi.getAll(1, 100).then(r => setSubjects(r.data.data?.subjects || [])).catch(() => {})
    questionsApi.getTypes().then(r => setQTypes(r.data.data || [])).catch(() => {})
    tagsApi.getAll().then(r => setAllTags(r.data.data || [])).catch(() => {})
  }, [])

  const openCreate = () => { setForm(emptyForm()); setError(''); setEditId(null); setModal('create') }
  const openView = async (q: QuestionResponse) => {
    try {
      const res = await questionsApi.getById(q.id)
      setViewDetail(res.data.data || null)
      setModal('view')
      tagsApi.getQuestionTags(q.id).then(r => setViewTags(r.data.data || [])).catch(() => setViewTags([]))
    } catch { setViewDetail(null) }
  }

  const handleAssignTag = async (questionId: number, tagId: number) => {
    setTagAdding(true)
    try {
      await tagsApi.assignTag(questionId, tagId)
      const res = await tagsApi.getQuestionTags(questionId)
      setViewTags(res.data.data || [])
    } catch { alert('Không thể gán tag') }
    finally { setTagAdding(false) }
  }

  const handleRemoveTag = async (questionId: number, tagId: number) => {
    try {
      await tagsApi.removeTag(questionId, tagId)
      setViewTags(prev => prev.filter(t => t.id !== tagId))
    } catch { alert('Không thể gỡ tag') }
  }

  const openEdit = async (q: QuestionResponse) => {
    try {
      const res = await questionsApi.getById(q.id)
      const detail = res.data.data
      if (!detail) return
      setEditId(q.id)
      const selectedType = qTypes.find(t => t.id === detail.questionTypeId)
      const typeName = selectedType?.name || 'MCQ'
      const isMCQ = typeName === 'MCQ' || typeName === 'TRUE_FALSE'
      setForm({
        subjectId: detail.subjectId,
        questionTypeId: detail.questionTypeId,
        content: detail.content,
        difficulty: detail.difficulty,
        correctAnswer: !isMCQ && detail.options.length > 0 ? detail.options[0].content : '',
        options: isMCQ
          ? detail.options.sort((a, b) => a.orderIndex - b.orderIndex).map(o => ({
              label: o.label,
              content: o.content,
              isCorrect: o.isCorrect,
              orderIndex: o.orderIndex,
            }))
          : typeName === 'TRUE_FALSE' ? trueFalseOptions() : defaultOptions(),
      })
      setError('')
      setModal('edit')
    } catch { alert('Không thể tải câu hỏi') }
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
    const selectedType = qTypes.find(t => t.id === form.questionTypeId)
    const typeName = selectedType?.name || 'MCQ'
    const isMCQ = typeName === 'MCQ' || typeName === 'TRUE_FALSE'

    if (isMCQ) {
      const filledOpts = form.options.filter(o => o.content.trim())
      if (filledOpts.length < 2) { setError('Vui lòng nhập ít nhất 2 lựa chọn'); return }
      if (!filledOpts.some(o => o.isCorrect)) { setError('Vui lòng chọn ít nhất 1 đáp án đúng'); return }
    }

    setSaving(true); setError('')
    try {
      const optionsPayload = isMCQ ? form.options.filter(o => o.content.trim()) : form.correctAnswer.trim()
        ? [{ label: 'ANSWER', content: form.correctAnswer, isCorrect: true, orderIndex: 0 }]
        : []

      if (modal === 'edit' && editId) {
        const payload: UpdateQuestionRequest = {
          content: form.content,
          difficulty: form.difficulty,
          isPublished: false,
          options: optionsPayload,
        }
        await questionsApi.update(editId, payload)
      } else {
        const payload: CreateQuestionRequest = {
          subjectId: form.subjectId,
          questionTypeId: form.questionTypeId || 1,
          content: form.content,
          difficulty: form.difficulty,
          options: optionsPayload,
        }
        await questionsApi.create(payload)
      }
      setModal(null); setEditId(null); fetchData()
    } catch (e: unknown) {
      const err = e as { response?: { data?: { message?: string } } }
      setError(err.response?.data?.message || (modal === 'edit' ? 'Lỗi cập nhật câu hỏi' : 'Lỗi tạo câu hỏi'))
    } finally { setSaving(false) }
  }

  const handleDelete = async (id: number) => {
    try { await questionsApi.delete(id); fetchData() }
    catch { alert('Không thể xóa câu hỏi này') }
    finally { setDeleteConfirm(null) }
  }

  const handlePublish = async (id: number, isPublished: boolean) => {
    try {
      if (isPublished) {
        await questionsApi.unpublish(id)
      } else {
        await questionsApi.publish(id)
      }
      fetchData()
    } catch { alert(isPublished ? 'Không thể hủy xuất bản' : 'Không thể xuất bản câu hỏi') }
  }

  const handleImport = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    if (!file) return
    setImporting(true); setImportResult(null)
    try {
      let res
      if (importFormat === 'pdf') res = await questionsApi.importPdf(file)
      else if (importFormat === 'docx') res = await questionsApi.importDocx(file)
      else if (importFormat === 'latex') res = await questionsApi.importLatex(file)
      else if (importFormat === 'excel') res = await questionsApi.importExcel(file)
      else res = await questionsApi.importFile(file)
      setImportResult({ success: res.data.success, message: res.data.message || 'Import hoàn tất' })
      fetchData()
    } catch (err: unknown) {
      const error = err as { response?: { data?: { message?: string } } }
      setImportResult({ success: false, message: error.response?.data?.message || 'Lỗi import' })
    } finally {
      setImporting(false)
      if (importRef.current) importRef.current.value = ''
    }
  }

  const totalPages = Math.ceil(total / pageSize)

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 20 }}>
        <div>
          <h2 style={{ marginBottom: 2 }}>Ngân hàng Câu hỏi</h2>
          <p style={{ fontSize: 13 }}>{total} câu hỏi trong hệ thống</p>
        </div>
        <div style={{ display: 'flex', gap: 8 }}>
          <input type="file" ref={importRef} accept=".xlsx,.xls,.pdf,.doc,.docx,.tex,.latex,.csv" style={{ display: 'none' }} onChange={handleImport} />
          <div style={{ position: 'relative' }}>
            <button className="btn btn-secondary" onClick={() => setShowImportMenu(!showImportMenu)} disabled={importing} title="Chọn định dạng và import">
              <span className="material-icons" style={{ fontSize: 18 }}>upload_file</span>
              {importing ? 'Đang import...' : 'Import'}
              <span className="material-icons" style={{ fontSize: 16 }}>arrow_drop_down</span>
            </button>
            {showImportMenu && (
              <div style={{ position: 'absolute', right: 0, top: '100%', marginTop: 4, background: 'var(--surface)', border: '1px solid var(--border)', borderRadius: 8, boxShadow: '0 4px 16px rgba(0,0,0,0.12)', zIndex: 10, minWidth: 180, overflow: 'hidden' }}>
                {([['auto', 'Tự động nhận dạng', '.xlsx,.xls,.pdf,.doc,.docx,.tex,.csv'],
                   ['excel', 'Excel (.xlsx, .xls)', '.xlsx,.xls'],
                   ['pdf', 'PDF (.pdf)', '.pdf'],
                   ['docx', 'Word (.docx)', '.doc,.docx'],
                   ['latex', 'LaTeX (.tex)', '.tex,.latex']] as const).map(([fmt, label, accept]) => (
                  <div key={fmt} style={{ padding: '8px 14px', fontSize: 13, cursor: 'pointer', borderBottom: '1px solid var(--border)' }}
                    onMouseEnter={e => (e.currentTarget.style.background = 'var(--bg)')}
                    onMouseLeave={e => (e.currentTarget.style.background = 'transparent')}
                    onClick={() => {
                      setImportFormat(fmt)
                      setShowImportMenu(false)
                      if (importRef.current) { importRef.current.accept = accept; importRef.current.click() }
                    }}>
                    {label}
                  </div>
                ))}
              </div>
            )}
          </div>
          <button className="btn btn-primary" onClick={openCreate}>
            <span className="material-icons" style={{ fontSize: 18 }}>add</span>
            Thêm câu hỏi
          </button>
        </div>
      </div>

      {importResult && (
        <div className={`alert ${importResult.success ? 'alert-success' : 'alert-error'}`} style={{ marginBottom: 16 }}>
          <span className="material-icons" style={{ fontSize: 18 }}>{importResult.success ? 'check_circle' : 'error'}</span>
          {importResult.message}
          <button className="btn btn-icon" style={{ marginLeft: 'auto' }} onClick={() => setImportResult(null)}>
            <span className="material-icons" style={{ fontSize: 16 }}>close</span>
          </button>
        </div>
      )}

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
          <select
            className="form-control"
            style={{ width: 160 }}
            value={filterTag}
            onChange={e => { setFilterTag(Number(e.target.value)); setPage(1) }}
          >
            <option value={0}>Tất cả tag</option>
            {allTags.map(t => <option key={t.id} value={t.id}>{t.name}</option>)}
          </select>
          <select
            className="form-control"
            style={{ width: 160 }}
            value={filterType}
            onChange={e => { setFilterType(Number(e.target.value)); setPage(1) }}
          >
            <option value={0}>Tất cả loại</option>
            {qTypes.map(t => <option key={t.id} value={t.id}>{typeLabel[t.name] || t.name}</option>)}
          </select>
          <select
            className="form-control"
            style={{ width: 140 }}
            value={filterPublished}
            onChange={e => { setFilterPublished(e.target.value as '' | 'true' | 'false'); setPage(1) }}
          >
            <option value="">Tất cả TT</option>
            <option value="true">Đã xuất bản</option>
            <option value="false">Nháp</option>
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
                  <th>Loại</th>
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
                      <td><span className="badge badge-purple">{typeLabel[q.questionTypeName || ''] || q.questionTypeName || '—'}</span></td>
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
                          <button className="btn-icon btn" title="Sửa" onClick={() => openEdit(q)} style={{ color: 'var(--primary)' }}>
                            <span className="material-icons" style={{ fontSize: 18 }}>edit</span>
                          </button>
                          <button
                            className="btn-icon btn"
                            title={q.isPublished ? 'Hủy xuất bản' : 'Xuất bản'}
                            onClick={() => handlePublish(q.id, q.isPublished)}
                            style={{ color: q.isPublished ? 'var(--warning)' : 'var(--success)' }}
                          >
                            <span className="material-icons" style={{ fontSize: 18 }}>
                              {q.isPublished ? 'unpublished' : 'publish'}
                            </span>
                          </button>
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

      {/* Create / Edit Modal */}
      {(modal === 'create' || modal === 'edit') && (() => {
        const selectedType = qTypes.find(t => t.id === form.questionTypeId)
        const typeName = selectedType?.name || 'MCQ'
        const isMCQ = typeName === 'MCQ' || typeName === 'TRUE_FALSE'
        const isText = typeName === 'SHORT_ANSWER' || typeName === 'ESSAY'
        const isDrawing = typeName === 'DRAWING'
        const isEditing = modal === 'edit'
        return (
        <div className="modal-overlay" onClick={e => e.target === e.currentTarget && setModal(null)}>
          <div className="modal" style={{ maxWidth: 640 }}>
            <div className="modal-header">
              <h3>{isEditing ? 'Chỉnh sửa câu hỏi' : 'Thêm câu hỏi mới'}</h3>
              <button className="btn btn-icon" onClick={() => setModal(null)}><span className="material-icons">close</span></button>
            </div>
            <div className="modal-body">
              {error && <div className="alert alert-error"><span className="material-icons" style={{ fontSize: 18 }}>error</span>{error}</div>}
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '0 16px' }}>
                <div className="form-group">
                  <label className="form-label">Môn học *</label>
                  <select className="form-control" value={form.subjectId} onChange={e => setForm(f => ({ ...f, subjectId: Number(e.target.value) }))} disabled={isEditing}>
                    <option value={0}>Chọn môn học</option>
                    {subjects.map(s => <option key={s.id} value={s.id}>{s.name}</option>)}
                  </select>
                </div>
                <div className="form-group">
                  <label className="form-label">Loại câu hỏi *</label>
                  <select className="form-control" value={form.questionTypeId} disabled={isEditing} onChange={e => {
                    const newId = Number(e.target.value)
                    const newType = qTypes.find(t => t.id === newId)
                    const newName = newType?.name || 'MCQ'
                    setForm(f => ({
                      ...f,
                      questionTypeId: newId,
                      options: newName === 'TRUE_FALSE' ? trueFalseOptions() : (newName === 'MCQ' ? defaultOptions() : []),
                      correctAnswer: '',
                    }))
                  }}>
                    {qTypes.map(t => <option key={t.id} value={t.id}>{typeLabel[t.name] || t.name}</option>)}
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

              {/* MCQ / TRUE_FALSE options */}
              {isMCQ && (
                <div style={{ marginTop: 4 }}>
                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 8 }}>
                    <div style={{ fontSize: 13, fontWeight: 600 }}>Các lựa chọn (chọn đáp án đúng)</div>
                    {typeName === 'MCQ' && (
                      <button className="btn btn-secondary" style={{ padding: '2px 10px', fontSize: 12 }} onClick={() => {
                        setForm(f => {
                          const nextLabel = String.fromCharCode(65 + f.options.length)
                          return { ...f, options: [...f.options, { label: nextLabel, content: '', isCorrect: false, orderIndex: f.options.length }] }
                        })
                      }}>
                        <span className="material-icons" style={{ fontSize: 14 }}>add</span> Thêm
                      </button>
                    )}
                  </div>
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
                      {typeName === 'MCQ' && form.options.length > 2 && (
                        <button className="btn btn-icon" title="Xóa lựa chọn"
                          style={{ color: 'var(--danger)', flexShrink: 0 }}
                          onClick={() => setForm(f => ({
                            ...f,
                            options: f.options.filter((_, j) => j !== i).map((o, j) => ({
                              ...o,
                              label: String.fromCharCode(65 + j),
                              orderIndex: j
                            }))
                          }))}
                        >
                          <span className="material-icons" style={{ fontSize: 18 }}>remove_circle_outline</span>
                        </button>
                      )}
                    </div>
                  ))}
                </div>
              )}

              {/* SHORT_ANSWER / ESSAY */}
              {isText && (
                <div style={{ marginTop: 4 }}>
                  <div style={{ fontSize: 13, fontWeight: 600, marginBottom: 8 }}>Đáp án mẫu (tùy chọn)</div>
                  <textarea
                    className="form-control"
                    rows={typeName === 'ESSAY' ? 5 : 2}
                    value={form.correctAnswer}
                    onChange={e => setForm(f => ({ ...f, correctAnswer: e.target.value }))}
                    placeholder={typeName === 'ESSAY' ? 'Nhập đáp án mẫu / gợi ý chấm điểm...' : 'Nhập đáp án mẫu...'}
                    style={{ resize: 'vertical' }}
                  />
                </div>
              )}

              {/* DRAWING */}
              {isDrawing && (
                <div style={{ marginTop: 4 }}>
                  <div style={{ fontSize: 13, fontWeight: 600, marginBottom: 8 }}>Mô tả yêu cầu vẽ</div>
                  <textarea
                    className="form-control"
                    rows={3}
                    value={form.correctAnswer}
                    onChange={e => setForm(f => ({ ...f, correctAnswer: e.target.value }))}
                    placeholder="Mô tả yêu cầu vẽ hình hoặc gợi ý chấm điểm..."
                    style={{ resize: 'vertical' }}
                  />
                  <p style={{ fontSize: 12, color: 'var(--text-muted)', marginTop: 4 }}>
                    Học sinh sẽ vẽ trực tiếp trên canvas khi làm bài.
                  </p>
                </div>
              )}
            </div>
            <div className="modal-footer">
              <button className="btn btn-secondary" onClick={() => setModal(null)}>Hủy</button>
              <button className="btn btn-primary" onClick={handleSave} disabled={saving}>
                {saving ? 'Đang lưu...' : isEditing ? 'Cập nhật' : 'Tạo câu hỏi'}
              </button>
            </div>
          </div>
        </div>
      )})()}

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
                <span className="badge badge-purple">{typeLabel[viewDetail.questionTypeName || ''] || viewDetail.questionTypeName || ''}</span>
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
              {viewDetail.options.length > 0 && (
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
              )}

              {/* Tags Section */}
              <div style={{ marginTop: 16, borderTop: '1px solid var(--border)', paddingTop: 12 }}>
                <div style={{ fontSize: 13, fontWeight: 600, marginBottom: 8 }}>Tags</div>
                <div style={{ display: 'flex', gap: 6, flexWrap: 'wrap', marginBottom: 8 }}>
                  {viewTags.length === 0 && <span style={{ fontSize: 12, color: 'var(--text-muted)' }}>Chưa có tag nào</span>}
                  {viewTags.map(t => (
                    <span key={t.id} className="badge badge-blue" style={{ display: 'flex', alignItems: 'center', gap: 4 }}>
                      {t.name}
                      <span
                        className="material-icons"
                        style={{ fontSize: 14, cursor: 'pointer', opacity: 0.7 }}
                        onClick={() => handleRemoveTag(viewDetail.id, t.id)}
                        title="Gỡ tag"
                      >close</span>
                    </span>
                  ))}
                </div>
                {allTags.filter(t => !viewTags.some(vt => vt.id === t.id)).length > 0 && (
                  <div style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
                    <select
                      className="form-control"
                      style={{ width: 180, fontSize: 12 }}
                      id="addTagSelect"
                      defaultValue=""
                    >
                      <option value="" disabled>Thêm tag...</option>
                      {allTags.filter(t => !viewTags.some(vt => vt.id === t.id)).map(t => (
                        <option key={t.id} value={t.id}>{t.name}</option>
                      ))}
                    </select>
                    <button
                      className="btn btn-secondary"
                      style={{ padding: '4px 10px', fontSize: 12 }}
                      disabled={tagAdding}
                      onClick={() => {
                        const sel = document.getElementById('addTagSelect') as HTMLSelectElement
                        const tagId = Number(sel?.value)
                        if (tagId) handleAssignTag(viewDetail.id, tagId)
                      }}
                    >
                      {tagAdding ? '...' : 'Gán'}
                    </button>
                  </div>
                )}
              </div>
            </div>
            <div className="modal-footer">
              <button className="btn btn-secondary" onClick={() => setModal(null)}>Đóng</button>
              <button className="btn btn-primary" onClick={() => {
                if (viewDetail) openEdit(viewDetail)
              }}>
                <span className="material-icons" style={{ fontSize: 16 }}>edit</span> Chỉnh sửa
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
            <div className="modal-body"><p>Bạn có chắc muốn xóa câu hỏi này?</p></div>
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
