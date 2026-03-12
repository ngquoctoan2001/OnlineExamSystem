import { useState, useEffect, useCallback } from 'react'
import { useNavigate } from 'react-router-dom'
import { examsApi } from '../api/exams'
import { gradingApi } from '../api/grading'
import type { ExamResponse, PendingGradingAttemptResponse } from '../types/api'

export default function GradingPage() {
  const navigate = useNavigate()
  const [exams, setExams] = useState<ExamResponse[]>([])
  const [selectedExam, setSelectedExam] = useState<number | null>(null)
  const [pending, setPending] = useState<PendingGradingAttemptResponse[]>([])
  const [loading, setLoading] = useState(true)
  const [loadingPending, setLoadingPending] = useState(false)
  const [statusFilter, setStatusFilter] = useState<string>('')
  const [searchTerm, setSearchTerm] = useState('')

  useEffect(() => {
    examsApi.getAll(1, 200).then(res => {
      const d = res.data?.data
      const items = d?.items || (Array.isArray(d) ? d : [])
      setExams(Array.isArray(items) ? items : [])
    }).catch(() => setExams([])).finally(() => setLoading(false))
  }, [])

  const fetchPending = useCallback(async (examId: number) => {
    setLoadingPending(true)
    try {
      const res = await gradingApi.getPending(examId)
      setPending(res.data?.data || [])
    } catch { setPending([]) }
    finally { setLoadingPending(false) }
  }, [])

  useEffect(() => {
    if (selectedExam) fetchPending(selectedExam)
    else setPending([])
  }, [selectedExam, fetchPending])

  const fmtDate = (d: string) => new Date(d).toLocaleString('vi-VN')

  // Filter exams by status and search term
  const filteredExams = exams.filter(e => {
    if (statusFilter && e.status !== statusFilter) return false
    if (searchTerm && !e.title.toLowerCase().includes(searchTerm.toLowerCase()) && !e.subjectName?.toLowerCase().includes(searchTerm.toLowerCase())) return false
    return true
  })

  if (loading) return <div className="loading-center"><div className="spinner" /></div>

  return (
    <div>
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 24 }}>
        <div>
          <h2 style={{ margin: 0 }}>Chấm bài thi</h2>
          <p style={{ color: 'var(--text-muted)', fontSize: 13, margin: '4px 0 0' }}>Chấm điểm bài tự luận, vẽ hình và câu hỏi cần chấm thủ công</p>
        </div>
      </div>

      {/* Exam selector */}
      <div className="card" style={{ marginBottom: 24 }}>
        <div className="card-header">
          <span className="card-title">Chọn kỳ thi</span>
        </div>
        <div style={{ padding: 16 }}>
          <div style={{ display: 'flex', gap: 12, marginBottom: 16, flexWrap: 'wrap' }}>
            <input
              className="form-control"
              style={{ flex: 1, minWidth: 200, height: 36, fontSize: 13 }}
              placeholder="Tìm kỳ thi theo tên hoặc môn..."
              value={searchTerm}
              onChange={e => setSearchTerm(e.target.value)}
            />
            <select className="form-control" style={{ minWidth: 180, width: 'auto', height: 36, fontSize: 13 }} value={statusFilter} onChange={e => setStatusFilter(e.target.value)}>
              <option value="">Tất cả trạng thái</option>
              <option value="ACTIVE">Đang mở</option>
              <option value="CLOSED">Đã đóng</option>
              <option value="DRAFT">Nháp</option>
            </select>
          </div>
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(280px, 1fr))', gap: 12 }}>
            {filteredExams.map(e => (
              <button
                key={e.id}
                onClick={() => setSelectedExam(e.id)}
                style={{
                  padding: '14px 18px', borderRadius: 10, border: `2px solid ${selectedExam === e.id ? 'var(--primary)' : 'var(--border)'}`,
                  background: selectedExam === e.id ? 'var(--primary-light)' : 'white', cursor: 'pointer', textAlign: 'left'
                }}
              >
                <div style={{ fontWeight: 600, fontSize: 14 }}>{e.title}</div>
                <div style={{ fontSize: 12, color: 'var(--text-muted)', marginTop: 4 }}>
                  {e.subjectName} ·{' '}
                  <span className={`badge badge-${e.status === 'ACTIVE' ? 'green' : e.status === 'CLOSED' ? 'red' : 'gray'}`} style={{ fontSize: 10 }}>
                    {e.status}
                  </span>
                </div>
              </button>
            ))}
          </div>
          {filteredExams.length === 0 && <div className="empty-state"><span className="material-icons">school</span><p>{exams.length === 0 ? 'Chưa có kỳ thi nào' : 'Không tìm thấy kỳ thi phù hợp'}</p></div>}
        </div>
      </div>

      {/* Pending attempts */}
      {selectedExam && (
        <div className="card">
          <div className="card-header">
            <span className="card-title">Bài thi cần chấm</span>
            <button className="btn btn-secondary btn-sm" onClick={() => fetchPending(selectedExam)}>
              <span className="material-icons" style={{ fontSize: 14 }}>refresh</span> Làm mới
            </button>
          </div>

          {loadingPending ? (
            <div className="loading-center"><div className="spinner" /></div>
          ) : pending.length === 0 ? (
            <div className="empty-state">
              <span className="material-icons">check_circle</span>
              <p>Không có bài thi nào cần chấm</p>
            </div>
          ) : (
            <div className="table-wrap">
              <table>
                <thead>
                  <tr>
                    <th>#</th>
                    <th>Học sinh</th>
                    <th>Nộp lúc</th>
                    <th>Trạng thái</th>
                    <th style={{ width: 120 }}>Hành động</th>
                  </tr>
                </thead>
                <tbody>
                  {pending.map((p, i) => (
                    <tr key={p.attemptId}>
                      <td style={{ color: 'var(--text-muted)', fontSize: 12 }}>{i + 1}</td>
                      <td style={{ fontWeight: 500 }}>{p.studentName}</td>
                      <td style={{ fontSize: 13, color: 'var(--text-muted)' }}>{fmtDate(p.submittedAt)}</td>
                      <td>
                        {p.hasUngraded
                          ? <span className="badge badge-orange">Chưa chấm xong</span>
                          : <span className="badge badge-green">Đã chấm</span>}
                      </td>
                      <td>
                        <button className="btn btn-primary btn-sm"
                          onClick={() => navigate(`/grading/${p.attemptId}`)}>
                          <span className="material-icons" style={{ fontSize: 14 }}>rate_review</span> Chấm
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      )}
    </div>
  )
}
