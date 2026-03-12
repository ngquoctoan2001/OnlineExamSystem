import { useState, useEffect, useCallback } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { examsApi } from '../api/exams'
import { subjectsApi } from '../api/subjects'
import { subjectExamTypesApi } from '../api/subjectExamTypes'
import { classesApi } from '../api/classes'
import type { ExamResponse, CreateExamRequest, SubjectResponse, ClassResponse, SubjectExamTypeResponse } from '../types/api'
import { useAuth } from '../contexts/AuthContext'
import { examAttemptsApi } from '../api/examAttempts'
import { teachersApi } from '../api/teachers'

interface AttemptInfo {
  id: number
  examId: number
  examTitle: string
  status: string
  score?: number
  totalPoints?: number
  startTime: string
  endTime?: string
}

const emptyForm = (): CreateExamRequest => ({
  title: '',
  subjectId: 0,
  createdBy: 0,
  durationMinutes: 60,
  startTime: '',
  endTime: '',
  description: '',
  subjectExamTypeId: null,
})

const statusBadge = (status: string) => {
  if (status === 'ACTIVE') return <span className="badge badge-green">Đang hoạt động</span>
  if (status === 'DRAFT')  return <span className="badge badge-gray">Nháp</span>
  return <span className="badge badge-red">Đã đóng</span>
}

const fmtDate = (d: string) => d ? new Date(d).toLocaleString('vi-VN', { dateStyle: 'short', timeStyle: 'short' }) : '—'

/* ═══════════════════════════════════════════════════════════════
   STUDENT EXAM VIEW — completely separate component
   ═══════════════════════════════════════════════════════════════ */
