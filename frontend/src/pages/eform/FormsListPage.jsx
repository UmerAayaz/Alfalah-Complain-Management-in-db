import { useEffect, useMemo, useState } from 'react'
import { api } from '../../api/client.js'
import PageHeader from '../../components/PageHeader.jsx'
import ErrorBanner from '../../components/ErrorBanner.jsx'
import EmptyState from '../../components/EmptyState.jsx'
import ConfirmDialog from '../../components/ConfirmDialog.jsx'
import { useToast, ToastContainer } from '../../components/Toast.jsx'
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

function parseWorkflowUsage(workflows, formId, formsById = {}) {
  const result = { workflows: [], totalStages: 0 }
  if (!formId) return result

  const targetFormId = formId.toLowerCase()
  const formVersion = formsById[targetFormId]?.version

  workflows.forEach((w) => {
    // Skip archived workflows
    if (w.status === 2 || w.status === 'Archived') return

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
          formVersion,
          stageNames: matching.map((n) => n.Name || n.name || 'Untitled'),
        })
      }
    } catch {
      // ignore malformed designer json
    }
  })

  return result
}

function getWorkflowStatusLabel(status) {
  if (status === 1) return { label: 'Published', key: 'published' }
  if (status === 0) return { label: 'Draft', key: 'draft' }
  return { label: 'Archived', key: 'archived' }
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
  const [updatingFormId, setUpdatingFormId] = useState('')
  const [hoveredFormId, setHoveredFormId] = useState(null)
  const [search, setSearch] = useState('')
  const [filterStatus, setFilterStatus] = useState('')

  // ✅ Confirm dialog state
  const [confirmState, setConfirmState] = useState(null)

  // ✅ Toast hook
  const { toasts, showToast, removeToast } = useToast()

  // ============================================================
  // LOAD
  // ============================================================
  const load = async () => {
    setLoading(true)
    try {
      const [formsData, wfs] = await Promise.all([
        api('/forms?includeAllVersions=true'),
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
  // MEMOS
  // ============================================================
  const formsById = useMemo(() => {
    const map = {}
    forms.forEach((f) => { map[f.id.toLowerCase()] = f })
    return map
  }, [forms])

  const usageMap = useMemo(() => {
    const map = {}
    forms.forEach((f) => {
      map[f.id.toLowerCase()] = parseWorkflowUsage(workflows, f.id, formsById)
    })
    return map
  }, [forms, workflows, formsById])

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

  const filtered = useMemo(() => {
    return forms.filter((f) => {
      const key = f.formKey || f.id
      if (latestByKey[key]?.id !== f.id) return false
      if (search && !f.name?.toLowerCase().includes(search.toLowerCase()))
        return false
      if (filterStatus && f.status !== filterStatus) return false
      return true
    })
  }, [forms, search, filterStatus, latestByKey])

  const stats = useMemo(() => {
    let total = 0
    let published = 0
    let needsAttention = 0
    filtered.forEach((f) => {
      total++
      if (f.status === 'Published') published++
      const hasOldInUse = forms.some(
        (old) =>
          old.formKey === f.formKey &&
          old.id !== f.id &&
          (usageMap[old.id.toLowerCase()]?.workflows.length || 0) > 0
      )
      if (hasOldInUse) needsAttention++
    })
    return { total, published, needsAttention }
  }, [filtered, forms, usageMap])

  // ============================================================
  // ACTIONS — Publish (with confirm + toast)
  // ============================================================
  const publishForm = (form) => {
    setConfirmState({
      title: `Publish "${form.name}"?`,
      variant: 'primary',
      confirmLabel: 'Publish',
      content: (
        <p style={{ margin: 0 }}>
          This will make <strong>{form.name}</strong> available in the workflow
          designer. You can still create new versions later.
        </p>
      ),
      onConfirm: async () => {
        setConfirmState(null)
        setBusyId(form.id)
        try {
          await api(`/forms/${form.id}/publish`, { method: 'POST' })
          showToast(`"${form.name}" published successfully`, 'success')
          await load()
        } catch (err) {
          showToast(`Publish failed: ${err.message}`, 'error')
        } finally {
          setBusyId('')
        }
      },
    })
  }

  // ============================================================
  // ACTIONS — Create new version (with confirm + toast)
  // ============================================================
  const createNewVersion = (form) => {
    setConfirmState({
      title: 'Create new version?',
      variant: 'primary',
      confirmLabel: 'Create version',
      content: (
        <p style={{ margin: 0 }}>
          A new <strong>Draft</strong> version of <strong>{form.name}</strong>{' '}
          will be created. You can edit it freely — workflows will continue
          using the current published version until you publish the new one.
        </p>
      ),
      onConfirm: async () => {
        setConfirmState(null)
        setBusyId(form.id)
        try {
          const created = await api(`/forms/${form.id}/new-version`, {
            method: 'POST',
          })
          showToast(`New version v${created.version} created`, 'success')
          navigate(`/forms/${created.id}`)
        } catch (err) {
          showToast(`Create version failed: ${err.message}`, 'error')
        } finally {
          setBusyId('')
        }
      },
    })
  }

  // ============================================================
  // ACTIONS — Migrate form to latest (with modal + toasts)
  // ============================================================
  const migrateFormToLatest = (latestForm, oldForm, workflowUsages) => {
    if (!oldForm || !workflowUsages || workflowUsages.length === 0) return

    const wfCount = workflowUsages.length

    setConfirmState({
      title: 'Update to latest version?',
      variant: 'primary',
      confirmLabel: `Update to v${latestForm.version}`,
      content: (
        <>
          <div className="migrate-version-row">
            <span className="migrate-version-from">v{oldForm.version}</span>
            <span className="migrate-version-arrow">→</span>
            <span className="migrate-version-to">v{latestForm.version}</span>
          </div>

          <div className="migrate-section">
            <div className="migrate-section-label">
              Workflows to migrate ({wfCount})
            </div>
            <div className="migrate-workflow-list">
              {workflowUsages.map((w) => {
                const st = getWorkflowStatusLabel(w.workflowStatus)
                return (
                  <div key={w.workflowId} className="migrate-workflow-item">
                    <span>
                      <strong>{w.workflowName}</strong> · v{w.workflowVersion}
                    </span>
                    <span
                      className={`migrate-workflow-badge migrate-workflow-badge--${st.key}`}
                    >
                      {st.label}
                    </span>
                  </div>
                )
              })}
            </div>
          </div>

          <div className="migrate-section">
            <div className="migrate-section-label">What will happen</div>
            <div className="migrate-impact-list">
              <div className="migrate-impact-item">
                A new version of each workflow will be created
              </div>
              <div className="migrate-impact-item">
                All existing nodes and stages stay exactly the same
              </div>
              <div className="migrate-impact-item">
                Only the form reference changes to v{latestForm.version}
              </div>
              <div className="migrate-impact-item">
                The new workflow version will be published automatically
              </div>
              <div className="migrate-impact-item">
                The old published version will be archived
              </div>
            </div>
          </div>
        </>
      ),
      onConfirm: async () => {
        setConfirmState(null)
        setUpdatingFormId(latestForm.id)
        try {
          const result = await api('/workflow-definitions/migrate-form', {
            method: 'POST',
            body: { oldFormId: oldForm.id, newFormId: latestForm.id },
          })

          // Success toast
          if (result.updatedCount > 0) {
            showToast(
              `${result.updatedCount} workflow${result.updatedCount !== 1 ? 's' : ''
              } updated to use v${latestForm.version}`,
              'success',
              5500
            )
          } else {
            showToast('No workflows were updated', 'info')
          }

          // Warnings
          if (result.warnings?.length > 0) {
            result.warnings.forEach((w) =>
              showToast(w, 'warning', 6500)
            )
          }

          // Skipped
          if (result.skipped?.length > 0) {
            showToast(
              `${result.skipped.length} workflow${result.skipped.length !== 1 ? 's' : ''
              } skipped (archived)`,
              'info'
            )
          }

          await load()
        } catch (err) {
          showToast(`Update failed: ${err.message}`, 'error', 6500)
        } finally {
          setUpdatingFormId('')
        }
      },
    })
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
          <span className="wf-stat-label">Total Forms</span>
          <strong className="wf-stat-value">{stats.total}</strong>
        </div>
        <div className="wf-stat wf-stat--latest">
          <span className="wf-stat-label">Published</span>
          <strong className="wf-stat-value">{stats.published}</strong>
        </div>
        <div className="wf-stat wf-stat--old-used">
          <span className="wf-stat-label">Needs Attention</span>
          <strong className="wf-stat-value">{stats.needsAttention}</strong>
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
            const formUsage = usageMap[form.id.toLowerCase()] || {
              workflows: [],
              totalStages: 0,
            }
            const formIsInUse = formUsage.workflows.length > 0

            const oldVersionsInUse = forms
              .filter(
                (old) =>
                  old.formKey === form.formKey &&
                  old.id !== form.id &&
                  (usageMap[old.id.toLowerCase()]?.workflows.length || 0) > 0
              )
              .sort((a, b) => (b.version || 0) - (a.version || 0))

            const mostRecentOld = oldVersionsInUse[0] || null
            const mostRecentOldUsage = mostRecentOld
              ? usageMap[mostRecentOld.id.toLowerCase()]
              : null
            const hasOldInUse = oldVersionsInUse.length > 0

            const isBusy = busyId === form.id
            const isUpdating = updatingFormId === form.id
            const isHovered = hoveredFormId === form.id

            return (
              <article
                key={form.id}
                className={`form-card form-card--${statusMeta.key} ${formIsInUse ? 'form-card--in-use' : ''
                  } form-card--latest ${hasOldInUse ? 'form-card--has-warning' : ''
                  }`}
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
                      {form.formKey && (
                        <span className="form-version-tag form-version-tag--latest">
                          Latest
                        </span>
                      )}
                      {form.fieldCount > 0 && ` · ${form.fieldCount} fields`}
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

                {/* WARNING */}
                {hasOldInUse && mostRecentOldUsage && (
                  <div className="form-outdated-banner">
                    <div className="form-outdated-icon" aria-hidden="true">
                      <svg
                        viewBox="0 0 24 24"
                        fill="none"
                        stroke="currentColor"
                        strokeWidth="2.5"
                        strokeLinecap="round"
                        strokeLinejoin="round"
                      >
                        <path d="M12 9v4" />
                        <path d="M12 17h.01" />
                      </svg>
                    </div>
                    <div className="form-outdated-content">
                      <div className="form-outdated-title">
                        Old version <strong>v{mostRecentOld.version}</strong>{' '}
                        is still used in {mostRecentOldUsage.workflows.length}{' '}
                        workflow
                        {mostRecentOldUsage.workflows.length !== 1 ? 's' : ''}
                      </div>
                      <div className="form-outdated-subtitle">
                        Update to <strong>v{form.version}</strong>. A new
                        workflow version will be created with only the form
                        reference changed, then published automatically.
                      </div>
                    </div>
                    <div className="form-outdated-actions">
                      <button
                        type="button"
                        className="form-outdated-action"
                        disabled={isUpdating}
                        onClick={(e) => {
                          e.stopPropagation()
                          migrateFormToLatest(
                            form,
                            mostRecentOld,
                            mostRecentOldUsage.workflows
                          )
                        }}
                      >
                        {isUpdating
                          ? 'Updating…'
                          : `Update to v${form.version} →`}
                      </button>
                    </div>
                  </div>
                )}

                {/* USAGE INDICATOR */}
                <div
                  className={`form-usage-indicator ${formIsInUse ? 'form-usage-indicator--active' : ''
                    } ${formIsInUse ? 'form-usage-indicator--latest-in-use' : ''
                    }`}
                >
                  {formIsInUse ? (
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
                        Used in {formUsage.workflows.length} workflow
                        {formUsage.workflows.length !== 1 ? 's' : ''}
                      </strong>
                      <span className="form-usage-stage-count">
                        · {formUsage.totalStages} stage
                        {formUsage.totalStages !== 1 ? 's' : ''}
                      </span>
                      <span className="form-usage-version-badge">
                        pinned at v{form.version}
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
                      <span>
                        {hasOldInUse
                          ? 'Not used directly — old versions in workflows above'
                          : 'Not used in any workflow'}
                      </span>
                    </>
                  )}
                </div>

                {/* TOOLTIP */}
                {isHovered && formIsInUse && (
                  <div className="form-hover-tooltip">
                    <strong>Used in:</strong>
                    <ul>
                      {formUsage.workflows.slice(0, 5).map((w) => (
                        <li key={w.workflowId}>
                          <span className="tooltip-workflow-name">
                            {w.workflowName}
                          </span>
                          {w.formVersion && (
                            <span className="tooltip-workflow-version">
                              v{w.formVersion}
                            </span>
                          )}
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

                    {form.status === 'Published' && (
                      <button
                        className="btn btn-secondary btn-sm"
                        disabled={isBusy}
                        onClick={(e) => {
                          e.stopPropagation()
                          createNewVersion(form)
                        }}
                        title="Published forms are immutable — a new Draft version will be created so you can edit it"
                      >
                        {isBusy ? 'Preparing…' : 'Edit'}
                      </button>
                    )}
                  </div>
                </div>
              </article>
            )
          })}
        </div>
      )}

      {/* ✅ Confirm Dialog */}
      <ConfirmDialog
        open={!!confirmState}
        title={confirmState?.title}
        variant={confirmState?.variant}
        confirmLabel={confirmState?.confirmLabel}
        onConfirm={confirmState?.onConfirm}
        onCancel={() => setConfirmState(null)}
      >
        {confirmState?.content}
      </ConfirmDialog>

      {/* ✅ Toast Container */}
      <ToastContainer toasts={toasts} onRemove={removeToast} />
    </>
  )
}