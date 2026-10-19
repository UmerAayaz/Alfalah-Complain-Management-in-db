import { useEffect, useState } from 'react'
import { api } from '../../api/client.js'
import PageHeader from '../../components/PageHeader.jsx'
import ErrorBanner from '../../components/ErrorBanner.jsx'
import './eform.css'

// ============================================================
// USAGE PARSER
// ============================================================
function parseWorkflowUsage(workflows, formId) {
  const result = {
    workflows: [],
    totalStages: 0,
    publishedCount: 0,
    draftCount: 0,
  }

  if (!formId) return result

  workflows.forEach((w) => {
    try {
      const designer = JSON.parse(w.designerJson || '{}')
      const nodes = designer.Nodes || designer.nodes || []

      const matchingNodes = nodes.filter((n) => {
        const fid = n.FormId || n.formId
        return fid && fid.toLowerCase() === formId.toLowerCase()
      })

      if (matchingNodes.length > 0) {
        result.totalStages += matchingNodes.length

        if (w.status === 1) result.publishedCount++
        else if (w.status === 0) result.draftCount++

        result.workflows.push({
          workflowId: w.id,
          workflowName: w.name,
          workflowStatus: w.status,
          workflowVersion: w.version,
          stages: matchingNodes.map((n) => ({
            name: n.Name || n.name || 'Untitled',
            role: n.RoleCode || n.roleCode || null,
          })),
        })
      }
    } catch (err) {
      console.warn('Could not parse workflow', w.id, err)
    }
  })

  return result
}

