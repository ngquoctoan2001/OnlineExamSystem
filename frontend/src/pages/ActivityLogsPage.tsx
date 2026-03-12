import { useState, useEffect, useCallback } from 'react'
import { activityLogsApi } from '../api/activityLogs'

interface LogEntry {
  id: number
  userId: number | null
  action: string
  entityType: string | null
  entityId: number | null
  detail: string | null
  ipAddress: string | null
  occurredAt: string
}

export default function ActivityLogsPage() {
  const [logs, setLogs] = useState<LogEntry[]>([])
  const [total, setTotal] = useState(0)
  const [page, setPage] = useState(1)
  const [loading, setLoading] = useState(true)
  const [filterAction, setFilterAction] = useState('')
  const [filterFrom, setFilterFrom] = useState('')
  const [filterTo, setFilterTo] = useState('')
  const pageSize = 20

  const fetchLogs = useCallback(async () => {
    setLoading(true)
    try {
      const res = await activityLogsApi.getLogs({
        page, pageSize,
        action: filterAction || undefined,
        from: filterFrom || undefined,
        to: filterTo || undefined,
      })
      const data = res.data?.data
      setLogs((data?.logs || []) as unknown as LogEntry[])
      setTotal(data?.totalCount || 0)
    } catch { setLogs([]) }
    finally { setLoading(false) }
  }, [page, filterAction, filterFrom, filterTo])

  useEffect(() => { fetchLogs() }, [fetchLogs])

  const totalPages = Math.ceil(total / pageSize)

  const actionColor = (action: string) => {
    if (action.includes('CREATE') || action.includes('ADD')) return 'var(--success)'
    if (action.includes('DELETE') || action.includes('REMOVE')) return 'var(--danger)'
    if (action.includes('UPDATE') || action.includes('EDIT')) return 'var(--primary)'
    if (action.includes('LOGIN')) return '#8b5cf6'
    return 'var(--text-muted)'
  }

  return (
    <div className="page-content">
      <div className="page-toolbar">
        <div style={{ display: 'flex', gap: 12, alignItems: 'center', flexWrap: 'wrap' }}>
          <div className="input-group" style={{ width: 200 }}>
            <span className="input-icon material-icons">filter_list</span>
            <input
              className="form-control"
              placeholder="Lọc theo hành động..."
              value={filterAction}
              onChange={e => { setFilterAction(e.target.value); setPage(1) }}
            />
          </div>
          <div style={{ display: 'flex', alignItems: 'center', gap: 6, fontSize: 13 }}>
            <span style={{ color: 'var(--text-muted)' }}>Từ:</span>
            <input
              type="date"
              className="form-control"
              style={{ width: 160, padding: '6px 10px' }}
              value={filterFrom}
              onChange={e => { setFilterFrom(e.target.value); setPage(1) }}
            />
            <span style={{ color: 'var(--text-muted)' }}>Đến:</span>
            <input
              type="date"
              className="form-control"
              style={{ width: 160, padding: '6px 10px' }}
              value={filterTo}
              onChange={e => { setFilterTo(e.target.value); setPage(1) }}
            />
          </div>
        </div>
        <span style={{ fontSize: 13, color: 'var(--text-muted)' }}>Tổng: {total}</span>
      </div>

      {loading ? (
        <div className="loading-center"><div className="spinner" /></div>
      ) : logs.length === 0 ? (
        <div style={{ textAlign: 'center', padding: 48, color: 'var(--text-muted)' }}>
          <span className="material-icons" style={{ fontSize: 48 }}>receipt_long</span>
          <p>Chưa có nhật ký hoạt động</p>
        </div>
      ) : (
        <div className="table-wrapper">
          <table className="data-table">
            <thead>
              <tr>
                <th style={{ width: 60 }}>#</th>
                <th>Hành động</th>
                <th>Đối tượng</th>
                <th>Chi tiết</th>
                <th>IP</th>
                <th>Thời gian</th>
              </tr>
            </thead>
            <tbody>
              {logs.map(log => (
                <tr key={log.id}>
                  <td style={{ color: 'var(--text-muted)', fontSize: 12 }}>{log.id}</td>
                  <td>
                    <span style={{ fontWeight: 600, fontSize: 12, color: actionColor(log.action), background: `${actionColor(log.action)}14`, padding: '2px 8px', borderRadius: 10 }}>
                      {log.action}
                    </span>
                  </td>
                  <td style={{ fontSize: 13 }}>
                    {log.entityType && <span style={{ color: 'var(--text-muted)' }}>{log.entityType} #{log.entityId}</span>}
                  </td>
                  <td style={{ fontSize: 13, maxWidth: 280, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }} title={log.detail || ''}>
                    {log.detail || '—'}
                  </td>
                  <td style={{ fontSize: 12, color: 'var(--text-muted)', fontFamily: 'monospace' }}>{log.ipAddress || '—'}</td>
                  <td style={{ fontSize: 12, color: 'var(--text-muted)', whiteSpace: 'nowrap' }}>
                    {new Date(log.occurredAt).toLocaleString('vi-VN')}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {totalPages > 1 && (
        <div className="pagination">
          <button className="page-btn" onClick={() => setPage(p => Math.max(1, p - 1))} disabled={page === 1}>
            <span className="material-icons">chevron_left</span>
          </button>
          {Array.from({ length: Math.min(totalPages, 7) }, (_, i) => {
            const p = totalPages <= 7 ? i + 1 : page <= 4 ? i + 1 : page >= totalPages - 3 ? totalPages - 6 + i : page - 3 + i
            return (
              <button key={p} className={`page-btn${p === page ? ' active' : ''}`} onClick={() => setPage(p)}>
                {p}
              </button>
            )
          })}
          <button className="page-btn" onClick={() => setPage(p => Math.min(totalPages, p + 1))} disabled={page === totalPages}>
            <span className="material-icons">chevron_right</span>
          </button>
        </div>
      )}
    </div>
  )
}
