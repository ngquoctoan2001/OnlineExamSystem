import { useState, useEffect, useCallback } from 'react'
import { useNavigate } from 'react-router-dom'
import { examAttemptsApi } from '../api/examAttempts'
import { examsApi } from '../api/exams'
import { scoresApi } from '../api/scores'
import { statisticsApi } from '../api/statistics'
import { useAuth } from '../contexts/AuthContext'
import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, Cell } from 'recharts'
import type { ExamStatisticResponse, ScoreDistributionResponse, StudentPerformanceResponse, ExamAttemptResponse } from '../types/api'

export default function ResultsPage() {
  const navigate = useNavigate()
  const { user } = useAuth()
  const isStudent = user?.role?.toUpperCase() === 'STUDENT'
  const [attempts, setAttempts] = useState<ExamAttemptResponse[]>([])
  const [total, setTotal] = useState(0)
  const [page, setPage] = useState(1)
  const [pageSize] = useState(20)
  const [loading, setLoading] = useState(true)
  const [exams, setExams] = useState<{ id: number; title: string }[]>([])
  const [filterExam, setFilterExam] = useState('')
  const [filterStatus, setFilterStatus] = useState('')
  const [chartData, setChartData] = useState<{ name: string; passed: number; failed: number }[]>([])
  const [examStats, setExamStats] = useState<ExamStatisticResponse | null>(null)
  const [distribution, setDistribution] = useState<ScoreDistributionResponse | null>(null)
  const [statsLoading, setStatsLoading] = useState(false)
  const [studentPerf, setStudentPerf] = useState<StudentPerformanceResponse | null>(null)
  const [rankings, setRankings] = useState<{ rank: number; studentId: number; studentName: string; score: number | null; submittedAt: string | null }[]>([])
  const [showRanking, setShowRanking] = useState(false)
  const [exportMsg, setExportMsg] = useState<{ type: 'success' | 'error'; text: string } | null>(null)
  const fetchAttempts = useCallback(async () => {
    setLoading(true)
    try {
      let res
      if (isStudent) {
        const studentId = user?.studentId || user?.id
        res = await examAttemptsApi.getStudentAttempts(studentId!)
      } else if (filterExam) {
        res = await examAttemptsApi.getExamAttempts(Number(filterExam), page, pageSize)
      } else {
        res = await examAttemptsApi.getAll(page, pageSize)
      }
      const data = res.data?.data
      let items: ExamAttemptResponse[]
      if (Array.isArray(data)) {
        items = data; setTotal(data.length)
      } else if (Array.isArray(data?.items)) {
        items = data.items; setTotal(data.totalCount || data.items.length)
      } else {
        items = []; setTotal(0)
      }
      // Client-side status filter
      if (filterStatus) {
        items = items.filter(a => a.status === filterStatus)
        setTotal(items.length)
      }
      setAttempts(items)
    } catch { setAttempts([]); setTotal(0) }
    finally { setLoading(false) }
  }, [page, pageSize, filterExam, filterStatus, isStudent, user])

  useEffect(() => { fetchAttempts() }, [fetchAttempts])

  // Fetch student performance summary
  useEffect(() => {
    if (!isStudent) return
    const studentId = user?.studentId || user?.id
    if (!studentId) return
    statisticsApi.getStudentPerformance(studentId).then(res => {
      setStudentPerf(res.data?.data ?? null)
    }).catch(() => {})
  }, [isStudent, user])

  useEffect(() => {
    examsApi.getAll(1, 100).then(res => {
      const rows = res.data?.data?.items || res.data?.data || []
      setExams(Array.isArray(rows) ? rows : [])
    }).catch(() => setExams([]))
  }, [])

  // Build chart from current data
  useEffect(() => {
    if (attempts.length === 0) { setChartData([]); return }
    const examMap: Record<string, { passed: number; failed: number }> = {}
    attempts.forEach(a => {
      const key = a.examTitle || 'Không xác định'
      if (!examMap[key]) examMap[key] = { passed: 0, failed: 0 }
      if (a.isPassed === true) examMap[key].passed++
      else if (a.isPassed === false) examMap[key].failed++
    })
    setChartData(
      Object.entries(examMap)
        .slice(0, 8)
        .map(([name, v]) => ({ name: name.length > 20 ? name.slice(0, 20) + '…' : name, ...v }))
    )
  }, [attempts])

  // Fetch exam statistics + score distribution + ranking when a specific exam is selected
  useEffect(() => {
    if (!filterExam) {
      setExamStats(null)
      setDistribution(null)
      setRankings([])
      return
    }
    const examId = Number(filterExam)
    const fetchStats = async () => {
      setStatsLoading(true)
      try {
        // Calculate first, then fetch
        await statisticsApi.calculateExamStats(examId).catch(() => {})
        const [statsRes, distRes, rankRes] = await Promise.allSettled([
          statisticsApi.getExamStats(examId),
          statisticsApi.getScoreDistribution(examId),
          scoresApi.getExamRanking(examId),
        ])
        if (statsRes.status === 'fulfilled') setExamStats(statsRes.value.data?.data ?? null)
        if (distRes.status === 'fulfilled') setDistribution(distRes.value.data?.data ?? null)
        if (rankRes.status === 'fulfilled') setRankings((rankRes.value.data?.data?.rankings || []) as unknown as typeof rankings)
        else setRankings([])
      } catch { /* optional */ }
      finally { setStatsLoading(false) }
    }
    fetchStats()
  }, [filterExam])

  const statusLabel = (s: string) => {
    if (s === 'COMPLETED' || s === 'SUBMITTED') return <span className="badge badge-green">Hoàn thành</span>
    if (s === 'IN_PROGRESS') return <span className="badge badge-blue">Đang thi</span>
    if (s === 'GRADED') return <span className="badge badge-green">Đã chấm</span>
    if (s === 'TIMEOUT') return <span className="badge badge-red">Hết giờ</span>
    return <span className="badge badge-gray">{s}</span>
  }

  const passedBadge = (isPassed?: boolean, status?: string) => {
    // If not yet graded (submitted but status not GRADED), show "—" instead of default result
    if (status === 'SUBMITTED' || isPassed == null) return <span className="badge badge-gray">—</span>
    return isPassed
      ? <span className="badge badge-green">Đạt</span>
      : <span className="badge badge-red">Không đạt</span>
  }

  const fmtDate = (d: string | null) => d ? new Date(d).toLocaleString('vi-VN') : '—'

  const totalPages = Math.ceil(total / pageSize)
  const passed = attempts.filter(a => a.isPassed === true).length
  const failed = attempts.filter(a => a.isPassed === false).length
  const gradedCount = attempts.filter(a => a.isPassed != null).length
  const passRate = gradedCount > 0 ? Math.round((passed / gradedCount) * 100) : 0

  return (
    <div>
      {/* Student performance summary */}
      {isStudent && studentPerf && (
        <div className="card" style={{ marginBottom: 24 }}>
          <div className="card-header">
            <span className="card-title">Tổng quan thành tích của bạn</span>
          </div>
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(160px, 1fr))', gap: 12, padding: '0 16px 16px' }}>
            <div style={{ textAlign: 'center', padding: 12, borderRadius: 8, background: 'rgba(19,127,236,0.06)' }}>
              <div style={{ fontSize: 24, fontWeight: 700, color: 'var(--primary)' }}>{studentPerf.totalAttempts}</div>
              <div style={{ fontSize: 12, color: 'var(--text-muted)' }}>Tổng lượt thi</div>
            </div>
            <div style={{ textAlign: 'center', padding: 12, borderRadius: 8, background: 'rgba(34,197,94,0.06)' }}>
              <div style={{ fontSize: 24, fontWeight: 700, color: '#22c55e' }}>{studentPerf.averageScore.toFixed(2)}</div>
              <div style={{ fontSize: 12, color: 'var(--text-muted)' }}>Điểm trung bình</div>
            </div>
          </div>
        </div>
      )}

      {/* Summary cards */}
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(180px, 1fr))', gap: 16, marginBottom: 24 }}>
        <div className="stat-card">
          <div className="stat-icon" style={{ background: 'rgba(19,127,236,0.1)' }}>
            <span className="material-icons" style={{ color: 'var(--primary)' }}>quiz</span>
          </div>
          <div className="stat-value">{total}</div>
          <div className="stat-label">Tổng lượt thi</div>
        </div>
        <div className="stat-card">
          <div className="stat-icon" style={{ background: 'rgba(34,197,94,0.1)' }}>
            <span className="material-icons" style={{ color: '#22c55e' }}>check_circle</span>
          </div>
          <div className="stat-value">{passed}</div>
          <div className="stat-label">Đạt (trang này)</div>
        </div>
        <div className="stat-card">
          <div className="stat-icon" style={{ background: 'rgba(239,68,68,0.1)' }}>
            <span className="material-icons" style={{ color: '#ef4444' }}>cancel</span>
          </div>
          <div className="stat-value">{failed}</div>
          <div className="stat-label">Không đạt</div>
        </div>
        <div className="stat-card">
          <div className="stat-icon" style={{ background: 'rgba(249,115,22,0.1)' }}>
            <span className="material-icons" style={{ color: '#f97316' }}>percent</span>
          </div>
          <div className="stat-value">{passRate}%</div>
          <div className="stat-label">Tỷ lệ đạt</div>
        </div>
      </div>

      {/* Exam Statistics (when specific exam selected) */}
      {filterExam && (
        <div style={{ marginBottom: 24 }}>
          {statsLoading ? (
            <div className="card"><div className="loading-center"><div className="spinner" /></div></div>
          ) : examStats ? (
            <div className="card" style={{ marginBottom: 16 }}>
              <div className="card-header">
                <span className="card-title">Thống kê kỳ thi: {examStats.examTitle}</span>
                <span style={{ fontSize: 12, color: 'var(--text-muted)' }}>
                  Cập nhật: {new Date(examStats.calculatedAt).toLocaleString('vi-VN')}
                </span>
              </div>
              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(140px, 1fr))', gap: 12, padding: '0 16px 16px' }}>
                <div style={{ textAlign: 'center', padding: 12, borderRadius: 8, background: 'rgba(19,127,236,0.06)' }}>
                  <div style={{ fontSize: 22, fontWeight: 700, color: 'var(--primary)' }}>{examStats.totalAttempts}</div>
                  <div style={{ fontSize: 12, color: 'var(--text-muted)' }}>Lượt thi</div>
                </div>
                <div style={{ textAlign: 'center', padding: 12, borderRadius: 8, background: 'rgba(34,197,94,0.06)' }}>
                  <div style={{ fontSize: 22, fontWeight: 700, color: '#22c55e' }}>{(examStats.passRate * 100).toFixed(1)}%</div>
                  <div style={{ fontSize: 12, color: 'var(--text-muted)' }}>Tỷ lệ đạt</div>
                </div>
                <div style={{ textAlign: 'center', padding: 12, borderRadius: 8, background: 'rgba(249,115,22,0.06)' }}>
                  <div style={{ fontSize: 22, fontWeight: 700, color: '#f97316' }}>{examStats.averageScore.toFixed(2)}</div>
                  <div style={{ fontSize: 12, color: 'var(--text-muted)' }}>Điểm TB</div>
                </div>
                <div style={{ textAlign: 'center', padding: 12, borderRadius: 8, background: 'rgba(139,92,246,0.06)' }}>
                  <div style={{ fontSize: 22, fontWeight: 700, color: '#8b5cf6' }}>{examStats.maxScore.toFixed(2)}</div>
                  <div style={{ fontSize: 12, color: 'var(--text-muted)' }}>Điểm cao nhất</div>
                </div>
                <div style={{ textAlign: 'center', padding: 12, borderRadius: 8, background: 'rgba(239,68,68,0.06)' }}>
                  <div style={{ fontSize: 22, fontWeight: 700, color: '#ef4444' }}>{examStats.minScore.toFixed(2)}</div>
                  <div style={{ fontSize: 12, color: 'var(--text-muted)' }}>Điểm thấp nhất</div>
                </div>
                <div style={{ textAlign: 'center', padding: 12, borderRadius: 8, background: 'rgba(34,197,94,0.06)' }}>
                  <div style={{ fontSize: 22, fontWeight: 700, color: '#22c55e' }}>{examStats.passCount}</div>
                  <div style={{ fontSize: 12, color: 'var(--text-muted)' }}>Đạt / {examStats.failCount} Không đạt</div>
                </div>
              </div>
            </div>
          ) : null}

          {/* Score Distribution Chart */}
          {distribution && distribution.buckets?.length > 0 && (
            <div className="card">
              <div className="card-header">
                <span className="card-title">Phân bố điểm</span>
              </div>
              <ResponsiveContainer width="100%" height={240}>
                <BarChart data={distribution.buckets} margin={{ top: 8, right: 16, left: 0, bottom: 8 }}>
                  <CartesianGrid strokeDasharray="3 3" stroke="#f0f0f0" />
                  <XAxis dataKey="label" tick={{ fontSize: 12 }} />
                  <YAxis tick={{ fontSize: 12 }} allowDecimals={false} />
                  <Tooltip formatter={(v: number) => [`${v} thí sinh`, 'Số lượng']} />
                  <Bar dataKey="count" name="Số thí sinh" fill="#137fec" radius={[4, 4, 0, 0]}>
                    {distribution.buckets.map((_, i) => (
                      <Cell key={i} fill={['#ef4444', '#f97316', '#eab308', '#22c55e', '#137fec'][Math.min(i, 4)]} />
                    ))}
                  </Bar>
                </BarChart>
              </ResponsiveContainer>
            </div>
          )}

          {/* Ranking / Leaderboard */}
          {rankings.length > 0 && (
            <div className="card" style={{ marginTop: 16 }}>
              <div className="card-header">
                <span className="card-title" style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                  <span className="material-icons" style={{ fontSize: 20, color: '#f59e0b' }}>emoji_events</span>
                  Bảng xếp hạng
                </span>
                <button className="btn btn-secondary btn-sm" onClick={() => setShowRanking(!showRanking)}>
                  {showRanking ? 'Thu gọn' : `Xem tất cả (${rankings.length})`}
                </button>
              </div>
              <div className="table-wrap">
                <table>
                  <thead>
                    <tr><th style={{ width: 60 }}>Hạng</th><th>Học sinh</th><th>Điểm</th><th>Thời gian nộp</th></tr>
                  </thead>
                  <tbody>
                    {(showRanking ? rankings : rankings.slice(0, 10)).map(r => (
                      <tr key={r.studentId}>
                        <td>
                          {r.rank <= 3 ? (
                            <span style={{
                              display: 'inline-flex', alignItems: 'center', justifyContent: 'center',
                              width: 28, height: 28, borderRadius: '50%', fontWeight: 700, fontSize: 13,
                              background: r.rank === 1 ? '#fef3c7' : r.rank === 2 ? '#f3f4f6' : '#fef2f2',
                              color: r.rank === 1 ? '#d97706' : r.rank === 2 ? '#6b7280' : '#b45309'
                            }}>
                              {r.rank === 1 ? '🥇' : r.rank === 2 ? '🥈' : '🥉'}
                            </span>
                          ) : (
                            <span style={{ fontWeight: 600, color: 'var(--text-muted)' }}>{r.rank}</span>
                          )}
                        </td>
                        <td style={{ fontWeight: 500 }}>{r.studentName}</td>
                        <td>
                          <span style={{ fontWeight: 700, color: 'var(--primary)' }}>
                            {r.score != null ? r.score.toFixed(2) : '—'}
                          </span>
                        </td>
                        <td style={{ fontSize: 12, color: 'var(--text-muted)' }}>
                          {r.submittedAt ? new Date(r.submittedAt).toLocaleString('vi-VN') : '—'}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          )}
        </div>
      )}

      {/* Chart */}
      {chartData.length > 0 && (
        <div className="card" style={{ marginBottom: 24 }}>
          <div className="card-header">
            <span className="card-title">Kết quả theo kỳ thi (trang hiện tại)</span>
          </div>
          <ResponsiveContainer width="100%" height={240}>
            <BarChart data={chartData} margin={{ top: 8, right: 16, left: 0, bottom: 40 }}>
              <CartesianGrid strokeDasharray="3 3" stroke="#f0f0f0" />
              <XAxis dataKey="name" tick={{ fontSize: 11 }} angle={-20} textAnchor="end" />
              <YAxis tick={{ fontSize: 12 }} allowDecimals={false} />
              <Tooltip />
              <Bar dataKey="passed" name="Đạt" fill="#22c55e" radius={[4, 4, 0, 0]}>
                {chartData.map((_, i) => <Cell key={i} fill="#22c55e" />)}
              </Bar>
              <Bar dataKey="failed" name="Không đạt" fill="#ef4444" radius={[4, 4, 0, 0]}>
                {chartData.map((_, i) => <Cell key={i} fill="#ef4444" />)}
              </Bar>
            </BarChart>
          </ResponsiveContainer>
        </div>
      )}

      {/* Filters + Table */}
      <div className="card">
        <div className="card-header" style={{ flexWrap: 'wrap', gap: 12 }}>
          <span className="card-title">Lịch sử thi</span>
          <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap', alignItems: 'center' }}>
            <select
              className="form-control"
              style={{ width: 220, height: 36, fontSize: 13 }}
              value={filterExam}
              onChange={e => { setFilterExam(e.target.value); setPage(1) }}
            >
              <option value="">Tất cả kỳ thi</option>
              {exams.map(e => <option key={e.id} value={e.id}>{e.title}</option>)}
            </select>
            <select
              className="form-control"
              style={{ width: 160, height: 36, fontSize: 13 }}
              value={filterStatus}
              onChange={e => { setFilterStatus(e.target.value); setPage(1) }}
            >
              <option value="">Tất cả trạng thái</option>
              <option value="COMPLETED">Hoàn thành</option>
              <option value="SUBMITTED">Đã nộp</option>
              <option value="GRADED">Đã chấm</option>
              <option value="IN_PROGRESS">Đang thi</option>
              <option value="TIMEOUT">Hết giờ</option>
            </select>
            {!isStudent && (
              <button
                className="btn btn-secondary btn-sm"
                onClick={async () => {
                  setExportMsg(null)
                  if (!filterExam) { setExportMsg({ type: 'error', text: 'Vui lòng chọn kỳ thi trước khi xuất báo cáo' }); return }
                  try {
                    const classRes = await examsApi.getClasses(Number(filterExam))
                    const classes = classRes.data?.data || []
                    if (classes.length === 0) { setExportMsg({ type: 'error', text: 'Không tìm thấy lớp nào cho kỳ thi này' }); return }
                    const classId = (classes[0] as { classId?: number; id?: number }).classId || (classes[0] as { id: number }).id
                    const res = await statisticsApi.exportClassResults(classId!, Number(filterExam))
                    const url = window.URL.createObjectURL(new Blob([res.data]))
                    const a = document.createElement('a'); a.href = url; a.download = `results_exam_${filterExam}.xlsx`; a.click()
                    window.URL.revokeObjectURL(url)
                    setExportMsg({ type: 'success', text: 'Đã xuất báo cáo thành công' })
                  } catch { setExportMsg({ type: 'error', text: 'Lỗi khi xuất báo cáo' }) }
                }}
                title="Xuất kết quả ra Excel"
              >
                <span className="material-icons" style={{ fontSize: 16 }}>download</span> Xuất Excel
              </button>
            )}
          </div>
        </div>

        {exportMsg && (
          <div style={{ padding: '10px 16px', margin: '0 16px 12px', borderRadius: 8, fontSize: 13,
            background: exportMsg.type === 'success' ? 'rgba(34,197,94,0.1)' : 'rgba(239,68,68,0.1)',
            color: exportMsg.type === 'success' ? '#16a34a' : '#dc2626',
            display: 'flex', alignItems: 'center', gap: 8
          }}>
            <span className="material-icons" style={{ fontSize: 16 }}>{exportMsg.type === 'success' ? 'check_circle' : 'error'}</span>
            {exportMsg.text}
          </div>
        )}

        {loading ? (
          <div className="loading-center"><div className="spinner" /></div>
        ) : attempts.length === 0 ? (
          <div className="empty-state">
            <span className="material-icons">assessment</span>
            <p>Chưa có kết quả thi nào</p>
          </div>
        ) : (
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>#</th>
                  <th>Học sinh</th>
                  <th>Kỳ thi</th>
                  <th>Điểm</th>
                  <th>Kết quả</th>
                  <th>Trạng thái</th>
                  <th>Thời gian làm</th>
                  <th>Bắt đầu</th>
                  <th style={{ width: 100 }}>Hành động</th>
                </tr>
              </thead>
              <tbody>
                {attempts.map((a, i) => (
                  <tr key={a.id}>
                    <td style={{ color: 'var(--text-muted)', fontSize: 12 }}>{(page - 1) * pageSize + i + 1}</td>
                    <td>
                      <div style={{ fontWeight: 500 }}>{a.studentName}</div>
                      <div style={{ fontSize: 12, color: 'var(--text-muted)' }}>{a.studentCode}</div>
                    </td>
                    <td style={{ maxWidth: 200 }}>
                      <div style={{ fontSize: 13, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{a.examTitle}</div>
                    </td>
                    <td>
                      {a.score != null && a.totalPoints
                        ? <span style={{ fontWeight: 600 }}>{a.score}<span style={{ color: 'var(--text-muted)', fontWeight: 400 }}>/{a.totalPoints}</span></span>
                        : '—'}
                    </td>
                    <td>{passedBadge(a.isPassed, a.status)}</td>
                    <td>{statusLabel(a.status)}</td>
                    <td style={{ fontSize: 13 }}>{(() => {
                      if (a.startTime && a.endTime) {
                        const mins = Math.round((new Date(a.endTime).getTime() - new Date(a.startTime).getTime()) / 60000)
                        return `${mins} phút`
                      }
                      return '—'
                    })()}</td>
                    <td style={{ fontSize: 12, color: 'var(--text-muted)', whiteSpace: 'nowrap' }}>{fmtDate(a.startTime)}</td>
                    <td>
                      {(a.status === 'COMPLETED' || a.status === 'SUBMITTED' || a.status === 'GRADED' || a.status === 'PUBLISHED') && (
                        <div style={{ display: 'flex', gap: 4 }}>
                          <button className="btn btn-secondary btn-sm" title="Xem / In" onClick={() => navigate(`/review/${a.id}`)}>
                            <span className="material-icons" style={{ fontSize: 14 }}>visibility</span>
                          </button>
                          {!isStudent && (
                            <button className="btn btn-primary btn-sm" title="Chấm bài" onClick={() => navigate(`/grading/${a.id}`)}>
                              <span className="material-icons" style={{ fontSize: 14 }}>rate_review</span>
                            </button>
                          )}
                        </div>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        {/* Pagination */}
        {totalPages > 1 && (
          <div className="pagination">
            <button className="btn btn-secondary btn-sm" disabled={page === 1} onClick={() => setPage(p => p - 1)}>
              <span className="material-icons" style={{ fontSize: 16 }}>chevron_left</span>
            </button>
            {Array.from({ length: Math.min(totalPages, 7) }, (_, i) => {
              const p = i + 1
              return (
                <button
                  key={p}
                  className={`btn btn-sm ${p === page ? 'btn-primary' : 'btn-secondary'}`}
                  onClick={() => setPage(p)}
                >
                  {p}
                </button>
              )
            })}
            <button className="btn btn-secondary btn-sm" disabled={page === totalPages} onClick={() => setPage(p => p + 1)}>
              <span className="material-icons" style={{ fontSize: 16 }}>chevron_right</span>
            </button>
            <span style={{ fontSize: 13, color: 'var(--text-muted)', marginLeft: 8 }}>
              {total} kết quả
            </span>
          </div>
        )}
      </div>
    </div>
  )
}
