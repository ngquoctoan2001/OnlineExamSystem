import { useState, useEffect, useRef, useCallback } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { gradingApi } from '../api/grading'
import type { AttemptGradingViewResponse, QuestionGradingItem, BatchGradeItem } from '../types/api'

// ── Teacher Overlay Drawing Canvas ──────────────────────────────────────
function TeacherOverlayCanvas({ studentImage, initialAnnotation, onChange }: {
  studentImage: string; initialAnnotation?: string | null; onChange: (dataUrl: string) => void
}) {
  const canvasRef = useRef<HTMLCanvasElement>(null)
  const drawing = useRef(false)
  const lastPos = useRef<{ x: number; y: number } | null>(null)
  const [color, setColor] = useState('#ef4444')
  const [lineWidth, setLineWidth] = useState(3)
  const [tool, setTool] = useState<'pen' | 'eraser'>('pen')
  const historyRef = useRef<string[]>([])
  const historyIdx = useRef(-1)
  const studentImgRef = useRef<HTMLImageElement | null>(null)

  const redrawComposite = useCallback(() => {
    const canvas = canvasRef.current
    if (!canvas) return
    const ctx = canvas.getContext('2d')
    if (!ctx) return
    ctx.clearRect(0, 0, canvas.width, canvas.height)
    if (studentImgRef.current) ctx.drawImage(studentImgRef.current, 0, 0)
    // Draw annotation layer on top
    if (historyRef.current[historyIdx.current]) {
      const annImg = new Image()
      annImg.onload = () => ctx.drawImage(annImg, 0, 0)
      annImg.src = historyRef.current[historyIdx.current]
    }
  }, [])

  useEffect(() => {
    const canvas = canvasRef.current
    if (!canvas) return
    const ctx = canvas.getContext('2d')
    if (!ctx) return
    // Load student image as background
    const img = new Image()
    img.onload = () => {
      studentImgRef.current = img
      ctx.drawImage(img, 0, 0)
      // If there's an existing annotation, load it on top
      if (initialAnnotation) {
        const annImg = new Image()
        annImg.onload = () => {
          ctx.drawImage(annImg, 0, 0)
          pushHistory()
        }
        annImg.src = initialAnnotation
      } else {
        pushHistory()
      }
    }
    img.src = studentImage
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  // We use a separate offscreen canvas for the annotation layer only
  const annotationCanvasRef = useRef<HTMLCanvasElement | null>(null)
  useEffect(() => {
    const offscreen = document.createElement('canvas')
    offscreen.width = 680
    offscreen.height = 450
    const ctx = offscreen.getContext('2d')
    if (ctx && initialAnnotation) {
      const img = new Image()
      img.onload = () => { ctx.drawImage(img, 0, 0) }
      img.src = initialAnnotation
    }
    annotationCanvasRef.current = offscreen
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  const pushHistory = () => {
    const offscreen = annotationCanvasRef.current
    if (!offscreen) return
    const data = offscreen.toDataURL('image/png')
    historyRef.current = historyRef.current.slice(0, historyIdx.current + 1)
    historyRef.current.push(data)
    historyIdx.current = historyRef.current.length - 1
  }

  const getPos = (e: React.MouseEvent<HTMLCanvasElement>) => {
    const rect = canvasRef.current!.getBoundingClientRect()
    const scaleX = canvasRef.current!.width / rect.width
    const scaleY = canvasRef.current!.height / rect.height
    return { x: (e.clientX - rect.left) * scaleX, y: (e.clientY - rect.top) * scaleY }
  }

  const getTouchPos = (e: React.TouchEvent<HTMLCanvasElement>) => {
    const rect = canvasRef.current!.getBoundingClientRect()
    const t = e.touches[0]
    const scaleX = canvasRef.current!.width / rect.width
    const scaleY = canvasRef.current!.height / rect.height
    return { x: (t.clientX - rect.left) * scaleX, y: (t.clientY - rect.top) * scaleY }
  }

  const startDraw = (pos: { x: number; y: number }) => {
    drawing.current = true
    lastPos.current = pos
  }

  const drawStroke = (pos: { x: number; y: number }) => {
    if (!drawing.current || !lastPos.current) return
    // Draw on both the visible canvas and annotation offscreen canvas
    const drawCtx = (ctx: CanvasRenderingContext2D) => {
      ctx.beginPath()
      ctx.moveTo(lastPos.current!.x, lastPos.current!.y)
      ctx.lineTo(pos.x, pos.y)
      ctx.strokeStyle = tool === 'eraser' ? 'rgba(0,0,0,1)' : color
      ctx.lineWidth = tool === 'eraser' ? lineWidth * 4 : lineWidth
      ctx.lineCap = 'round'
      ctx.lineJoin = 'round'
      if (tool === 'eraser') {
        ctx.globalCompositeOperation = 'destination-out'
      } else {
        ctx.globalCompositeOperation = 'source-over'
      }
      ctx.stroke()
      ctx.globalCompositeOperation = 'source-over'
    }
    // Draw on annotation offscreen
    const offCtx = annotationCanvasRef.current?.getContext('2d')
    if (offCtx) drawCtx(offCtx)
    // Composite to visible canvas
    const ctx = canvasRef.current?.getContext('2d')
    if (ctx) {
      ctx.clearRect(0, 0, canvasRef.current!.width, canvasRef.current!.height)
      if (studentImgRef.current) ctx.drawImage(studentImgRef.current, 0, 0)
      if (annotationCanvasRef.current) ctx.drawImage(annotationCanvasRef.current, 0, 0)
    }
    lastPos.current = pos
  }

  const endDraw = () => {
    if (drawing.current) {
      drawing.current = false
      lastPos.current = null
      pushHistory()
      // Export only the annotation layer
      onChange(annotationCanvasRef.current!.toDataURL('image/png'))
    }
  }

  const undo = () => {
    if (historyIdx.current <= 0) return
    historyIdx.current--
    const img = new Image()
    img.onload = () => {
      const offCtx = annotationCanvasRef.current?.getContext('2d')
      if (offCtx) {
        offCtx.clearRect(0, 0, 680, 450)
        offCtx.drawImage(img, 0, 0)
      }
      redrawComposite()
      onChange(annotationCanvasRef.current!.toDataURL('image/png'))
    }
    img.src = historyRef.current[historyIdx.current]
  }

  const clearAnnotations = () => {
    const offCtx = annotationCanvasRef.current?.getContext('2d')
    if (offCtx) offCtx.clearRect(0, 0, 680, 450)
    pushHistory()
    const ctx = canvasRef.current?.getContext('2d')
    if (ctx) {
      ctx.clearRect(0, 0, canvasRef.current!.width, canvasRef.current!.height)
      if (studentImgRef.current) ctx.drawImage(studentImgRef.current, 0, 0)
    }
    onChange(annotationCanvasRef.current!.toDataURL('image/png'))
  }

  const colors = ['#ef4444', '#3b82f6', '#22c55e', '#f97316', '#8b5cf6', '#000000']

  return (
    <div style={{ border: '2px solid var(--border)', borderRadius: 10, overflow: 'hidden', background: 'white' }}>
      <div style={{ display: 'flex', alignItems: 'center', gap: 8, padding: '8px 12px', borderBottom: '1px solid var(--border)', flexWrap: 'wrap', background: '#fef3c7' }}>
        <span className="material-icons" style={{ fontSize: 16, color: '#d97706' }}>draw</span>
        <span style={{ fontSize: 12, fontWeight: 600, color: '#d97706', marginRight: 8 }}>Sửa bài</span>
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
          <option value={2}>Mảnh</option>
          <option value={3}>Vừa</option>
          <option value={6}>Đậm</option>
          <option value={10}>Rất đậm</option>
        </select>
        <div style={{ flex: 1 }} />
        <button className="btn btn-sm btn-secondary" onClick={undo} title="Hoàn tác">
          <span className="material-icons" style={{ fontSize: 16 }}>undo</span>
        </button>
        <button className="btn btn-sm btn-secondary" onClick={clearAnnotations} title="Xóa sửa bài">
          <span className="material-icons" style={{ fontSize: 16 }}>layers_clear</span>
        </button>
      </div>
      <canvas ref={canvasRef} width={680} height={450}
        style={{ display: 'block', cursor: tool === 'eraser' ? 'cell' : 'crosshair', touchAction: 'none', width: '100%', height: 'auto' }}
        onMouseDown={e => startDraw(getPos(e))}
        onMouseMove={e => drawStroke(getPos(e))}
        onMouseUp={endDraw}
        onMouseLeave={endDraw}
        onTouchStart={e => { e.preventDefault(); startDraw(getTouchPos(e)) }}
        onTouchMove={e => { e.preventDefault(); drawStroke(getTouchPos(e)) }}
        onTouchEnd={endDraw}
      />
    </div>
  )
}

export default function GradingDetailPage() {
  const { attemptId } = useParams<{ attemptId: string }>()
  const navigate = useNavigate()
  const [view, setView] = useState<AttemptGradingViewResponse | null>(null)
  const [loading, setLoading] = useState(true)
  const [grades, setGrades] = useState<Record<number, { score: string; comment: string; annotations?: string }>>({})
  const [savingAll, setSavingAll] = useState(false)
  const [publishing, setPublishing] = useState(false)
  const [message, setMessage] = useState<{ type: 'success' | 'error'; text: string } | null>(null)

  const initGrades = (data: AttemptGradingViewResponse | null) => {
    if (!data) return
    const g: Record<number, { score: string; comment: string; annotations?: string }> = {}
    data.questions?.forEach(q => {
      if (q.gradingResult) {
        g[q.questionId] = { score: String(q.gradingResult.score), comment: q.gradingResult.comment || '', annotations: q.gradingResult.annotations || undefined }
      }
    })
    setGrades(g)
  }

  useEffect(() => {
    if (!attemptId) return
    gradingApi.getGradingView(Number(attemptId)).then(res => {
      const data = res.data?.data ?? null
      setView(data)
      initGrades(data)
    }).catch(() => {}).finally(() => setLoading(false))
  }, [attemptId])

  const isMcqType = (t: string) => t === 'SINGLE_CHOICE' || t === 'MULTIPLE_CHOICE' || t === 'TRUE_FALSE'

  const handleAutoGrade = async () => {
    if (!attemptId) return
    setMessage(null)
    try {
      await gradingApi.autoGrade(Number(attemptId))
      const res = await gradingApi.getGradingView(Number(attemptId))
      const data = res.data?.data ?? null
      setView(data)
      initGrades(data)
      setMessage({ type: 'success', text: 'Đã tự chấm các câu trắc nghiệm' })
    } catch { setMessage({ type: 'error', text: 'Lỗi tự chấm điểm' }) }
  }

  const handleSaveAll = async () => {
    if (!attemptId) return
    setMessage(null)
    // Collect all non-MCQ grades that have been entered
    const gradesToSave: BatchGradeItem[] = []
    for (const q of view?.questions || []) {
      if (isMcqType(q.questionType)) continue
      const g = grades[q.questionId]
      if (!g || g.score === '') continue
      const score = Number(g.score)
      if (isNaN(score) || score < 0 || score > q.points) {
        setMessage({ type: 'error', text: `Câu "${q.content.slice(0, 30)}...": Điểm phải từ 0 đến ${q.points}` })
        return
      }
      gradesToSave.push({ questionId: q.questionId, score, comment: g.comment || undefined, annotations: g.annotations || undefined })
    }

    if (gradesToSave.length === 0) {
      setMessage({ type: 'error', text: 'Chưa có câu nào được chấm điểm' })
      return
    }

    setSavingAll(true)
    try {
      await gradingApi.batchGrade(Number(attemptId), { grades: gradesToSave })
      const res = await gradingApi.getGradingView(Number(attemptId))
      const data = res.data?.data ?? null
      setView(data)
      initGrades(data)
      setMessage({ type: 'success', text: `Đã lưu điểm ${gradesToSave.length} câu hỏi` })
    } catch (e: unknown) {
      const err = e as { response?: { data?: { message?: string } } }
      setMessage({ type: 'error', text: 'Lỗi lưu điểm: ' + (err.response?.data?.message || 'Không xác định') })
    }
    finally { setSavingAll(false) }
  }

  const handlePublish = async () => {
    if (!attemptId || !window.confirm('Công bố kết quả cho học sinh?')) return
    setPublishing(true)
    setMessage(null)
    try {
      await gradingApi.markAsGraded(Number(attemptId))
      await gradingApi.publish(Number(attemptId))
      const res = await gradingApi.getGradingView(Number(attemptId))
      setView(res.data?.data ?? null)
      setMessage({ type: 'success', text: 'Đã công bố kết quả cho học sinh' })
    } catch { setMessage({ type: 'error', text: 'Lỗi công bố kết quả' }) }
    finally { setPublishing(false) }
  }

  if (loading) return <div className="loading-center"><div className="spinner" /></div>
  if (!view) return <div className="empty-state"><span className="material-icons">error</span><p>Không tìm thấy dữ liệu</p></div>

  const totalGraded = view.questions.filter(q => q.gradingResult).length
  const totalQuestions = view.questions.length
  const allGraded = totalGraded === totalQuestions

  return (
    <div style={{ maxWidth: 900, margin: '0 auto' }}>
      {/* Header */}
      <div style={{ display: 'flex', alignItems: 'center', gap: 12, marginBottom: 24 }}>
        <button className="btn btn-secondary btn-sm" onClick={() => navigate('/grading')}>
          <span className="material-icons" style={{ fontSize: 16 }}>arrow_back</span>
        </button>
        <div style={{ flex: 1 }}>
          <h2 style={{ margin: 0, fontSize: 20 }}>Chấm bài: {view.examTitle}</h2>
          <p style={{ color: 'var(--text-muted)', fontSize: 13, margin: '4px 0 0' }}>
            Học sinh: <strong>{view.studentName}</strong> · Trạng thái: <span className="badge badge-blue">{view.status}</span>
            {view.totalScore !== null && <> · Tổng điểm: <strong>{view.totalScore}</strong></>}
          </p>
        </div>
        <div style={{ display: 'flex', gap: 8 }}>
          <button className="btn btn-secondary btn-sm" onClick={handleAutoGrade}>
            <span className="material-icons" style={{ fontSize: 14 }}>auto_awesome</span> Tự chấm MCQ
          </button>
          <button className="btn btn-primary btn-sm" onClick={handleSaveAll} disabled={savingAll}>
            {savingAll ? <span className="spinner" style={{ width: 14, height: 14 }} />
              : <><span className="material-icons" style={{ fontSize: 14 }}>save</span> Lưu tất cả điểm</>}
          </button>
          <button className="btn btn-primary btn-sm" onClick={handlePublish} disabled={publishing || !allGraded} style={{ background: '#22c55e', borderColor: '#22c55e' }}>
            {publishing ? <span className="spinner" style={{ width: 14, height: 14 }} />
              : <><span className="material-icons" style={{ fontSize: 14 }}>publish</span> Công bố</>}
          </button>
        </div>
      </div>

      {/* Progress */}
      <div className="card" style={{ marginBottom: 20, padding: '12px 20px' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 16 }}>
          <span style={{ fontSize: 13, fontWeight: 600 }}>Tiến độ: {totalGraded}/{totalQuestions}</span>
          <div style={{ flex: 1, height: 8, background: 'var(--bg)', borderRadius: 4, overflow: 'hidden' }}>
            <div style={{ width: `${totalQuestions > 0 ? (totalGraded / totalQuestions) * 100 : 0}%`, height: '100%', background: allGraded ? '#22c55e' : 'var(--primary)', borderRadius: 4, transition: 'width 0.3s' }} />
          </div>
        </div>
      </div>

      {/* Message */}
      {message && (
        <div style={{ marginBottom: 16, padding: '10px 16px', borderRadius: 8, fontSize: 13,
          background: message.type === 'success' ? 'rgba(34,197,94,0.1)' : 'rgba(239,68,68,0.1)',
          color: message.type === 'success' ? '#16a34a' : '#dc2626',
          border: `1px solid ${message.type === 'success' ? '#86efac' : '#fca5a5'}`,
          display: 'flex', alignItems: 'center', gap: 8
        }}>
          <span className="material-icons" style={{ fontSize: 18 }}>{message.type === 'success' ? 'check_circle' : 'error'}</span>
          {message.text}
        </div>
      )}

      {/* Questions */}
      {view.questions.map((q, idx) => {
        const g = grades[q.questionId] || { score: '', comment: '' }
        const isMcq = isMcqType(q.questionType)
        return (
          <div key={q.questionId} className="card" style={{ marginBottom: 16 }}>
            <div className="card-header" style={{ background: q.gradingResult ? 'rgba(34,197,94,0.05)' : 'rgba(249,115,22,0.05)' }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
                <div style={{
                  width: 30, height: 30, borderRadius: 8, display: 'flex', alignItems: 'center', justifyContent: 'center',
                  background: q.gradingResult ? '#22c55e' : 'var(--primary)', color: 'white', fontWeight: 700, fontSize: 13
                }}>{idx + 1}</div>
                <div>
                  <span style={{ fontWeight: 600, fontSize: 14 }}>Câu {idx + 1}</span>
                  <span style={{ fontSize: 12, color: 'var(--text-muted)', marginLeft: 8 }}>
                    {q.questionType} · {q.points} điểm
                  </span>
                </div>
              </div>
              {q.gradingResult && (
                <span className="badge badge-green" style={{ fontSize: 11 }}>
                  {q.gradingResult.score}/{q.points} điểm {q.gradingResult.isAutoGraded ? '(tự động)' : '(thủ công)'}
                </span>
              )}
            </div>

            <div style={{ padding: 20 }}>
              {/* Question content */}
              <div style={{ fontSize: 14, lineHeight: 1.6, marginBottom: 16, fontWeight: 500 }}>{q.content}</div>

              {/* MCQ options display */}
              {q.options.length > 0 && (
                <div style={{ display: 'flex', flexDirection: 'column', gap: 8, marginBottom: 16 }}>
                  {q.options.map(opt => {
                    const isCorrect = opt.isCorrect
                    const wasSelected = opt.wasSelected
                    let bg = 'white', border = 'var(--border)'
                    if (isCorrect && wasSelected) { bg = 'rgba(34,197,94,0.1)'; border = '#22c55e' }
                    else if (isCorrect) { bg = 'rgba(34,197,94,0.05)'; border = '#86efac' }
                    else if (wasSelected) { bg = 'rgba(239,68,68,0.08)'; border = '#ef4444' }
                    return (
                      <div key={opt.id} style={{ display: 'flex', alignItems: 'center', gap: 10, padding: '10px 14px', borderRadius: 8, border: `2px solid ${border}`, background: bg }}>
                        {wasSelected && <span className="material-icons" style={{ fontSize: 18, color: isCorrect ? '#22c55e' : '#ef4444' }}>{isCorrect ? 'check_circle' : 'cancel'}</span>}
                        {!wasSelected && isCorrect && <span className="material-icons" style={{ fontSize: 18, color: '#22c55e' }}>radio_button_unchecked</span>}
                        {!wasSelected && !isCorrect && <span className="material-icons" style={{ fontSize: 18, color: 'var(--text-muted)' }}>radio_button_unchecked</span>}
                        <span style={{ fontSize: 13 }}>{opt.content}</span>
                      </div>
                    )
                  })}
                </div>
              )}

              {/* Text answer */}
              {(q.textContent || q.essayContent) && (
                <div style={{ marginBottom: 16 }}>
                  <div style={{ fontSize: 12, fontWeight: 600, color: 'var(--text-muted)', marginBottom: 6 }}>Câu trả lời:</div>
                  <div style={{ padding: 14, background: 'var(--bg)', borderRadius: 8, fontSize: 13, lineHeight: 1.7, whiteSpace: 'pre-wrap', border: '1px solid var(--border)' }}>
                    {q.essayContent || q.textContent || <em style={{ color: 'var(--text-muted)' }}>Không có câu trả lời</em>}
                  </div>
                </div>
              )}

              {/* Canvas image - teacher can draw overlay */}
              {q.canvasImage && (
                <div style={{ marginBottom: 16 }}>
                  <div style={{ fontSize: 12, fontWeight: 600, color: 'var(--text-muted)', marginBottom: 6 }}>Bài vẽ (vẽ đè để sửa bài):</div>
                  <TeacherOverlayCanvas
                    studentImage={q.canvasImage}
                    initialAnnotation={g.annotations}
                    onChange={(dataUrl) => setGrades(prev => ({ ...prev, [q.questionId]: { ...prev[q.questionId] || { score: '', comment: '' }, annotations: dataUrl } }))}
                  />
                </div>
              )}

              {/* No answer */}
              {!q.textContent && !q.essayContent && !q.canvasImage && q.options.length === 0 && (
                <div style={{ padding: 14, background: 'var(--bg)', borderRadius: 8, fontSize: 13, color: 'var(--text-muted)', fontStyle: 'italic' }}>
                  Học sinh không trả lời
                </div>
              )}
              {!q.textContent && !q.essayContent && !q.canvasImage && q.selectedOptionIds.length === 0 && q.options.length > 0 && !q.options.some(o => o.wasSelected) && (
                <div style={{ padding: 10, background: 'rgba(249,115,22,0.08)', borderRadius: 8, fontSize: 13, color: '#f97316', marginBottom: 16 }}>
                  ⚠ Học sinh không chọn đáp án nào
                </div>
              )}

              {/* Grading input */}
              {!isMcq && (
                <div style={{ display: 'flex', gap: 12, alignItems: 'flex-end', marginTop: 8, padding: 14, background: 'var(--bg)', borderRadius: 8 }}>
                  <div style={{ width: 100 }}>
                    <label style={{ fontSize: 12, fontWeight: 600, color: 'var(--text-muted)' }}>Điểm (/{q.points})</label>
                    <input type="number" className="form-control" min={0} max={q.points} step={0.5}
                      value={g.score} onChange={e => setGrades(prev => ({ ...prev, [q.questionId]: { ...prev[q.questionId] || { score: '', comment: '' }, score: e.target.value } }))}
                      style={{ fontSize: 14, marginTop: 4 }}
                    />
                  </div>
                  <div style={{ flex: 1 }}>
                    <label style={{ fontSize: 12, fontWeight: 600, color: 'var(--text-muted)' }}>Nhận xét</label>
                    <input type="text" className="form-control" placeholder="Nhận xét (tùy chọn)..."
                      value={g.comment} onChange={e => setGrades(prev => ({ ...prev, [q.questionId]: { ...prev[q.questionId] || { score: '', comment: '' }, comment: e.target.value } }))}
                      style={{ fontSize: 13, marginTop: 4 }}
                    />
                  </div>
                </div>
              )}
            </div>
          </div>
        )
      })}
    </div>
  )
}