function StudentExamView({ user, navigate }: { user: { id: number; studentId?: number; fullName: string } | null; navigate: ReturnType<typeof useNavigate> }) {
  const studentId = user?.studentId || 0
  const [tab, setTab] = useState<'available' | 'upcoming' | 'history'>('available')
  const [available, setAvailable] = useState<ExamResponse[]>([])
  const [upcoming, setUpcoming] = useState<ExamResponse[]>([])
  const [attempts, setAttempts] = useState<AttemptInfo[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    if (!studentId) return
    setLoading(true)

    const load = async () => {
      const [availRes, upcomingRes, attemptsRes] = await Promise.allSettled([
        examsApi.getAvailableForStudent(studentId),
        examsApi.getUpcomingForStudent(studentId),
        examsApi.getStudentAttempts(studentId),
      ])

      const avail: ExamResponse[] = availRes.status === 'fulfilled' ? (availRes.value.data.data || []) : []
      const up: ExamResponse[] = upcomingRes.status === 'fulfilled' ? (upcomingRes.value.data.data || []) : []
      let atts: AttemptInfo[] = attemptsRes.status === 'fulfilled' ? (attemptsRes.value.data.data || []) : []

      setAvailable(avail)
      setUpcoming(up)

      // Auto-submit stale IN_PROGRESS attempts (time ran out or exam ended)
      const allExams = [...avail, ...up]
      const stale = atts.filter(a => {
        if (a.status !== 'IN_PROGRESS') return false
        const exam = allExams.find(e => e.id === a.examId)
        const startMs = new Date(a.startTime).getTime()
        const nowMs = Date.now()
        if (!exam) return true // exam no longer in active/upcoming lists → ended
        const durationMs = exam.durationMinutes * 60 * 1000
        return nowMs > startMs + durationMs // attempt time exceeded
      })

      if (stale.length > 0) {
        await Promise.allSettled(
          stale.map(a => examAttemptsApi.submit(a.id))
        )
        // Re-fetch attempts after auto-submitting stale ones
        try {
          const refreshRes = await examsApi.getStudentAttempts(studentId)
          atts = refreshRes.data?.data || atts
        } catch { /* keep original */ }
      }

      setAttempts(atts)
      setLoading(false)
    }

    load()
  }, [studentId])

  if (!studentId) {
    return (
      <div className="empty-state" style={{ marginTop: 40 }}>
        <span className="material-icons" style={{ fontSize: 48, color: 'var(--warning)' }}>warning</span>
        <p>Đang tải thông tin học sinh...</p>
      </div>
    )
  }

  const completedAttempts = attempts.filter(a => a.status === 'SUBMITTED' || a.status === 'GRADED' || a.status === 'PUBLISHED')
  const inProgressAttempts = attempts.filter(a => a.status === 'IN_PROGRESS')

  const tabs = [
    { key: 'available' as const, label: 'Bài thi có thể làm', icon: 'play_circle', count: available.length },
    { key: 'upcoming' as const, label: 'Sắp diễn ra', icon: 'event', count: upcoming.length },
    { key: 'history' as const, label: 'Đã làm', icon: 'history', count: completedAttempts.length },
  ]

  return (
    <div>
      {/* Header */}
      <div style={{ marginBottom: 24 }}>
        <h2 style={{ marginBottom: 4 }}>
          <span className="material-icons" style={{ fontSize: 28, verticalAlign: 'middle', marginRight: 8, color: 'var(--primary)' }}>school</span>
          Phòng thi Online
        </h2>
        <p style={{ fontSize: 13, color: 'var(--text-muted)' }}>Xin chào <strong>{user?.fullName}</strong> — chọn bài thi để bắt đầu làm bài</p>
      </div>

      {/* In-progress exam banner */}
      {inProgressAttempts.length > 0 && (
        <div style={{ background: 'linear-gradient(135deg, #fef3c7, #fde68a)', border: '1px solid #f59e0b', borderRadius: 12, padding: 16, marginBottom: 20, display: 'flex', alignItems: 'center', gap: 12 }}>
          <span className="material-icons" style={{ fontSize: 32, color: '#d97706' }}>timer</span>
          <div style={{ flex: 1 }}>
            <div style={{ fontWeight: 600, color: '#92400e' }}>Bạn đang có bài thi chưa hoàn thành!</div>
            <div style={{ fontSize: 13, color: '#a16207' }}>{inProgressAttempts[0].examTitle}</div>
          </div>
          <button className="btn btn-primary" onClick={() => navigate(`/exam-player/${inProgressAttempts[0].examId}`)}>
            <span className="material-icons" style={{ fontSize: 16 }}>play_arrow</span>
            Tiếp tục làm bài
          </button>
        </div>
      )}

      {/* Tabs */}
      <div style={{ display: 'flex', gap: 4, marginBottom: 20, borderBottom: '2px solid var(--border)', paddingBottom: 0 }}>
        {tabs.map(t => (
          <button
            key={t.key}
            onClick={() => setTab(t.key)}
            style={{
              display: 'flex', alignItems: 'center', gap: 6, padding: '10px 20px',
              border: 'none', background: 'none', cursor: 'pointer',
              fontSize: 14, fontWeight: tab === t.key ? 600 : 400,
              color: tab === t.key ? 'var(--primary)' : 'var(--text-secondary)',
              borderBottom: tab === t.key ? '2px solid var(--primary)' : '2px solid transparent',
              marginBottom: -2, transition: 'all 0.2s',
            }}
          >
            <span className="material-icons" style={{ fontSize: 18 }}>{t.icon}</span>
            {t.label}
            {t.count > 0 && (
              <span style={{
                background: tab === t.key ? 'var(--primary)' : 'var(--text-muted)',
                color: '#fff', borderRadius: 10, padding: '1px 8px', fontSize: 11, fontWeight: 600,
              }}>{t.count}</span>
            )}
          </button>
        ))}
      </div>

      {loading ? (
        <div className="loading-center"><div className="spinner" /></div>
      ) : (
        <>
          {/* Available exams */}
          {tab === 'available' && (
            available.length === 0 ? (
              <div className="empty-state">
                <span className="material-icons" style={{ fontSize: 48 }}>assignment_turned_in</span>
                <p>Hiện tại không có bài thi nào cần làm</p>
                <p style={{ fontSize: 12, color: 'var(--text-muted)' }}>Kiểm tra lại tab "Sắp diễn ra" để xem lịch thi</p>
              </div>
            ) : (
              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(340px, 1fr))', gap: 16 }}>
                {available.map(exam => (
                  <div key={exam.id} style={{
                    background: '#fff', borderRadius: 12, border: '1px solid var(--border)',
                    overflow: 'hidden', transition: 'box-shadow 0.2s',
                  }}>
                    <div style={{ background: 'linear-gradient(135deg, var(--primary), #6366f1)', padding: '16px 20px', color: '#fff' }}>
                      <div style={{ fontSize: 16, fontWeight: 600 }}>{exam.title}</div>
                      <div style={{ fontSize: 12, opacity: 0.85, marginTop: 2 }}>{exam.subjectName}</div>
                    </div>
                    <div style={{ padding: 20 }}>
                      <div style={{ display: 'flex', gap: 20, fontSize: 13, color: 'var(--text-secondary)', marginBottom: 12 }}>
                        <span style={{ display: 'flex', alignItems: 'center', gap: 4 }}>
                          <span className="material-icons" style={{ fontSize: 16 }}>schedule</span>
                          {exam.durationMinutes} phút
                        </span>
                        <span style={{ display: 'flex', alignItems: 'center', gap: 4 }}>
                          <span className="material-icons" style={{ fontSize: 16 }}>event</span>
                          Hạn: {fmtDate(exam.endTime)}
                        </span>
                      </div>
                      {exam.description && <p style={{ fontSize: 13, margin: '0 0 12px', color: 'var(--text-secondary)' }}>{exam.description}</p>}
                      <button
                        className="btn btn-primary"
                        onClick={() => navigate(`/exam-player/${exam.id}`)}
                        style={{ width: '100%', justifyContent: 'center', padding: '10px 0', fontSize: 15, fontWeight: 600 }}
                      >
                        <span className="material-icons" style={{ fontSize: 20 }}>play_circle</span>
                        Bắt đầu làm bài
                      </button>
                    </div>
                  </div>
                ))}
              </div>
            )
          )}

          {/* Upcoming exams */}
          {tab === 'upcoming' && (
            upcoming.length === 0 ? (
              <div className="empty-state">
                <span className="material-icons" style={{ fontSize: 48 }}>calendar_today</span>
                <p>Không có bài thi nào sắp diễn ra</p>
              </div>
            ) : (
              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(340px, 1fr))', gap: 16 }}>
                {upcoming.map(exam => (
                  <div key={exam.id} className="card" style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
                    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
                      <div>
                        <div style={{ fontWeight: 600, fontSize: 15 }}>{exam.title}</div>
                        <div style={{ fontSize: 12, color: 'var(--text-muted)', marginTop: 2 }}>{exam.subjectName}</div>
                      </div>
                      <div style={{ display: 'flex', gap: 4 }}>
                        {exam.status === 'DRAFT' && <span className="badge badge-gray">Chưa kích hoạt</span>}
                        <span className="badge badge-blue">Sắp tới</span>
                      </div>
                    </div>
                    <div style={{ display: 'flex', gap: 16, fontSize: 13, color: 'var(--text-secondary)' }}>
                      <span style={{ display: 'flex', alignItems: 'center', gap: 4 }}>
                        <span className="material-icons" style={{ fontSize: 16 }}>schedule</span>
                        {exam.durationMinutes} phút
                      </span>
                      <span style={{ display: 'flex', alignItems: 'center', gap: 4 }}>
                        <span className="material-icons" style={{ fontSize: 16 }}>event</span>
                        {fmtDate(exam.startTime)}
                      </span>
                    </div>
                    {exam.description && <p style={{ fontSize: 12, margin: 0, color: 'var(--text-muted)' }}>{exam.description}</p>}
                    <div style={{ padding: '8px 12px', background: 'var(--surface-alt)', borderRadius: 8, fontSize: 12, color: 'var(--text-muted)', display: 'flex', alignItems: 'center', gap: 6 }}>
                      <span className="material-icons" style={{ fontSize: 16 }}>info</span>
                      Bài thi sẽ mở vào lúc {fmtDate(exam.startTime)}
                    </div>
                  </div>
                ))}
              </div>
            )
          )}

          {/* History */}
          {tab === 'history' && (
            completedAttempts.length === 0 ? (
              <div className="empty-state">
                <span className="material-icons" style={{ fontSize: 48 }}>history</span>
                <p>Bạn chưa hoàn thành bài thi nào</p>
              </div>
            ) : (
              <div className="card">
                <div className="table-wrap">
                  <table>
                    <thead>
                      <tr>
                        <th>#</th>
                        <th>Bài thi</th>
                        <th>Trạng thái</th>
                        <th>Điểm</th>
                        <th>Thời gian làm</th>
                        <th>Bắt đầu</th>
                        <th>Nộp bài</th>
                      </tr>
                    </thead>
                    <tbody>
                      {completedAttempts.map((a, idx) => {
                        const timeTaken = a.startTime && a.endTime
                          ? Math.round((new Date(a.endTime).getTime() - new Date(a.startTime).getTime()) / 60000)
                          : null
                        return (
                        <tr key={a.id}>
                          <td style={{ color: 'var(--text-muted)' }}>{idx + 1}</td>
                          <td style={{ fontWeight: 500 }}>{a.examTitle}</td>
                          <td>
                            {a.status === 'GRADED' || a.status === 'PUBLISHED'
                              ? <span className="badge badge-green">Đã chấm</span>
                              : <span className="badge badge-yellow">Chờ chấm</span>}
                          </td>
                          <td>
                            {a.score != null
                              ? <span style={{ fontWeight: 600, color: 'var(--primary)' }}>{a.score}/{a.totalPoints || '?'}</span>
                              : <span style={{ color: 'var(--text-muted)' }}>—</span>}
                          </td>
                          <td style={{ fontSize: 12, color: 'var(--text-muted)' }}>{timeTaken != null ? `${timeTaken} phút` : '—'}</td>
                          <td style={{ fontSize: 12, color: 'var(--text-muted)' }}>{fmtDate(a.startTime)}</td>
                          <td style={{ fontSize: 12, color: 'var(--text-muted)' }}>{fmtDate(a.endTime || '')}</td>
                        </tr>
                        )
                      })}
                    </tbody>
                  </table>
                </div>
              </div>
            )
          )}
        </>
      )}
    </div>
  )
}

