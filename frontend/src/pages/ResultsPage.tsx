import { useState, useEffect, useCallback } from 'react'
import apiClient from '../api/client'
import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, Cell } from 'recharts'

interface AttemptRow {
  id: number
  studentName: string
  studentCode: string
  examTitle: string
  score: number | null
  totalPoints: number
  isPassed: boolean | null
  status: string
  startedAt: string
  completedAt: string | null
  timeTakenMinutes: number | null
}

interface ExamOption { id: number; title: string }

export default function ResultsPage() {
  const [attempts, setAttempts] = useState<AttemptRow[]>([])
  const [total, setTotal] = useState(0)
  const [page, setPage] = useState(1)
  const [pageSize] = useState(20)
  const [loading, setLoading] = useState(true)
  const [exams, setExams] = useState<ExamOption[]>([])
  const [filterExam, setFilterExam] = useState('')
  const [filterStatus, setFilterStatus] = useState('')
  const [chartData, setChartData] = useState<{ name: string; passed: number; failed: number }[]>([])

  const fetchAttempts = useCallback(async () => {
    setLoading(true)
    try {
      const params: Record<string, string | number> = { page, pageSize }
      if (filterExam) params.examId = filterExam
      if (filterStatus) params.status = filterStatus
      const res = await apiClient.get('/exam-attempts', { params })
      const data = res.data?.data
      if (Array.isArray(data)) {
        setAttempts(data); setTotal(res.data?.total || data.length)
      } else if (Array.isArray(data?.items)) {
        setAttempts(data.items); setTotal(data.totalCount || data.items.length)
      }
    } catch { setAttempts([]); setTotal(0) }
    finally { setLoading(false) }
  }, [page, pageSize, filterExam, filterStatus])

  useEffect(() => { fetchAttempts() }, [fetchAttempts])

  useEffect(() => {
    apiClient.get('/exams', { params: { page: 1, pageSize: 100 } }).then(res => {
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

  const statusLabel = (s: string) => {
    if (s === 'COMPLETED') return <span className="badge badge-green">Hoàn thành</span>
    if (s === 'IN_PROGRESS') return <span className="badge badge-blue">Đang thi</span>
    if (s === 'TIMEOUT') return <span className="badge badge-red">Hết giờ</span>
    return <span className="badge badge-gray">{s}</span>
  }

  const passedBadge = (isPassed: boolean | null) => {
    if (isPassed === null) return <span className="badge badge-gray">—</span>
    return isPassed
      ? <span className="badge badge-green">Đạt</span>
      : <span className="badge badge-red">Không đạt</span>
  }

  const fmtDate = (d: string | null) => d ? new Date(d).toLocaleString('vi-VN') : '—'

  const totalPages = Math.ceil(total / pageSize)
  const passed = attempts.filter(a => a.isPassed === true).length
  const failed = attempts.filter(a => a.isPassed === false).length
  const passRate = attempts.length > 0 ? Math.round((passed / attempts.filter(a => a.isPassed !== null).length) * 100) : 0

  return (
    <div>
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
          <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap' }}>
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
              <option value="IN_PROGRESS">Đang thi</option>
              <option value="TIMEOUT">Hết giờ</option>
            </select>
          </div>
        </div>

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
                      {a.score !== null && a.totalPoints
                        ? <span style={{ fontWeight: 600 }}>{a.score}<span style={{ color: 'var(--text-muted)', fontWeight: 400 }}>/{a.totalPoints}</span></span>
                        : '—'}
                    </td>
                    <td>{passedBadge(a.isPassed)}</td>
                    <td>{statusLabel(a.status)}</td>
                    <td style={{ fontSize: 13 }}>{a.timeTakenMinutes != null ? `${a.timeTakenMinutes} phút` : '—'}</td>
                    <td style={{ fontSize: 12, color: 'var(--text-muted)', whiteSpace: 'nowrap' }}>{fmtDate(a.startedAt)}</td>
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
