import { useState, useEffect } from 'react'
import { useParams, Link, useNavigate } from 'react-router-dom'
import { examsApi } from '../api/exams'
import apiClient from '../api/client'
import type { ExamResponse } from '../types/api'

interface ExamQuestion {
  id: number
  questionId: number
  questionContent: string
  questionDifficulty: string
  questionOrder: number
  maxScore: number
}

export default function ExamDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const [exam, setExam] = useState<ExamResponse | null>(null)
  const [questions, setQuestions] = useState<ExamQuestion[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [addQModal, setAddQModal] = useState(false)
  const [availableQs, setAvailableQs] = useState<{ id: number; content: string; difficulty: string; subjectName: string }[]>([])
  const [selectedQ, setSelectedQ] = useState<number[]>([])

  useEffect(() => {
    if (!id) return
    const fetchAll = async () => {
      setLoading(true)
      try {
        const examRes = await examsApi.getById(Number(id))
        setExam(examRes.data.data || null)
        const qRes = await apiClient.get(`/exams/${id}/questions`)
        setQuestions(qRes.data?.data?.questions || [])
      } catch { setError('Không tải được dữ liệu kỳ thi') }
      finally { setLoading(false) }
    }
    fetchAll()
  }, [id])

  const handleChangeStatus = async (status: string) => {
    if (!id) return
    try { await examsApi.changeStatus(Number(id), status); navigate(0) }
    catch { alert('Không thể thay đổi trạng thái') }
  }

  const loadAvailableQuestions = async () => {
    if (!exam) return
    try {
      const res = await apiClient.get(`/questions`, { params: { subjectId: exam.subjectId, page: 1, pageSize: 200 } })
      const all = res.data?.data?.items || res.data?.data || []
      const usedIds = new Set(questions.map(q => q.questionId))
      setAvailableQs((Array.isArray(all) ? all : []).filter((q: { id: number }) => !usedIds.has(q.id)))
    } catch { setAvailableQs([]) }
    setAddQModal(true); setSelectedQ([])
  }

  const handleAddQuestions = async () => {
    if (!id || selectedQ.length === 0) return
    try {
      await Promise.all(selectedQ.map((qId, idx) =>
        apiClient.post(`/exams/${id}/questions`, { questionId: qId, questionOrder: questions.length + idx + 1, maxScore: 1 })
      ))
      setAddQModal(false)
      const qRes = await apiClient.get(`/exams/${id}/questions`)
      setQuestions(qRes.data?.data?.questions || [])
    } catch { alert('Lỗi khi thêm câu hỏi') }
  }

  const handleRemoveQuestion = async (examQId: number, questionId: number) => {
    try {
      await apiClient.delete(`/exams/${id}/questions/${questionId}`)
      setQuestions(qs => qs.filter(q => q.id !== examQId))
    } catch { alert('Không thể xóa câu hỏi') }
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
                {questions.length} câu hỏi
              </span>
            </div>
          </div>
          <div style={{ display: 'flex', gap: 8, alignItems: 'center', flexWrap: 'wrap' }}>
            {exam.status === 'DRAFT' && (
              <span className="badge badge-gray" style={{ fontSize: 13 }}>Nháp</span>
            )}
            {exam.status === 'ACTIVE' && (
              <span className="badge badge-green" style={{ fontSize: 13 }}>Đang hoạt động</span>
            )}
            {exam.status === 'CLOSED' && (
              <span className="badge badge-red" style={{ fontSize: 13 }}>Đã đóng</span>
            )}
            {exam.status === 'DRAFT' && (
              <button className="btn btn-success btn-sm" onClick={() => handleChangeStatus('ACTIVE')}>
                <span className="material-icons" style={{ fontSize: 16 }}>play_arrow</span>
                Kích hoạt
              </button>
            )}
            {exam.status === 'ACTIVE' && (
              <button className="btn btn-danger btn-sm" onClick={() => handleChangeStatus('CLOSED')}>
                <span className="material-icons" style={{ fontSize: 16 }}>stop</span>
                Đóng
              </button>
            )}
          </div>
        </div>
      </div>

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
            {questions.map((q, idx) => (
              <div key={q.id} style={{
                display: 'flex', gap: 12, alignItems: 'flex-start', padding: '12px 0',
                borderBottom: idx < questions.length - 1 ? '1px solid var(--border)' : 'none'
              }}>
                <div style={{
                  width: 28, height: 28, borderRadius: 6, background: 'var(--primary-light)',
                  color: 'var(--primary)', display: 'flex', alignItems: 'center', justifyContent: 'center',
                  fontWeight: 600, fontSize: 13, flexShrink: 0
                }}>
                  {idx + 1}
                </div>
                <div style={{ flex: 1 }}>
                  <div style={{ fontSize: 14, lineHeight: 1.5 }}>{q.questionContent}</div>
                  <div style={{ display: 'flex', gap: 8, marginTop: 6 }}>
                    {diffBadge(q.questionDifficulty)}
                    <span style={{ fontSize: 12, color: 'var(--text-muted)' }}>{q.maxScore} điểm</span>
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
    </div>
  )
}
