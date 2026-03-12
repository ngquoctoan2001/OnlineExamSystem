import { useState, useEffect } from 'react'
import { examsApi } from '../api/exams'
import { statisticsApi } from '../api/statistics'
import { scoresApi } from '../api/scores'
import { classesApi } from '../api/classes'
import { useAuth } from '../contexts/AuthContext'
import { teachersApi } from '../api/teachers'
import {
  BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, Cell,
  PieChart, Pie, Legend,
} from 'recharts'
import type {
  ExamResponse, ExamStatisticResponse, ScoreDistributionResponse,
  ClassResponse, ClassResultsResponse,
} from '../types/api'

const COLORS = ['#ef4444', '#f97316', '#eab308', '#22c55e', '#137fec']

export default function ReportsPage() {
  const { user } = useAuth()
  const role = user?.role?.toUpperCase() || ''

  return (
    <div>
      <div style={{ marginBottom: 24 }}>
        <h2 style={{ marginBottom: 4 }}>
          <span className="material-icons" style={{ fontSize: 28, verticalAlign: 'middle', marginRight: 8, color: 'var(--primary)' }}>analytics</span>
          Báo cáo & Thống kê
        </h2>
        <p style={{ fontSize: 13, color: 'var(--text-muted)' }}>Phân tích chi tiết kết quả thi và hiệu suất</p>
      </div>

      <ExamStatisticsSection role={role} userId={user?.id} />
      {(role === 'ADMIN' || role === 'TEACHER') && <ClassResultsSection />}
    </div>
  )
}

/* ═══════════════════════════════════════════════════════════════
   EXAM STATISTICS SECTION
   ═══════════════════════════════════════════════════════════════ */
