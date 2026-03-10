import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import {
  AreaChart, Area, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer
} from 'recharts'
import { statisticsApi } from '../api/statistics'
import { examsApi } from '../api/exams'
import type { ExamResponse } from '../types/api'
import { useAuth } from '../contexts/AuthContext'

interface Stats {
  totalTeachers: number
  totalStudents: number
  activeExams: number
  completionRate: number
  totalQuestions: number
  totalClasses: number
}

interface TrendPoint { day: string; count: number }

interface Activity {
  icon: string
  text: string
  description?: string
  sub: string
  time: string
}

const defaultTrend: TrendPoint[] = [
  { day: 'T2', count: 12 },
  { day: 'T3', count: 18 },
  { day: 'T4', count: 15 },
  { day: 'T5', count: 22 },
  { day: 'T6', count: 30 },
  { day: 'T7', count: 28 },
  { day: 'CN', count: 24 },
]

export default function DashboardPage() {
  const { user } = useAuth()
  const [stats, setStats] = useState<Stats>({
    totalTeachers: 0, totalStudents: 0, activeExams: 0,
    completionRate: 0, totalQuestions: 0, totalClasses: 0,
  })
  const [trend, setTrend] = useState<TrendPoint[]>(defaultTrend)
  const [recentExams, setRecentExams] = useState<ExamResponse[]>([])
  const [activity, setActivity] = useState<Activity[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    const fetchAll = async () => {
      setLoading(true)
      try {
        const [overviewRes, examsRes] = await Promise.allSettled([
          statisticsApi.getDashboard(),
          examsApi.getAll(1, 5),
        ])

        if (overviewRes.status === 'fulfilled' && overviewRes.value.data) {
          const d = overviewRes.value.data as Record<string, unknown>
          setStats({
            totalTeachers: Number(d.totalTeachers || d.teachers || 0),
            totalStudents: Number(d.totalStudents || d.students || 0),
            activeExams: Number(d.activeExams || d.examsTaking || 0),
            completionRate: Number(d.completionRate || d.passRate || 0),
            totalQuestions: Number(d.totalQuestions || 0),
            totalClasses: Number(d.totalClasses || 0),
          })
          const trendData = d.examTrend || d.trend
          if (Array.isArray(trendData)) setTrend(trendData as TrendPoint[])

          const acts = d.recentActivity || d.activities
          if (Array.isArray(acts)) setActivity(acts as Activity[])
        }

        if (examsRes.status === 'fulfilled' && examsRes.value.data?.data) {
          setRecentExams(examsRes.value.data.data.items?.slice(0, 5) || [])
        }
      } finally {
        setLoading(false)
      }
    }
    fetchAll()
  }, [])

  const statCards = [
    { icon: 'supervisor_account', color: 'blue',   label: 'Giáo viên',    value: stats.totalTeachers, trend: '+2%',  up: true,  link: '/teachers' },
    { icon: 'group',              color: 'green',  label: 'Học sinh',     value: stats.totalStudents, trend: '+5%',  up: true,  link: '/students' },
    { icon: 'history_edu',        color: 'orange', label: 'Đang thi',     value: stats.activeExams,   trend: '',     up: null,  link: '/exams' },
    { icon: 'check_circle',       color: 'purple', label: 'Hoàn thành',   value: `${stats.completionRate}%`, trend: '+3%', up: true, link: '/results' },
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
              {card.trend && (
                <div className={`stat-trend ${card.up ? 'up' : 'down'}`}>
                  <span className="material-icons" style={{ fontSize: 14 }}>
                    {card.up ? 'trending_up' : 'trending_down'}
                  </span>
                  {card.trend}
                </div>
              )}
            </div>
          </Link>
        ))}
      </div>

      {/* Chart + Activity */}
      <div style={{ display: 'grid', gridTemplateColumns: '1fr 340px', gap: 20, marginBottom: 24 }}>
        {/* Trend Chart */}
        <div className="card">
          <div className="card-header">
            <span className="card-title">Xu hướng thi</span>
            <span style={{ fontSize: 12, color: 'var(--text-muted)' }}>7 ngày gần nhất</span>
          </div>
          <ResponsiveContainer width="100%" height={200}>
            <AreaChart data={trend} margin={{ top: 4, right: 8, bottom: 0, left: -20 }}>
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
              {activity.slice(0, 5).map((a, i) => (
                <div key={i} style={{ display: 'flex', gap: 12, alignItems: 'flex-start' }}>
                  <div style={{
                    width: 34, height: 34, borderRadius: 8,
                    background: 'var(--primary-light)', color: 'var(--primary)',
                    display: 'flex', alignItems: 'center', justifyContent: 'center', flexShrink: 0,
                  }}>
                    <span className="material-icons" style={{ fontSize: 18 }}>{a.icon || 'info'}</span>
                  </div>
                  <div>
                    <div style={{ fontSize: 13, fontWeight: 500 }}>{a.text || a.description}</div>
                    <div style={{ fontSize: 12, color: 'var(--text-muted)' }}>{a.sub} {a.time}</div>
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
