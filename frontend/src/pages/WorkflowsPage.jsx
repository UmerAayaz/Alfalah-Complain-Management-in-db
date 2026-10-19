import { useEffect, useMemo, useState } from 'react'
import { api } from '../api/client.js'
import { useDevUser } from '../auth/DevUserContext.jsx'
import PageHeader from '../components/PageHeader.jsx'
import ErrorBanner from '../components/ErrorBanner.jsx'
import EmptyState from '../components/EmptyState.jsx'
import { WorkflowStatusBadge } from '../components/Badge.jsx'
import { formatDate } from '../constants.js'
import Pagination from '../components/Pagination.jsx'
import usePagination from '../hooks/usePagination.js'
import './workflows.css'

const PRIORITY_LABELS = ['Low', 'Normal', 'High', 'Critical']
const PRIORITY_COLORS = {
  0: 'priority-low',
  1: 'priority-normal',
  2: 'priority-high',
  3: 'priority-critical',
}

function timeAgo(dateStr) {
  if (!dateStr) return 'Never'
  const diff = Date.now() - new Date(dateStr).getTime()
  const mins = Math.floor(diff / 60000)
  if (mins < 1) return 'Just now'
  if (mins < 60) return `${mins}m ago`
  const hours = Math.floor(mins / 60)
  if (hours < 24) return `${hours}h ago`
  const days = Math.floor(hours / 24)
  if (days < 30) return `${days}d ago`
  return new Date(dateStr).toLocaleDateString()
}