function ExamStatisticsSection({ role, userId }: { role: string; userId?: number }) {
  const [exams, setExams] = useState<ExamResponse[]>([])
  const [selectedExamId, setSelectedExamId] = useState<number | null>(null)
  const [stats, setStats] = useState<ExamStatisticResponse | null>(null)
  const [distribution, setDistribution] = useState<ScoreDistributionResponse | null>(null)
  const [loading, setLoading] = useState(false)
  const [examsLoading, setExamsLoading] = useState(true)
  const [exportMsg, setExportMsg] = useState<{ type: 'success' | 'error'; text: string } | null>(null)

  useEffect(() => {
    const load = async () => {
      setExamsLoading(true)
      try {
        if (role === 'TEACHER') {
          try {
            const meRes = await teachersApi.getMe()
            const teacherId = meRes.data?.data?.id
            if (teacherId) {
              const r = await examsApi.getByTeacher(teacherId)
              setExams(r.data?.data || [])
              return
            }
          } catch { /* fallback */ }
        }
        const r = await examsApi.getAll(1, 200)
        const d = r.data?.data
        setExams(Array.isArray(d) ? d : (d?.items || []))
      } catch { setExams([]) }
      finally { setExamsLoading(false) }
    }
    load()
  }, [role])

  useEffect(() => {
    if (!selectedExamId) { setStats(null); setDistribution(null); return }
    setLoading(true)
    Promise.allSettled([
      statisticsApi.calculateExamStats(selectedExamId).then(() => statisticsApi.getExamStats(selectedExamId)),
      statisticsApi.getScoreDistribution(selectedExamId),
    ]).then(([statsRes, distRes]) => {
      if (statsRes.status === 'fulfilled') setStats(statsRes.value.data?.data || null)
      else {
        // Try scores endpoint as fallback
        scoresApi.getExamStatistics(selectedExamId)
          .then(r => setStats(r.data?.data || null))
          .catch(() => setStats(null))
      }
      if (distRes.status === 'fulfilled') setDistribution(distRes.value.data?.data || null)
      else setDistribution(null)
    }).finally(() => setLoading(false))
  }, [selectedExamId])

  const handleExport = async () => {
    if (!selectedExamId) return
    setExportMsg(null)
    try {
      // Export exam scores as Excel
      const res = await scoresApi.getExamScores(selectedExamId)
      const rows = res.data?.data || []
      if (rows.length === 0) {
        setExportMsg({ type: 'error', text: 'Không có dữ liệu để xuất' })
        return
      }
      // Build CSV
      const header = 'Học sinh,Điểm,Trạng thái,Thời gian nộp\n'
      const csv = header + rows.map(r => `"${r.studentName}",${r.score ?? ''},${r.status},${r.endTime || ''}`).join('\n')
      const blob = new Blob(["\uFEFF" + csv], { type: 'text/csv;charset=utf-8' })
      const url = window.URL.createObjectURL(blob)
      const a = document.createElement('a')
      a.href = url
      a.download = `exam_${selectedExamId}_results.csv`
      a.click()
      window.URL.revokeObjectURL(url)
      setExportMsg({ type: 'success', text: 'Đã tải file báo cáo!' })
    } catch {
      setExportMsg({ type: 'error', text: 'Không thể xuất báo cáo' })
    }
  }

  const passRate = stats ? (stats.totalAttempts > 0 ? stats.passRate : 0) : 0
  const pieData = stats ? [
    { name: 'Đạt', value: stats.passCount },
    { name: 'Chưa đạt', value: stats.failCount },
  ] : []

  return (
    <div className="card" style={{ marginBottom: 20 }}>
      <div className="card-header">
        <span className="card-title">
          <span className="material-icons" style={{ fontSize: 20, verticalAlign: 'middle', marginRight: 6 }}>bar_chart</span>
          Thống kê kỳ thi
        </span>
        {selectedExamId && (
          <button className="btn btn-secondary btn-sm" onClick={handleExport}>
            <span className="material-icons" style={{ fontSize: 16 }}>file_download</span>
            Xuất Excel
          </button>
        )}
      </div>

      {/* Exam selector */}
      <div style={{ marginBottom: 16 }}>
        <select
          className="form-input"
          style={{ maxWidth: 400 }}
          value={selectedExamId ?? ''}
          onChange={e => setSelectedExamId(e.target.value ? Number(e.target.value) : null)}
        >
          <option value="">-- Chọn kỳ thi --</option>
          {exams.map(ex => (
            <option key={ex.id} value={ex.id}>{ex.title} ({ex.subjectName || '—'})</option>
          ))}
        </select>
        {examsLoading && <span style={{ marginLeft: 8, fontSize: 13, color: 'var(--text-muted)' }}>Đang tải...</span>}
      </div>

      {exportMsg && (
        <div style={{
          padding: '8px 14px', borderRadius: 8, marginBottom: 12, fontSize: 13,
          background: exportMsg.type === 'success' ? 'rgba(34,197,94,0.08)' : 'rgba(239,68,68,0.08)',
          color: exportMsg.type === 'success' ? '#16a34a' : '#dc2626',
        }}>{exportMsg.text}</div>
      )}

      {!selectedExamId && (
        <div className="empty-state" style={{ padding: '40px 0' }}>
          <span className="material-icons" style={{ fontSize: 48, color: 'var(--text-muted)' }}>insert_chart_outlined</span>
          <p>Chọn một kỳ thi để xem thống kê chi tiết</p>
        </div>
      )}

      {selectedExamId && loading && (
        <div className="loading-center"><div className="spinner" /></div>
      )}

      {selectedExamId && !loading && stats && (
        <>
          {/* Stat cards */}
          <div className="stat-grid" style={{ marginBottom: 20 }}>
            <div className="stat-card">
              <div className="stat-icon blue"><span className="material-icons">people</span></div>
              <div className="stat-value">{stats.totalAttempts}</div>
              <div className="stat-label">Tổng lượt thi</div>
            </div>
            <div className="stat-card">
              <div className="stat-icon green"><span className="material-icons">trending_up</span></div>
              <div className="stat-value">{stats.averageScore.toFixed(1)}</div>
              <div className="stat-label">Điểm trung bình</div>
            </div>
            <div className="stat-card">
              <div className="stat-icon orange"><span className="material-icons">emoji_events</span></div>
              <div className="stat-value">{stats.maxScore.toFixed(1)}</div>
              <div className="stat-label">Điểm cao nhất</div>
            </div>
            <div className="stat-card">
              <div className="stat-icon" style={{ background: 'rgba(239,68,68,0.1)', color: '#ef4444' }}>
                <span className="material-icons">trending_down</span>
              </div>
              <div className="stat-value">{stats.minScore.toFixed(1)}</div>
              <div className="stat-label">Điểm thấp nhất</div>
            </div>
          </div>

          {/* Charts row */}
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16, marginBottom: 16 }}>
            {/* Pass rate pie */}
            <div style={{ background: 'var(--surface)', borderRadius: 12, padding: 16, border: '1px solid var(--border)' }}>
              <h4 style={{ fontSize: 14, marginBottom: 12, fontWeight: 600 }}>Tỷ lệ đạt/chưa đạt</h4>
              {stats.totalAttempts > 0 ? (
                <>
                  <ResponsiveContainer width="100%" height={200}>
                    <PieChart>
                      <Pie data={pieData} dataKey="value" nameKey="name"  cx="50%" cy="50%" outerRadius={70} label={({ name, percent }) => `${name}: ${(percent * 100).toFixed(0)}%`}>
                        <Cell fill="#22c55e" />
                        <Cell fill="#ef4444" />
                      </Pie>
                      <Tooltip />
                      <Legend />
                    </PieChart>
                  </ResponsiveContainer>
                  <div style={{ textAlign: 'center', marginTop: 8 }}>
                    <span style={{ fontSize: 24, fontWeight: 700, color: passRate >= 50 ? '#22c55e' : '#ef4444' }}>
                      {passRate.toFixed(1)}%
                    </span>
                    <div style={{ fontSize: 12, color: 'var(--text-muted)' }}>Tỷ lệ đạt</div>
                  </div>
                </>
              ) : (
                <div className="empty-state" style={{ padding: '30px 0' }}>
                  <p>Chưa có lượt thi</p>
                </div>
              )}
            </div>

            {/* Score distribution bar */}
            <div style={{ background: 'var(--surface)', borderRadius: 12, padding: 16, border: '1px solid var(--border)' }}>
              <h4 style={{ fontSize: 14, marginBottom: 12, fontWeight: 600 }}>Phân bổ điểm</h4>
              {distribution && distribution.buckets.length > 0 ? (
                <ResponsiveContainer width="100%" height={230}>
                  <BarChart data={distribution.buckets} margin={{ top: 4, right: 8, bottom: 0, left: -20 }}>
                    <CartesianGrid strokeDasharray="3 3" stroke="var(--border)" />
                    <XAxis dataKey="label" tick={{ fontSize: 12 }} />
                    <YAxis tick={{ fontSize: 12 }} allowDecimals={false} />
                    <Tooltip contentStyle={{ borderRadius: 8, border: '1px solid var(--border)' }} />
                    <Bar dataKey="count" radius={[4, 4, 0, 0]}>
                      {distribution.buckets.map((_, i) => (
                        <Cell key={i} fill={COLORS[i % COLORS.length]} />
                      ))}
                    </Bar>
                  </BarChart>
                </ResponsiveContainer>
              ) : (
                <div className="empty-state" style={{ padding: '30px 0' }}>
                  <p>Chưa có dữ liệu phân bổ điểm</p>
                </div>
              )}
            </div>
          </div>
        </>
      )}

      {selectedExamId && !loading && !stats && (
        <div className="empty-state" style={{ padding: '40px 0' }}>
          <span className="material-icons" style={{ fontSize: 48, color: 'var(--text-muted)' }}>info</span>
          <p>Chưa có thống kê cho kỳ thi này (chưa có lượt thi nào)</p>
        </div>
      )}
    </div>
  )
}

