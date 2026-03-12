import { useState, useEffect } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { gradingApi } from '../api/grading'
import type { AttemptGradingViewResponse, QuestionGradingItem } from '../types/api'

export default function ExamReviewPage() {
  const { attemptId } = useParams<{ attemptId: string }>()
  const navigate = useNavigate()
  const [data, setData] = useState<AttemptGradingViewResponse | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  const questionTypeLabel = (t: string) => {
    switch (t) {
      case 'SINGLE_CHOICE': return 'Trắc nghiệm'
      case 'MULTIPLE_CHOICE': return 'Nhiều đáp án'
      case 'TRUE_FALSE': return 'Đúng/Sai'
      case 'SHORT_ANSWER': return 'Trả lời ngắn'
      case 'ESSAY': return 'Tự luận'
      case 'FILL_IN_BLANK': return 'Điền vào chỗ trống'
      case 'DRAWING': return 'Vẽ hình'
      default: return t
    }
  }

  useEffect(() => {
    if (!attemptId) return
    gradingApi.getStudentResult(Number(attemptId)).then(res => {
      if (res.data?.success) setData(res.data.data ?? null)
      else setError(res.data?.message || 'Không thể tải kết quả')
    }).catch(() => setError('Không thể tải kết quả. Kết quả có thể chưa được công bố.'))
      .finally(() => setLoading(false))
  }, [attemptId])

  const handlePrint = () => {
    window.print()
  }

  if (loading) return <div className="loading-center"><div className="spinner" /></div>
  if (error) return (
    <div style={{ maxWidth: 480, margin: '80px auto', textAlign: 'center' }}>
      <div className="card" style={{ padding: 40 }}>
        <span className="material-icons" style={{ fontSize: 48, color: 'var(--danger)', marginBottom: 12 }}>error_outline</span>
        <p style={{ color: 'var(--danger)', marginBottom: 20 }}>{error}</p>
        <button className="btn btn-secondary" onClick={() => navigate(-1)}>Quay lại</button>
      </div>
    </div>
  )
  if (!data) return null

  const totalPoints = data.questions.reduce((s, q) => s + q.points, 0)
  const hasGrades = data.totalScore !== null || data.questions.some(q => q.gradingResult)
  const earnedScore = data.totalScore ?? data.questions.reduce((s, q) => s + (q.gradingResult?.score ?? 0), 0)

  return (
    <>
      {/* Print-specific styles */}
      <style>{`
        @media print {
          body * { visibility: hidden !important; }
          .print-area, .print-area * { visibility: visible !important; }
          .print-area { position: absolute !important; left: 0; top: 0; width: 100%; padding: 20mm !important; }
          .no-print { display: none !important; }
          .print-area .card { box-shadow: none !important; border: 1px solid #ddd !important; break-inside: avoid; }
          @page { size: A4; margin: 15mm; }
        }
      `}</style>

      {/* Action bar - hidden on print */}
      <div className="no-print" style={{ display: 'flex', alignItems: 'center', gap: 12, marginBottom: 24 }}>
        <button className="btn btn-secondary btn-sm" onClick={() => navigate(-1)}>
          <span className="material-icons" style={{ fontSize: 16 }}>arrow_back</span> Quay lại
        </button>
        <div style={{ flex: 1 }} />
        <button className="btn btn-primary btn-sm" onClick={handlePrint}>
          <span className="material-icons" style={{ fontSize: 16 }}>print</span> In bài kiểm tra
        </button>
      </div>

      {/* Printable area */}
      <div className="print-area" style={{ maxWidth: 800, margin: '0 auto' }}>
        {/* Header */}
        <div style={{ textAlign: 'center', marginBottom: 32, paddingBottom: 20, borderBottom: '2px solid var(--border)' }}>
          <h1 style={{ fontSize: 22, margin: 0, fontWeight: 700 }}>BÀI KIỂM TRA</h1>
          <h2 style={{ fontSize: 18, margin: '8px 0', color: 'var(--primary)', fontWeight: 600 }}>{data.examTitle}</h2>
          <div style={{ display: 'flex', justifyContent: 'center', gap: 32, marginTop: 16, fontSize: 14 }}>
            <div>Học sinh: <strong>{data.studentName}</strong></div>
            {hasGrades
              ? <div>Tổng điểm: <strong style={{ color: 'var(--primary)', fontSize: 18 }}>{earnedScore}</strong>/{totalPoints}</div>
              : <div style={{ color: 'var(--text-muted)' }}>Chưa chấm điểm</div>
            }
          </div>
        </div>

        {/* Questions */}
        {data.questions.map((q, idx) => (
          <div key={q.questionId} style={{ marginBottom: 24, pageBreakInside: 'avoid' }}>
            <div style={{ display: 'flex', gap: 10, alignItems: 'flex-start', marginBottom: 12 }}>
              <div style={{
                minWidth: 32, height: 32, borderRadius: 8, display: 'flex', alignItems: 'center', justifyContent: 'center',
                background: q.gradingResult ? (q.gradingResult.score >= q.points ? '#22c55e' : q.gradingResult.score > 0 ? '#f97316' : '#ef4444') : '#6b7280',
                color: 'white', fontWeight: 700, fontSize: 14
              }}>{idx + 1}</div>
              <div style={{ flex: 1 }}>
                <div style={{ fontSize: 14, lineHeight: 1.7, fontWeight: 500 }}>{q.content}</div>
                <div style={{ fontSize: 12, color: 'var(--text-muted)', marginTop: 4 }}>
                  {questionTypeLabel(q.questionType)} · {q.points} điểm
                  {q.gradingResult && (
                    <span style={{ marginLeft: 12, fontWeight: 600, color: q.gradingResult.score >= q.points ? '#22c55e' : q.gradingResult.score > 0 ? '#f97316' : '#ef4444' }}>
                      → {q.gradingResult.score}/{q.points} điểm
                    </span>
                  )}
                </div>
              </div>
            </div>

            {/* MCQ options */}
            {q.options.length > 0 && (
              <div style={{ display: 'flex', flexDirection: 'column', gap: 6, marginLeft: 42 }}>
                {q.options.map((opt, oi) => {
                  let icon = 'radio_button_unchecked', iconColor = 'var(--text-muted)', bg = 'white', border = 'var(--border)'
                  if (opt.isCorrect && opt.wasSelected) { icon = 'check_circle'; iconColor = '#22c55e'; bg = 'rgba(34,197,94,0.06)'; border = '#22c55e' }
                  else if (opt.isCorrect) { icon = 'check_circle_outline'; iconColor = '#22c55e'; bg = 'rgba(34,197,94,0.04)'; border = '#86efac' }
                  else if (opt.wasSelected) { icon = 'cancel'; iconColor = '#ef4444'; bg = 'rgba(239,68,68,0.06)'; border = '#ef4444' }
                  return (
                    <div key={opt.id} style={{ display: 'flex', alignItems: 'center', gap: 8, padding: '8px 12px', borderRadius: 8, border: `1.5px solid ${border}`, background: bg, fontSize: 13 }}>
                      <span className="material-icons" style={{ fontSize: 18, color: iconColor }}>{icon}</span>
                      <span style={{ fontWeight: 600, color: 'var(--primary)', marginRight: 4 }}>{String.fromCharCode(65 + oi)}.</span>
                      {opt.content}
                    </div>
                  )
                })}
              </div>
            )}

            {/* Text / Essay */}
            {(q.textContent || q.essayContent) && (
              <div style={{ marginLeft: 42, marginTop: 8 }}>
                <div style={{ fontSize: 12, fontWeight: 600, color: 'var(--text-muted)', marginBottom: 4 }}>Câu trả lời:</div>
                <div style={{ padding: 12, background: 'var(--bg)', borderRadius: 8, fontSize: 13, lineHeight: 1.7, whiteSpace: 'pre-wrap', border: '1px solid var(--border)' }}>
                  {q.essayContent || q.textContent}
                </div>
              </div>
            )}

            {/* Canvas image + annotations */}
            {q.canvasImage && (
              <div style={{ marginLeft: 42, marginTop: 8 }}>
                <div style={{ fontSize: 12, fontWeight: 600, color: 'var(--text-muted)', marginBottom: 4 }}>Bài vẽ:</div>
                <div style={{ border: '1px solid var(--border)', borderRadius: 8, overflow: 'hidden', background: 'white', position: 'relative' }}>
                  <img src={q.canvasImage} alt="Bài vẽ" style={{ width: '100%', display: 'block' }} />
                  {q.gradingResult?.annotations && (
                    <img src={q.gradingResult.annotations} alt="Sửa bài" style={{ position: 'absolute', top: 0, left: 0, width: '100%', height: '100%', pointerEvents: 'none' }} />
                  )}
                </div>
              </div>
            )}

            {/* Teacher comment */}
            {q.gradingResult?.comment && (
              <div style={{ marginLeft: 42, marginTop: 8, padding: '10px 14px', background: 'rgba(59,130,246,0.06)', borderRadius: 8, border: '1px solid rgba(59,130,246,0.2)' }}>
                <span className="material-icons" style={{ fontSize: 14, color: 'var(--primary)', verticalAlign: 'text-top', marginRight: 6 }}>comment</span>
                <span style={{ fontSize: 13, color: 'var(--text-secondary)' }}>{q.gradingResult.comment}</span>
              </div>
            )}

            {/* No answer */}
            {!q.textContent && !q.essayContent && !q.canvasImage && !q.options.some(o => o.wasSelected) && (
              <div style={{ marginLeft: 42, marginTop: 8, padding: '8px 12px', background: 'rgba(249,115,22,0.06)', borderRadius: 8, fontSize: 13, color: '#f97316', fontStyle: 'italic' }}>
                Không trả lời
              </div>
            )}
          </div>
        ))}

        {/* Footer */}
        <div style={{ textAlign: 'center', marginTop: 40, paddingTop: 20, borderTop: '2px solid var(--border)', fontSize: 13, color: 'var(--text-muted)' }}>
          Bài kiểm tra · {data.examTitle} · {data.studentName}{hasGrades ? ` · Tổng điểm: ${earnedScore}/${totalPoints}` : ' · Chưa chấm điểm'}
        </div>
      </div>
    </>
  )
}
