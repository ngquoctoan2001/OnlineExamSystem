import { useState, useEffect, useCallback } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { classesApi } from '../api/classes'
import { studentsApi } from '../api/students'
import { teachingAssignmentsApi } from '../api/teachingAssignments'
import type { ClassResponse, StudentResponse } from '../types/api'

interface TeachingAssignment {
  id: number
  teacherId: number
  teacherName: string
  classId: number
  className: string
  subjectId: number
  subjectName: string
  academicYear: string
  semester: number
}

export default function ClassDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const classId = Number(id)

  const [classInfo, setClassInfo] = useState<ClassResponse | null>(null)
  const [students, setStudents] = useState<any[]>([])
  const [assignments, setAssignments] = useState<TeachingAssignment[]>([])
  const [loading, setLoading] = useState(true)
  const [tab, setTab] = useState<'students' | 'teachers'>('students')

  // Add student modal
  const [showAddStudent, setShowAddStudent] = useState(false)
  const [allStudents, setAllStudents] = useState<StudentResponse[]>([])
  const [studentSearch, setStudentSearch] = useState('')
  const [addingStudent, setAddingStudent] = useState(false)

  // Transfer student modal
  const [showTransfer, setShowTransfer] = useState(false)
  const [transferStudent, setTransferStudent] = useState<{ id: number; name: string } | null>(null)
  const [allClasses, setAllClasses] = useState<ClassResponse[]>([])
  const [targetClassId, setTargetClassId] = useState<number | ''>('')
  const [transferring, setTransferring] = useState(false)

  const fetchData = useCallback(async () => {
    setLoading(true)
    try {
      const [classRes, studentsRes, assignRes] = await Promise.all([
        classesApi.getById(classId),
        classesApi.getStudents(classId),
        teachingAssignmentsApi.getByClass(classId).catch(() => ({ data: { data: [] } })),
      ])
      setClassInfo(classRes.data.data || null)
      const sList = (studentsRes.data?.data as any)?.students || studentsRes.data?.data || []
      setStudents(Array.isArray(sList) ? sList : [])
      const aList = (assignRes.data?.data as any)?.assignments || assignRes.data?.data || []
      setAssignments(Array.isArray(aList) ? aList : [])
    } catch { /* ignore */ }
    finally { setLoading(false) }
  }, [classId])

  useEffect(() => { fetchData() }, [fetchData])

  const handleRemoveStudent = async (studentId: number) => {
    if (!confirm('Xác nhận xóa học sinh khỏi lớp?')) return
    try {
      await classesApi.removeStudent(classId, studentId)
      fetchData()
    } catch { alert('Không thể xóa học sinh khỏi lớp') }
  }

  const openAddStudent = async () => {
    setShowAddStudent(true)
    setStudentSearch('')
    try {
      const res = await studentsApi.getAll(1, 500)
      const all = res.data.data?.students || []
      const currentIds = new Set(students.map((s: any) => s.studentId || s.id))
      setAllStudents(all.filter((s: StudentResponse) => !currentIds.has(s.id)))
    } catch { setAllStudents([]) }
  }

  const handleAddStudent = async (studentId: number) => {
    setAddingStudent(true)
    try {
      await classesApi.addStudent(classId, studentId)
      setShowAddStudent(false)
      fetchData()
    } catch (e: any) {
      alert(e.response?.data?.message || 'Không thể thêm học sinh vào lớp')
    } finally { setAddingStudent(false) }
  }

  const openTransfer = async (studentId: number, studentName: string) => {
    setTransferStudent({ id: studentId, name: studentName })
    setTargetClassId('')
    setShowTransfer(true)
    try {
      const res = await classesApi.getAll(1, 200)
      const classes = res.data?.data?.classes || []
      setAllClasses(classes.filter((c: ClassResponse) => c.id !== classId))
    } catch { setAllClasses([]) }
  }

  const handleTransfer = async () => {
    if (!transferStudent || !targetClassId) return
    setTransferring(true)
    try {
      // Remove from current class
      await classesApi.removeStudent(classId, transferStudent.id)
      // Add to target class
      await classesApi.addStudent(Number(targetClassId), transferStudent.id)
      setShowTransfer(false)
      setTransferStudent(null)
      fetchData()
    } catch (e: any) {
      alert(e.response?.data?.message || 'Không thể chuyển lớp học sinh')
    } finally { setTransferring(false) }
  }

  const filteredAllStudents = studentSearch
    ? allStudents.filter(s => s.fullName.toLowerCase().includes(studentSearch.toLowerCase()) || s.studentCode.toLowerCase().includes(studentSearch.toLowerCase()))
    : allStudents

  if (loading) return <div className="loading-center"><div className="spinner" /></div>
  if (!classInfo) return <div className="empty-state"><span className="material-icons">error</span><p>Không tìm thấy lớp học</p></div>

  return (
    <div>
      {/* Breadcrumb */}
      <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 20, fontSize: 13 }}>
        <span style={{ color: 'var(--primary)', cursor: 'pointer' }} onClick={() => navigate('/classes')}>Lớp học</span>
        <span className="material-icons" style={{ fontSize: 16, color: 'var(--text-muted)' }}>chevron_right</span>
        <span>{classInfo.name}</span>
      </div>

      {/* Class info header */}
      <div className="card" style={{ marginBottom: 20 }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
          <div>
            <h2 style={{ marginBottom: 4 }}>{classInfo.name}</h2>
            <div style={{ display: 'flex', gap: 16, fontSize: 13, color: 'var(--text-secondary)' }}>
              <span>Khối: <strong>{classInfo.grade}</strong></span>
              <span>GVCN: <strong>{classInfo.homeroomTeacherName || 'Chưa phân công'}</strong></span>
            </div>
          </div>
          <div style={{ display: 'flex', gap: 16 }}>
            <div style={{ textAlign: 'center', padding: '8px 16px', background: 'var(--bg)', borderRadius: 8 }}>
              <div style={{ fontSize: 22, fontWeight: 700, color: 'var(--primary)' }}>{students.length}</div>
              <div style={{ fontSize: 11, color: 'var(--text-muted)' }}>Học sinh</div>
            </div>
            <div style={{ textAlign: 'center', padding: '8px 16px', background: 'var(--bg)', borderRadius: 8 }}>
              <div style={{ fontSize: 22, fontWeight: 700, color: 'var(--primary)' }}>{assignments.length}</div>
              <div style={{ fontSize: 11, color: 'var(--text-muted)' }}>GV bộ môn</div>
            </div>
          </div>
        </div>
      </div>

      {/* Tabs */}
      <div style={{ display: 'flex', gap: 8, marginBottom: 16 }}>
        <button className={`btn ${tab === 'students' ? 'btn-primary' : ''}`} onClick={() => setTab('students')}>
          <span className="material-icons" style={{ fontSize: 16 }}>people</span> Danh sách học sinh
        </button>
        <button className={`btn ${tab === 'teachers' ? 'btn-primary' : ''}`} onClick={() => setTab('teachers')}>
          <span className="material-icons" style={{ fontSize: 16 }}>school</span> Giáo viên bộ môn
        </button>
      </div>

      {/* Students Tab */}
      {tab === 'students' && (
        <div className="card">
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
            <h3 style={{ margin: 0 }}>Học sinh ({students.length})</h3>
            <button className="btn btn-primary" onClick={openAddStudent}>
              <span className="material-icons" style={{ fontSize: 16 }}>person_add</span> Thêm học sinh
            </button>
          </div>
          {students.length === 0 ? (
            <div className="empty-state">
              <span className="material-icons">people_outline</span>
              <p>Chưa có học sinh nào trong lớp</p>
            </div>
          ) : (
            <div className="table-wrap">
              <table>
                <thead>
                  <tr>
                    <th>#</th>
                    <th>Họ tên</th>
                    <th>Mã học sinh</th>
                    <th>Email</th>
                    <th></th>
                  </tr>
                </thead>
                <tbody>
                  {students.map((s: any, idx: number) => (
                    <tr key={s.studentId || s.id}>
                      <td style={{ color: 'var(--text-muted)' }}>{idx + 1}</td>
                      <td style={{ fontWeight: 500 }}>{s.fullName || s.studentName}</td>
                      <td><span className="badge badge-blue">{s.studentCode || s.code || '-'}</span></td>
                      <td style={{ color: 'var(--text-muted)', fontSize: 13 }}>{s.email || '-'}</td>
                      <td>
                        <div style={{ display: 'flex', gap: 4 }}>
                          <button className="btn-icon btn" title="Chuyển lớp" onClick={() => openTransfer(s.studentId || s.id, s.fullName || s.studentName)} style={{ color: 'var(--primary)' }}>
                            <span className="material-icons" style={{ fontSize: 18 }}>swap_horiz</span>
                          </button>
                          <button className="btn-icon btn" title="Xóa khỏi lớp" onClick={() => handleRemoveStudent(s.studentId || s.id)} style={{ color: 'var(--danger)' }}>
                            <span className="material-icons" style={{ fontSize: 18 }}>person_remove</span>
                          </button>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      )}

      {/* Teachers Tab */}
      {tab === 'teachers' && (
        <div className="card">
          <h3 style={{ margin: '0 0 16px' }}>Giáo viên bộ môn ({assignments.length})</h3>
          {assignments.length === 0 ? (
            <div className="empty-state">
              <span className="material-icons">school</span>
              <p>Chưa có phân công giáo viên bộ môn</p>
            </div>
          ) : (
            <div className="table-wrap">
              <table>
                <thead>
                  <tr>
                    <th>#</th>
                    <th>Giáo viên</th>
                    <th>Môn học</th>
                    <th>Năm học</th>
                    <th>Học kỳ</th>
                  </tr>
                </thead>
                <tbody>
                  {assignments.map((a, idx) => (
                    <tr key={a.id}>
                      <td style={{ color: 'var(--text-muted)' }}>{idx + 1}</td>
                      <td style={{ fontWeight: 500 }}>{a.teacherName}</td>
                      <td><span className="badge badge-green">{a.subjectName}</span></td>
                      <td>{a.academicYear}</td>
                      <td>HK{a.semester}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      )}

      {/* Add Student Modal */}
      {showAddStudent && (
        <div className="modal-overlay" onClick={e => e.target === e.currentTarget && setShowAddStudent(false)}>
          <div className="modal" style={{ maxWidth: 500 }}>
            <div className="modal-header">
              <h3>Thêm học sinh vào lớp</h3>
              <button className="btn btn-icon" onClick={() => setShowAddStudent(false)}><span className="material-icons">close</span></button>
            </div>
            <div className="modal-body">
              <div className="form-group">
                <div className="input-group search-input">
                  <span className="material-icons input-icon">search</span>
                  <input className="form-control" placeholder="Tìm học sinh..." value={studentSearch} onChange={e => setStudentSearch(e.target.value)} autoFocus />
                </div>
              </div>
              <div style={{ maxHeight: 300, overflowY: 'auto' }}>
                {filteredAllStudents.length === 0 ? (
                  <div style={{ textAlign: 'center', padding: 20, color: 'var(--text-muted)', fontSize: 13 }}>Không tìm thấy học sinh</div>
                ) : filteredAllStudents.map(s => (
                  <div key={s.id} style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '8px 0', borderBottom: '1px solid var(--border)' }}>
                    <div>
                      <div style={{ fontWeight: 500, fontSize: 14 }}>{s.fullName}</div>
                      <div style={{ fontSize: 12, color: 'var(--text-muted)' }}>{s.studentCode}</div>
                    </div>
                    <button className="btn btn-primary" style={{ padding: '4px 12px', fontSize: 12 }} onClick={() => handleAddStudent(s.id)} disabled={addingStudent}>
                      <span className="material-icons" style={{ fontSize: 14 }}>add</span> Thêm
                    </button>
                  </div>
                ))}
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Transfer Student Modal */}
      {showTransfer && transferStudent && (
        <div className="modal-overlay" onClick={e => e.target === e.currentTarget && setShowTransfer(false)}>
          <div className="modal" style={{ maxWidth: 450 }}>
            <div className="modal-header">
              <h3>Chuyển lớp học sinh</h3>
              <button className="btn btn-icon" onClick={() => setShowTransfer(false)}><span className="material-icons">close</span></button>
            </div>
            <div className="modal-body">
              <div style={{ marginBottom: 16, padding: 12, background: 'var(--bg)', borderRadius: 8 }}>
                <div style={{ fontSize: 13, color: 'var(--text-muted)' }}>Học sinh</div>
                <div style={{ fontWeight: 600 }}>{transferStudent.name}</div>
                <div style={{ fontSize: 13, color: 'var(--text-muted)', marginTop: 4 }}>
                  Từ lớp: <strong>{classInfo.name}</strong>
                </div>
              </div>
              <div className="form-group">
                <label className="form-label">Chuyển đến lớp</label>
                <select className="form-control" value={targetClassId} onChange={e => setTargetClassId(Number(e.target.value) || '')}>
                  <option value="">-- Chọn lớp đích --</option>
                  {allClasses.map(c => (
                    <option key={c.id} value={c.id}>{c.name} ({c.code})</option>
                  ))}
                </select>
              </div>
            </div>
            <div className="modal-footer">
              <button className="btn btn-secondary" onClick={() => setShowTransfer(false)}>Hủy</button>
              <button className="btn btn-primary" onClick={handleTransfer} disabled={!targetClassId || transferring}>
                {transferring ? <span className="spinner" style={{ width: 16, height: 16 }} /> : (
                  <><span className="material-icons" style={{ fontSize: 16 }}>swap_horiz</span> Chuyển lớp</>
                )}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