export default function ExamsPage() {
  const { user } = useAuth()
  const navigate = useNavigate()
  const role = user?.role?.toUpperCase() || 'USER'
  const isStudent = role === 'STUDENT'
  const canManage = role === 'ADMIN' || role === 'TEACHER'

  const [exams, setExams] = useState<ExamResponse[]>([])
  const [total, setTotal] = useState(0)
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [loading, setLoading] = useState(true)
  const [subjects, setSubjects] = useState<SubjectResponse[]>([])
  const [allClasses, setAllClasses] = useState<ClassResponse[]>([])
  const [modal, setModal] = useState<'create' | 'edit' | null>(null)
  const [form, setForm] = useState<CreateExamRequest>(emptyForm())
  const [selectedClassIds, setSelectedClassIds] = useState<number[]>([])
  const [editId, setEditId] = useState<number | null>(null)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const [deleteConfirm, setDeleteConfirm] = useState<number | null>(null)
  const [viewMode, setViewMode] = useState<'card' | 'list'>('card')
  const [filterSubject, setFilterSubject] = useState(0)
  const [teachers, setTeachers] = useState<{ id: number; fullName: string }[]>([])
  const [filterTeacher, setFilterTeacher] = useState(0)
  const [subjectExamTypes, setSubjectExamTypes] = useState<SubjectExamTypeResponse[]>([])
  const pageSize = 20

  const fetchExams = useCallback(async () => {
    setLoading(true)
    try {
      if (search.trim()) {
        const res = await examsApi.search(search)
        const data = res.data.data || []
        setExams(Array.isArray(data) ? data : [])
        setTotal(Array.isArray(data) ? data.length : 0)
      } else if (filterTeacher) {
        const res = await examsApi.getByTeacher(filterTeacher)
        const data = res.data.data || []
        let list = Array.isArray(data) ? data : []
        if (filterSubject) list = list.filter(e => e.subjectId === filterSubject)
        setExams(list); setTotal(list.length)
      } else if (filterSubject) {
        const res = await examsApi.getBySubject(filterSubject)
        const data = res.data.data || []
        setExams(Array.isArray(data) ? data : [])
        setTotal(Array.isArray(data) ? data.length : 0)
      } else {
        const res = await examsApi.getAll(page, pageSize)
        const d = res.data.data
        setExams(d?.items || [])
        setTotal(d?.totalCount || 0)
      }
    } catch { setExams([]); setTotal(0) }
    finally { setLoading(false) }
  }, [page, search, filterSubject, filterTeacher])

  useEffect(() => { fetchExams() }, [fetchExams])
  useEffect(() => {
    if (canManage) {
      subjectsApi.getAll(1, 100).then(r => setSubjects(r.data.data?.subjects || [])).catch(() => {})
      classesApi.getAll(1, 200).then(r => setAllClasses(r.data.data?.classes || [])).catch(() => {})
      teachersApi.getAll(1, 200).then(r => {
        const items = r.data?.data?.teachers || (r.data?.data as any)?.items || r.data?.data || []
        setTeachers(Array.isArray(items) ? items.map((t: { id: number; fullName: string }) => ({ id: t.id, fullName: t.fullName })) : [])
      }).catch(() => {})
    }
  }, [canManage])

  const openCreate = () => {
    setForm({ ...emptyForm(), createdBy: user?.id || 0 })
    setSelectedClassIds([])
    setSubjectExamTypes([])
    setEditId(null); setError(''); setModal('create')
  }

  const openEdit = async (e: ExamResponse) => {
    const toInputDt = (d: string) => d ? new Date(d).toISOString().slice(0, 16) : ''
    setForm({
      title: e.title, subjectId: e.subjectId, createdBy: e.createdBy,
      durationMinutes: e.durationMinutes, description: e.description,
      startTime: toInputDt(e.startTime), endTime: toInputDt(e.endTime),
      subjectExamTypeId: e.subjectExamTypeId || null,
    })
    setEditId(e.id); setError(''); setModal('edit')
    try {
      const res = await examsApi.getClasses(e.id)
      const classes = res.data?.data?.classes || []
      setSelectedClassIds(classes.map((c: { classId: number }) => c.classId))
    } catch { setSelectedClassIds([]) }
    if (e.subjectId) {
      try {
        const etRes = await subjectExamTypesApi.getBySubject(e.subjectId)
        setSubjectExamTypes(etRes.data.data || [])
      } catch { setSubjectExamTypes([]) }
    }
  }

  const toggleClass = (classId: number) => {
    setSelectedClassIds(prev =>
      prev.includes(classId) ? prev.filter(id => id !== classId) : [...prev, classId]
    )
  }

  const handleSave = async () => {
    if (!form.title || !form.subjectId || !form.startTime || !form.endTime) {
      setError('Vui lòng điền đầy đủ thông tin bắt buộc'); return
    }
    if (selectedClassIds.length === 0) {
      setError('Vui lòng chọn ít nhất 1 lớp thi'); return
    }
    setSaving(true); setError('')
    try {
      let examId: number
      if (modal === 'create') {
        const res = await examsApi.create({ ...form, startTime: new Date(form.startTime).toISOString(), endTime: new Date(form.endTime).toISOString() })
        examId = res.data.data?.id || 0
      } else {
        examId = editId!
        await examsApi.update(editId!, { ...form, startTime: new Date(form.startTime).toISOString(), endTime: new Date(form.endTime).toISOString() })
      }
      if (examId) {
        try {
          const existingRes = await examsApi.getClasses(examId)
          const existing: number[] = (existingRes.data?.data?.classes || []).map((c: { classId: number }) => c.classId)
          const toAdd = selectedClassIds.filter(id => !existing.includes(id))
          const toRemove = existing.filter((id: number) => !selectedClassIds.includes(id))
          for (const cid of toAdd) { await examsApi.assignClass(examId, cid) }
          for (const cid of toRemove) { await examsApi.removeClass(examId, cid) }
        } catch { /* best-effort */ }
      }
      setModal(null); fetchExams()
    } catch (e: unknown) {
      const err = e as { response?: { data?: { message?: string } } }
      setError(err.response?.data?.message || 'Lỗi lưu kỳ thi')
    } finally { setSaving(false) }
  }

  const handleDelete = async (id: number) => {
    try { await examsApi.delete(id); fetchExams() }
    catch (e: unknown) {
      const err = e as { response?: { data?: { message?: string } } }
      alert(err.response?.data?.message || 'Không thể xóa kỳ thi này (chỉ xóa được kỳ thi ở trạng thái Nháp)')
    }
    finally { setDeleteConfirm(null) }
  }

  const handleChangeStatus = async (id: number, status: string) => {
    try {
      if (status === 'ACTIVE') await examsApi.activate(id)
      else if (status === 'CLOSED') await examsApi.close(id)
      fetchExams()
    }
    catch { alert('Không thể thay đổi trạng thái kỳ thi') }
  }

  const totalPages = Math.ceil(total / pageSize)
  const fmtDate = (d: string) => d ? new Date(d).toLocaleString('vi-VN', { dateStyle: 'short', timeStyle: 'short' }) : '—'

  /* ───── Student view ───── */
  if (isStudent) {
    return <StudentExamView user={user} navigate={navigate} />
  }

  /* ───── Admin / Teacher view ───── */
  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 20 }}>
        <div>
          <h2 style={{ marginBottom: 2 }}>Quản lý Kỳ thi</h2>
          <p style={{ fontSize: 13 }}>{total} kỳ thi trong hệ thống</p>
        </div>
        <div style={{ display: 'flex', gap: 8 }}>
          <button className={`btn ${viewMode === 'card' ? 'btn-primary' : 'btn-secondary'}`} onClick={() => setViewMode('card')}>
            <span className="material-icons" style={{ fontSize: 18 }}>grid_view</span>
          </button>
          <button className={`btn ${viewMode === 'list' ? 'btn-primary' : 'btn-secondary'}`} onClick={() => setViewMode('list')}>
            <span className="material-icons" style={{ fontSize: 18 }}>list</span>
          </button>
          <button className="btn btn-primary" onClick={openCreate}>
            <span className="material-icons" style={{ fontSize: 18 }}>add</span>
            Tạo kỳ thi
          </button>
        </div>
      </div>

      <div className="search-bar" style={{ marginBottom: 16, display: 'flex', gap: 10, flexWrap: 'wrap' }}>
        <div className="input-group search-input">
          <span className="material-icons input-icon">search</span>
          <input className="form-control" placeholder="Tìm kỳ thi..." value={search} onChange={e => { setSearch(e.target.value); setPage(1) }} />
        </div>
        <select className="form-control" style={{ minWidth: 180, width: 'auto' }} value={filterSubject} onChange={e => { setFilterSubject(Number(e.target.value)); setPage(1) }}>
          <option value={0}>Tất cả môn</option>
          {subjects.map(s => <option key={s.id} value={s.id}>{s.name}</option>)}
        </select>
        <select className="form-control" style={{ minWidth: 200, width: 'auto' }} value={filterTeacher} onChange={e => { setFilterTeacher(Number(e.target.value)); setPage(1) }}>
          <option value={0}>Tất cả giáo viên</option>
          {teachers.map(t => <option key={t.id} value={t.id}>{t.fullName}</option>)}
        </select>
      </div>

      {loading ? (
        <div className="loading-center"><div className="spinner" /></div>
      ) : exams.length === 0 ? (
        <div className="empty-state">
          <span className="material-icons">edit_calendar</span>
          <p>Chưa có kỳ thi nào</p>
          <button className="btn btn-primary btn-sm" onClick={openCreate}>Tạo kỳ thi đầu tiên</button>
        </div>
      ) : viewMode === 'card' ? (
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(300px, 1fr))', gap: 16 }}>
          {exams.map(exam => (
            <div key={exam.id} className="card" style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
                <div style={{ flex: 1 }}>
                  <Link to={`/exams/${exam.id}`} style={{ fontWeight: 600, fontSize: 15, color: 'var(--text)', textDecoration: 'none' }}>{exam.title}</Link>
                  <div style={{ fontSize: 12, color: 'var(--text-muted)', marginTop: 2 }}>
                    {exam.subjectName}
                    {exam.subjectExamTypeName && <span className="badge badge-blue" style={{ marginLeft: 6, fontSize: 10 }}>{exam.subjectExamTypeName} (x{exam.subjectExamTypeCoefficient})</span>}
                  </div>
                </div>
                {statusBadge(exam.status)}
              </div>
              <div style={{ display: 'flex', gap: 16, fontSize: 12, color: 'var(--text-secondary)' }}>
                <span style={{ display: 'flex', alignItems: 'center', gap: 4 }}>
                  <span className="material-icons" style={{ fontSize: 14 }}>schedule</span>
                  {exam.durationMinutes} phút
                </span>
                <span style={{ display: 'flex', alignItems: 'center', gap: 4 }}>
                  <span className="material-icons" style={{ fontSize: 14 }}>event</span>
                  {fmtDate(exam.startTime)}
                </span>
              </div>
              {exam.description && <p style={{ fontSize: 12, margin: 0 }}>{exam.description}</p>}
              <div style={{ display: 'flex', gap: 6, marginTop: 'auto' }}>
                <Link to={`/exams/${exam.id}`} className="btn btn-secondary btn-sm" style={{ flex: 1, justifyContent: 'center' }}>
                  <span className="material-icons" style={{ fontSize: 16 }}>visibility</span> Xem
                </Link>
                <button className="btn btn-secondary btn-sm" onClick={() => openEdit(exam)}>
                  <span className="material-icons" style={{ fontSize: 16 }}>edit</span>
                </button>
                {exam.status === 'DRAFT' && (
                  <button className="btn btn-success btn-sm" onClick={() => handleChangeStatus(exam.id, 'ACTIVE')}>
                    <span className="material-icons" style={{ fontSize: 16 }}>play_arrow</span>
                  </button>
                )}
                {exam.status === 'ACTIVE' && (
                  <button className="btn btn-secondary btn-sm" onClick={() => handleChangeStatus(exam.id, 'CLOSED')} style={{ color: 'var(--danger)' }}>
                    <span className="material-icons" style={{ fontSize: 16 }}>stop</span>
                  </button>
                )}
                {exam.status === 'DRAFT' && (
                  <button className="btn btn-secondary btn-sm" onClick={() => setDeleteConfirm(exam.id)} style={{ color: 'var(--danger)' }}>
                    <span className="material-icons" style={{ fontSize: 16 }}>delete</span>
                  </button>
                )}
              </div>
            </div>
          ))}
        </div>
      ) : (
        <div className="card">
          <div className="table-wrap">
            <table>
              <thead><tr><th>#</th><th>Tên kỳ thi</th><th>Môn học</th><th>Thời lượng</th><th>Bắt đầu</th><th>Kết thúc</th><th>Trạng thái</th><th></th></tr></thead>
              <tbody>
                {exams.map((exam, idx) => (
                  <tr key={exam.id}>
                    <td style={{ color: 'var(--text-muted)' }}>{(page - 1) * pageSize + idx + 1}</td>
                    <td><Link to={`/exams/${exam.id}`} style={{ fontWeight: 500, color: 'var(--primary)', textDecoration: 'none' }}>{exam.title}</Link></td>
                    <td>{exam.subjectName || '—'}</td>
                    <td>{exam.durationMinutes} phút</td>
                    <td style={{ fontSize: 12 }}>{fmtDate(exam.startTime)}</td>
                    <td style={{ fontSize: 12 }}>{fmtDate(exam.endTime)}</td>
                    <td>{statusBadge(exam.status)}</td>
                    <td>
                      <div className="actions">
                        <button className="btn-icon btn" title="Sửa" onClick={() => openEdit(exam)}><span className="material-icons" style={{ fontSize: 18 }}>edit</span></button>
                        {exam.status === 'DRAFT' && (
                          <button className="btn-icon btn" title="Xóa" onClick={() => setDeleteConfirm(exam.id)} style={{ color: 'var(--danger)' }}><span className="material-icons" style={{ fontSize: 18 }}>delete</span></button>
                        )}
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {totalPages > 1 && (
        <div className="pagination" style={{ marginTop: 20 }}>
          <button className="page-btn" onClick={() => setPage(p => Math.max(1, p - 1))} disabled={page === 1}>
            <span className="material-icons" style={{ fontSize: 16 }}>chevron_left</span>
          </button>
          {Array.from({ length: Math.min(totalPages, 7) }, (_, i) => (
            <button key={i + 1} className={`page-btn${page === i + 1 ? ' active' : ''}`} onClick={() => setPage(i + 1)}>{i + 1}</button>
          ))}
          <button className="page-btn" onClick={() => setPage(p => Math.min(totalPages, p + 1))} disabled={page === totalPages}>
            <span className="material-icons" style={{ fontSize: 16 }}>chevron_right</span>
          </button>
        </div>
      )}

      {/* Create/Edit Modal */}
      {modal && (
        <div className="modal-overlay" onClick={e => e.target === e.currentTarget && setModal(null)}>
          <div className="modal" style={{ maxWidth: 620 }}>
            <div className="modal-header">
              <h3>{modal === 'create' ? 'Tạo kỳ thi mới' : 'Cập nhật kỳ thi'}</h3>
              <button className="btn btn-icon" onClick={() => setModal(null)}><span className="material-icons">close</span></button>
            </div>
            <div className="modal-body">
              {error && <div className="alert alert-error"><span className="material-icons" style={{ fontSize: 18 }}>error</span>{error}</div>}
              <div className="form-group">
                <label className="form-label">Tên kỳ thi *</label>
                <input className="form-control" value={form.title} onChange={e => setForm(f => ({ ...f, title: e.target.value }))} placeholder="Kiểm tra giữa kỳ Toán 12" />
              </div>
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '0 16px' }}>
                <div className="form-group">
                  <label className="form-label">Môn học *</label>
                  <select className="form-control" value={form.subjectId} onChange={async e => {
                    const sid = Number(e.target.value)
                    setForm(f => ({ ...f, subjectId: sid, subjectExamTypeId: null }))
                    if (sid) {
                      try {
                        const res = await subjectExamTypesApi.getBySubject(sid)
                        setSubjectExamTypes(res.data.data || [])
                      } catch { setSubjectExamTypes([]) }
                    } else { setSubjectExamTypes([]) }
                  }}>
                    <option value={0}>Chọn môn học</option>
                    {subjects.map(s => <option key={s.id} value={s.id}>{s.name}</option>)}
                  </select>
                </div>
                <div className="form-group">
                  <label className="form-label">Loại bài kiểm tra</label>
                  <select className="form-control" value={form.subjectExamTypeId || ''} onChange={e => setForm(f => ({ ...f, subjectExamTypeId: e.target.value ? Number(e.target.value) : null }))}>
                    <option value="">Không (thi thử / kiểm tra thử)</option>
                    {subjectExamTypes.sort((a, b) => a.sortOrder - b.sortOrder).map(et => (
                      <option key={et.id} value={et.id}>{et.name} (hệ số {et.coefficient})</option>
                    ))}
                  </select>
                </div>
                <div className="form-group">
                  <label className="form-label">Thời lượng (phút) *</label>
                  <input className="form-control" type="number" min={1} max={600} value={form.durationMinutes} onChange={e => setForm(f => ({ ...f, durationMinutes: Number(e.target.value) }))} />
                </div>
                <div className="form-group">
                  <label className="form-label">Bắt đầu *</label>
                  <input className="form-control" type="datetime-local" value={form.startTime} onChange={e => setForm(f => ({ ...f, startTime: e.target.value }))} />
                </div>
                <div className="form-group">
                  <label className="form-label">Kết thúc *</label>
                  <input className="form-control" type="datetime-local" value={form.endTime} onChange={e => setForm(f => ({ ...f, endTime: e.target.value }))} />
                </div>
              </div>

              <div className="form-group">
                <label className="form-label">Lớp thi * <span style={{ fontWeight: 400, color: 'var(--text-muted)' }}>({selectedClassIds.length} lớp đã chọn)</span></label>
                <div style={{ border: '1.5px solid var(--border)', borderRadius: 'var(--radius)', maxHeight: 160, overflowY: 'auto', padding: 8 }}>
                  {allClasses.length === 0 ? (
                    <div style={{ padding: 8, color: 'var(--text-muted)', fontSize: 13 }}>Chưa có lớp nào</div>
                  ) : allClasses.map(c => (
                    <label key={c.id} style={{
                      display: 'flex', alignItems: 'center', gap: 8, padding: '6px 8px',
                      borderRadius: 6, cursor: 'pointer', fontSize: 13,
                      background: selectedClassIds.includes(c.id) ? 'var(--primary-light)' : 'transparent'
                    }}>
                      <input type="checkbox" checked={selectedClassIds.includes(c.id)} onChange={() => toggleClass(c.id)}
                        style={{ width: 16, height: 16, accentColor: 'var(--primary)' }} />
                      <span style={{ fontWeight: 500 }}>{c.name}</span>
                      <span style={{ color: 'var(--text-muted)' }}>({c.code})</span>
                      {c.studentCount > 0 && <span className="badge badge-blue" style={{ marginLeft: 'auto', fontSize: 11 }}>{c.studentCount} HS</span>}
                    </label>
                  ))}
                </div>
              </div>

              <div className="form-group">
                <label className="form-label">Mô tả</label>
                <textarea className="form-control" rows={2} value={form.description || ''} onChange={e => setForm(f => ({ ...f, description: e.target.value }))} placeholder="Mô tả kỳ thi (tùy chọn)" style={{ resize: 'vertical' }} />
              </div>
            </div>
            <div className="modal-footer">
              <button className="btn btn-secondary" onClick={() => setModal(null)}>Hủy</button>
              <button className="btn btn-primary" onClick={handleSave} disabled={saving}>
                {saving ? 'Đang lưu...' : modal === 'create' ? 'Tạo kỳ thi' : 'Cập nhật'}
              </button>
            </div>
          </div>
        </div>
      )}

      {deleteConfirm !== null && (
        <div className="modal-overlay">
          <div className="modal" style={{ maxWidth: 380 }}>
            <div className="modal-header">
              <h3>Xác nhận xóa</h3>
              <button className="btn btn-icon" onClick={() => setDeleteConfirm(null)}><span className="material-icons">close</span></button>
            </div>
            <div className="modal-body"><p>Bạn có chắc muốn xóa kỳ thi này? Tất cả dữ liệu liên quan sẽ bị xóa.</p></div>
            <div className="modal-footer">
              <button className="btn btn-secondary" onClick={() => setDeleteConfirm(null)}>Hủy</button>
              <button className="btn btn-danger" onClick={() => handleDelete(deleteConfirm)}>Xóa</button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
