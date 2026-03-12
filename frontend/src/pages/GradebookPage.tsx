import { useState, useEffect } from 'react'
import { useAuth } from '../contexts/AuthContext'
import { gradebookApi } from '../api/gradebook'
import { classesApi } from '../api/classes'
import { subjectsApi } from '../api/subjects'
import { studentsApi } from '../api/students'
import type { StudentFullGradebookResponse, ClassSubjectGradebookResponse, SubjectResponse, ClassResponse, StudentResponse } from '../types/api'

const fmtScore = (v?: number) => v != null ? v.toFixed(2) : '—'

export default function GradebookPage() {
  const { user } = useAuth()
  const role = user?.role?.toUpperCase() || ''
  const isStudent = role === 'STUDENT'

  if (isStudent) {
    const studentId = user?.studentId
    if (!studentId) return <div className="empty-state"><p>Đang tải thông tin học sinh...</p></div>
    return <StudentGradebookView studentId={studentId} studentName={user?.fullName || ''} />
  }
  return <TeacherGradebookView />
}

/* ═══════════════════════════════════════════════════════════════
   STUDENT GRADEBOOK VIEW
   ═══════════════════════════════════════════════════════════════ */
function StudentGradebookView({ studentId, studentName }: { studentId: number; studentName: string }) {
  const [data, setData] = useState<StudentFullGradebookResponse | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    if (!studentId) return
    setLoading(true)
    gradebookApi.getStudentGradebook(studentId)
      .then(r => setData(r.data.data || null))
      .catch(() => setData(null))
      .finally(() => setLoading(false))
  }, [studentId])

  if (!studentId) return <div className="empty-state"><p>Đang tải thông tin học sinh...</p></div>

  return (
    <div>
      <div style={{ marginBottom: 24 }}>
        <h2 style={{ marginBottom: 4 }}>
          <span className="material-icons" style={{ fontSize: 28, verticalAlign: 'middle', marginRight: 8, color: 'var(--primary)' }}>grade</span>
          Bảng điểm cá nhân
        </h2>
        <p style={{ fontSize: 13, color: 'var(--text-muted)' }}>
          Xin chào <strong>{studentName}</strong> — Xem điểm tất cả các môn học
        </p>
      </div>

      {loading ? (
        <div className="loading-center"><div className="spinner" /></div>
      ) : !data || data.subjects.length === 0 ? (
        <div className="empty-state">
          <span className="material-icons" style={{ fontSize: 48 }}>school</span>
          <p>Chưa có dữ liệu điểm nào</p>
        </div>
      ) : (
        <>
          {/* Overall summary */}
          <div className="card" style={{ marginBottom: 24, background: 'linear-gradient(135deg, var(--primary-light), #e0e7ff)', padding: 20 }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <div>
                <div style={{ fontSize: 13, color: 'var(--text-muted)' }}>Điểm trung bình tổng</div>
                <div style={{ fontSize: 32, fontWeight: 700, color: 'var(--primary)' }}>{fmtScore(data.overallAverage)}</div>
              </div>
              <div style={{ textAlign: 'right' }}>
                <div style={{ fontSize: 13, color: 'var(--text-muted)' }}>Số môn học</div>
                <div style={{ fontSize: 24, fontWeight: 600 }}>{data.subjects.length}</div>
              </div>
            </div>
          </div>

          {/* Subject cards */}
          {data.subjects.map(subj => (
            <div key={subj.subjectId} className="card" style={{ marginBottom: 16 }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 12 }}>
                <h3 style={{ margin: 0 }}>
                  <span className="material-icons" style={{ fontSize: 20, verticalAlign: 'middle', marginRight: 6 }}>library_books</span>
                  {subj.subjectName}
                </h3>
                <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                  <span style={{ fontSize: 13, color: 'var(--text-muted)' }}>TB Môn:</span>
                  <span className={`badge ${subj.weightedAverage != null && subj.weightedAverage >= 5 ? 'badge-green' : 'badge-red'}`}
                    style={{ fontSize: 14, padding: '4px 12px' }}>
                    {fmtScore(subj.weightedAverage)}
                  </span>
                </div>
              </div>
              {subj.entries.length === 0 ? (
                <p style={{ fontSize: 13, color: 'var(--text-muted)' }}>Chưa có bài kiểm tra nào</p>
              ) : (
                <table className="table">
                  <thead>
                    <tr>
                      <th>Bài kiểm tra</th>
                      <th>Loại</th>
                      <th>Hệ số</th>
                      <th>Điểm gốc</th>
                      <th>Điểm /10</th>
                      <th>Trạng thái</th>
                    </tr>
                  </thead>
                  <tbody>
                    {subj.entries.map(e => (
                      <tr key={e.examId}>
                        <td><strong>{e.examTitle}</strong></td>
                        <td>{e.subjectExamTypeName || <span style={{ color: 'var(--text-muted)', fontStyle: 'italic' }}>Thi thử</span>}</td>
                        <td><span className="badge badge-blue">x{e.coefficient}</span></td>
                        <td>{e.score != null ? `${e.score}/${e.totalPoints}` : '—'}</td>
                        <td style={{ fontWeight: 600, color: e.scoreOn10 != null && e.scoreOn10 >= 5 ? 'var(--success)' : e.scoreOn10 != null ? 'var(--danger)' : undefined }}>
                          {fmtScore(e.scoreOn10)}
                        </td>
                        <td>{statusLabel(e.status)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              )}
            </div>
          ))}
        </>
      )}
    </div>
  )
}

/* ═══════════════════════════════════════════════════════════════
   TEACHER / ADMIN GRADEBOOK VIEW
   ═══════════════════════════════════════════════════════════════ */
function TeacherGradebookView() {
  const [tab, setTab] = useState<'student' | 'class'>('class')
  const [classes, setClasses] = useState<ClassResponse[]>([])
  const [subjects, setSubjects] = useState<SubjectResponse[]>([])
  const [students, setStudents] = useState<StudentResponse[]>([])
  const [loading, setLoading] = useState(false)

  // Class view state
  const [selectedClass, setSelectedClass] = useState(0)
  const [selectedSubject, setSelectedSubject] = useState(0)
  const [classGradebook, setClassGradebook] = useState<ClassSubjectGradebookResponse | null>(null)

  // Student view state
  const [selectedStudentId, setSelectedStudentId] = useState(0)
  const [studentGradebook, setStudentGradebook] = useState<StudentFullGradebookResponse | null>(null)
  const [studentSearch, setStudentSearch] = useState('')

  useEffect(() => {
    classesApi.getAll(1, 200).then(r => {
      const d = r.data.data
      setClasses(d?.classes || (Array.isArray(d) ? d : []))
    }).catch(() => {})
    subjectsApi.getAll(1, 200).then(r => {
      const d = r.data.data
      setSubjects(d?.subjects || (Array.isArray(d) ? d : []))
    }).catch(() => {})
    studentsApi.getAll(1, 500).then(r => {
      const d = r.data.data
      setStudents(d?.students || (Array.isArray(d) ? d : []))
    }).catch(() => {})
  }, [])

  const loadClassGradebook = async () => {
    if (!selectedClass || !selectedSubject) return
    setLoading(true); setClassGradebook(null)
    try {
      const res = await gradebookApi.getClassSubjectGradebook(selectedClass, selectedSubject)
      setClassGradebook(res.data.data || null)
    } catch { setClassGradebook(null) }
    finally { setLoading(false) }
  }

  const loadStudentGradebook = async (studentId: number) => {
    if (!studentId) return
    setSelectedStudentId(studentId)
    setLoading(true); setStudentGradebook(null)
    try {
      const res = await gradebookApi.getStudentGradebook(studentId)
      setStudentGradebook(res.data.data || null)
    } catch { setStudentGradebook(null) }
    finally { setLoading(false) }
  }

  const filteredStudents = studentSearch.trim()
    ? students.filter(s =>
        s.fullName.toLowerCase().includes(studentSearch.toLowerCase()) ||
        s.studentCode.toLowerCase().includes(studentSearch.toLowerCase()))
    : students

  const tabs = [
    { key: 'class' as const, label: 'Điểm theo lớp', icon: 'groups' },
    { key: 'student' as const, label: 'Điểm cá nhân', icon: 'person' },
  ]

  return (
    <div>
      <div style={{ marginBottom: 24 }}>
        <h2 style={{ marginBottom: 4 }}>
          <span className="material-icons" style={{ fontSize: 28, verticalAlign: 'middle', marginRight: 8, color: 'var(--primary)' }}>grade</span>
          Bảng điểm
        </h2>
        <p style={{ fontSize: 13, color: 'var(--text-muted)' }}>Xem điểm theo lớp hoặc theo từng học sinh</p>
      </div>

      {/* Tabs */}
      <div style={{ display: 'flex', gap: 4, marginBottom: 20, borderBottom: '2px solid var(--border)' }}>
        {tabs.map(t => (
          <button key={t.key} onClick={() => setTab(t.key)} style={{
            display: 'flex', alignItems: 'center', gap: 6, padding: '10px 20px', border: 'none', background: 'none', cursor: 'pointer',
            fontSize: 14, fontWeight: tab === t.key ? 600 : 400, color: tab === t.key ? 'var(--primary)' : 'var(--text-secondary)',
            borderBottom: tab === t.key ? '2px solid var(--primary)' : '2px solid transparent', marginBottom: -2,
          }}>
            <span className="material-icons" style={{ fontSize: 18 }}>{t.icon}</span>
            {t.label}
          </button>
        ))}
      </div>

      {/* Class Gradebook Tab */}
      {tab === 'class' && (
        <div>
          <div className="card" style={{ marginBottom: 20 }}>
            <div style={{ display: 'flex', gap: 12, alignItems: 'end', flexWrap: 'wrap' }}>
              <div className="form-group" style={{ marginBottom: 0, flex: 1, minWidth: 180 }}>
                <label className="form-label">Lớp</label>
                <select className="form-control" value={selectedClass} onChange={e => setSelectedClass(Number(e.target.value))}>
                  <option value={0}>Chọn lớp</option>
                  {classes.map(c => <option key={c.id} value={c.id}>{c.name} ({c.code})</option>)}
                </select>
              </div>
              <div className="form-group" style={{ marginBottom: 0, flex: 1, minWidth: 180 }}>
                <label className="form-label">Môn học</label>
                <select className="form-control" value={selectedSubject} onChange={e => setSelectedSubject(Number(e.target.value))}>
                  <option value={0}>Chọn môn</option>
                  {subjects.map(s => <option key={s.id} value={s.id}>{s.name}</option>)}
                </select>
              </div>
              <button className="btn btn-primary" onClick={loadClassGradebook} disabled={!selectedClass || !selectedSubject || loading} style={{ whiteSpace: 'nowrap' }}>
                <span className="material-icons" style={{ fontSize: 16 }}>search</span>
                Xem điểm
              </button>
            </div>
          </div>

          {loading && <div className="loading-center"><div className="spinner" /></div>}

          {classGradebook && (
            <div className="card">
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
                <h3 style={{ margin: 0 }}>
                  Lớp {classGradebook.className} — {classGradebook.subjectName}
                </h3>
                <span className="badge badge-blue">{classGradebook.students.length} học sinh</span>
              </div>

              {classGradebook.students.length === 0 ? (
                <div className="empty-state"><p>Không có dữ liệu</p></div>
              ) : (
                <div style={{ overflowX: 'auto' }}>
                  <table className="table" style={{ minWidth: 800 }}>
                    <thead>
                      <tr>
                        <th style={{ position: 'sticky', left: 0, background: 'var(--surface)', zIndex: 1 }}>STT</th>
                        <th style={{ position: 'sticky', left: 40, background: 'var(--surface)', zIndex: 1 }}>Họ tên</th>
                        <th>Mã HS</th>
                        {classGradebook.examTypes.map(et => (
                          <th key={et.id} style={{ textAlign: 'center' }}>
                            <div>{et.name}</div>
                            <div style={{ fontSize: 10, color: 'var(--text-muted)' }}>(x{et.coefficient})</div>
                          </th>
                        ))}
                        <th style={{ textAlign: 'center' }}>Thi thử</th>
                        <th style={{ textAlign: 'center', fontWeight: 700 }}>TB Môn</th>
                      </tr>
                    </thead>
                    <tbody>
                      {classGradebook.students.map((s, idx) => {
                        const byType = new Map<number, number[]>()
                        const practice: number[] = []
                        s.entries.forEach(e => {
                          if (e.subjectExamTypeId && e.scoreOn10 != null) {
                            const arr = byType.get(e.subjectExamTypeId) || []
                            arr.push(e.scoreOn10)
                            byType.set(e.subjectExamTypeId, arr)
                          } else if (!e.subjectExamTypeId && e.scoreOn10 != null) {
                            practice.push(e.scoreOn10)
                          }
                        })
                        return (
                          <tr key={s.studentId}>
                            <td style={{ position: 'sticky', left: 0, background: '#fff', zIndex: 1 }}>{idx + 1}</td>
                            <td style={{ position: 'sticky', left: 40, background: '#fff', zIndex: 1 }}>
                              <button onClick={() => { setTab('student'); loadStudentGradebook(s.studentId) }}
                                style={{ background: 'none', border: 'none', cursor: 'pointer', color: 'var(--primary)', fontWeight: 500, textDecoration: 'underline', padding: 0 }}>
                                {s.studentName}
                              </button>
                            </td>
                            <td style={{ fontSize: 12 }}>{s.studentCode}</td>
                            {classGradebook.examTypes.map(et => {
                              const scores = byType.get(et.id) || []
                              return (
                                <td key={et.id} style={{ textAlign: 'center' }}>
                                  {scores.length > 0 ? scores.map((sc, i) => (
                                    <span key={i} style={{ display: 'inline-block', margin: '0 2px', fontWeight: 500, color: sc >= 5 ? 'var(--success)' : 'var(--danger)' }}>
                                      {sc.toFixed(1)}{i < scores.length - 1 ? ',' : ''}
                                    </span>
                                  )) : '—'}
                                </td>
                              )
                            })}
                            <td style={{ textAlign: 'center' }}>
                              {practice.length > 0 ? practice.map((sc, i) => (
                                <span key={i} style={{ display: 'inline-block', margin: '0 2px', color: 'var(--text-muted)' }}>
                                  {sc.toFixed(1)}{i < practice.length - 1 ? ',' : ''}
                                </span>
                              )) : '—'}
                            </td>
                            <td style={{ textAlign: 'center', fontWeight: 700, fontSize: 15, color: s.weightedAverage != null && s.weightedAverage >= 5 ? 'var(--success)' : s.weightedAverage != null ? 'var(--danger)' : undefined }}>
                              {fmtScore(s.weightedAverage)}
                            </td>
                          </tr>
                        )
                      })}
                    </tbody>
                  </table>
                </div>
              )}
            </div>
          )}
        </div>
      )}

      {/* Student Gradebook Tab */}
      {tab === 'student' && (
        <div>
          <div className="card" style={{ marginBottom: 20 }}>
            <div style={{ display: 'flex', gap: 12, alignItems: 'end', flexWrap: 'wrap' }}>
              <div className="form-group" style={{ marginBottom: 0, flex: 1, minWidth: 250 }}>
                <label className="form-label">Tìm học sinh</label>
                <input className="form-control" placeholder="Tên hoặc mã học sinh..." value={studentSearch} onChange={e => setStudentSearch(e.target.value)} />
              </div>
              <div className="form-group" style={{ marginBottom: 0, flex: 1, minWidth: 250 }}>
                <label className="form-label">Chọn học sinh</label>
                <select className="form-control" value={selectedStudentId} onChange={e => loadStudentGradebook(Number(e.target.value))}>
                  <option value={0}>Chọn học sinh</option>
                  {filteredStudents.map(s => <option key={s.id} value={s.id}>{s.fullName} ({s.studentCode})</option>)}
                </select>
              </div>
            </div>
          </div>

          {loading && <div className="loading-center"><div className="spinner" /></div>}

          {studentGradebook && selectedStudentId > 0 && (
            <StudentGradebookView studentId={selectedStudentId} studentName={studentGradebook.studentName} />
          )}
        </div>
      )}
    </div>
  )
}

/* ═══════════════════════════════════════════════════════════════
   HELPERS
   ═══════════════════════════════════════════════════════════════ */
function statusLabel(status: string) {
  switch (status) {
    case 'GRADED': return <span className="badge badge-green">Đã chấm</span>
    case 'PUBLISHED': return <span className="badge badge-green">Đã công bố</span>
    case 'SUBMITTED': return <span className="badge badge-blue">Đã nộp</span>
    case 'IN_PROGRESS': return <span className="badge badge-yellow">Đang làm</span>
    case 'NOT_ATTEMPTED': return <span className="badge badge-gray">Chưa làm</span>
    default: return <span className="badge badge-gray">{status}</span>
  }
}
