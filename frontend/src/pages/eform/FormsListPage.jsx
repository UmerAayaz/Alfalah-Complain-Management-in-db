import { useEffect, useMemo, useState } from 'react'
import { api } from '../../api/client.js'
import PageHeader from '../../components/PageHeader.jsx'
import ErrorBanner from '../../components/ErrorBanner.jsx'
import EmptyState from '../../components/EmptyState.jsx'
import './eform.css'

// ============================================================
// HELPERS
// ============================================================
const STATUS_MAP = {
  Draft: { key: 'draft', label: 'Draft', icon: '○' },
  Published: { key: 'published', label: 'Published', icon: '●' },
  Archived: { key: 'archived', label: 'Archived', icon: '◌' },
}

function getStatusMeta(status) {
  return STATUS_MAP[status] || STATUS_MAP.Draft
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

function parseWorkflowUsage(workflows, formId) {
  const result = { workflows: [], totalStages: 0 }
  if (!formId) return result

  const targetFormId = formId.toLowerCase()

  workflows.forEach((w) => {
    try {
      const designer = JSON.parse(w.designerJson || '{}')
      const nodes = designer.Nodes || designer.nodes || []
      const matching = nodes.filter((n) => {
        const fid = (n.FormId || n.formId || '').toLowerCase()
        return fid === targetFormId
      })
      if (matching.length > 0) {
        result.totalStages += matching.length
        result.workflows.push({
          workflowId: w.id,
          workflowName: w.name,
          workflowStatus: w.status,
          workflowVersion: w.version,
          stageNames: matching.map((n) => n.Name || n.name || 'Untitled'),
        })
      }
    } catch {
      // ignore
    }
  })

  return result
}

// ============================================================
// COMPONENT
// ============================================================
export default function FormsListPage({ navigate }) {
  const [forms, setForms] = useState([])
  const [workflows, setWorkflows] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [busyId, setBusyId] = useState('')
  const [hoveredFormId, setHoveredFormId] = useState(null)

  const [search, setSearch] = useState('')
  const [filterStatus, setFilterStatus] = useState('')

  // ============================================================
  // LOAD
  // ============================================================
  const load = async () => {
    setLoading(true)
    try {
      const [formsData, wfs] = await Promise.all([
        api('/forms'),
        api('/workflow-definitions').catch(() => []),
      ])
      setForms(Array.isArray(formsData) ? formsData : [])
      setWorkflows(Array.isArray(wfs) ? wfs : [])
      setError('')
    } catch (err) {
      console.error('LOAD FAILED:', err)
      setError(err.message)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    load()
  }, [])

  // ============================================================
  // FORM USAGE MAP
  // ============================================================
  const usageMap = useMemo(() => {
    const map = {}
    forms.forEach((f) => {
      map[f.id.toLowerCase()] = parseWorkflowUsage(workflows, f.id)
    })
    return map
  }, [forms, workflows])

  // ============================================================
  // VERSION GROUPING — kaunsa latest hai per FormKey
  // ============================================================
  const latestByKey = useMemo(() => {
    const map = {}
    forms.forEach((f) => {
      const key = f.formKey || f.id
      if (!map[key] || (f.version || 0) > (map[key].version || 0)) {
        map[key] = f
      }
    })
    return map
  }, [forms])

  // ============================================================
  // FILTER
  // ============================================================
const filtered = useMemo(() => {
  return forms.filter((f) => {
    // Filter old-unused forms (hide them completely)
    const key = f.formKey || f.id
    const latest = latestByKey[key]
    const isLatest = latest?.id === f.id
    const usage = usageMap[f.id.toLowerCase()] || { workflows: [] }
    const inUse = usage.workflows.length > 0

    // Old + unused → hide
    if (!isLatest && !inUse) return false

    if (search && !f.name?.toLowerCase().includes(search.toLowerCase()))
      return false
    if (filterStatus && f.status !== filterStatus) return false
    return true
  })
}, [forms, search, filterStatus, latestByKey, usageMap])

  // ============================================================
  // STATS
  // ============================================================
  const stats = useMemo(() => {
  let latestCount = 0
  let oldButUsedCount = 0
  let visibleTotal = 0

  forms.forEach((f) => {
    const key = f.formKey || f.id
    const isLatest = latestByKey[key]?.id === f.id
    const usage = usageMap[f.id.toLowerCase()] || { workflows: [] }
    const inUse = usage.workflows.length > 0

    if (isLatest) {
      latestCount++
      visibleTotal++
    } else if (inUse) {
      oldButUsedCount++
      visibleTotal++
    }
    // old-unused → not counted
  })

  return {
    total: visibleTotal,
    latestCount,
    oldButUsedCount,
  }
}, [forms, usageMap, latestByKey])

  // ============================================================
  // ACTIONS
  // ============================================================
  const publishForm = async (form) => {
    if (
      !window.confirm(
        `Publish "${form.name}"? It will become available in the workflow designer.`
      )
    )
      return

    setBusyId(form.id)
    try {
      await api(`/forms/${form.id}/publish`, { method: 'POST' })
      window.alert('Form published successfully!')
      await load()
    } catch (err) {
      alert(`Publish failed: ${err.message}`)
    } finally {
      setBusyId('')
    }
  }

  const createNewVersion = async (form) => {
    if (
      !window.confirm(
        `Create a new version of "${form.name}"? A new Draft version will be created.`
      )
    )
      return

    setBusyId(form.id)
    try {
      const created = await api(`/forms/${form.id}/new-version`, {
        method: 'POST',
      })
      window.alert(`New version v${created.version} created!`)
      navigate(`/forms/${created.id}`)
    } catch (err) {
      alert(`Create version failed: ${err.message}`)
    } finally {
      setBusyId('')
    }
  }

  // ============================================================
  // RENDER
  // ============================================================
  return (
    <>
      <PageHeader
        eyebrow="E-FORM BUILDER"
        title="Forms Library"
        description="Create and manage reusable forms attached to workflow stages"
        actions={
          <div className="page-actions">
            <button
              className="btn btn-secondary"
              onClick={() => navigate('/forms/library')}
            >
              Field Library
            </button>
            <button
              className="btn btn-primary"
              onClick={() => navigate('/forms/new')}
            >
              + Create Form
            </button>
          </div>
        }
      />

      <ErrorBanner message={error} />

      {/* STATS */}
      <div className="wf-stats">
  <div className="wf-stat">
    <span className="wf-stat-label">Total Visible</span>
    <strong className="wf-stat-value">{stats.total}</strong>
  </div>
  <div className="wf-stat wf-stat--latest">
    <span className="wf-stat-label">Latest</span>
    <strong className="wf-stat-value">{stats.latestCount}</strong>
  </div>
  <div className="wf-stat wf-stat--old-used">
    <span className="wf-stat-label">Old · In Use</span>
    <strong className="wf-stat-value">{stats.oldButUsedCount}</strong>
  </div>
</div>

      {/* FILTERS */}
      <div className="wf-filters">
        <input
          className="input wf-search"
          placeholder="🔍 Search forms by name…"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
        <select
          className="input"
          value={filterStatus}
          onChange={(e) => setFilterStatus(e.target.value)}
        >
          <option value="">All statuses</option>
          <option value="Draft">Draft</option>
          <option value="Published">Published</option>
          <option value="Archived">Archived</option>
        </select>
        {(search || filterStatus) && (
          <button
            className="btn btn-secondary btn-sm"
            onClick={() => {
              setSearch('')
              setFilterStatus('')
            }}
          >
            Clear filters
          </button>
        )}
      </div>

      {/* LIST */}
      {loading ? (
        <div className="loading-block">Loading forms…</div>
      ) : filtered.length === 0 ? (
        <section className="panel">
          <EmptyState
            title={
              forms.length === 0
                ? 'No forms yet'
                : 'No forms match your filters'
            }
            description={
              forms.length === 0
                ? 'Create your first form to get started.'
                : 'Try clearing filters or search for something else.'
            }
          />
        </section>
      ) : (
        <div className="form-grid">
          {filtered.map((form) => {
            const statusMeta = getStatusMeta(form.status)
            const usage = usageMap[form.id.toLowerCase()] || {
              workflows: [],
              totalStages: 0,
            }
            const inUse = usage.workflows.length > 0
            const latestForKey = latestByKey[form.formKey || form.id]
            const isLatest = latestForKey?.id === form.id
            const isOldVersion = !isLatest
            const isBusy = busyId === form.id
            const isHovered = hoveredFormId === form.id

            return (
              <article
                key={form.id}
                className={`form-card form-card--${statusMeta.key} ${
                  inUse ? 'form-card--in-use' : ''
                } ${isOldVersion ? 'form-card--old-version' : ''}`}
                onClick={() => navigate(`/forms/${form.id}`)}
                onMouseEnter={() => setHoveredFormId(form.id)}
                onMouseLeave={() => setHoveredFormId(null)}
              >
                {/* HEADER */}
                <div className="form-card-header">
                  <div className="form-card-titles">
                    <div className="form-card-name">{form.name}</div>
                    <div className="form-card-meta">
                      v{form.version}
                      {isLatest && form.formKey && (
                        <span className="form-version-tag form-version-tag--latest">
                          Latest
                        </span>
                      )}
                      {isOldVersion && (
                        <span className="form-version-tag form-version-tag--old">
                          Superseded
                        </span>
                      )}
                      {form.fields?.length > 0 &&
                        ` · ${form.fields.length} fields`}
                    </div>
                  </div>
                  <span
                    className={`form-status form-status--${statusMeta.key}`}
                  >
                    <span className="form-status-dot">{statusMeta.icon}</span>
                    {statusMeta.label}
                  </span>
                </div>

                {/* DESCRIPTION */}
                {form.description && (
                  <div className="form-card-description">
                    {form.description}
                  </div>
                )}

                {/* USAGE INDICATOR */}
                <div
                  className={`form-usage-indicator ${
                    inUse ? 'form-usage-indicator--active' : ''
                  }`}
                >
                  {inUse ? (
                    <>
                      <svg
                        width="14"
                        height="14"
                        viewBox="0 0 24 24"
                        fill="none"
                        stroke="currentColor"
                        strokeWidth="2"
                        strokeLinecap="round"
                        strokeLinejoin="round"
                      >
                        <path d="M2 3h6a4 4 0 0 1 4 4v14a3 3 0 0 0-3-3H2z" />
                        <path d="M22 3h-6a4 4 0 0 0-4 4v14a3 3 0 0 1 3-3h7z" />
                      </svg>
                      <strong>
                        Used in {usage.workflows.length} workflow
                        {usage.workflows.length !== 1 ? 's' : ''}
                      </strong>
                      <span className="form-usage-stage-count">
                        · {usage.totalStages} stage
                        {usage.totalStages !== 1 ? 's' : ''}
                      </span>
                    </>
                  ) : (
                    <>
                      <svg
                        width="14"
                        height="14"
                        viewBox="0 0 24 24"
                        fill="none"
                        stroke="currentColor"
                        strokeWidth="2"
                        strokeLinecap="round"
                        strokeLinejoin="round"
                      >
                        <circle cx="12" cy="12" r="10" />
                        <line x1="12" y1="8" x2="12" y2="12" />
                        <line x1="12" y1="16" x2="12.01" y2="16" />
                      </svg>
                      <span>Not used in any workflow</span>
                    </>
                  )}
                </div>

                {/* HOVER TOOLTIP — WORKFLOWS */}
                {isHovered && inUse && (
                  <div className="form-hover-tooltip">
                    <strong>Used in:</strong>
                    <ul>
                      {usage.workflows.slice(0, 5).map((w) => (
                        <li key={w.workflowId}>
                          <span className="tooltip-workflow-name">
                            {w.workflowName}
                          </span>
                          <span className="tooltip-workflow-stages">
                            {w.stageNames.join(', ')}
                          </span>
                        </li>
                      ))}
                    </ul>
                  </div>
                )}

                {/* FOOTER */}
                <div className="form-card-footer">
                  <span className="form-card-chip">
                    🕒 {timeAgo(form.createdAtUtc || form.updatedAtUtc)}
                  </span>
                  <div className="form-card-actions">
                    <button
                      className="btn btn-secondary btn-sm"
                      onClick={(e) => {
                        e.stopPropagation()
                        navigate(`/forms/${form.id}`)
                      }}
                    >
                      Open
                    </button>

                    {form.status === 'Draft' && (
                      <>
                        <button
                          className="btn btn-secondary btn-sm"
                          onClick={(e) => {
                            e.stopPropagation()
                            navigate(`/forms/${form.id}`)
                          }}
                        >
                          Edit
                        </button>
                        <button
                          className="btn btn-primary btn-sm"
                          disabled={isBusy}
                          onClick={(e) => {
                            e.stopPropagation()
                            publishForm(form)
                          }}
                        >
                          {isBusy ? 'Publishing…' : 'Publish'}
                        </button>
                      </>
                    )}

                    {form.status === 'Published' && isLatest && (
                      <button
                        className="btn btn-primary btn-sm"
                        disabled={isBusy}
                        onClick={(e) => {
                          e.stopPropagation()
                          createNewVersion(form)
                        }}
                      >
                        {isBusy ? 'Creating…' : '+ New Version'}
                      </button>
                    )}
                  </div>
                </div>
              </article>
            )
          })}
        </div>
      )}
    </>
  )
}