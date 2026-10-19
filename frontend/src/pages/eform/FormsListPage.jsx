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

// ============================================================
// FIX 1: Robust field count (multiple fallbacks)
// ============================================================
function getFieldCount(form) {
  if (!form) return 0
  if (Array.isArray(form.fields)) return form.fields.length
  if (typeof form.fieldCount === 'number') return form.fieldCount
  if (typeof form.fieldsCount === 'number') return form.fieldsCount
  return 0
}

// ============================================================
// FIX 2: Parse workflows — FormId check only (NO nodeType)
// ============================================================
function buildFormUsageMap(workflows) {
  const usageMap = {}

  workflows.forEach((w) => {
    try {
      const designer = JSON.parse(w.designerJson || '{}')
      const nodes = designer.Nodes || designer.nodes || []

      nodes.forEach((node) => {
        const formId = node.FormId || node.formId
        if (!formId) return

        const key = formId.toLowerCase()
        if (!usageMap[key]) {
          usageMap[key] = []
        }

        // Skip duplicates (same workflow + stage)
        const alreadyExists = usageMap[key].some(
          (u) => u.workflowId === w.id && u.stageName === (node.Name || node.name)
        )
        if (alreadyExists) return

        usageMap[key].push({
          workflowId: w.id,
          workflowName: w.name,
          workflowStatus: w.status,
          workflowVersion: w.version,
          stageName: node.Name || node.name || 'Untitled stage',
          roleCode: node.RoleCode || node.roleCode || null,
          nodeType: Number(node.Type ?? node.type ?? -1),
        })
      })
    } catch (err) {
      console.warn('Could not parse workflow', w.id, err)
    }
  })

  return usageMap
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

      // DEBUG — check karo console mein
      console.log('FORMS FROM API:', formsData)

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
  // USAGE MAP
  // ============================================================
  const usageMap = useMemo(() => buildFormUsageMap(workflows), [workflows])

  // ============================================================
  // PUBLISH FROM LIST
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
      if (err.message.includes('404') || err.message.includes('not found')) {
        alert(
          'Publish endpoint not found on backend. Please add POST /api/forms/{id}/publish'
        )
      } else {
        alert(`Publish failed: ${err.message}`)
      }
    } finally {
      setBusyId('')
    }
  }

  // ============================================================
  // FILTER
  // ============================================================
  const filtered = useMemo(() => {
    return forms.filter((f) => {
      if (search && !f.name?.toLowerCase().includes(search.toLowerCase()))
        return false
      if (filterStatus && f.status !== filterStatus) return false
      return true
    })
  }, [forms, search, filterStatus])

  // ============================================================
  // STATS
  // ============================================================
  const stats = useMemo(
    () => ({
      total: forms.length,
      published: forms.filter((f) => f.status === 'Published').length,
      draft: forms.filter((f) => f.status === 'Draft').length,
      inUse: forms.filter(
        (f) => (usageMap[f.id.toLowerCase()] || []).length > 0
      ).length,
    }),
    [forms, usageMap]
  )

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
        <div className="wf-stat wf-stat--used">
          <span className="wf-stat-label">In Use</span>
          <strong className="wf-stat-value">{stats.inUse}</strong>
        </div>
      </div>

      {/* ============ FILTERS ============ */}
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

      {/* ============ LIST ============ */}
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
            const usage = usageMap[form.id.toLowerCase()] || []
            const uniqueWorkflows = new Set(usage.map((u) => u.workflowId)).size
            const fieldCount = getFieldCount(form)
            const isDraft = form.status === 'Draft'
            const isBusy = busyId === form.id

            return (
              <article
                key={form.id}
                className={`form-card form-card--${statusMeta.key}`}
                onClick={() => navigate(`/forms/${form.id}`)}
              >
                {/* HEADER */}
                <div className="form-card-header">
                  <div className="form-card-titles">
                    <div className="form-card-name">{form.name}</div>
                    <div className="form-card-meta">
                      {fieldCount} field{fieldCount !== 1 ? 's' : ''}
                      {form.version > 1 ? ` · v${form.version}` : ''}
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

                {/* USAGE */}
                <div className="form-card-usage">
                  {usage.length === 0 ? (
                    <div className="form-card-usage-empty">
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
                      <span>Not used in any workflow yet</span>
                    </div>
                  ) : (
                    <>
                      <div className="form-card-usage-title">
                        <svg
                          width="12"
                          height="12"
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
                        Used in {uniqueWorkflows} workflow
                        {uniqueWorkflows !== 1 ? 's' : ''} ·
                         {/* {usage.length} stage
                        {usage.length !== 1 ? 's' : ''} */}
                      </div>
                      <div className="form-card-usage-list">
                        {usage.slice(0, 4).map((u, idx) => {
                          const wfStatus = getStatusMeta(
                            u.workflowStatus === 0
                              ? 'Draft'
                              : u.workflowStatus === 1
                              ? 'Published'
                              : 'Archived'
                          )
                          return (
                            <div
                              className="form-card-usage-item"
                              key={`${u.workflowId}-${idx}`}
                            >
                              <div className="form-card-usage-workflow">
                                <span className="form-card-usage-workflow-name">
                                  {u.workflowName}
                                </span>
                                <span
                                  className={`form-card-usage-workflow-status form-card-usage-workflow-status--${wfStatus.key}`}
                                >
                                  {wfStatus.label}
                                </span>
                              </div>
                              <div className="form-card-usage-stage">
                                <span className="form-card-usage-stage-label">
                                  Stage:
                                </span>
                                <span className="form-card-usage-stage-name">
                                  {u.stageName}
                                </span>
                                {u.roleCode && (
                                  <span className="form-card-usage-role">
                                    {u.roleCode}
                                  </span>
                                )}
                              </div>
                            </div>
                          )
                        })}
                        {usage.length > 4 && (
                          <div className="form-card-usage-more">
                            + {usage.length - 4} more stage
                            {usage.length - 4 !== 1 ? 's' : ''}
                          </div>
                        )}
                      </div>
                    </>
                  )}
                </div>

                {/* FOOTER */}
                <div className="form-card-footer">
  <span className="form-card-chip">
    🕒 {timeAgo(form.createdAtUtc || form.updatedAtUtc)}
  </span>
  <div className="form-card-actions">
    {/* Draft: only Edit + Publish */}
    {isDraft && (
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

    {/* Published/Archived: only Open */}
    {!isDraft && (
      <button
        className="btn btn-secondary btn-sm"
        onClick={(e) => {
          e.stopPropagation()
          navigate(`/forms/${form.id}`)
        }}
      >
        Open
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