export default function WorkflowsPage({ navigate }) {
  const { userId, roles } = useDevUser()
  const [workflows, setWorkflows] = useState([])
  const [categories, setCategories] = useState([])
  const [error, setError] = useState('')
  const [busyId, setBusyId] = useState('')

  const [search, setSearch] = useState('')
  const [filterCategory, setFilterCategory] = useState('')
  const [filterStatus, setFilterStatus] = useState('')

  // ============================================================
  // FILTER (before pagination)
  // ============================================================
  const filteredWorkflows = useMemo(() => {
    return workflows.filter((w) => {
      if (
        search &&
        !w.name?.toLowerCase().includes(search.toLowerCase())
      )
        return false
      if (filterCategory && w.categoryId !== filterCategory) return false
      if (filterStatus !== '' && String(w.status) !== filterStatus)
        return false
      return true
    })
  }, [workflows, search, filterCategory, filterStatus])

  const {
    page,
    setPage,
    pageSize,
    setPageSize,
    total,
    pageItems: pagedWorkflows,
  } = usePagination(filteredWorkflows, 10)

  // ============================================================
  // STATS
  // ============================================================
  const stats = useMemo(
    () => ({
      total: workflows.length,
      published: workflows.filter((w) => w.status === 1).length,
      draft: workflows.filter((w) => w.status === 0).length,
      archived: workflows.filter((w) => w.status === 2).length,
    }),
    [workflows]
  )

  // ============================================================
  // LOAD
  // ============================================================
  const load = async () => {
    try {
      const [items, cats] = await Promise.all([
        api('/workflow-definitions'),
        api('/reference/categories'),
      ])
      setWorkflows(items)
      setCategories(cats)
      setError('')
    } catch (err) {
      setError(err.message)
    }
  }

  useEffect(() => {
    load()
  }, [userId])

  if (!roles.has('DeptAdmin') && !roles.has('UnitHead'))
    return (
      <section className="panel">
        <ErrorBanner message="Unit Head or Department Admin access is required for the workflow designer." />
      </section>
    )

  // ============================================================
  // ACTIONS
  // ============================================================
  const publish = async (id) => {
    setBusyId(id)
    try {
      await api(`/workflow-definitions/${id}/publish`, { method: 'POST' })
      await load()
    } catch (err) {
      setError(err.message)
    } finally {
      setBusyId('')
    }
  }

  const newVersion = async (id) => {
    setBusyId(id)
    try {
      const created = await api(
        `/workflow-definitions/${id}/new-version`,
        { method: 'POST' }
      )
      navigate(`/workflows/${created.id}/designer`)
    } catch (err) {
      setError(err.message)
    } finally {
      setBusyId('')
    }
  }

  const categoryName = (id) =>
    categories.find((x) => x.id === id)?.name || 'Unknown category'

  // ============================================================
  // RENDER
  // ============================================================
  return (
    <>
      <PageHeader
        eyebrow="AUTOMATION"
        title="Workflow library"
        description="Design, publish, and manage complaint workflows"
        actions={
          <button
            className="btn btn-primary"
            onClick={() => navigate('/workflows/new')}
          >
            + Create workflow
          </button>
        }
      />

      <ErrorBanner message={error} />

      {/* ============ STATS ============ */}
      <div className="wf-stats">
        <div className="wf-stat">
          <span className="wf-stat-label">Total</span>
          <strong className="wf-stat-value">{stats.total}</strong>
        </div>
        <div className="wf-stat wf-stat--published">
          <span className="wf-stat-label">Published</span>
          <strong className="wf-stat-value">{stats.published}</strong>
        </div>
        <div className="wf-stat wf-stat--draft">
          <span className="wf-stat-label">Draft</span>
          <strong className="wf-stat-value">{stats.draft}</strong>
        </div>
        <div className="wf-stat wf-stat--archived">
          <span className="wf-stat-label">Archived</span>
          <strong className="wf-stat-value">{stats.archived}</strong>
        </div>
      </div>

      {/* ============ FILTERS — ONE LINE ============ */}
      <div className="wf-filters">
        <div className="wf-search-wrap">
          <svg
            className="wf-search-icon"
            width="16"
            height="16"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            strokeWidth="2"
            strokeLinecap="round"
            strokeLinejoin="round"
          >
            <circle cx="11" cy="11" r="8" />
            <path d="m21 21-4.35-4.35" />
          </svg>
          <input
            className="input wf-search"
            placeholder="Search workflows…"
            value={search}
            onChange={(e) => {
              setSearch(e.target.value)
              setPage(1)
            }}
            style={{ paddingLeft: 36 }}
          />
        </div>

        <select
          className="input"
          value={filterCategory}
          onChange={(e) => {
            setFilterCategory(e.target.value)
            setPage(1)
          }}
        >
          <option value="">All categories</option>
          {categories.map((c) => (
            <option key={c.id} value={c.id}>
              {c.name}
            </option>
          ))}
        </select>

        <select
          className="input"
          value={filterStatus}
          onChange={(e) => {
            setFilterStatus(e.target.value)
            setPage(1)
          }}
        >
          <option value="">All statuses</option>
          <option value="0">Draft</option>
          <option value="1">Published</option>
          <option value="2">Archived</option>
        </select>

        {(search || filterCategory || filterStatus) && (
          <button
            className="btn btn-secondary btn-sm"
            onClick={() => {
              setSearch('')
              setFilterCategory('')
              setFilterStatus('')
              setPage(1)
            }}
          >
            Clear
          </button>
        )}
      </div>

      {/* ============ TABLE ============ */}
      <section className="panel">
        {workflows.length === 0 ? (
          <EmptyState
            title="No workflows yet"
            description="Create the first workflow in the visual designer."
          />
        ) : filteredWorkflows.length === 0 ? (
          <EmptyState
            title="No workflows match your filters"
            description="Try clearing the filters or changing the search query."
          />
        ) : (
          <>
            <div className="table-wrap">
              <table className="data-table">
                <thead>
                  <tr>
                    <th>Workflow</th>
                    <th>Category</th>
                    <th>Version</th>
                    <th>Status</th>
                    <th>Published</th>
                    <th>Actions</th>
                  </tr>
                </thead>

                <tbody>
                  {pagedWorkflows.map((item) => {
                    const priority =
                      PRIORITY_LABELS[item.defaultPriority ?? 1] || 'Normal'
                    const priorityClass =
                      PRIORITY_COLORS[item.defaultPriority ?? 1] ||
                      'priority-normal'

                    return (
                      <tr key={item.id}>
                        <td>
                          <strong>{item.name}</strong>
                          <div className="wf-row-sub">
                            <span
                              className={`wf-priority ${priorityClass}`}
                            >
                              {priority}
                            </span>
                            <span className="wf-row-meta">
  <svg
    width="11"
    height="11"
    viewBox="0 0 24 24"
    fill="none"
    stroke="currentColor"
    strokeWidth="2"
    strokeLinecap="round"
    strokeLinejoin="round"
    style={{ marginRight: 4, verticalAlign: 'middle' }}
  >
    <circle cx="12" cy="12" r="10" />
    <polyline points="12 6 12 12 16 14" />
  </svg>
  {timeAgo(item.publishedAtUtc || item.createdAtUtc)}
</span>
                          </div>
                        </td>

                        <td>{categoryName(item.categoryId)}</td>

                        <td>v{item.version}</td>

                        <td>
                          <WorkflowStatusBadge status={item.status} />
                        </td>

                        <td>{formatDate(item.publishedAtUtc)}</td>

                        <td>
                          <div className="table-actions">
                            <button
                              className="link-button"
                              onClick={() =>
                                navigate(`/workflows/${item.id}/designer`)
                              }
                            >
                              {item.status === 0 ? 'Edit' : 'View'}
                            </button>

                            {item.status === 0 && (
                              <button
                                className="link-button"
                                disabled={busyId === item.id}
                                onClick={() => publish(item.id)}
                              >
                                Publish
                              </button>
                            )}

                            {item.status !== 0 && (
                              <button
                                className="link-button"
                                disabled={busyId === item.id}
                                onClick={() => newVersion(item.id)}
                              >
                                New version
                              </button>
                            )}
                          </div>
                        </td>
                      </tr>
                    )
                  })}
                </tbody>
              </table>
            </div>

            {total > 0 && (
              <Pagination
                page={page}
                pageSize={pageSize}
                total={total}
                onPageChange={setPage}
                onPageSizeChange={setPageSize}
              />
            )}
          </>
        )}
      </section>
    </>
  )
}