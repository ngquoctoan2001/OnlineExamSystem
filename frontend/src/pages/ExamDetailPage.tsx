import { useState, useEffect } from 'react'
import { useParams, Link, useNavigate } from 'react-router-dom'
import { examsApi } from '../api/exams'
import { questionsApi } from '../api/questions'
import type { ExamResponse, ExamSettingsResponse, ConfigureExamSettingsRequest, QuestionOptionResponse } from '../types/api'

interface ExamQuestion {
  id: number
  questionId: number
  questionContent: string
  questionDifficulty: string
  questionOrder: number
  maxScore: number
  options?: QuestionOptionResponse[]
  questionTypeName?: string
}

export default function ExamDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const examId = Number(id)
  const [exam, setExam] = useState<ExamResponse | null>(null)
  const [questions, setQuestions] = useState<ExamQuestion[]>([])
  const [totalScore, setTotalScore] = useState(0)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [addQModal, setAddQModal] = useState(false)
  const [availableQs, setAvailableQs] = useState<{ id: number; content: string; difficulty: string; subjectName: string }[]>([])
  const [selectedQ, setSelectedQ] = useState<number[]>([])
  // Settings
  const [settings, setSettings] = useState<ExamSettingsResponse | null>(null)
  const [settingsModal, setSettingsModal] = useState(false)
  const [settingsForm, setSettingsForm] = useState<ConfigureExamSettingsRequest>({ shuffleQuestions: false, shuffleAnswers: false, showResultImmediately: false, allowReview: false })
  const [savingSettings, setSavingSettings] = useState(false)
  // Inline max score edit
  const [editScoreId, setEditScoreId] = useState<number | null>(null)
  const [editScoreVal, setEditScoreVal] = useState(1)
  // Print preview
  const [showPrintPreview, setShowPrintPreview] = useState(false)

  const fetchQuestions = async () => {
    const qRes = await examsApi.getQuestions(examId)
    const data = qRes.data?.data
    setQuestions(data?.questions || [])
    setTotalScore(data?.totalScore || 0)
  }

  useEffect(() => {
    if (!id) return
    const fetchAll = async () => {
      setLoading(true)
      try {
        const examRes = await examsApi.getById(examId)
        setExam(examRes.data.data || null)
        await fetchQuestions()
        examsApi.getSettings(examId).then(r => setSettings(r.data.data || null)).catch(() => {})
      } catch { setError('Không tải được dữ liệu kỳ thi') }
      finally { setLoading(false) }
    }
    fetchAll()
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [id])

  const handleActivate = async () => {
    try { await examsApi.activate(examId); navigate(0) }
    catch { alert('Không thể kích hoạt kỳ thi') }
  }

  const handleClose = async () => {
    try { await examsApi.close(examId); navigate(0) }
    catch { alert('Không thể đóng kỳ thi') }
  }

  const loadAvailableQuestions = async () => {
    if (!exam) return
    try {
      const res = await questionsApi.getBySubject(exam.subjectId)
      const all = (res.data?.data as any)?.items || res.data?.data || []
      const usedIds = new Set(questions.map(q => q.questionId))
      setAvailableQs((Array.isArray(all) ? all : []).filter((q: { id: number }) => !usedIds.has(q.id)))
    } catch { setAvailableQs([]) }
    setAddQModal(true); setSelectedQ([])
  }

  const handleAddQuestions = async () => {
    if (!id || selectedQ.length === 0) return
    try {
      await Promise.all(selectedQ.map((qId, idx) =>
        examsApi.addQuestion(examId, qId, questions.length + idx + 1, 1)
      ))
      setAddQModal(false)
      await fetchQuestions()
    } catch { alert('Lỗi khi thêm câu hỏi') }
  }

  const handleRemoveQuestion = async (examQId: number, questionId: number) => {
    try {
      await examsApi.removeQuestion(examId, questionId)
      setQuestions(qs => qs.filter(q => q.id !== examQId))
    } catch { alert('Không thể xóa câu hỏi') }
  }

  // Move question up/down (reorder)
  const handleMoveQuestion = async (idx: number, direction: -1 | 1) => {
    const swapIdx = idx + direction
    if (swapIdx < 0 || swapIdx >= questions.length) return
    const sorted = [...questions].sort((a, b) => a.questionOrder - b.questionOrder)
    const a = sorted[idx]
    const b = sorted[swapIdx]
    try {
      await examsApi.reorderQuestions(examId, {
        questions: [
          { examQuestionId: a.id, newOrder: b.questionOrder },
          { examQuestionId: b.id, newOrder: a.questionOrder },
        ]
      })
      await fetchQuestions()
    } catch { alert('Không thể sắp xếp lại') }
  }

  // Max score edit
  const handleSaveMaxScore = async (examQId: number) => {
    if (editScoreVal < 1) return
    try {
      await examsApi.updateQuestionMaxScore(examId, examQId, editScoreVal)
      await fetchQuestions()
      setEditScoreId(null)
    } catch { alert('Không thể cập nhật điểm') }
  }

  // Settings
  const openSettingsModal = () => {
    setSettingsForm({
      shuffleQuestions: settings?.shuffleQuestions || false,
      shuffleAnswers: settings?.shuffleAnswers || false,
      showResultImmediately: settings?.showResultImmediately || false,
      allowReview: settings?.allowReview || false,
    })
    setSettingsModal(true)
  }

  const handleSaveSettings = async () => {
    setSavingSettings(true)
    try {
      const res = await examsApi.configureSettings(examId, settingsForm)
      setSettings(res.data.data || null)
      setSettingsModal(false)
    } catch { alert('Không thể lưu cấu hình') }
    finally { setSavingSettings(false) }
  }

  const diffBadge = (d: string = '') => {
    if (d === 'EASY')   return <span className="badge diff-easy">Dễ</span>
    if (d === 'HARD')   return <span className="badge diff-hard">Khó</span>
    return <span className="badge diff-medium">Trung bình</span>
  }

  if (loading) return <div className="loading-center"><div className="spinner" /></div>
  if (error || !exam) return (
    <div className="empty-state">
      <span className="material-icons">error_outline</span>
      <p>{error || 'Không tìm thấy kỳ thi'}</p>
      <Link to="/exams" className="btn btn-secondary btn-sm">Quay lại</Link>
    </div>
  )

  const fmtDate = (d: string) => d ? new Date(d).toLocaleString('vi-VN') : '—'
  const sortedQs = [...questions].sort((a, b) => a.questionOrder - b.questionOrder)

  return (
    <div>
      {/* Breadcrumb */}
      <div style={{ display: 'flex', alignItems: 'center', gap: 8, fontSize: 13, color: 'var(--text-muted)', marginBottom: 16 }}>
        <Link to="/exams" style={{ color: 'var(--primary)', textDecoration: 'none' }}>Kỳ thi</Link>
        <span className="material-icons" style={{ fontSize: 16 }}>chevron_right</span>
        <span>{exam.title}</span>
      </div>

      {/* Header */}
      <div className="card" style={{ marginBottom: 20 }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', flexWrap: 'wrap', gap: 16 }}>
          <div>
            <h2 style={{ marginBottom: 8 }}>{exam.title}</h2>
            {exam.description && <p style={{ marginBottom: 12 }}>{exam.description}</p>}
            <div style={{ display: 'flex', gap: 16, flexWrap: 'wrap', fontSize: 13 }}>
              <span style={{ display: 'flex', alignItems: 'center', gap: 6, color: 'var(--text-secondary)' }}>
                <span className="material-icons" style={{ fontSize: 16 }}>library_books</span>
                {exam.subjectName}
              </span>
              <span style={{ display: 'flex', alignItems: 'center', gap: 6, color: 'var(--text-secondary)' }}>
                <span className="material-icons" style={{ fontSize: 16 }}>schedule</span>
                {exam.durationMinutes} phút
              </span>
              <span style={{ display: 'flex', alignItems: 'center', gap: 6, color: 'var(--text-secondary)' }}>
                <span className="material-icons" style={{ fontSize: 16 }}>event</span>
                {fmtDate(exam.startTime)} – {fmtDate(exam.endTime)}
              </span>
              <span style={{ display: 'flex', alignItems: 'center', gap: 6, color: 'var(--text-secondary)' }}>
                <span className="material-icons" style={{ fontSize: 16 }}>quiz</span>
                {questions.length} câu hỏi · {totalScore} điểm
              </span>
            </div>
          </div>
          <div style={{ display: 'flex', gap: 8, alignItems: 'center', flexWrap: 'wrap' }}>
            {exam.status === 'DRAFT' && <span className="badge badge-gray" style={{ fontSize: 13 }}>Nháp</span>}
            {exam.status === 'ACTIVE' && <span className="badge badge-green" style={{ fontSize: 13 }}>Đang hoạt động</span>}
            {exam.status === 'CLOSED' && <span className="badge badge-red" style={{ fontSize: 13 }}>Đã đóng</span>}
            <button className="btn btn-secondary btn-sm" onClick={openSettingsModal}>
              <span className="material-icons" style={{ fontSize: 16 }}>settings</span>
              Cấu hình
            </button>
            {exam.status === 'DRAFT' && (
              <button className="btn btn-success btn-sm" onClick={handleActivate}>
                <span className="material-icons" style={{ fontSize: 16 }}>play_arrow</span>
                Kích hoạt
              </button>
            )}
            {exam.status === 'ACTIVE' && (
              <button className="btn btn-danger btn-sm" onClick={handleClose}>
                <span className="material-icons" style={{ fontSize: 16 }}>stop</span>
                Đóng
              </button>
            )}
            <button className="btn btn-sm" onClick={() => setShowPrintPreview(true)}>
              <span className="material-icons" style={{ fontSize: 16 }}>print</span>
              In đề thi
            </button>
          </div>
        </div>
      </div>

      {/* Settings summary */}
      {settings && (
        <div className="card" style={{ marginBottom: 20, padding: '12px 20px' }}>
          <div style={{ display: 'flex', gap: 20, flexWrap: 'wrap', fontSize: 13 }}>
            <span style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
              <span className="material-icons" style={{ fontSize: 16, color: settings.shuffleQuestions ? '#22c55e' : 'var(--text-muted)' }}>
                {settings.shuffleQuestions ? 'check_circle' : 'cancel'}
              </span>
              Trộn câu hỏi
            </span>
            <span style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
              <span className="material-icons" style={{ fontSize: 16, color: settings.shuffleAnswers ? '#22c55e' : 'var(--text-muted)' }}>
                {settings.shuffleAnswers ? 'check_circle' : 'cancel'}
              </span>
              Trộn đáp án
            </span>
            <span style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
              <span className="material-icons" style={{ fontSize: 16, color: settings.showResultImmediately ? '#22c55e' : 'var(--text-muted)' }}>
                {settings.showResultImmediately ? 'check_circle' : 'cancel'}
              </span>
              Hiện kết quả ngay
            </span>
            <span style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
              <span className="material-icons" style={{ fontSize: 16, color: settings.allowReview ? '#22c55e' : 'var(--text-muted)' }}>
                {settings.allowReview ? 'check_circle' : 'cancel'}
              </span>
              Cho phép xem lại
            </span>
          </div>
        </div>
      )}

      {/* Questions */}
      <div className="card">
        <div className="card-header">
          <span className="card-title">Danh sách câu hỏi ({questions.length})</span>
          {exam.status !== 'CLOSED' && (
            <button className="btn btn-primary btn-sm" onClick={loadAvailableQuestions}>
              <span className="material-icons" style={{ fontSize: 16 }}>add</span>
              Thêm câu hỏi
            </button>
          )}
        </div>

        {questions.length === 0 ? (
          <div className="empty-state">
            <span className="material-icons">quiz</span>
            <p>Chưa có câu hỏi nào trong kỳ thi này</p>
            {exam.status !== 'CLOSED' && (
              <button className="btn btn-primary btn-sm" onClick={loadAvailableQuestions}>Thêm câu hỏi</button>
            )}
          </div>
        ) : (
          <div>
            {sortedQs.map((q, idx) => (
              <div key={q.id} style={{
                display: 'flex', gap: 12, alignItems: 'flex-start', padding: '12px 0',
                borderBottom: idx < sortedQs.length - 1 ? '1px solid var(--border)' : 'none'
              }}>
                {/* Reorder buttons */}
                {exam.status === 'DRAFT' && (
                  <div style={{ display: 'flex', flexDirection: 'column', gap: 2, flexShrink: 0 }}>
                    <button className="btn-icon btn" disabled={idx === 0} onClick={() => handleMoveQuestion(idx, -1)} style={{ padding: 0, minWidth: 24 }}>
                      <span className="material-icons" style={{ fontSize: 16 }}>keyboard_arrow_up</span>
                    </button>
                    <button className="btn-icon btn" disabled={idx === sortedQs.length - 1} onClick={() => handleMoveQuestion(idx, 1)} style={{ padding: 0, minWidth: 24 }}>
                      <span className="material-icons" style={{ fontSize: 16 }}>keyboard_arrow_down</span>
                    </button>
                  </div>
                )}
                <div style={{
                  width: 28, height: 28, borderRadius: 6, background: 'var(--primary-light)',
                  color: 'var(--primary)', display: 'flex', alignItems: 'center', justifyContent: 'center',
                  fontWeight: 600, fontSize: 13, flexShrink: 0
                }}>
                  {idx + 1}
                </div>
                <div style={{ flex: 1 }}>
                  <div style={{ fontSize: 14, lineHeight: 1.5 }}>{q.questionContent}</div>
                  <div style={{ display: 'flex', gap: 8, marginTop: 6, alignItems: 'center' }}>
                    {diffBadge(q.questionDifficulty)}
                    {editScoreId === q.id ? (
                      <span style={{ display: 'flex', alignItems: 'center', gap: 4 }}>
                        <input type="number" min={1} max={100} value={editScoreVal}
                          onChange={e => setEditScoreVal(Number(e.target.value))}
                          className="form-control" style={{ width: 60, height: 26, fontSize: 12, padding: '0 6px' }}
                          onKeyDown={e => { if (e.key === 'Enter') handleSaveMaxScore(q.id); if (e.key === 'Escape') setEditScoreId(null) }}
                          autoFocus
                        />
                        <button className="btn-icon btn" onClick={() => handleSaveMaxScore(q.id)} style={{ padding: 0 }}>
                          <span className="material-icons" style={{ fontSize: 16, color: '#22c55e' }}>check</span>
                        </button>
                        <button className="btn-icon btn" onClick={() => setEditScoreId(null)} style={{ padding: 0 }}>
                          <span className="material-icons" style={{ fontSize: 16, color: 'var(--text-muted)' }}>close</span>
                        </button>
                      </span>
                    ) : (
                      <span
                        style={{ fontSize: 12, color: 'var(--text-muted)', cursor: exam.status === 'DRAFT' ? 'pointer' : 'default' }}
                        onClick={() => { if (exam.status === 'DRAFT') { setEditScoreId(q.id); setEditScoreVal(q.maxScore) } }}
                        title={exam.status === 'DRAFT' ? 'Bấm để sửa điểm' : ''}
                      >
                        {q.maxScore} điểm {exam.status === 'DRAFT' && <span className="material-icons" style={{ fontSize: 12, verticalAlign: 'middle' }}>edit</span>}
                      </span>
                    )}
                  </div>
                </div>
                {exam.status !== 'CLOSED' && (
                  <button className="btn-icon btn" onClick={() => handleRemoveQuestion(q.id, q.questionId)} style={{ color: 'var(--danger)', flexShrink: 0 }}>
                    <span className="material-icons" style={{ fontSize: 18 }}>remove_circle_outline</span>
                  </button>
                )}
              </div>
            ))}
          </div>
        )}
      </div>

      {/* Add Questions Modal */}
      {addQModal && (
        <div className="modal-overlay" onClick={e => e.target === e.currentTarget && setAddQModal(false)}>
          <div className="modal" style={{ maxWidth: 640 }}>
            <div className="modal-header">
              <h3>Thêm câu hỏi vào kỳ thi</h3>
              <button className="btn btn-icon" onClick={() => setAddQModal(false)}><span className="material-icons">close</span></button>
            </div>
            <div className="modal-body" style={{ maxHeight: 400, overflowY: 'auto' }}>
              {availableQs.length === 0 ? (
                <div className="empty-state">
                  <span className="material-icons">database</span>
                  <p>Không có câu hỏi nào khả dụng</p>
                </div>
              ) : availableQs.map(q => (
                <div
                  key={q.id}
                  style={{
                    display: 'flex', gap: 10, alignItems: 'flex-start', padding: '10px 0',
                    borderBottom: '1px solid var(--border)', cursor: 'pointer',
                  }}
                  onClick={() => setSelectedQ(prev =>
                    prev.includes(q.id) ? prev.filter(x => x !== q.id) : [...prev, q.id]
                  )}
                >
                  <input type="checkbox" checked={selectedQ.includes(q.id)} readOnly style={{ marginTop: 2 }} />
                  <div>
                    <div style={{ fontSize: 13 }}>{q.content}</div>
                    <div style={{ display: 'flex', gap: 8, marginTop: 4 }}>
                      <span className="badge badge-blue" style={{ fontSize: 11 }}>{q.subjectName}</span>
                    </div>
                  </div>
                </div>
              ))}
            </div>
            <div className="modal-footer">
              <span style={{ fontSize: 13, color: 'var(--text-muted)' }}>Đã chọn {selectedQ.length} câu hỏi</span>
              <button className="btn btn-secondary" onClick={() => setAddQModal(false)}>Hủy</button>
              <button className="btn btn-primary" onClick={handleAddQuestions} disabled={selectedQ.length === 0}>
                Thêm {selectedQ.length > 0 ? `(${selectedQ.length})` : ''}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Settings Modal */}
      {settingsModal && (
        <div className="modal-overlay" onClick={e => e.target === e.currentTarget && setSettingsModal(false)}>
          <div className="modal" style={{ maxWidth: 480 }}>
            <div className="modal-header">
              <h3>Cấu hình kỳ thi</h3>
              <button className="btn btn-icon" onClick={() => setSettingsModal(false)}><span className="material-icons">close</span></button>
            </div>
            <div className="modal-body">
              {[
                { key: 'shuffleQuestions' as const, label: 'Trộn thứ tự câu hỏi', desc: 'Mỗi thí sinh nhận thứ tự câu hỏi khác nhau', icon: 'shuffle' },
                { key: 'shuffleAnswers' as const, label: 'Trộn thứ tự đáp án', desc: 'Đáp án trắc nghiệm được xáo trộn', icon: 'swap_vert' },
                { key: 'showResultImmediately' as const, label: 'Hiện kết quả ngay', desc: 'Thí sinh thấy kết quả ngay sau khi nộp bài', icon: 'visibility' },
                { key: 'allowReview' as const, label: 'Cho phép xem lại bài', desc: 'Thí sinh có thể xem lại bài làm và đáp án', icon: 'preview' },
              ].map(item => (
                <label key={item.key} style={{
                  display: 'flex', alignItems: 'flex-start', gap: 12, padding: '14px 0',
                  borderBottom: '1px solid var(--border)', cursor: 'pointer'
                }}>
                  <input
                    type="checkbox"
                    checked={settingsForm[item.key]}
                    onChange={e => setSettingsForm(f => ({ ...f, [item.key]: e.target.checked }))}
                    style={{ width: 18, height: 18, marginTop: 2, accentColor: 'var(--primary)' }}
                  />
                  <div>
                    <div style={{ display: 'flex', alignItems: 'center', gap: 6, fontWeight: 500, fontSize: 14 }}>
                      <span className="material-icons" style={{ fontSize: 18, color: 'var(--primary)' }}>{item.icon}</span>
                      {item.label}
                    </div>
                    <div style={{ fontSize: 12, color: 'var(--text-muted)', marginTop: 2 }}>{item.desc}</div>
                  </div>
                </label>
              ))}
            </div>
            <div className="modal-footer">
              <button className="btn btn-secondary" onClick={() => setSettingsModal(false)}>Hủy</button>
              <button className="btn btn-primary" onClick={handleSaveSettings} disabled={savingSettings}>
                {savingSettings ? 'Đang lưu...' : 'Lưu cấu hình'}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Print Preview Modal */}
      {showPrintPreview && exam && (
        <div className="modal-overlay" onClick={e => e.target === e.currentTarget && setShowPrintPreview(false)}>
          <div className="modal" style={{ maxWidth: 800, maxHeight: '90vh', overflow: 'auto' }}>
            <div className="modal-header" style={{ position: 'sticky', top: 0, background: 'var(--surface)', zIndex: 1 }}>
              <h3>Xem trước đề thi (A4)</h3>
              <div style={{ display: 'flex', gap: 8 }}>
                <button className="btn btn-primary btn-sm" onClick={() => {
                  const printContent = document.getElementById('print-preview-content')
                  if (!printContent) return
                  const win = window.open('', '', 'width=800,height=600')
                  if (!win) return
                  win.document.write(`<html><head><title>${exam.title}</title><style>
                    body { font-family: 'Times New Roman', serif; padding: 40px; font-size: 14px; line-height: 1.6; }
                    h1 { text-align: center; font-size: 18px; margin-bottom: 4px; }
                    .exam-info { text-align: center; margin-bottom: 20px; font-size: 13px; color: #666; }
                    .question { margin-bottom: 16px; }
                    .question-num { font-weight: bold; }
                    .options { margin-left: 20px; }
                    .option { margin: 4px 0; }
                    .student-info { margin-bottom: 24px; border: 1px solid #ccc; padding: 12px; }
                    .student-info span { display: inline-block; width: 50%; }
                    hr { border: none; border-top: 1px solid #ccc; margin: 16px 0; }
                    @page { size: A4; margin: 20mm; }
                  </style></head><body>${printContent.innerHTML}</body></html>`)
                  win.document.close()
                  win.print()
                }}>
                  <span className="material-icons" style={{ fontSize: 16 }}>print</span> In
                </button>
                <button className="btn btn-icon" onClick={() => setShowPrintPreview(false)}><span className="material-icons">close</span></button>
              </div>
            </div>
            <div id="print-preview-content" style={{ padding: '20px 24px', fontFamily: "'Times New Roman', serif" }}>
              <h1 style={{ textAlign: 'center', fontSize: 18, marginBottom: 4 }}>ĐỀ KIỂM TRA</h1>
              <div style={{ textAlign: 'center', fontSize: 16, fontWeight: 'bold', marginBottom: 4 }}>{exam.title}</div>
              <div style={{ textAlign: 'center', fontSize: 13, color: '#666', marginBottom: 16 }}>
                Môn: {exam.subjectName} · Thời gian: {exam.durationMinutes} phút · {questions.length} câu · Tổng điểm: {totalScore}
              </div>
              <div style={{ border: '1px solid #ccc', padding: 12, marginBottom: 20 }}>
                <div style={{ display: 'flex', gap: 20 }}>
                  <span>Họ tên: ...............................</span>
                  <span>Lớp: ............</span>
                  <span>Mã HS: ............</span>
                </div>
              </div>
              <hr />
              {(() => {
                const printQs = [...questions].sort((a, b) => a.questionOrder - b.questionOrder)
                const answerKeys: { num: number; answer: string }[] = []
                return (
                  <>
                    {printQs.map((q, idx) => {
                      const opts = (q.options || []).sort((a, b) => a.orderIndex - b.orderIndex)
                      const isMCQ = opts.length > 0 && opts.some(o => o.label)
                      if (isMCQ) {
                        const correct = opts.find(o => o.isCorrect)
                        if (correct) answerKeys.push({ num: idx + 1, answer: correct.label })
                      }
                      return (
                        <div key={q.id} style={{ marginBottom: 16 }}>
                          <div>
                            <strong>Câu {idx + 1}</strong> ({q.maxScore} điểm): {q.questionContent}
                          </div>
                          {isMCQ && (
                            <div style={{ marginLeft: 20, marginTop: 4 }}>
                              {opts.map(o => (
                                <div key={o.id} style={{ margin: '4px 0' }}>
                                  <strong>{o.label}.</strong> {o.content}
                                </div>
                              ))}
                            </div>
                          )}
                          {!isMCQ && q.questionTypeName !== 'MULTIPLE_CHOICE' && q.questionTypeName !== 'TRUE_FALSE' && (
                            <div style={{ marginLeft: 20, marginTop: 8, borderBottom: '1px dotted #ccc', minHeight: 60 }} />
                          )}
                        </div>
                      )
                    })}
                    {answerKeys.length > 0 && (
                      <div style={{ marginTop: 32, pageBreakBefore: 'always' }}>
                        <hr />
                        <h3 style={{ textAlign: 'center', fontSize: 16, marginBottom: 12 }}>ĐÁP ÁN</h3>
                        <div style={{ display: 'flex', flexWrap: 'wrap', gap: '4px 24px' }}>
                          {answerKeys.map(ak => (
                            <span key={ak.num} style={{ minWidth: 80 }}>
                              <strong>Câu {ak.num}:</strong> {ak.answer}
                            </span>
                          ))}
                        </div>
                      </div>
                    )}
                  </>
                )
              })()}
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