/* ═══════════════════════════════════════════════════════════════
   CLASS RESULTS SECTION
   ═══════════════════════════════════════════════════════════════ */
function ClassResultsSection() {
  const [classes, setClasses] = useState<ClassResponse[]>([])
  const [exams, setExams] = useState<ExamResponse[]>([])
  const [selectedClassId, setSelectedClassId] = useState<number | null>(null)
  const [selectedExamId, setSelectedExamId] = useState<number | null>(null)
  const [results, setResults] = useState<ClassResultsResponse | null>(null)
  const [loading, setLoading] = useState(false)
  const [initLoading, setInitLoading] = useState(true)
  const [exportMsg, setExportMsg] = useState<{ type: 'success' | 'error'; text: string } | null>(null)

  useEffect(() => {
    Promise.allSettled([
      classesApi.getAll(1, 200),
      examsApi.getAll(1, 200),
    ]).then(([clsRes, exRes]) => {
      if (clsRes.status === 'fulfilled') {
        const d = clsRes.value.data?.data
        setClasses(Array.isArray(d) ? d : (d?.classes || []))
      }
      if (exRes.status === 'fulfilled') {
        const d = exRes.value.data?.data
        setExams(Array.isArray(d) ? d : (d?.items || []))
      }
    }).finally(() => setInitLoading(false))
  }, [])

  useEffect(() => {
    if (!selectedClassId || !selectedExamId) { setResults(null); return }
    setLoading(true)
    statisticsApi.getClassResults(selectedClassId, selectedExamId)
      .then(r => setResults(r.data?.data || null))
      .catch(() => setResults(null))
      .finally(() => setLoading(false))
  }, [selectedClassId, selectedExamId])

  const handleExport = async () => {
    if (!selectedClassId || !selectedExamId) return
    setExportMsg(null)
    try {
      const res = await statisticsApi.exportClassResults(selectedClassId, selectedExamId)
      const url = window.URL.createObjectURL(new Blob([res.data]))
      const a = document.createElement('a')
      a.href = url
      a.download = `class_${selectedClassId}_exam_${selectedExamId}_results.xlsx`
      a.click()
      window.URL.revokeObjectURL(url)
      setExportMsg({ type: 'success', text: 'Đã tải file báo cáo lớp!' })
    } catch {
      setExportMsg({ type: 'error', text: 'Không thể xuất báo cáo' })
    }
  }

  const fmtDate = (d?: string | null) => d ? new Date(d).toLocaleString('vi-VN', { dateStyle: 'short', timeStyle: 'short' }) : '—'

  return (
    <div className="card">
      <div className="card-header">
        <span className="card-title">
          <span className="material-icons" style={{ fontSize: 20, verticalAlign: 'middle', marginRight: 6 }}>group_work</span>
          Kết quả theo lớp
        </span>
        {results && (
          <button className="btn btn-secondary btn-sm" onClick={handleExport}>
            <span className="material-icons" style={{ fontSize: 16 }}>file_download</span>
            Xuất Excel
          </button>
        )}
      </div>

      {/* Selectors */}
      <div style={{ display: 'flex', gap: 12, marginBottom: 16, flexWrap: 'wrap' }}>
        <select
          className="form-input"
          style={{ maxWidth: 280 }}
          value={selectedClassId ?? ''}
          onChange={e => setSelectedClassId(e.target.value ? Number(e.target.value) : null)}
        >
          <option value="">-- Chọn lớp --</option>
          {classes.map(c => (
            <option key={c.id} value={c.id}>{c.name}</option>
          ))}
        </select>
        <select
          className="form-input"
          style={{ maxWidth: 320 }}
          value={selectedExamId ?? ''}
          onChange={e => setSelectedExamId(e.target.value ? Number(e.target.value) : null)}
        >
          <option value="">-- Chọn kỳ thi --</option>
          {exams.map(ex => (
            <option key={ex.id} value={ex.id}>{ex.title}</option>
          ))}
        </select>
        {initLoading && <span style={{ fontSize: 13, color: 'var(--text-muted)', alignSelf: 'center' }}>Đang tải...</span>}
      </div>

      {exportMsg && (
        <div style={{
          padding: '8px 14px', borderRadius: 8, marginBottom: 12, fontSize: 13,
          background: exportMsg.type === 'success' ? 'rgba(34,197,94,0.08)' : 'rgba(239,68,68,0.08)',
          color: exportMsg.type === 'success' ? '#16a34a' : '#dc2626',
        }}>{exportMsg.text}</div>
      )}

      {(!selectedClassId || !selectedExamId) && (
        <div className="empty-state" style={{ padding: '40px 0' }}>
          <span className="material-icons" style={{ fontSize: 48, color: 'var(--text-muted)' }}>group_work</span>
          <p>Chọn lớp và kỳ thi để xem kết quả chi tiết</p>
        </div>
      )}

      {selectedClassId && selectedExamId && loading && (
        <div className="loading-center"><div className="spinner" /></div>
      )}

      {selectedClassId && selectedExamId && !loading && results && (
        <>
          {/* Summary cards */}
          <div className="stat-grid" style={{ marginBottom: 16 }}>
            <div className="stat-card">
              <div className="stat-icon blue"><span className="material-icons">group</span></div>
              <div className="stat-value">{results.totalStudents}</div>
              <div className="stat-label">Tổng học sinh</div>
            </div>
            <div className="stat-card">
              <div className="stat-icon green"><span className="material-icons">check_circle</span></div>
              <div className="stat-value">{results.attemptedCount}</div>
              <div className="stat-label">Đã thi</div>
            </div>
            <div className="stat-card">
              <div className="stat-icon orange"><span className="material-icons">trending_up</span></div>
              <div className="stat-value">{results.averageScore.toFixed(1)}</div>
              <div className="stat-label">Điểm trung bình</div>
            </div>
            <div className="stat-card">
              <div className="stat-icon purple"><span className="material-icons">percent</span></div>
              <div className="stat-value">
                {results.totalStudents > 0
                  ? ((results.attemptedCount / results.totalStudents) * 100).toFixed(0) + '%'
                  : '—'}
              </div>
              <div className="stat-label">Tỷ lệ tham gia</div>
            </div>
          </div>

          {/* Student results table */}
          {results.studentResults.length > 0 ? (
            <div className="table-wrap">
              <table>
                <thead>
                  <tr>
                    <th>#</th>
                    <th>Học sinh</th>
                    <th>Điểm</th>
                    <th>Trạng thái</th>
                    <th>Thời gian nộp</th>
                  </tr>
                </thead>
                <tbody>
                  {results.studentResults
                    .sort((a, b) => (b.score ?? 0) - (a.score ?? 0))
                    .map((sr, idx) => (
                    <tr key={sr.attemptId}>
                      <td style={{ fontWeight: 500, color: 'var(--text-muted)' }}>{idx + 1}</td>
                      <td style={{ fontWeight: 500 }}>{sr.studentName}</td>
                      <td>
                        {sr.score != null ? (
                          <span style={{
                            fontWeight: 600,
                            color: sr.score >= 5 ? '#16a34a' : '#dc2626',
                          }}>{sr.score.toFixed(1)}</span>
                        ) : '—'}
                      </td>
                      <td>
                        {sr.status === 'GRADED' || sr.status === 'PUBLISHED'
                          ? <span className="badge badge-green">Đã chấm</span>
                          : sr.status === 'SUBMITTED'
                            ? <span className="badge badge-blue">Đã nộp</span>
                            : <span className="badge badge-gray">{sr.status}</span>}
                      </td>
                      <td style={{ color: 'var(--text-muted)' }}>{fmtDate(sr.endTime)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          ) : (
            <div className="empty-state" style={{ padding: '30px 0' }}>
              <p>Chưa có học sinh nào thi</p>
            </div>
          )}
        </>
      )}

      {selectedClassId && selectedExamId && !loading && !results && (
        <div className="empty-state" style={{ padding: '40px 0' }}>
          <span className="material-icons" style={{ fontSize: 48, color: 'var(--text-muted)' }}>info</span>
          <p>Không có dữ liệu kết quả cho lớp và kỳ thi đã chọn</p>
        </div>
      )}
    </div>
  )
}