// ============================================================
// COMPONENT
// ============================================================
export default function FormBuilderPage({ formId, isNew, navigate }) {
  const [libraryFields, setLibraryFields] = useState([])
  const [formName, setFormName] = useState('')
  const [formDescription, setFormDescription] = useState('')
  const [formStatus, setFormStatus] = useState('Draft')
  const [selectedFields, setSelectedFields] = useState([])
  const [search, setSearch] = useState('')
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [saving, setSaving] = useState(false)
  const [publishing, setPublishing] = useState(false)

  const [usage, setUsage] = useState({
    workflows: [],
    totalStages: 0,
    publishedCount: 0,
    draftCount: 0,
  })
  const [usageLoading, setUsageLoading] = useState(false)

  // ============================================================
  // LOAD LIBRARY + FORM
  // ============================================================
  useEffect(() => {
    const load = async () => {
      setLoading(true)
      try {
        const fields = await api('/library-fields')
        setLibraryFields(Array.isArray(fields) ? fields : [])

        if (formId) {
          const form = await api(`/forms/${formId}`)
          console.log('FORM FROM API:', form)
          setFormName(form.name)
          setFormDescription(form.description || '')
          setFormStatus(form.status || 'Draft')
          setSelectedFields(
            (form.fields || []).map((f) => ({
              id: f.libraryFieldId || f.id,
              label: f.label,
              fieldType: f.fieldType,
              isRequired: f.isRequired ?? false,
              regexPattern: f.regexPattern,
            }))
          )
        }
      } catch (err) {
        console.error('LOAD FAILED:', err)
        setError(err.message)
      } finally {
        setLoading(false)
      }
    }
    load()
  }, [formId])

  // ============================================================
  // LOAD USAGE
  // ============================================================
  useEffect(() => {
    if (!formId) {
      setUsage({ workflows: [], totalStages: 0, publishedCount: 0, draftCount: 0 })
      return
    }

    setUsageLoading(true)
    api('/workflow-definitions')
      .then((workflows) => {
        setUsage(parseWorkflowUsage(workflows || [], formId))
      })
      .catch((err) => {
        console.error('Usage load failed:', err)
      })
      .finally(() => setUsageLoading(false))
  }, [formId])

  // ============================================================
  // FIELD OPERATIONS
  // ============================================================
  const addField = (field) => {
    if (selectedFields.some((f) => f.id === field.id)) return
    setSelectedFields((prev) => [
      ...prev,
      {
        ...field,
        isRequired: field.isRequired ?? false,
      },
    ])
  }

  const removeField = (id) => {
    setSelectedFields((prev) => prev.filter((f) => f.id !== id))
  }

  const moveField = (index, direction) => {
    const next = [...selectedFields]
    const target = index + direction
    if (target < 0 || target >= next.length) return
    ;[next[index], next[target]] = [next[target], next[index]]
    setSelectedFields(next)
  }

  const filtered = libraryFields.filter((f) =>
    f.label.toLowerCase().includes(search.toLowerCase())
  )

  // ============================================================
  // BUILD PAYLOAD + VALIDATE
  // ============================================================
  const buildPayload = () => ({
    name: formName.trim(),
    description: formDescription.trim() || null,
    fields: selectedFields.map((f) => ({
      libraryFieldId: f.id,
      isRequiredOverride: f.isRequired ?? null,
    })),
  })

  const validate = () => {
    if (!formName.trim()) {
      setError('Form name is required.')
      return false
    }
    if (selectedFields.length === 0) {
      setError('Add at least one field.')
      return false
    }
    return true
  }

  // ============================================================
  // SAVE (Draft only)
  // ============================================================
  const save = async () => {
    if (!validate()) return

    setSaving(true)
    setError('')

    try {
      const payload = buildPayload()

      if (formId) {
        await api(`/forms/${formId}`, { method: 'PUT', body: payload })
        window.alert('Form saved as draft.')
      } else {
        const created = await api('/forms', { method: 'POST', body: payload })
        window.alert('Form created as draft.')
        navigate(`/forms/${created.id}`)
      }
    } catch (err) {
      console.error('SAVE FAILED:', err)
      setError(err.message)
    } finally {
      setSaving(false)
    }
  }

  // ============================================================
  // PUBLISH (Save + Publish)
  // ============================================================
  const publish = async () => {
    if (!validate()) return

    setPublishing(true)
    setError('')

    try {
      const payload = buildPayload()
      let formIdToPublish = formId

      if (formId) {
        await api(`/forms/${formId}`, { method: 'PUT', body: payload })
      } else {
        const created = await api('/forms', { method: 'POST', body: payload })
        formIdToPublish = created.id
      }

      try {
        await api(`/forms/${formIdToPublish}/publish`, { method: 'POST' })
      } catch (pubErr) {
        if (
          pubErr.message.includes('404') ||
          pubErr.message.includes('not found')
        ) {
          setError(
            'Publish API not found on backend. Form was saved as draft only.'
          )
          if (!formId) navigate(`/forms/${formIdToPublish}`)
          return
        }
        throw pubErr
      }

      window.alert('Form published successfully!')

      if (!formId) {
        navigate(`/forms/${formIdToPublish}`)
      } else {
        setFormStatus('Published')
        const workflows = await api('/workflow-definitions').catch(() => [])
        setUsage(parseWorkflowUsage(workflows || [], formIdToPublish))
      }
    } catch (err) {
      console.error('PUBLISH FAILED:', err)
      setError(err.message)
    } finally {
      setPublishing(false)
    }
  }

  // ============================================================
  // STATUS HELPER
  // ============================================================
  const statusLabel = (s) => {
    if (s === 0) return { text: 'Draft', cls: 'form-usage-status--draft' }
    if (s === 1) return { text: 'Published', cls: 'form-usage-status--published' }
    return { text: 'Archived', cls: 'form-usage-status--archived' }
  }

  const isPublished = formStatus === 'Published'

  // ============================================================
  // RENDER
  // ============================================================
  return (
    <>
      <PageHeader
        eyebrow="E-FORM BUILDER"
        title={isNew ? 'Create Form' : 'Edit Form'}
        description="Build your form by adding fields from the library"
        actions={
          <div className="page-actions">
            <button
              className="btn btn-secondary"
              onClick={() => navigate('/forms')}
            >
              ← Back
            </button>

            {!isPublished && (
              <>
                <button
                  className="btn btn-secondary"
                  onClick={save}
                  disabled={saving || publishing}
                >
                  {saving ? 'Saving…' : 'Save as Draft'}
                </button>
                <button
                  className="btn btn-primary"
                  onClick={publish}
                  disabled={saving || publishing}
                >
                  {publishing ? 'Publishing…' : 'Publish'}
                </button>
              </>
            )}

            {isPublished && (
              <span className="form-status form-status--published">
                <span className="form-status-dot">●</span>
                Published
              </span>
            )}
          </div>
        }
      />

      <ErrorBanner message={error} />

      <div className="form-builder-shell">
        {/* ============ LEFT — Library ============ */}
        <aside className="form-builder-library panel">
          <div className="designer-section-title">Field Library</div>

          <input
            className="input"
            placeholder="Search fields…"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            style={{ marginBottom: 12 }}
          />

          {loading ? (
            <div className="loading-block">Loading…</div>
          ) : filtered.length === 0 ? (
            <div className="inline-empty">
              {libraryFields.length === 0
                ? 'No fields in library.'
                : 'No fields match your search.'}
            </div>
          ) : (
            <div className="library-field-list">
              {filtered.map((f) => {
                const alreadyAdded = selectedFields.some((s) => s.id === f.id)
                return (
                  <button
                    key={f.id}
                    className={`library-field-item ${alreadyAdded ? 'added' : ''}`}
                    onClick={() => addField(f)}
                    disabled={alreadyAdded || isPublished}
                  >
                    <span className="library-field-label">{f.label}</span>
                    <span className="library-field-type">{f.fieldType}</span>
                    {!alreadyAdded && <span className="library-field-add">+</span>}
                    {alreadyAdded && <span className="library-field-check">✓</span>}
                  </button>
                )
              })}
            </div>
          )}
        </aside>

        {/* ============ RIGHT — Canvas ============ */}
        <section className="form-builder-canvas panel">
          <div className="designer-section-title">Form Details</div>

          <div className="form-row">
            <label className="field compact">
              <span>Form Name *</span>
              <input
                className="input"
                value={formName}
                onChange={(e) => setFormName(e.target.value)}
                placeholder="e.g. Customer Info Form"
                disabled={isPublished}
              />
            </label>
          </div>

          <label className="field compact">
            <span>Description</span>
            <input
              className="input"
              value={formDescription}
              onChange={(e) => setFormDescription(e.target.value)}
              placeholder="Short description"
              disabled={isPublished}
            />
          </label>

          {/* ==================================================
              FORM USAGE
              ================================================== */}
          {formId && (
            <div className="form-usage-section">
              <div className="designer-section-title" style={{ marginTop: 24 }}>
                Form Usage
              </div>

              {usageLoading ? (
                <div className="inline-empty">Loading usage…</div>
              ) : usage.workflows.length === 0 ? (
                <div className="form-usage-empty">
                  <svg
                    width="20"
                    height="20"
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
                  <span>This form is not yet used in any workflow.</span>
                </div>
              ) : (
                <>
                  <div className="form-usage-stats">
                    <div className="form-usage-stat form-usage-stat--workflows">
                      <div className="form-usage-stat-value">
                        {usage.workflows.length}
                      </div>
                      <div className="form-usage-stat-label">
                        Workflow{usage.workflows.length !== 1 ? 's' : ''}
                      </div>
                    </div>
                    <div className="form-usage-stat form-usage-stat--stages">
                      <div className="form-usage-stat-value">
                        {usage.totalStages}
                      </div>
                      <div className="form-usage-stat-label">
                        Stage{usage.totalStages !== 1 ? 's' : ''}
                      </div>
                    </div>
                    <div className="form-usage-stat form-usage-stat--published">
                      <div className="form-usage-stat-value">
                        {usage.publishedCount}
                      </div>
                      <div className="form-usage-stat-label">Published</div>
                    </div>
                    <div className="form-usage-stat form-usage-stat--draft">
                      <div className="form-usage-stat-value">
                        {usage.draftCount}
                      </div>
                      <div className="form-usage-stat-label">Draft</div>
                    </div>
                  </div>

                  <div className="form-usage-list">
                    {usage.workflows.map((w) => {
                      const status = statusLabel(w.workflowStatus)
                      return (
                        <div className="form-usage-workflow" key={w.workflowId}>
                          <div className="form-usage-workflow-header">
                            <div className="form-usage-workflow-name">
                              <strong>{w.workflowName}</strong>
                              <span className="form-usage-workflow-version">
                                v{w.workflowVersion}
                              </span>
                            </div>
                            <span className={`form-usage-status ${status.cls}`}>
                              {status.text}
                            </span>
                          </div>

                          <div className="form-usage-stages">
                            {w.stages.map((s, idx) => (
                              <div className="form-usage-stage" key={idx}>
                                <span className="form-usage-stage-icon">
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
                                    <circle cx="12" cy="12" r="3" />
                                    <path d="M12 1v6m0 6v6m11-7h-6m-6 0H1" />
                                  </svg>
                                </span>
                                <span className="form-usage-stage-name">
                                  {s.name}
                                </span>
                                {s.role && (
                                  <span className="form-usage-stage-role">
                                    {s.role}
                                  </span>
                                )}
                              </div>
                            ))}
                          </div>
                        </div>
                      )
                    })}
                  </div>
                </>
              )}
            </div>
          )}

          {/* Form Fields */}
          <div className="designer-section-title" style={{ marginTop: 20 }}>
            Form Fields ({selectedFields.length})
          </div>

          {selectedFields.length === 0 ? (
            <div className="inline-empty" style={{ padding: 24 }}>
              Click fields on the left to add them to this form.
            </div>
          ) : (
            <div className="form-canvas-list">
              {selectedFields.map((field, index) => (
                <div className="form-canvas-item" key={field.id}>
                  <div className="form-canvas-item-info">
                    <strong>{field.label}</strong>
                    <div className="muted-text">
                      {field.fieldType}
                      {field.regexPattern ? ' · Regex' : ''}
                    </div>
                  </div>

                  <div className="form-canvas-item-actions">
                    <label
                      className="checkbox-field-inline"
                      title="Required in this form"
                    >
                      <input
                        type="checkbox"
                        checked={!!field.isRequired}
                        onChange={(e) => {
                          const updated = selectedFields.map((f) =>
                            f.id === field.id
                              ? { ...f, isRequired: e.target.checked }
                              : f
                          )
                          setSelectedFields(updated)
                        }}
                        disabled={isPublished}
                      />
                      Required
                    </label>

                    <button
                      className="btn btn-secondary btn-sm"
                      onClick={() => moveField(index, -1)}
                      disabled={index === 0 || isPublished}
                    >
                      ↑
                    </button>
                    <button
                      className="btn btn-secondary btn-sm"
                      onClick={() => moveField(index, 1)}
                      disabled={
                        index === selectedFields.length - 1 || isPublished
                      }
                    >
                      ↓
                    </button>
                    <button
                      className="btn btn-danger-soft btn-sm"
                      onClick={() => removeField(field.id)}
                      disabled={isPublished}
                    >
                      ×
                    </button>
                  </div>
                </div>
              ))}
            </div>
          )}
        </section>
      </div>
    </>
  )
}