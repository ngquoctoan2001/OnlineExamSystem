import { useEffect, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import {
  AreaChart, Area, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer,
  LineChart, Line,
} from 'recharts'
import { examsApi } from '../api/exams'
import { gradingApi } from '../api/grading'
import { teachersApi } from '../api/teachers'
import { studentsApi } from '../api/students'
import { classesApi } from '../api/classes'
import { questionsApi } from '../api/questions'
import { activityLogsApi } from '../api/activityLogs'
import { statisticsApi } from '../api/statistics'
import type { ExamResponse, ExamStatisticResponse } from '../types/api'
import { useAuth } from '../contexts/AuthContext'

interface Stats {
  totalTeachers: number
  totalStudents: number
  totalExams: number
  totalQuestions: number
  totalClasses: number
  activeExams: number
}

/* ═══════════════════════════════════════════════════════════════
   STUDENT DASHBOARD
   ═══════════════════════════════════════════════════════════════ */
interface StudentAttempt {
  id: number
  examId: number
  examTitle: string
  status: string
  score?: number
  totalPoints?: number
  startTime: string
  endTime?: string
}

function StudentDashboard() {
  const { user } = useAuth()
  const navigate = useNavigate()
  const studentId = user?.studentId || 0
  const [available, setAvailable] = useState<ExamResponse[]>([])
  const [upcoming, setUpcoming] = useState<ExamResponse[]>([])
  const [attempts, setAttempts] = useState<StudentAttempt[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    if (!studentId) return
    setLoading(true)
    Promise.all([
      examsApi.getAvailableForStudent(studentId).then(r => setAvailable(r.data.data || [])).catch(() => setAvailable([])),
      examsApi.getUpcomingForStudent(studentId).then(r => setUpcoming(r.data.data || [])).catch(() => setUpcoming([])),
      examsApi.getStudentAttempts(studentId).then(r => setAttempts(r.data.data || [])).catch(() => setAttempts([])),
    ]).finally(() => setLoading(false))
  }, [studentId])

  const completed = attempts.filter(a => a.status === 'SUBMITTED' || a.status === 'GRADED' || a.status === 'PUBLISHED')
  const graded = completed.filter(a => a.score != null && a.totalPoints)
  const avgScore = graded.length > 0
    ? Math.round(graded.reduce((s, a) => s + ((a.score! / a.totalPoints!) * 10), 0) / graded.length * 10) / 10
    : null

  const fmtDate = (d: string) => d ? new Date(d).toLocaleString('vi-VN', { dateStyle: 'short', timeStyle: 'short' }) : '—'

  if (loading) {
    return (
      <div>
        <div className="stat-grid">
          {[...Array(4)].map((_, i) => (
            <div key={i} className="stat-card" style={{ animation: 'none' }}>
              <div style={{ height: 80, background: 'var(--surface-alt)', borderRadius: 8 }} />
            </div>
          ))}
        </div>
        <div className="loading-center"><div className="spinner" /></div>
      </div>
    )
  }

  return (
    <div>
      {/* Welcome */}
      <div style={{ marginBottom: 24 }}>
        <h1 style={{ fontSize: '1.4rem', marginBottom: 4 }}>
          Xin chào, {user?.fullName?.split(' ').pop() || user?.username} 👋
        </h1>
        <p style={{ color: 'var(--text-muted)' }}>Chúc bạn có một buổi thi tốt lành!</p>
      </div>

      {/* Stat Cards */}
      <div className="stat-grid">
        <Link to="/exams" style={{ textDecoration: 'none' }}>
          <div className="stat-card" style={{ cursor: 'pointer' }}>
            <div className="stat-icon green"><span className="material-icons">play_circle</span></div>
            <div className="stat-value">{available.length}</div>
            <div className="stat-label">Bài thi có thể làm</div>
          </div>
        </Link>
        <div className="stat-card">
          <div className="stat-icon blue"><span className="material-icons">event</span></div>
          <div className="stat-value">{upcoming.length}</div>
          <div className="stat-label">Sắp diễn ra</div>
        </div>
        <Link to="/results" style={{ textDecoration: 'none' }}>
          <div className="stat-card" style={{ cursor: 'pointer' }}>
            <div className="stat-icon orange"><span className="material-icons">history</span></div>
            <div className="stat-value">{completed.length}</div>
            <div className="stat-label">Đã hoàn thành</div>
          </div>
        </Link>
        <div className="stat-card">
          <div className="stat-icon purple"><span className="material-icons">grade</span></div>
          <div className="stat-value">{avgScore != null ? avgScore : '—'}</div>
          <div className="stat-label">Điểm trung bình</div>
        </div>
      </div>

      {/* Score Trend Chart */}
      {graded.length >= 2 && (
        <div className="card" style={{ marginBottom: 20 }}>
          <div className="card-header">
            <span className="card-title">
              <span className="material-icons" style={{ fontSize: 18, verticalAlign: 'middle', marginRight: 6 }}>show_chart</span>
              Xu hướng điểm số
            </span>
            <span style={{ fontSize: 12, color: 'var(--text-muted)' }}>{graded.length} bài gần nhất</span>
          </div>
          <ResponsiveContainer width="100%" height={200}>
            <LineChart
              data={graded.slice(-10).map(a => ({
                name: a.examTitle.length > 15 ? a.examTitle.slice(0, 15) + '…' : a.examTitle,
                score: Math.round(((a.score! / a.totalPoints!) * 10) * 10) / 10,
              }))}
              margin={{ top: 8, right: 16, bottom: 0, left: -20 }}
            >
              <CartesianGrid strokeDasharray="3 3" stroke="var(--border)" />
              <XAxis dataKey="name" tick={{ fontSize: 11 }} />
              <YAxis tick={{ fontSize: 12 }} domain={[0, 10]} />
              <Tooltip contentStyle={{ borderRadius: 8, border: '1px solid var(--border)' }} formatter={(v: number) => [`${v} điểm`, 'Điểm']} />
              <Line type="monotone" dataKey="score" stroke="#137fec" strokeWidth={2} dot={{ r: 4 }} activeDot={{ r: 6 }} />
            </LineChart>
          </ResponsiveContainer>
        </div>
      )}

      {/* Available Exams */}
      <div className="card" style={{ marginBottom: 20 }}>
        <div className="card-header">
          <span className="card-title">Bài thi có thể làm ngay</span>
          <Link to="/exams" className="btn btn-secondary btn-sm">
            <span className="material-icons" style={{ fontSize: 16 }}>arrow_forward</span>
            Phòng thi
          </Link>
        </div>
        {available.length > 0 ? (
          <div className="table-wrap">
            <table>
              <thead><tr><th>Tên bài thi</th><th>Môn</th><th>Thời lượng</th><th>Hạn chót</th><th></th></tr></thead>
              <tbody>
                {available.slice(0, 5).map(e => (
                  <tr key={e.id}>
                    <td style={{ fontWeight: 500 }}>{e.title}</td>
                    <td>{e.subjectName || '—'}</td>
                    <td>{e.durationMinutes} phút</td>
                    <td style={{ color: 'var(--text-muted)' }}>{fmtDate(e.endTime)}</td>
                    <td>
                      <button className="btn btn-primary btn-sm" onClick={() => navigate(`/exams`)}>
                        <span className="material-icons" style={{ fontSize: 16 }}>play_arrow</span> Vào thi
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ) : (
          <div className="empty-state">
            <span className="material-icons">event_available</span>
            <p>Hiện chưa có bài thi nào</p>
          </div>
        )}
      </div>

      {/* Recent Results */}
      {completed.length > 0 && (
        <div className="card">
          <div className="card-header">
            <span className="card-title">Kết quả gần đây</span>
            <Link to="/results" className="btn btn-secondary btn-sm">
              <span className="material-icons" style={{ fontSize: 16 }}>arrow_forward</span>
              Xem tất cả
            </Link>
          </div>
          <div className="table-wrap">
            <table>
              <thead><tr><th>Bài thi</th><th>Điểm</th><th>Trạng thái</th><th>Thời gian</th></tr></thead>
              <tbody>
                {completed.slice(0, 5).map(a => (
                  <tr key={a.id}>
                    <td style={{ fontWeight: 500 }}>{a.examTitle}</td>
                    <td>{a.score != null ? `${a.score}/${a.totalPoints}` : '—'}</td>
                    <td>
                      {a.status === 'GRADED' || a.status === 'PUBLISHED'
                        ? <span className="badge badge-green">Đã chấm</span>
                        : <span className="badge badge-gray">Chờ chấm</span>}
                    </td>
                    <td style={{ color: 'var(--text-muted)' }}>{fmtDate(a.startTime)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </div>
  )
}

interface LogEntry {
  id: number
  action: string
  entityType: string | null
  detail: string | null
  occurredAt: string
}

interface Activity {
  icon: string
  text: string
  sub: string
}

const actionIcon: Record<string, string> = {
  CREATE: 'add_circle',
  UPDATE: 'edit',
  DELETE: 'delete',
  LOGIN: 'login',
  SUBMIT: 'send',
  GRADE: 'rate_review',
  START_EXAM: 'play_arrow',
}

export default function DashboardPage() {
  const { user } = useAuth()
  const isStudent = user?.role?.toUpperCase() === 'STUDENT'
  const isTeacher = user?.role?.toUpperCase() === 'TEACHER'
  if (isStudent) return <StudentDashboard />
  if (isTeacher) return <TeacherDashboard />
  return <AdminDashboard />
}

/* ═══════════════════════════════════════════════════════════════
   TEACHER DASHBOARD
   ═══════════════════════════════════════════════════════════════ */
interface PendingExam { examId: number; examTitle: string; pendingCount: number }

function TeacherDashboard() {
  const { user } = useAuth()
  const navigate = useNavigate()
  const [myExams, setMyExams] = useState<ExamResponse[]>([])
  const [pendingExams, setPendingExams] = useState<PendingExam[]>([])
  const [examStats, setExamStats] = useState<ExamStatisticResponse[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    const fetch = async () => {
      setLoading(true)
      try {
        // Try teacher-specific endpoint first, fallback to all exams
        let items: ExamResponse[] = []
        try {
          const meRes = await teachersApi.getMe()
          const teacherId = meRes.data?.data?.id
          if (teacherId) {
            const examsRes = await examsApi.getByTeacher(teacherId)
            items = examsRes.data?.data || []
          }
        } catch {
          const examsRes = await examsApi.getAll(1, 100)
          const d = examsRes.data?.data
          items = Array.isArray(d) ? d : (d?.items || [])
        }
        setMyExams(items)

        // Find pending grading for each active/closed exam
        const pending: PendingExam[] = []
        const gradableExams = items.filter(e => e.status === 'ACTIVE' || e.status === 'CLOSED')
        const results = await Promise.allSettled(
          gradableExams.map(e =>
            gradingApi.getPending(e.id)
          )
        )
        results.forEach((r, i) => {
          if (r.status === 'fulfilled' && r.value.data?.success) {
            const list = r.value.data.data || []
            if (list.length > 0) {
              pending.push({ examId: gradableExams[i].id, examTitle: gradableExams[i].title, pendingCount: list.length })
            }
          }
        })
        setPendingExams(pending)

        // Load exam statistics for closed/active exams (up to 5)
        const statsExams = items.filter(e => e.status === 'ACTIVE' || e.status === 'CLOSED').slice(0, 5)
        const statsResults = await Promise.allSettled(
          statsExams.map(e => statisticsApi.getExamStats(e.id))
        )
        const loadedStats: ExamStatisticResponse[] = []
        statsResults.forEach(r => {
          if (r.status === 'fulfilled' && r.value.data?.data) loadedStats.push(r.value.data.data)
        })
        setExamStats(loadedStats)
      } catch { /* ignore */ }
      finally { setLoading(false) }
    }
    fetch()
  }, [user])

  const totalPending = pendingExams.reduce((s, e) => s + e.pendingCount, 0)
  const activeExams = myExams.filter(e => e.status === 'ACTIVE').length
  const statusBadge = (status: string) => {
    if (status === 'ACTIVE') return <span className="badge badge-green">Đang hoạt động</span>
    if (status === 'DRAFT') return <span className="badge badge-gray">Nháp</span>
    return <span className="badge badge-red">Đã đóng</span>
  }

  if (loading) return <div className="loading-center"><div className="spinner" /></div>

  return (
    <div>
      <div style={{ marginBottom: 24 }}>
        <h1 style={{ fontSize: '1.4rem', marginBottom: 4 }}>
          Chào mừng, {user?.fullName?.split(' ').pop() || user?.username} 👋
        </h1>
        <p style={{ color: 'var(--text-muted)' }}>Bảng điều khiển giáo viên</p>
      </div>

      {/* Stat Cards */}
      <div className="stat-grid">
        <Link to="/exams" style={{ textDecoration: 'none' }}>
          <div className="stat-card" style={{ cursor: 'pointer' }}>
            <div className="stat-icon blue"><span className="material-icons">assignment</span></div>
            <div className="stat-value">{myExams.length}</div>
            <div className="stat-label">Kỳ thi của tôi</div>
          </div>
        </Link>
        <div className="stat-card">
          <div className="stat-icon green"><span className="material-icons">play_circle</span></div>
          <div className="stat-value">{activeExams}</div>
          <div className="stat-label">Đang hoạt động</div>
        </div>
        <Link to="/grading" style={{ textDecoration: 'none' }}>
          <div className="stat-card" style={{ cursor: 'pointer', border: totalPending > 0 ? '2px solid #f97316' : undefined }}>
            <div className="stat-icon orange"><span className="material-icons">rate_review</span></div>
            <div className="stat-value" style={{ color: totalPending > 0 ? '#f97316' : undefined }}>{totalPending}</div>
            <div className="stat-label">Bài cần chấm</div>
          </div>
        </Link>
        <Link to="/questions" style={{ textDecoration: 'none' }}>
          <div className="stat-card" style={{ cursor: 'pointer' }}>
            <div className="stat-icon purple"><span className="material-icons">quiz</span></div>
            <div className="stat-value">{myExams.length > 0 ? '—' : '0'}</div>
            <div className="stat-label">Câu hỏi</div>
          </div>
        </Link>
      </div>

      {/* Pending Grading Section */}
      {pendingExams.length > 0 && (
        <div className="card" style={{ marginBottom: 20, borderLeft: '4px solid #f97316' }}>
          <div className="card-header">
            <span className="card-title" style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
              <span className="material-icons" style={{ color: '#f97316', fontSize: 20 }}>pending_actions</span>
              Bài cần chấm ({totalPending})
            </span>
            <Link to="/grading" className="btn btn-secondary btn-sm">
              <span className="material-icons" style={{ fontSize: 16 }}>arrow_forward</span>
              Chấm bài
            </Link>
          </div>
          <div className="table-wrap">
            <table>
              <thead><tr><th>Kỳ thi</th><th>Số bài chờ chấm</th><th></th></tr></thead>
              <tbody>
                {pendingExams.map(pe => (
                  <tr key={pe.examId}>
                    <td style={{ fontWeight: 500 }}>{pe.examTitle}</td>
                    <td>
                      <span className="badge badge-orange" style={{ background: 'rgba(249,115,22,0.1)', color: '#f97316' }}>
                        {pe.pendingCount} bài
                      </span>
                    </td>
                    <td>
                      <button className="btn btn-primary btn-sm" onClick={() => navigate(`/grading`)}>
                        <span className="material-icons" style={{ fontSize: 14 }}>rate_review</span> Chấm
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* Exam Performance Summary */}
      {examStats.length > 0 && (
        <div className="card" style={{ marginBottom: 20 }}>
          <div className="card-header">
            <span className="card-title" style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
              <span className="material-icons" style={{ color: 'var(--primary)', fontSize: 20 }}>bar_chart</span>
              Hiệu suất kỳ thi
            </span>
            <Link to="/reports" className="btn btn-secondary btn-sm">
              <span className="material-icons" style={{ fontSize: 16 }}>arrow_forward</span>
              Xem chi tiết
            </Link>
          </div>
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Kỳ thi</th>
                  <th>Lượt thi</th>
                  <th>Điểm TB</th>
                  <th>Cao nhất</th>
                  <th>Tỷ lệ đạt</th>
                </tr>
              </thead>
              <tbody>
                {examStats.map(es => (
                  <tr key={es.examId}>
                    <td style={{ fontWeight: 500 }}>{es.examTitle}</td>
                    <td>{es.totalAttempts}</td>
                    <td style={{ fontWeight: 600 }}>{es.averageScore.toFixed(1)}</td>
                    <td>{es.maxScore.toFixed(1)}</td>
                    <td>
                      <span style={{
                        fontWeight: 600,
                        color: es.passRate >= 50 ? '#16a34a' : '#dc2626',
                      }}>{es.passRate.toFixed(0)}%</span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* My Exams */}
      <div className="card">
        <div className="card-header">
          <span className="card-title">Kỳ thi của tôi</span>
          <Link to="/exams" className="btn btn-secondary btn-sm">
            <span className="material-icons" style={{ fontSize: 16 }}>arrow_forward</span>
            Xem tất cả
          </Link>
        </div>
        {myExams.length > 0 ? (
          <div className="table-wrap">
            <table>
              <thead><tr><th>Tên kỳ thi</th><th>Môn học</th><th>Thời lượng</th><th>Trạng thái</th></tr></thead>
              <tbody>
                {myExams.slice(0, 8).map(exam => (
                  <tr key={exam.id}>
                    <td>
                      <Link to={`/exams/${exam.id}`} style={{ color: 'var(--primary)', textDecoration: 'none', fontWeight: 500 }}>
                        {exam.title}
                      </Link>
                    </td>
                    <td>{exam.subjectName || '—'}</td>
                    <td>{exam.durationMinutes} phút</td>
                    <td>{statusBadge(exam.status)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ) : (
          <div className="empty-state">
            <span className="material-icons">edit_calendar</span>
            <p>Chưa có kỳ thi nào</p>
            <Link to="/exams" className="btn btn-primary btn-sm">Tạo kỳ thi</Link>
          </div>
        )}
      </div>
    </div>
  )
}

function AdminDashboard() {
  const { user } = useAuth()
  const [stats, setStats] = useState<Stats>({
    totalTeachers: 0, totalStudents: 0, totalExams: 0,
    totalQuestions: 0, totalClasses: 0, activeExams: 0,
  })
  const [trendData, setTrendData] = useState<{ day: string; count: number }[]>([])
  const [recentExams, setRecentExams] = useState<ExamResponse[]>([])
  const [activity, setActivity] = useState<Activity[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    const fetchAll = async () => {
      setLoading(true)
      try {
        // Fetch counts from all list endpoints with pageSize=1
        const [teachersRes, studentsRes, examsCountRes, questionsRes, classesRes, logsRes, recentExamsRes] = await Promise.allSettled([
          teachersApi.getAll(1, 1),
          studentsApi.getAll(1, 1),
          examsApi.getAll(1, 1),
          questionsApi.getAll(1, 1),
          classesApi.getAll(1, 1),
          activityLogsApi.getLogs({ page: 1, pageSize: 10 }),
          examsApi.getAll(1, 5),
        ])

        const getCount = (res: PromiseSettledResult<any>) =>
          res.status === 'fulfilled' ? (res.value.data?.data?.totalCount ?? 0) : 0

        // Count active exams from recent full fetch
        let activeCount = 0
        if (examsCountRes.status === 'fulfilled') {
          const allExamsRes = await examsApi.getAll(1, 200)
          const items = allExamsRes.data?.data?.items || []
          activeCount = items.filter((e: any) => e.status === 'ACTIVE').length
        }

        setStats({
          totalTeachers: getCount(teachersRes),
          totalStudents: getCount(studentsRes),
          totalExams: getCount(examsCountRes),
          totalQuestions: getCount(questionsRes),
          totalClasses: getCount(classesRes),
          activeExams: activeCount,
        })

        // Activity logs
        if (logsRes.status === 'fulfilled') {
          const logs = (logsRes.value.data?.data?.logs || []) as unknown as LogEntry[]
          setActivity(logs.slice(0, 8).map(l => ({
            icon: actionIcon[l.action] || 'info',
            text: l.detail || `${l.action} ${l.entityType || ''}`,
            sub: new Date(l.occurredAt).toLocaleString('vi-VN'),
          })))
        }

        // Build trend from logs (last 7 days)
        if (logsRes.status === 'fulfilled') {
          try {
            const fullLogsRes = await activityLogsApi.getLogs({ page: 1, pageSize: 200 })
            const allLogs = (fullLogsRes.data?.data?.logs || []) as unknown as LogEntry[]
            const days = ['CN', 'T2', 'T3', 'T4', 'T5', 'T6', 'T7']
            const counts: Record<string, number> = {}
            const now = new Date()
            for (let i = 6; i >= 0; i--) {
              const d = new Date(now)
              d.setDate(d.getDate() - i)
              const key = d.toISOString().slice(0, 10)
              counts[key] = 0
            }
            allLogs.forEach(l => {
              const key = l.occurredAt.slice(0, 10)
              if (key in counts) counts[key]++
            })
            setTrendData(Object.entries(counts).map(([dateStr, count]) => ({
              day: days[new Date(dateStr).getDay()],
              count,
            })))
          } catch { /* trend is optional */ }
        }

        // Recent exams
        if (recentExamsRes.status === 'fulfilled' && recentExamsRes.value.data?.data) {
          setRecentExams(recentExamsRes.value.data.data.items?.slice(0, 5) || [])
        }
      } finally {
        setLoading(false)
      }
    }
    fetchAll()
  }, [])

  const statCards = [
    { icon: 'supervisor_account', color: 'blue',   label: 'Giáo viên',    value: stats.totalTeachers, link: '/teachers' },
    { icon: 'group',              color: 'green',  label: 'Học sinh',     value: stats.totalStudents, link: '/students' },
    { icon: 'history_edu',        color: 'orange', label: 'Đang thi',     value: stats.activeExams,   link: '/exams' },
    { icon: 'quiz',               color: 'purple', label: 'Câu hỏi',     value: stats.totalQuestions, link: '/questions' },
    { icon: 'school',             color: 'blue',   label: 'Lớp học',      value: stats.totalClasses,  link: '/classes' },
    { icon: 'assignment',         color: 'green',  label: 'Tổng kỳ thi',  value: stats.totalExams,    link: '/exams' },
  ]

  const statusBadge = (status: string) => {
    if (status === 'ACTIVE') return <span className="badge badge-green">Đang hoạt động</span>
    if (status === 'DRAFT')  return <span className="badge badge-gray">Nháp</span>
    return <span className="badge badge-red">Đã đóng</span>
  }

  if (loading) {
    return (
      <div>
        <div className="stat-grid">
          {[...Array(4)].map((_, i) => (
            <div key={i} className="stat-card" style={{ animation: 'none' }}>
              <div style={{ height: 80, background: 'var(--surface-alt)', borderRadius: 8 }} />
            </div>
          ))}
        </div>
        <div className="loading-center"><div className="spinner" /></div>
      </div>
    )
  }

  return (
    <div>
      {/* Welcome */}
      <div style={{ marginBottom: 24 }}>
        <h1 style={{ fontSize: '1.4rem', marginBottom: 4 }}>
          Chào mừng trở lại, {user?.fullName?.split(' ').pop() || user?.username} 👋
        </h1>
        <p>Đây là tổng quan hệ thống thi trực tuyến của bạn</p>
      </div>

      {/* Stat Cards */}
      <div className="stat-grid">
        {statCards.map(card => (
          <Link key={card.label} to={card.link} style={{ textDecoration: 'none' }}>
            <div className="stat-card" style={{ cursor: 'pointer', transition: 'var(--transition)' }}>
              <div className={`stat-icon ${card.color}`}>
                <span className="material-icons">{card.icon}</span>
              </div>
              <div className="stat-value">{typeof card.value === 'number' ? card.value.toLocaleString() : card.value}</div>
              <div className="stat-label">{card.label}</div>
            </div>
          </Link>
        ))}
      </div>

      {/* Chart + Activity */}
      <div className="dashboard-chart-grid">
        {/* Trend Chart */}
        <div className="card">
          <div className="card-header">
            <span className="card-title">Xu hướng thi</span>
            <span style={{ fontSize: 12, color: 'var(--text-muted)' }}>7 ngày gần nhất</span>
          </div>
          <ResponsiveContainer width="100%" height={200}>
            <AreaChart data={trendData} margin={{ top: 4, right: 8, bottom: 0, left: -20 }}>
              <defs>
                <linearGradient id="colorExam" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="5%" stopColor="#137fec" stopOpacity={0.15} />
                  <stop offset="95%" stopColor="#137fec" stopOpacity={0} />
                </linearGradient>
              </defs>
              <CartesianGrid strokeDasharray="3 3" stroke="var(--border)" />
              <XAxis dataKey="day" tick={{ fontSize: 12, fontFamily: 'Space Grotesk' }} />
              <YAxis tick={{ fontSize: 12, fontFamily: 'Space Grotesk' }} />
              <Tooltip contentStyle={{ fontFamily: 'Space Grotesk', borderRadius: 8, border: '1px solid var(--border)' }} />
              <Area type="monotone" dataKey="count" stroke="#137fec" strokeWidth={2} fill="url(#colorExam)" />
            </AreaChart>
          </ResponsiveContainer>
        </div>

        {/* Recent Activity */}
        <div className="card">
          <div className="card-header">
            <span className="card-title">Hoạt động gần đây</span>
          </div>
          {activity.length > 0 ? (
            <div style={{ display: 'flex', flexDirection: 'column', gap: 14 }}>
              {activity.slice(0, 8).map((a, i) => (
                <div key={i} style={{ display: 'flex', gap: 12, alignItems: 'flex-start' }}>
                  <div style={{
                    width: 34, height: 34, borderRadius: 8,
                    background: 'var(--primary-light)', color: 'var(--primary)',
                    display: 'flex', alignItems: 'center', justifyContent: 'center', flexShrink: 0,
                  }}>
                    <span className="material-icons" style={{ fontSize: 18 }}>{a.icon}</span>
                  </div>
                  <div>
                    <div style={{ fontSize: 13, fontWeight: 500 }}>{a.text}</div>
                    <div style={{ fontSize: 12, color: 'var(--text-muted)' }}>{a.sub}</div>
                  </div>
                </div>
              ))}
            </div>
          ) : (
            <div style={{ display: 'flex', flexDirection: 'column', gap: 14 }}>
              {[
                { icon: 'assignment',  text: 'Hệ thống đã sẵn sàng', sub: 'Backend đang hoạt động' },
                { icon: 'person_add',  text: 'Chào mừng đến với Antigravity', sub: 'Hệ thống thi trực tuyến' },
              ].map((a, i) => (
                <div key={i} style={{ display: 'flex', gap: 12, alignItems: 'flex-start' }}>
                  <div style={{
                    width: 34, height: 34, borderRadius: 8,
                    background: 'var(--primary-light)', color: 'var(--primary)',
                    display: 'flex', alignItems: 'center', justifyContent: 'center', flexShrink: 0,
                  }}>
                    <span className="material-icons" style={{ fontSize: 18 }}>{a.icon}</span>
                  </div>
                  <div>
                    <div style={{ fontSize: 13, fontWeight: 500 }}>{a.text}</div>
                    <div style={{ fontSize: 12, color: 'var(--text-muted)' }}>{a.sub}</div>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>

      {/* Recent Exams */}
      <div className="card">
        <div className="card-header">
          <span className="card-title">Kỳ thi gần đây</span>
          <Link to="/exams" className="btn btn-secondary btn-sm">
            <span className="material-icons" style={{ fontSize: 16 }}>arrow_forward</span>
            Xem tất cả
          </Link>
        </div>
        {recentExams.length > 0 ? (
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Tên kỳ thi</th>
                  <th>Môn học</th>
                  <th>Thời lượng</th>
                  <th>Trạng thái</th>
                  <th>Ngày tạo</th>
                </tr>
              </thead>
              <tbody>
                {recentExams.map(exam => (
                  <tr key={exam.id}>
                    <td>
                      <Link to={`/exams/${exam.id}`} style={{ color: 'var(--primary)', textDecoration: 'none', fontWeight: 500 }}>
                        {exam.title}
                      </Link>
                    </td>
                    <td>{exam.subjectName || '—'}</td>
                    <td>{exam.durationMinutes} phút</td>
                    <td>{statusBadge(exam.status)}</td>
                    <td style={{ color: 'var(--text-muted)' }}>
                      {new Date(exam.createdAt).toLocaleDateString('vi-VN')}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ) : (
          <div className="empty-state">
            <span className="material-icons">edit_calendar</span>
            <p>Chưa có kỳ thi nào</p>
            <Link to="/exams" className="btn btn-primary btn-sm">Tạo kỳ thi</Link>
          </div>
        )}
      </div>
    </div>
  )
}
