import { useState, useEffect, useCallback, useRef } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import apiClient from '../api/client'
import { useAuth } from '../contexts/AuthContext'

interface Option { id: number; content: string; orderIndex: number }
interface Question {
  questionId: number
  content: string
  questionType: string
  orderIndex: number
  points: number
  options: Option[]
  isAnswered: boolean
  currentAnswer: { selectedOptionIds?: number[]; textContent?: string } | null
}
interface Attempt { id: number; examId: number; examTitle: string; status: string; startTime: string }
interface ExamInfo { id: number; title: string; durationMinutes: number; subjectName: string }

export default function ExamPlayerPage() {
  const { examId } = useParams<{ examId: string }>()
  const navigate = useNavigate()
  const { user } = useAuth()

  const [step, setStep] = useState<'loading' | 'start' | 'playing' | 'submitted'>('loading')
  const [exam, setExam] = useState<ExamInfo | null>(null)
  const [attempt, setAttempt] = useState<Attempt | null>(null)
  const [questions, setQuestions] = useState<Question[]>([])
  const [current, setCurrent] = useState(0)
  const [answers, setAnswers] = useState<Record<number, { selectedOptionIds: number[]; textContent: string }>>({})
  const [timeLeft, setTimeLeft] = useState(0)
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState('')
  const autoSaveTimer = useRef<ReturnType<typeof setTimeout> | null>(null)
  const timerRef = useRef<ReturnType<typeof setInterval> | null>(null)

  // Load exam info
  useEffect(() => {
    if (!examId) return
    apiClient.get(`/exams/${examId}`).then(res => {
      setExam(res.data?.data || null)
      setStep('start')
    }).catch(() => { setError('Không tìm thấy kỳ thi'); setStep('start') })
  }, [examId])

  // Countdown timer
  useEffect(() => {
    if (step !== 'playing' || timeLeft <= 0) return
    timerRef.current = setInterval(() => {
      setTimeLeft(t => {
        if (t <= 1) {
          clearInterval(timerRef.current!)
          handleSubmit()
          return 0
        }
        return t - 1
      })
    }, 1000)
    return () => clearInterval(timerRef.current!)
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [step])

  const startExam = async () => {
    if (!examId || !user) return
    setStep('loading')
    try {
      const res = await apiClient.post('/exam-attempts/start', {
        examId: Number(examId),
        studentId: user.id
      })
      const att = res.data?.data
      setAttempt(att)
      // Load questions for this attempt
      const qRes = await apiClient.get(`/exam-attempts/${att.id}/questions`)
      const qs: Question[] = qRes.data?.data || []
      setQuestions(qs)
      // Restore existing answers
      const restored: Record<number, { selectedOptionIds: number[]; textContent: string }> = {}
      qs.forEach(q => {
        if (q.currentAnswer) {
          restored[q.questionId] = {
            selectedOptionIds: q.currentAnswer.selectedOptionIds || [],
            textContent: q.currentAnswer.textContent || ''
          }
        }
      })
      setAnswers(restored)
      // Set timer
      if (exam) setTimeLeft(exam.durationMinutes * 60)
      setStep('playing')
    } catch (e: unknown) {
      const err = e as { response?: { data?: { message?: string } } }
      setError(err.response?.data?.message || 'Không thể bắt đầu kỳ thi')
      setStep('start')
    }
  }

  const saveAnswer = useCallback(async (questionId: number, answer: { selectedOptionIds: number[]; textContent: string }) => {
    if (!attempt) return
    try {
      await apiClient.post(`/attempts/${attempt.id}/answers/autosave`, {
        questionId,
        selectedOptionIds: answer.selectedOptionIds,
        textContent: answer.textContent || null,
      })
    } catch { /* silent autosave fail */ }
  }, [attempt])

  const handleOptionChange = (questionId: number, optionId: number, multi: boolean) => {
    setAnswers(prev => {
      const cur = prev[questionId] || { selectedOptionIds: [], textContent: '' }
      let newIds: number[]
      if (multi) {
        newIds = cur.selectedOptionIds.includes(optionId)
          ? cur.selectedOptionIds.filter(id => id !== optionId)
          : [...cur.selectedOptionIds, optionId]
      } else {
        newIds = [optionId]
      }
      const updated = { ...cur, selectedOptionIds: newIds }
      // debounce autosave
      if (autoSaveTimer.current) clearTimeout(autoSaveTimer.current)
      autoSaveTimer.current = setTimeout(() => saveAnswer(questionId, updated), 800)
      return { ...prev, [questionId]: updated }
    })
  }

  const handleTextChange = (questionId: number, text: string) => {
    setAnswers(prev => {
      const updated = { ...(prev[questionId] || { selectedOptionIds: [], textContent: '' }), textContent: text }
      if (autoSaveTimer.current) clearTimeout(autoSaveTimer.current)
      autoSaveTimer.current = setTimeout(() => saveAnswer(questionId, updated), 1000)
      return { ...prev, [questionId]: updated }
    })
  }

  const handleSubmit = async () => {
    if (!attempt || submitting) return
    if (!window.confirm('Bạn có chắc muốn nộp bài thi không?')) return
    setSubmitting(true)
    clearInterval(timerRef.current!)
    try {
      await apiClient.post(`/exam-attempts/${attempt.id}/submit`)
      setStep('submitted')
    } catch {
      alert('Lỗi khi nộp bài. Vui lòng thử lại.')
      setSubmitting(false)
    }
  }

  const fmtTime = (s: number) => {
    const m = Math.floor(s / 60)
    const sec = s % 60
    return `${String(m).padStart(2, '0')}:${String(sec).padStart(2, '0')}`
  }

  const q = questions[current]
  const answeredCount = questions.filter(q => {
    const a = answers[q.questionId]
    return a && (a.selectedOptionIds.length > 0 || a.textContent.trim().length > 0)
  }).length
  const isMultiChoice = q?.questionType === 'MULTIPLE_CHOICE'
  const isText = q?.questionType === 'SHORT_ANSWER' || q?.questionType === 'ESSAY'
  const curAnswer = q ? (answers[q.questionId] || { selectedOptionIds: [], textContent: '' }) : null
  const timerDanger = timeLeft > 0 && timeLeft <= 300 // last 5 min

  // ── LOADING ──────────────────────────────────────────────────────────────
  if (step === 'loading') return <div className="loading-center"><div className="spinner" /></div>

  // ── START SCREEN ─────────────────────────────────────────────────────────
  if (step === 'start') return (
    <div style={{ maxWidth: 520, margin: '60px auto' }}>
      <div className="card" style={{ textAlign: 'center', padding: 40 }}>
        <span className="material-icons" style={{ fontSize: 56, color: 'var(--primary)', marginBottom: 16 }}>quiz</span>
        {error ? (
          <>
            <p style={{ color: 'var(--danger)', marginBottom: 20 }}>{error}</p>
            <button className="btn btn-secondary" onClick={() => navigate(-1)}>Quay lại</button>
          </>
        ) : exam ? (
          <>
            <h2 style={{ marginBottom: 8 }}>{exam.title}</h2>
            <p style={{ color: 'var(--text-secondary)', marginBottom: 24 }}>{exam.subjectName}</p>
            <div style={{ display: 'flex', justifyContent: 'center', gap: 32, marginBottom: 32 }}>
              <div>
                <div style={{ fontSize: 28, fontWeight: 700, color: 'var(--primary)' }}>{exam.durationMinutes}</div>
                <div style={{ fontSize: 12, color: 'var(--text-muted)' }}>Phút làm bài</div>
              </div>
            </div>
            <div className="alert" style={{ marginBottom: 24, textAlign: 'left', fontSize: 13 }}>
              <strong>Lưu ý:</strong>
              <ul style={{ margin: '8px 0 0 16px', paddingLeft: 0 }}>
                <li>Đọc kỹ câu hỏi trước khi trả lời.</li>
                <li>Câu trả lời được tự động lưu.</li>
                <li>Khi hết giờ, bài sẽ tự động nộp.</li>
              </ul>
            </div>
            <button className="btn btn-primary" style={{ width: '100%', fontSize: 15, padding: '12px 0' }} onClick={startExam}>
              <span className="material-icons" style={{ fontSize: 20 }}>play_arrow</span>
              Bắt đầu thi
            </button>
          </>
        ) : (
          <p>Đang tải thông tin kỳ thi...</p>
        )}
      </div>
    </div>
  )

  // ── SUBMITTED ─────────────────────────────────────────────────────────────
  if (step === 'submitted') return (
    <div style={{ maxWidth: 480, margin: '80px auto', textAlign: 'center' }}>
      <div className="card" style={{ padding: 40 }}>
        <span className="material-icons" style={{ fontSize: 64, color: '#22c55e', marginBottom: 16 }}>check_circle</span>
        <h2 style={{ marginBottom: 8 }}>Nộp bài thành công!</h2>
        <p style={{ color: 'var(--text-secondary)', marginBottom: 8 }}>Bài làm của bạn đã được ghi nhận.</p>
        <p style={{ color: 'var(--text-muted)', fontSize: 13, marginBottom: 28 }}>
          Đã trả lời {answeredCount}/{questions.length} câu hỏi
        </p>
        <div style={{ display: 'flex', justifyContent: 'center', gap: 12 }}>
          <button className="btn btn-secondary" onClick={() => navigate('/results')}>
            <span className="material-icons" style={{ fontSize: 16 }}>assessment</span>
            Xem kết quả
          </button>
          <button className="btn btn-primary" onClick={() => navigate('/exams')}>
            <span className="material-icons" style={{ fontSize: 16 }}>list</span>
            Danh sách thi
          </button>
        </div>
      </div>
    </div>
  )

  // ── PLAYING ───────────────────────────────────────────────────────────────
  return (
    <div style={{ height: '100vh', display: 'flex', flexDirection: 'column', background: 'var(--bg)' }}>
      {/* Top bar */}
      <div style={{
        background: 'white', borderBottom: '1px solid var(--border)', padding: '0 24px',
        display: 'flex', alignItems: 'center', justifyContent: 'space-between', height: 56, flexShrink: 0
      }}>
        <div style={{ fontWeight: 600, fontSize: 15, color: 'var(--text)' }}>{exam?.title}</div>
        <div style={{ display: 'flex', alignItems: 'center', gap: 16 }}>
          <div style={{
            display: 'flex', alignItems: 'center', gap: 6,
            fontWeight: 700, fontSize: 16,
            color: timerDanger ? 'var(--danger)' : 'var(--primary)',
            background: timerDanger ? 'rgba(239,68,68,0.08)' : 'var(--primary-light)',
            padding: '4px 14px', borderRadius: 20
          }}>
            <span className="material-icons" style={{ fontSize: 18 }}>schedule</span>
            {fmtTime(timeLeft)}
          </div>
          <span style={{ fontSize: 13, color: 'var(--text-muted)' }}>
            {answeredCount}/{questions.length} câu
          </span>
          <button className="btn btn-primary btn-sm" onClick={handleSubmit} disabled={submitting}>
            {submitting ? <span className="spinner" style={{ width: 16, height: 16 }} /> : (
              <><span className="material-icons" style={{ fontSize: 16 }}>send</span> Nộp bài</>
            )}
          </button>
        </div>
      </div>

      <div style={{ flex: 1, display: 'flex', overflow: 'hidden' }}>
        {/* Question navigator sidebar */}
        <div style={{
          width: 200, flexShrink: 0, borderRight: '1px solid var(--border)',
          background: 'white', overflowY: 'auto', padding: 12
        }}>
          <div style={{ fontSize: 12, color: 'var(--text-muted)', fontWeight: 600, marginBottom: 10, textTransform: 'uppercase', letterSpacing: '0.05em' }}>
            Câu hỏi
          </div>
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: 6 }}>
            {questions.map((question, idx) => {
              const a = answers[question.questionId]
              const done = a && (a.selectedOptionIds.length > 0 || a.textContent.trim().length > 0)
              return (
                <button
                  key={question.questionId}
                  onClick={() => setCurrent(idx)}
                  style={{
                    height: 36, borderRadius: 6, border: `2px solid ${current === idx ? 'var(--primary)' : done ? '#22c55e' : 'var(--border)'}`,
                    background: current === idx ? 'var(--primary)' : done ? 'rgba(34,197,94,0.1)' : 'var(--bg)',
                    color: current === idx ? 'white' : done ? '#16a34a' : 'var(--text)',
                    fontWeight: 600, fontSize: 13, cursor: 'pointer'
                  }}
                >
                  {idx + 1}
                </button>
              )
            })}
          </div>

          <div style={{ marginTop: 16 }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: 6, fontSize: 12, color: 'var(--text-muted)', marginBottom: 4 }}>
              <div style={{ width: 12, height: 12, borderRadius: 3, background: 'rgba(34,197,94,0.3)', border: '2px solid #22c55e' }} />
              Đã làm ({answeredCount})
            </div>
            <div style={{ display: 'flex', alignItems: 'center', gap: 6, fontSize: 12, color: 'var(--text-muted)' }}>
              <div style={{ width: 12, height: 12, borderRadius: 3, background: 'var(--bg)', border: '2px solid var(--border)' }} />
              Chưa làm ({questions.length - answeredCount})
            </div>
          </div>
        </div>

        {/* Main question area */}
        <div style={{ flex: 1, overflowY: 'auto', padding: 32 }}>
          {q ? (
            <div style={{ maxWidth: 720, margin: '0 auto' }}>
              {/* Question header */}
              <div style={{ display: 'flex', gap: 12, alignItems: 'flex-start', marginBottom: 28 }}>
                <div style={{
                  width: 36, height: 36, borderRadius: 10, background: 'var(--primary)',
                  color: 'white', display: 'flex', alignItems: 'center', justifyContent: 'center',
                  fontWeight: 700, fontSize: 15, flexShrink: 0
                }}>
                  {current + 1}
                </div>
                <div>
                  <div style={{ fontSize: 15, lineHeight: 1.6, fontWeight: 500 }}>{q.content}</div>
                  <div style={{ marginTop: 6, fontSize: 12, color: 'var(--text-muted)' }}>
                    {q.points} điểm
                    {isMultiChoice && ' · Chọn nhiều đáp án'}
                  </div>
                </div>
              </div>

              {/* MCQ / TRUE_FALSE */}
              {!isText && q.options.length > 0 && (
                <div style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
                  {q.options.map((opt, oi) => {
                    const selected = curAnswer?.selectedOptionIds?.includes(opt.id) ?? false
                    return (
                      <label
                        key={opt.id}
                        style={{
                          display: 'flex', alignItems: 'flex-start', gap: 12, padding: '14px 18px',
                          borderRadius: 10, border: `2px solid ${selected ? 'var(--primary)' : 'var(--border)'}`,
                          background: selected ? 'var(--primary-light)' : 'white',
                          cursor: 'pointer', transition: 'all 0.15s'
                        }}
                      >
                        <input
                          type={isMultiChoice ? 'checkbox' : 'radio'}
                          checked={selected}
                          onChange={() => handleOptionChange(q.questionId, opt.id, isMultiChoice)}
                          style={{ marginTop: 2 }}
                        />
                        <div style={{ fontSize: 13 }}>
                          <span style={{ fontWeight: 600, color: 'var(--primary)', marginRight: 8 }}>
                            {String.fromCharCode(65 + oi)}.
                          </span>
                          {opt.content}
                        </div>
                      </label>
                    )
                  })}
                </div>
              )}

              {/* Short answer */}
              {q.questionType === 'SHORT_ANSWER' && (
                <input
                  type="text"
                  className="form-control"
                  placeholder="Nhập câu trả lời ngắn..."
                  value={curAnswer?.textContent || ''}
                  onChange={e => handleTextChange(q.questionId, e.target.value)}
                  style={{ fontSize: 14 }}
                />
              )}

              {/* Essay */}
              {q.questionType === 'ESSAY' && (
                <textarea
                  className="form-control"
                  placeholder="Nhập bài luận của bạn..."
                  rows={10}
                  value={curAnswer?.textContent || ''}
                  onChange={e => handleTextChange(q.questionId, e.target.value)}
                  style={{ fontSize: 14, resize: 'vertical' }}
                />
              )}

              {/* Navigation */}
              <div style={{ display: 'flex', justifyContent: 'space-between', marginTop: 32 }}>
                <button
                  className="btn btn-secondary"
                  disabled={current === 0}
                  onClick={() => setCurrent(c => c - 1)}
                >
                  <span className="material-icons" style={{ fontSize: 18 }}>chevron_left</span>
                  Câu trước
                </button>
                {current < questions.length - 1 ? (
                  <button className="btn btn-primary" onClick={() => setCurrent(c => c + 1)}>
                    Câu tiếp
                    <span className="material-icons" style={{ fontSize: 18 }}>chevron_right</span>
                  </button>
                ) : (
                  <button className="btn btn-success" onClick={handleSubmit} disabled={submitting}>
                    <span className="material-icons" style={{ fontSize: 18 }}>send</span>
                    Nộp bài
                  </button>
                )}
              </div>
            </div>
          ) : (
            <div className="empty-state"><span className="material-icons">quiz</span><p>Không có câu hỏi</p></div>
          )}
        </div>
      </div>
    </div>
  )
}
