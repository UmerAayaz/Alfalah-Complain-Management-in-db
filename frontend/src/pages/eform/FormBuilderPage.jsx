import { useEffect, useState } from 'react'
import { api } from '../../api/client.js'
import PageHeader from '../../components/PageHeader.jsx'
import ErrorBanner from '../../components/ErrorBanner.jsx'
import './eform.css'

export default function FormBuilderPage({ formId, isNew, navigate }) {
  const [libraryFields, setLibraryFields] = useState([])
  const [formName, setFormName] = useState('')
  const [formDescription, setFormDescription] = useState('')
  const [selectedFields, setSelectedFields] = useState([])
  const [search, setSearch] = useState('')
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [saving, setSaving] = useState(false)

  // ============================================================
  // LOAD LIBRARY FIELDS
  // ============================================================
  useEffect(() => {
  const load = async () => {
    setLoading(true)
    try {
      // Library fields always
      const fields = await api('/library-fields')
      setLibraryFields(Array.isArray(fields) ? fields : [])

      // Agar edit mode (formId diya hai) → us form ko load karo
      if (formId) {
        const form = await api(`/forms/${formId}`)
        setFormName(form.name)
        setFormDescription(form.description || '')
        setSelectedFields(
          form.fields.map((f) => ({
            id: f.libraryFieldId,
            label: f.label,
            fieldType: f.fieldType,
            isRequired: f.isRequired,
            regexPattern: f.regexPattern,
          }))
        )
      }
    } catch (err) {
      setError(err.message)
    } finally {
      setLoading(false)
    }
  }
  load()
}, [formId])

  // ============================================================
  // FIELD ADD / REMOVE
  // ============================================================
const addField = (field) => {
  if (selectedFields.some((f) => f.id === field.id)) return
  setSelectedFields((prev) => [
    ...prev,
    {
      ...field,
      // default from library, but overridable per form
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

  // ============================================================
  // FILTER LIBRARY
  // ============================================================
  const filtered = libraryFields.filter((f) =>
    f.label.toLowerCase().includes(search.toLowerCase())
  )

  // ============================================================
  // SAVE (placeholder — backend form API baad me)
  // ============================================================
const save = async () => {
  if (!formName.trim()) {
    setError('Form name is required.')
    return
  }
  if (selectedFields.length === 0) {
    setError('Add at least one field.')
    return
  }

  setSaving(true)
  setError('')

  try {
      const payload = {
      name: formName.trim(),
      description: formDescription.trim() || null,
      fields: selectedFields.map((f) => ({
        libraryFieldId: f.id,
        isRequiredOverride: f.isRequired ?? null,
      })),
    }

    if (formId) {
      await api(`/forms/${formId}`, { method: 'PUT', body: payload })
      window.alert('Form updated successfully.')
    } else {
      const created = await api('/forms', { method: 'POST', body: payload })
      window.alert('Form created successfully.')
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
           <button
  className="btn btn-primary"
  onClick={save}
  disabled={saving}
>
  {saving ? 'Saving…' : 'Save Form'}
</button>
          </div>
        }
      />

      <ErrorBanner message={error} />

      <div className="form-builder-shell">
        {/* LEFT — Library */}
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
                    disabled={alreadyAdded}
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

        {/* RIGHT — Form Canvas */}
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
            />
          </label>

          <div className="designer-section-title" style={{ marginTop: 20 }}>
            Form Fields ({selectedFields.length})
          </div>

          {selectedFields.length === 0 ? (
            <div className="inline-empty" style={{ padding: 24 }}>
              Click fields on the left to add them to this form.
            </div>
          ) : (
            <div className="form-canvas-list">
              {/* {selectedFields.map((field, index) => (
                <div className="form-canvas-item" key={field.id}>
                  <div className="form-canvas-item-info">
                    <strong>{field.label}</strong>
                    <div className="muted-text">
                      {field.fieldType}
                      {field.isRequired ? ' · Required' : ''}
                      {field.regexPattern ? ' · Regex' : ''}
                    </div>
                  </div>

                  <div className="form-canvas-item-actions">
                    <button
                      className="btn btn-secondary btn-sm"
                      onClick={() => moveField(index, -1)}
                      disabled={index === 0}
                    >
                      ↑
                    </button>
                    <button
                      className="btn btn-secondary btn-sm"
                      onClick={() => moveField(index, 1)}
                      disabled={index === selectedFields.length - 1}
                    >
                      ↓
                    </button>
                    <button
                      className="btn btn-danger-soft btn-sm"
                      onClick={() => removeField(field.id)}
                    >
                      ×
                    </button>
                  </div>
                </div>
              ))} */}
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
      {/* ---- Required toggle for THIS form ---- */}
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
        />
        Required
      </label>

      <button
        className="btn btn-secondary btn-sm"
        onClick={() => moveField(index, -1)}
        disabled={index === 0}
      >
        ↑
      </button>
      <button
        className="btn btn-secondary btn-sm"
        onClick={() => moveField(index, 1)}
        disabled={index === selectedFields.length - 1}
      >
        ↓
      </button>
      <button
        className="btn btn-danger-soft btn-sm"
        onClick={() => removeField(field.id)}
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