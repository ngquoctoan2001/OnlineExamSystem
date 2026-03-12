import { useState, useEffect, useCallback, useRef } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { examsApi } from '../api/exams'
import { examAttemptsApi } from '../api/examAttempts'
import { answersApi } from '../api/answers'
import { studentsApi } from '../api/students'
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
  currentAnswer: { selectedOptionIds?: number[]; textContent?: string; canvasImage?: string } | null
}
interface Attempt { id: number; examId: number; examTitle: string; status: string; startTime: string }
interface ExamInfo { id: number; title: string; durationMinutes: number; subjectName: string }

// ── Drawing Canvas Component ────────────────────────────────────────────────
function DrawingCanvas({ initialImage, onChange }: { initialImage?: string; onChange: (dataUrl: string) => void }) {
  const canvasRef = useRef<HTMLCanvasElement>(null)
  const drawing = useRef(false)
  const lastPos = useRef<{ x: number; y: number } | null>(null)
  const [color, setColor] = useState('#000000')
  const [lineWidth, setLineWidth] = useState(3)
  const [tool, setTool] = useState<'pen' | 'eraser'>('pen')
  const historyRef = useRef<string[]>([])
  const historyIdx = useRef(-1)

  useEffect(() => {
    const canvas = canvasRef.current
    if (!canvas) return
    const ctx = canvas.getContext('2d')
    if (!ctx) return
    if (initialImage) {
      const img = new Image()
      img.onload = () => { ctx.drawImage(img, 0, 0); pushHistory() }
      img.src = initialImage
    } else {
      ctx.fillStyle = '#ffffff'
      ctx.fillRect(0, 0, canvas.width, canvas.height)
      pushHistory()
    }
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  const pushHistory = () => {
    const canvas = canvasRef.current
    if (!canvas) return
    const data = canvas.toDataURL('image/png')
    historyRef.current = historyRef.current.slice(0, historyIdx.current + 1)
    historyRef.current.push(data)
    historyIdx.current = historyRef.current.length - 1
  }

  const getPos = (e: React.MouseEvent<HTMLCanvasElement>) => {
    const rect = canvasRef.current!.getBoundingClientRect()
    return { x: e.clientX - rect.left, y: e.clientY - rect.top }
  }

  const getTouchPos = (e: React.TouchEvent<HTMLCanvasElement>) => {
    const rect = canvasRef.current!.getBoundingClientRect()
    const t = e.touches[0]
    return { x: t.clientX - rect.left, y: t.clientY - rect.top }
  }

  const startDraw = (pos: { x: number; y: number }) => {
    drawing.current = true
    lastPos.current = pos
  }

  const draw = (pos: { x: number; y: number }) => {
    if (!drawing.current || !lastPos.current) return
    const ctx = canvasRef.current?.getContext('2d')
    if (!ctx) return
    ctx.beginPath()
    ctx.moveTo(lastPos.current.x, lastPos.current.y)
    ctx.lineTo(pos.x, pos.y)
    ctx.strokeStyle = tool === 'eraser' ? '#ffffff' : color
    ctx.lineWidth = tool === 'eraser' ? lineWidth * 4 : lineWidth
    ctx.lineCap = 'round'
    ctx.lineJoin = 'round'
    ctx.stroke()
    lastPos.current = pos
  }

  const endDraw = () => {
    if (drawing.current) {
      drawing.current = false
      lastPos.current = null
      pushHistory()
      onChange(canvasRef.current!.toDataURL('image/png'))
    }
  }

  const undo = () => {
    if (historyIdx.current <= 0) return
    historyIdx.current--
    const img = new Image()
    img.onload = () => {
      const ctx = canvasRef.current?.getContext('2d')
      if (ctx) { ctx.clearRect(0, 0, canvasRef.current!.width, canvasRef.current!.height); ctx.drawImage(img, 0, 0) }
      onChange(canvasRef.current!.toDataURL('image/png'))
    }
    img.src = historyRef.current[historyIdx.current]
  }

  const clearCanvas = () => {
    const ctx = canvasRef.current?.getContext('2d')
    if (!ctx) return
    ctx.fillStyle = '#ffffff'
    ctx.fillRect(0, 0, canvasRef.current!.width, canvasRef.current!.height)
    pushHistory()
    onChange(canvasRef.current!.toDataURL('image/png'))
  }

  const colors = ['#000000', '#ef4444', '#3b82f6', '#22c55e', '#f97316', '#8b5cf6', '#ec4899']

  return (
    <div style={{ border: '2px solid var(--border)', borderRadius: 10, overflow: 'hidden', background: 'white' }}>
      {/* Toolbar */}
      <div style={{ display: 'flex', alignItems: 'center', gap: 8, padding: '8px 12px', borderBottom: '1px solid var(--border)', flexWrap: 'wrap', background: 'var(--bg)' }}>
        <button className={`btn btn-sm ${tool === 'pen' ? 'btn-primary' : 'btn-secondary'}`} onClick={() => setTool('pen')} title="Bút vẽ">
          <span className="material-icons" style={{ fontSize: 16 }}>edit</span>
        </button>
        <button className={`btn btn-sm ${tool === 'eraser' ? 'btn-primary' : 'btn-secondary'}`} onClick={() => setTool('eraser')} title="Tẩy">
          <span className="material-icons" style={{ fontSize: 16 }}>auto_fix_normal</span>
        </button>
        <div style={{ width: 1, height: 24, background: 'var(--border)' }} />
        {colors.map(c => (
          <button key={c} onClick={() => { setColor(c); setTool('pen') }}
            style={{ width: 22, height: 22, borderRadius: '50%', background: c, border: color === c && tool === 'pen' ? '3px solid var(--primary)' : '2px solid var(--border)', cursor: 'pointer', padding: 0 }}
          />
        ))}
        <div style={{ width: 1, height: 24, background: 'var(--border)' }} />
        <select value={lineWidth} onChange={e => setLineWidth(Number(e.target.value))} className="form-control" style={{ width: 70, height: 28, fontSize: 12, padding: '0 4px' }}>
          <option value={1}>Mảnh</option>
          <option value={3}>Vừa</option>
          <option value={6}>Đậm</option>
          <option value={10}>Rất đậm</option>
        </select>
        <div style={{ flex: 1 }} />
        <button className="btn btn-sm btn-secondary" onClick={undo} title="Hoàn tác">
          <span className="material-icons" style={{ fontSize: 16 }}>undo</span>
        </button>
        <button className="btn btn-sm btn-secondary" onClick={clearCanvas} title="Xóa tất cả">
          <span className="material-icons" style={{ fontSize: 16 }}>delete</span>
        </button>
      </div>
      <canvas
        ref={canvasRef}
        width={680}
        height={450}
        style={{ display: 'block', cursor: tool === 'eraser' ? 'cell' : 'crosshair', touchAction: 'none', width: '100%', height: 'auto' }}
        onMouseDown={e => startDraw(getPos(e))}
        onMouseMove={e => draw(getPos(e))}
        onMouseUp={endDraw}
        onMouseLeave={endDraw}
        onTouchStart={e => { e.preventDefault(); startDraw(getTouchPos(e)) }}
        onTouchMove={e => { e.preventDefault(); draw(getTouchPos(e)) }}
        onTouchEnd={endDraw}
      />
    </div>
  )
}

export default function ExamPlayerPage() {
  const { examId } = useParams<{ examId: string }>()
  const navigate = useNavigate()
  const { user } = useAuth()

  const [step, setStep] = useState<'loading' | 'start' | 'playing' | 'submitted'>('loading')
  const [exam, setExam] = useState<ExamInfo | null>(null)
  const [attempt, setAttempt] = useState<Attempt | null>(null)
  const [questions, setQuestions] = useState<Question[]>([])
  const [current, setCurrent] = useState(0)
  const [answers, setAnswers] = useState<Record<number, { selectedOptionIds: number[]; textContent: string; canvasImage?: string }>>({})
  const [timeLeft, setTimeLeft] = useState(0)
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState('')
  const [flagged, setFlagged] = useState<Set<number>>(new Set())
  const autoSaveTimer = useRef<ReturnType<typeof setTimeout> | null>(null)
  const timerRef = useRef<ReturnType<typeof setInterval> | null>(null)

  const toggleFlag = (questionId: number) => {
    setFlagged(prev => {
      const next = new Set(prev)
      if (next.has(questionId)) next.delete(questionId)
      else next.add(questionId)
      return next
    })
  }

  // Load exam info
  useEffect(() => {
    if (!examId) return
    examsApi.getById(Number(examId)).then(res => {
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

  // ── Anti-cheat: detect tab switch / page leave ──
  const [violations, setViolations] = useState(0)
  useEffect(() => {
    if (step !== 'playing' || !attempt) return
    const logViolation = async (type: string) => {
      setViolations(v => v + 1)
      try {
        await examAttemptsApi.logViolation(attempt.id, { violationType: type, description: `${type} detected` })
      } catch { /* ignore */ }
    }
    const handleVisibility = () => {
      if (document.hidden) logViolation('TAB_SWITCH')
    }
    const handleBlur = () => logViolation('WINDOW_BLUR')
    document.addEventListener('visibilitychange', handleVisibility)
    window.addEventListener('blur', handleBlur)
    return () => {
      document.removeEventListener('visibilitychange', handleVisibility)
      window.removeEventListener('blur', handleBlur)
    }
  }, [step, attempt])

  // ── Fullscreen enforcement ──
  const [fullscreenWarning, setFullscreenWarning] = useState(false)
  useEffect(() => {
    if (step !== 'playing') return
    const enterFullscreen = () => {
      try { document.documentElement.requestFullscreen?.() } catch { /* ignore */ }
    }
    enterFullscreen()
    const handleFullscreenChange = async () => {
      if (!document.fullscreenElement && step === 'playing') {
        setFullscreenWarning(true)
        if (attempt) {
          try {
            await examAttemptsApi.logViolation(attempt.id, { violationType: 'FULLSCREEN_EXIT', description: 'Student exited fullscreen' })
          } catch { /* ignore */ }
        }
      } else {
        setFullscreenWarning(false)
      }
    }
    document.addEventListener('fullscreenchange', handleFullscreenChange)
    return () => {
      document.removeEventListener('fullscreenchange', handleFullscreenChange)
      if (document.fullscreenElement) document.exitFullscreen?.().catch(() => {})
    }
  }, [step, attempt])

  // ── Connection loss detection ──
  const [offline, setOffline] = useState(false)
  useEffect(() => {
    if (step !== 'playing') return
    const handleOffline = () => setOffline(true)
    const handleOnline = () => setOffline(false)
    window.addEventListener('offline', handleOffline)
    window.addEventListener('online', handleOnline)
    return () => {
      window.removeEventListener('offline', handleOffline)
      window.removeEventListener('online', handleOnline)
    }
  }, [step])

  const loadAttemptQuestions = async (att: Attempt) => {
    setAttempt(att)
    const qRes = await examAttemptsApi.getQuestions(att.id)
    const qs = (qRes.data?.data || []) as unknown as Question[]
    setQuestions(qs)
    // Restore existing answers
    const restored: Record<number, { selectedOptionIds: number[]; textContent: string; canvasImage?: string }> = {}
    qs.forEach(q => {
      if (q.currentAnswer) {
        restored[q.questionId] = {
          selectedOptionIds: q.currentAnswer.selectedOptionIds || [],
          textContent: q.currentAnswer.textContent || '',
          canvasImage: q.currentAnswer.canvasImage || undefined
        }
      }
    })
    setAnswers(restored)
    if (exam) setTimeLeft(exam.durationMinutes * 60)
    setStep('playing')
  }

  const startExam = async () => {
    if (!examId || !user) return
    setStep('loading')
    setError('')
    let studentId = user.studentId
    // If studentId not yet loaded, try to fetch it
    if (!studentId && user.role?.toUpperCase() === 'STUDENT') {
      try {
        const meRes = await studentsApi.getMe()
        studentId = meRes.data?.data?.id
      } catch { /* ignore */ }
    }
    if (!studentId) {
      studentId = user.id
    }
    try {
      const res = await examAttemptsApi.start({
        examId: Number(examId),
        studentId
      })
      // Backend returns 200 even on failure — check success field
      if (!res.data?.success) {
        // If student already has an active attempt, try to resume it
        try {
          const curRes = await examAttemptsApi.getCurrentAttempt(studentId, Number(examId))
          if (curRes.data?.success && curRes.data.data) {
            await loadAttemptQuestions(curRes.data.data as unknown as Attempt)
            return
          }
        } catch { /* fallthrough to error */ }
        setError(res.data?.message || 'Không thể bắt đầu kỳ thi')
        setStep('start')
        return
      }
      const att = res.data.data
      if (att) await loadAttemptQuestions(att as unknown as Attempt)
    } catch (e: unknown) {
      const err = e as { response?: { data?: { message?: string } } }
      setError(err.response?.data?.message || 'Không thể bắt đầu kỳ thi')
      setStep('start')
    }
  }

  const saveAnswer = useCallback(async (questionId: number, answer: { selectedOptionIds: number[]; textContent: string; canvasImage?: string }) => {
    if (!attempt) return
    try {
      await answersApi.autoSave(attempt.id, {
        questionId,
        selectedOptionIds: answer.selectedOptionIds,
        textContent: answer.textContent || null,
        canvasImage: answer.canvasImage || null,
      } as any)
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
    // Flush all pending autosaves before submitting
    if (autoSaveTimer.current) clearTimeout(autoSaveTimer.current)
    try {
      const savePromises = Object.entries(answers).map(([qId, answer]) =>
        saveAnswer(Number(qId), answer)
      )
      await Promise.all(savePromises)
    } catch { /* ignore flush errors */ }
    try {
      await examAttemptsApi.submit(attempt.id)
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
    return a && (a.selectedOptionIds.length > 0 || a.textContent.trim().length > 0 || !!a.canvasImage)
  }).length
  const isMultiChoice = q?.questionType === 'MULTIPLE_CHOICE'
  const isText = q?.questionType === 'SHORT_ANSWER' || q?.questionType === 'ESSAY'
  const isDrawing = q?.questionType === 'DRAWING'
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
          {attempt && (
            <button className="btn btn-secondary" onClick={() => navigate(`/review/${attempt.id}`)}>
              <span className="material-icons" style={{ fontSize: 16 }}>visibility</span>
              Xem lại bài làm
            </button>
          )}
          <button className="btn btn-secondary" onClick={() => navigate('/results')}>
            <span className="material-icons" style={{ fontSize: 16 }}>assessment</span>
            Kết quả
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
          {violations > 0 && (
            <span style={{ fontSize: 12, color: 'var(--danger)', display: 'flex', alignItems: 'center', gap: 4, background: 'rgba(239,68,68,0.08)', padding: '3px 10px', borderRadius: 12 }} title="Số lần rời trang / chuyển tab">
              <span className="material-icons" style={{ fontSize: 14 }}>warning</span>
              {violations}
            </span>
          )}
          <button className="btn btn-primary btn-sm" onClick={handleSubmit} disabled={submitting}>
            {submitting ? <span className="spinner" style={{ width: 16, height: 16 }} /> : (

              <><span className="material-icons" style={{ fontSize: 16 }}>send</span> Nộp bài</>
            )}
          </button>
        </div>
      </div>

      {/* Fullscreen warning */}
      {fullscreenWarning && (
        <div style={{ background: '#fef3cd', color: '#856404', padding: '8px 24px', display: 'flex', alignItems: 'center', justifyContent: 'space-between', fontSize: 13, borderBottom: '1px solid #ffc107' }}>
          <span style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
            <span className="material-icons" style={{ fontSize: 18 }}>fullscreen</span>
            Bạn đã thoát chế độ toàn màn hình. Vi phạm này đã được ghi nhận.
          </span>
          <button className="btn btn-sm" onClick={() => { document.documentElement.requestFullscreen?.(); setFullscreenWarning(false) }} style={{ fontSize: 12, padding: '2px 10px' }}>
            Quay lại toàn màn hình
          </button>
        </div>
      )}

      {/* Offline warning */}
      {offline && (
        <div style={{ background: '#f8d7da', color: '#721c24', padding: '8px 24px', display: 'flex', alignItems: 'center', gap: 8, fontSize: 13, borderBottom: '1px solid #f5c6cb' }}>
          <span className="material-icons" style={{ fontSize: 18 }}>wifi_off</span>
          Mất kết nối mạng! Bài làm đã được lưu tạm. Hãy kiểm tra kết nối internet.
        </div>
      )}

      <div style={{ flex: 1, display: 'flex', overflow: 'hidden' }}>
        {/* Question navigator sidebar */}
        <div className="exam-player-sidebar">
          <div style={{ fontSize: 12, color: 'var(--text-muted)', fontWeight: 600, marginBottom: 10, textTransform: 'uppercase', letterSpacing: '0.05em' }}>
            Câu hỏi
          </div>
          <div className="exam-player-qgrid">
            {questions.map((question, idx) => {
              const a = answers[question.questionId]
              const done = a && (a.selectedOptionIds.length > 0 || a.textContent.trim().length > 0 || !!a.canvasImage)
              const isFlagged = flagged.has(question.questionId)
              return (
                <button
                  key={question.questionId}
                  onClick={() => setCurrent(idx)}
                  style={{
                    height: 36, borderRadius: 6, border: `2px solid ${current === idx ? 'var(--primary)' : done ? '#22c55e' : isFlagged ? '#f97316' : 'var(--border)'}`,
                    background: current === idx ? 'var(--primary)' : done ? 'rgba(34,197,94,0.1)' : isFlagged ? 'rgba(249,115,22,0.1)' : 'var(--bg)',
                    color: current === idx ? 'white' : done ? '#16a34a' : isFlagged ? '#f97316' : 'var(--text)',
                    fontWeight: 600, fontSize: 13, cursor: 'pointer', position: 'relative'
                  }}
                >
                  {idx + 1}
                  {isFlagged && (
                    <span style={{ position: 'absolute', top: -4, right: -4, fontSize: 10, color: '#f97316' }}>
                      <span className="material-icons" style={{ fontSize: 12 }}>flag</span>
                    </span>
                  )}
                </button>
              )
            })}
          </div>

          <div style={{ marginTop: 16 }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: 6, fontSize: 12, color: 'var(--text-muted)', marginBottom: 4 }}>
              <div style={{ width: 12, height: 12, borderRadius: 3, background: 'rgba(34,197,94,0.3)', border: '2px solid #22c55e' }} />
              Đã làm ({answeredCount})
            </div>
            <div style={{ display: 'flex', alignItems: 'center', gap: 6, fontSize: 12, color: 'var(--text-muted)', marginBottom: 4 }}>
              <div style={{ width: 12, height: 12, borderRadius: 3, background: 'rgba(249,115,22,0.3)', border: '2px solid #f97316' }} />
              Đánh dấu ({flagged.size})
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
                <div style={{ flex: 1 }}>
                  <div style={{ fontSize: 15, lineHeight: 1.6, fontWeight: 500 }}>{q.content}</div>
                  <div style={{ marginTop: 6, fontSize: 12, color: 'var(--text-muted)' }}>
                    {q.points} điểm
                    {isMultiChoice && ' · Chọn nhiều đáp án'}
                  </div>
                </div>
                <button
                  onClick={() => toggleFlag(q.questionId)}
                  title={flagged.has(q.questionId) ? 'Bỏ đánh dấu' : 'Đánh dấu để xem lại'}
                  style={{
                    background: flagged.has(q.questionId) ? 'rgba(249,115,22,0.1)' : 'transparent',
                    border: `1px solid ${flagged.has(q.questionId) ? '#f97316' : 'var(--border)'}`,
                    borderRadius: 8, padding: '6px 10px', cursor: 'pointer', display: 'flex',
                    alignItems: 'center', gap: 4, fontSize: 12, color: flagged.has(q.questionId) ? '#f97316' : 'var(--text-muted)',
                    flexShrink: 0
                  }}
                >
                  <span className="material-icons" style={{ fontSize: 16 }}>
                    {flagged.has(q.questionId) ? 'flag' : 'outlined_flag'}
                  </span>
                  {flagged.has(q.questionId) ? 'Đã đánh dấu' : 'Đánh dấu'}
                </button>
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
                <div>
                  <div style={{ display: 'flex', gap: 4, padding: '6px 8px', background: 'var(--bg)', border: '1px solid var(--border)', borderBottom: 'none', borderRadius: '8px 8px 0 0', flexWrap: 'wrap' }}>
                    {[
                      { cmd: 'bold', icon: 'format_bold', title: 'In đậm' },
                      { cmd: 'italic', icon: 'format_italic', title: 'In nghiêng' },
                      { cmd: 'underline', icon: 'format_underlined', title: 'Gạch chân' },
                      { cmd: 'strikethrough', icon: 'strikethrough_s', title: 'Gạch ngang' },
                    ].map(btn => (
                      <button
                        key={btn.cmd}
                        type="button"
                        title={btn.title}
                        onClick={() => {
                          const ta = document.getElementById(`essay-${q.questionId}`) as HTMLTextAreaElement | null
                          if (!ta) return
                          const start = ta.selectionStart
                          const end = ta.selectionEnd
                          const text = curAnswer?.textContent || ''
                          const selected = text.substring(start, end)
                          const markers: Record<string, [string, string]> = {
                            bold: ['**', '**'], italic: ['*', '*'], underline: ['__', '__'], strikethrough: ['~~', '~~']
                          }
                          const [pre, post] = markers[btn.cmd]
                          const newText = text.substring(0, start) + pre + selected + post + text.substring(end)
                          handleTextChange(q.questionId, newText)
                          setTimeout(() => { ta.focus(); ta.setSelectionRange(start + pre.length, end + pre.length) }, 0)
                        }}
                        style={{ background: 'white', border: '1px solid var(--border)', borderRadius: 4, padding: '2px 6px', cursor: 'pointer', display: 'flex', alignItems: 'center' }}
                      >
                        <span className="material-icons" style={{ fontSize: 16 }}>{btn.icon}</span>
                      </button>
                    ))}
                    <div style={{ width: 1, height: 24, background: 'var(--border)', margin: '0 4px' }} />
                    {[
                      { label: 'H1', markup: '# ', title: 'Tiêu đề 1' },
                      { label: 'H2', markup: '## ', title: 'Tiêu đề 2' },
                      { label: '•', markup: '- ', title: 'Danh sách' },
                    ].map(btn => (
                      <button
                        key={btn.label}
                        type="button"
                        title={btn.title}
                        onClick={() => {
                          const ta = document.getElementById(`essay-${q.questionId}`) as HTMLTextAreaElement | null
                          if (!ta) return
                          const start = ta.selectionStart
                          const text = curAnswer?.textContent || ''
                          const lineStart = text.lastIndexOf('\n', start - 1) + 1
                          const newText = text.substring(0, lineStart) + btn.markup + text.substring(lineStart)
                          handleTextChange(q.questionId, newText)
                          setTimeout(() => { ta.focus(); ta.setSelectionRange(start + btn.markup.length, start + btn.markup.length) }, 0)
                        }}
                        style={{ background: 'white', border: '1px solid var(--border)', borderRadius: 4, padding: '2px 8px', cursor: 'pointer', fontSize: 12, fontWeight: 600 }}
                      >
                        {btn.label}
                      </button>
                    ))}
                  </div>
                  <textarea
                    id={`essay-${q.questionId}`}
                    className="form-control"
                    placeholder="Nhập bài luận của bạn... (hỗ trợ Markdown: **đậm**, *nghiêng*, __gạch chân__)"
                    rows={10}
                    value={curAnswer?.textContent || ''}
                    onChange={e => handleTextChange(q.questionId, e.target.value)}
                    style={{ fontSize: 14, resize: 'vertical', borderRadius: '0 0 8px 8px', borderTop: 'none' }}
                  />
                </div>
              )}

              {/* Drawing Canvas */}
              {isDrawing && (
                <DrawingCanvas
                  key={q.questionId}
                  initialImage={curAnswer?.canvasImage}
                  onChange={(dataUrl) => {
                    setAnswers(prev => {
                      const updated = { ...(prev[q.questionId] || { selectedOptionIds: [], textContent: '' }), canvasImage: dataUrl }
                      if (autoSaveTimer.current) clearTimeout(autoSaveTimer.current)
                      autoSaveTimer.current = setTimeout(() => saveAnswer(q.questionId, updated), 1500)
                      return { ...prev, [q.questionId]: updated }
                    })
                  }}
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
