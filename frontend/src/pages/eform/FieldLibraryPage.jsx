import { useEffect, useState } from 'react'
import { api } from '../../api/client.js'
import PageHeader from '../../components/PageHeader.jsx'
import ErrorBanner from '../../components/ErrorBanner.jsx'
import './eform.css'
import Pagination from '../../components/Pagination.jsx'
import usePagination from '../../hooks/usePagination.js'

// ============================================================
// CONSTANTS
// ============================================================
const FIELD_TYPES = [
  { value: 'text', label: 'Text' },
  { value: 'textarea', label: 'Text Area' },
  { value: 'number', label: 'Number' },
  { value: 'date', label: 'Date' },
  { value: 'dropdown', label: 'Dropdown' },
  { value: 'radio', label: 'Radio' },
  { value: 'checkbox', label: 'Checkbox' },
  { value: 'attachment', label: 'Attachment' },
]

const ATTACHMENT_TYPES = [
  { value: 'pdf', label: 'PDF' },
  { value: 'jpg', label: 'JPG' },
  { value: 'png', label: 'PNG' },
  { value: 'docx', label: 'DOCX' },
  { value: 'xlsx', label: 'XLSX' },
]

function emptyField() {
  return {
    fieldKey: '',
    label: '',
    fieldType: 'text',
    placeholder: '',
    isRequired: false,
    options: [],
    regexPattern: '',
    regexMessage: '',
    minLength: null,
    maxLength: null,
    minValue: null,
    maxValue: null,
    validationType: '',
    allowedFileTypes: [],
    maxFileSizeMb: 5,
    allowMultiple: false,
  }
}

export default function FieldLibraryPage({ navigate }) {
  const [fields, setFields] = useState([])
  const [presets, setPresets] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [saving, setSaving] = useState(false)

  const [showModal, setShowModal] = useState(false)
  const [editingId, setEditingId] = useState(null)
  const [draft, setDraft] = useState(emptyField())
  const [testValue, setTestValue] = useState('')
  const [optionsText, setOptionsText] = useState('')
  const {
    page,
    setPage,
    pageSize,
    setPageSize,
    total,
    pageItems: pagedFields,
  } = usePagination(fields, 10)
  // ============================================================
  // LOAD
  // ============================================================
  const load = async () => {
    setLoading(true)
    try {
      const [fieldsData, presetsData] = await Promise.all([
        api('/library-fields'),
        api('/library-fields/regex-presets'),
      ])
      setFields(Array.isArray(fieldsData) ? fieldsData : [])
      setPresets(Array.isArray(presetsData) ? presetsData : [])
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
  // MODAL
  // ============================================================
  const openAdd = () => {
    setEditingId(null)
    setDraft(emptyField())
    setOptionsText('')
    setTestValue('')
    setShowModal(true)
    setError('')
  }

  const openEdit = (field) => {
    setEditingId(field.id)
    setDraft({
      fieldKey: field.fieldKey,
      label: field.label,
      fieldType: field.fieldType,
      placeholder: field.placeholder || '',
      isRequired: !!field.isRequired,
      options: field.options || [],
      regexPattern: field.regexPattern || '',
      regexMessage: field.regexMessage || '',
      minLength: field.minLength ?? null,
      maxLength: field.maxLength ?? null,
      minValue: field.minValue ?? null,
      maxValue: field.maxValue ?? null,
      validationType: field.validationType || '',
      allowedFileTypes: field.allowedFileTypes || [],
      maxFileSizeMb: field.maxFileSizeMb ?? 5,
      allowMultiple: field.allowMultiple ?? false,
    })
    setOptionsText((field.options || []).join('\n'))
    setTestValue('')
    setShowModal(true)
    setError('')
  }

  const closeModal = () => {
    setShowModal(false)
    setEditingId(null)
    setDraft(emptyField())
    setOptionsText('')
    setTestValue('')
    setError('')
  }

  const update = (patch) => setDraft((d) => ({ ...d, ...patch }))

  // ============================================================
  // SAVE
  // ============================================================
  const save = async () => {
    if (!draft.label.trim()) {
      setError('Label is required.')
      return
    }

    setSaving(true)
    setError('')

    try {
      const payload = {
        fieldKey:
          draft.fieldKey.trim() ||
          draft.label
            .trim()
            .toLowerCase()
            .replace(/[^a-z0-9]+/g, '_')
            .replace(/^_+|_+$/g, ''),
        label: draft.label.trim(),
        fieldType: draft.fieldType,
        placeholder: draft.placeholder?.trim() || null,
        isRequired: !!draft.isRequired,
        options:
          draft.fieldType === 'dropdown' || draft.fieldType === 'radio'
            ? optionsText
                .split('\n')
                .map((x) => x.trim())
                .filter(Boolean)
            : null,
        regexPattern: draft.regexPattern?.trim() || null,
        regexMessage: draft.regexMessage?.trim() || null,
        minLength: draft.minLength ?? null,
        maxLength: draft.maxLength ?? null,
        minValue: draft.minValue ?? null,
        maxValue: draft.maxValue ?? null,
        validationType: draft.validationType?.trim() || null,
        allowedFileTypes:
          draft.fieldType === 'attachment' ? draft.allowedFileTypes || [] : null,
        maxFileSizeMb:
          draft.fieldType === 'attachment' ? draft.maxFileSizeMb ?? 5 : null,
        allowMultiple:
          draft.fieldType === 'attachment' ? !!draft.allowMultiple : null,
      }

      if (editingId) {
        const { fieldKey, ...updatePayload } = payload
        await api(`/library-fields/${editingId}`, {
          method: 'PUT',
          body: updatePayload,
        })
      } else {
        await api('/library-fields', { method: 'POST', body: payload })
      }

      closeModal()
      await load()
    } catch (err) {
      console.error('SAVE FAILED:', err)
      setError(err.message)
    } finally {
      setSaving(false)
    }
  }

  // ============================================================
  // DELETE
  // ============================================================
  const remove = async (id, label) => {
    if (!window.confirm(`Delete field "${label}"? This cannot be undone.`)) return
    try {
      await api(`/library-fields/${id}`, { method: 'DELETE' })
      await load()
    } catch (err) {
      setError(err.message)
    }
  }

  // ============================================================
  // REGEX TEST
  // ============================================================
  const testResult = (() => {
    if (!draft.regexPattern || !testValue) return null
    try {
      return new RegExp(draft.regexPattern).test(testValue)
    } catch {
      return 'invalid'
    }
  })()

  const showValidationSection =
    draft.fieldType === 'text' ||
    draft.fieldType === 'textarea' ||
    draft.fieldType === 'number'

  const showOptionsSection =
    draft.fieldType === 'dropdown' || draft.fieldType === 'radio'

  const showAttachmentSection = draft.fieldType === 'attachment'

  // ============================================================
  // RENDER
  // ============================================================
  return (
    <>
      <PageHeader
        eyebrow="E-FORM BUILDER"
        title="Field Library"
        description="Reusable fields available across all forms"
        actions={
          <div className="page-actions">
            <button
              className="btn btn-secondary"
              onClick={() => navigate('/forms')}
            >
              ← Back to Forms
            </button>
            <button className="btn btn-primary" onClick={openAdd}>
              + Add Field
            </button>
          </div>
        }
      />

      <ErrorBanner message={error} />

<section className="panel">
  {loading ? (
    <div className="loading-block">Loading fields…</div>
  ) : fields.length === 0 ? (
    <div className="inline-empty" style={{ margin: 20 }}>
      No fields yet. Click "Add Field" to create one.
    </div>
  ) : (
    <>
      <table className="data-table">
        <thead>
          <tr>
            <th>Label</th>
            <th>Key</th>
            <th>Type</th>
            <th>Validation</th>
            <th>Required</th>
            <th style={{ width: 170 }}>Actions</th>
          </tr>
        </thead>

        <tbody>
          {pagedFields.map((f) => (
            <tr key={f.id}>
              <td>
                <strong>{f.label}</strong>
              </td>

              <td>
                <code>{f.fieldKey}</code>
              </td>

              <td>
                <span className="badge badge-type">
                  {f.fieldType}
                </span>
              </td>

              <td>
                {f.regexPattern ? (
                  <span className="badge badge-info">
                    {f.validationType
                      ? f.validationType.toUpperCase()
                      : "REGEX"}
                  </span>
                ) : (
                  <span className="muted">—</span>
                )}
              </td>

              <td>
                {f.isRequired ? (
                  <span className="badge badge-success">
                    Yes
                  </span>
                ) : (
                  <span className="muted">No</span>
                )}
              </td>

              <td>
                <button
                  className="btn btn-secondary btn-sm"
                  onClick={() => openEdit(f)}
                >
                  Edit
                </button>

                <button
                  className="btn btn-danger-soft btn-sm"
                  style={{ marginLeft: 6 }}
                  onClick={() => remove(f.id, f.label)}
                >
                  Delete
                </button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>

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
      {/* =========================================================
          ADD / EDIT MODAL
      ========================================================== */}
      {showModal && (
        <div
          className="modal-backdrop"
          onMouseDown={(e) => {
            if (e.target === e.currentTarget) closeModal()
          }}
        >
          <div className="modal modal-wide">
            {/* ---------- HEADER ---------- */}
            <div className="modal-header">
              <div>
                <h2>{editingId ? 'Edit Field' : 'Add Field'}</h2>
                <p className="muted-text">
                  {editingId
                    ? 'Update the field configuration'
                    : 'Create a reusable field for all forms'}
                </p>
              </div>
              <button className="icon-button" onClick={closeModal}>
                ×
              </button>
            </div>

            {/* ---------- BODY ---------- */}
            <div className="modal-body-scroll">
              <ErrorBanner message={error} />

              {/* ==================================================
                  SECTION 1 — BASIC INFORMATION
              ================================================== */}
              <div className="section-block">
                <div className="designer-section-title">
                  Basic Information
                </div>

                <div className="form-row">
                  <label className="field compact">
                    <span>
                      Label <span className="req">*</span>
                    </span>
                    <input
                      className="input"
                      value={draft.label}
                      onChange={(e) => update({ label: e.target.value })}
                      placeholder="e.g. CNIC Number"
                      autoFocus
                    />
                  </label>

                  <label className="field compact">
                    <span>Field Key</span>
                    <input
                      className="input mono"
                      value={draft.fieldKey}
                      onChange={(e) => update({ fieldKey: e.target.value })}
                      placeholder="cnic_number"
                      disabled={!!editingId}
                    />
                    <small className="muted-text">
                      {editingId
                        ? 'Cannot be changed after creation'
                        : 'Leave empty to auto-generate'}
                    </small>
                  </label>
                </div>

                <div className="form-row">
                  <label className="field compact">
                    <span>Field Type</span>
                    <select
                      className="input"
                      value={draft.fieldType}
                      onChange={(e) => update({ fieldType: e.target.value })}
                    >
                      {FIELD_TYPES.map((t) => (
                        <option key={t.value} value={t.value}>
                          {t.label}
                        </option>
                      ))}
                    </select>
                  </label>

                  <label className="field compact">
                    <span>Placeholder</span>
                    <input
                      className="input"
                      value={draft.placeholder}
                      onChange={(e) =>
                        update({ placeholder: e.target.value })
                      }
                      placeholder="e.g. 42101-1234567-1"
                    />
                  </label>
                </div>
{/* 
                <label className="checkbox-field">
                  <input
                    type="checkbox"
                    checked={!!draft.isRequired}
                    onChange={(e) =>
                      update({ isRequired: e.target.checked })
                    }
                  />
                  Mark as required field
                </label> */}
              </div>

              {/* ==================================================
                  SECTION 2 — OPTIONS
              ================================================== */}
              {showOptionsSection && (
                <div className="section-block">
                  <div className="designer-section-title">Options</div>

                  <label className="field compact">
                    <span>One option per line</span>
                    <textarea
                      className="input textarea"
                      rows={5}
                      value={optionsText}
                      onChange={(e) => setOptionsText(e.target.value)}
                      placeholder={'Option 1\nOption 2\nOption 3'}
                    />
                    <small className="muted-text">
                      {
                        optionsText
                          .split('\n')
                          .map((x) => x.trim())
                          .filter(Boolean).length
                      }{' '}
                      option(s)
                    </small>
                  </label>
                </div>
              )}

              {/* ==================================================
                  SECTION 3 — ATTACHMENT SETTINGS
              ================================================== */}
              {showAttachmentSection && (
                <div className="section-block">
                  <div className="designer-section-title">
                    Attachment Settings
                  </div>

                  <div className="field compact">
                    <span>Allowed File Types</span>
                    <div className="chip-row">
                      {ATTACHMENT_TYPES.map((t) => {
                        const checked = (
                          draft.allowedFileTypes || []
                        ).includes(t.value)
                        return (
                          <button
                            type="button"
                            key={t.value}
                            className={`chip ${checked ? 'chip-active' : ''}`}
                            onClick={() => {
                              const cur = draft.allowedFileTypes || []
                              update({
                                allowedFileTypes: cur.includes(t.value)
                                  ? cur.filter((x) => x !== t.value)
                                  : [...cur, t.value],
                              })
                            }}
                          >
                            {checked ? '✓ ' : ''}
                            {t.label}
                          </button>
                        )
                      })}
                    </div>
                    <small className="muted-text">
                      Select all file types allowed for this field
                    </small>
                  </div>

                  <div className="form-row">
                    <label className="field compact">
                      <span>Max File Size (MB)</span>
                      <input
                        className="input"
                        type="number"
                        min="1"
                        max="100"
                        value={draft.maxFileSizeMb ?? 5}
                        onChange={(e) =>
                          update({ maxFileSizeMb: Number(e.target.value) })
                        }
                      />
                    </label>

                    <div className="field compact">
                      <span>Multiple Files</span>
                      <label
                        className="checkbox-field"
                        style={{ margin: '10px 0' }}
                      >
                        <input
                          type="checkbox"
                          checked={!!draft.allowMultiple}
                          onChange={(e) =>
                            update({ allowMultiple: e.target.checked })
                          }
                        />
                        Allow uploading multiple files
                      </label>
                    </div>
                  </div>
                </div>
              )}

              {/* ==================================================
                  SECTION 4 — VALIDATION
              ================================================== */}
              {showValidationSection && (
                <div className="section-block">
                  <div className="designer-section-title">Validation</div>

                  <label className="field compact">
                    <span>Regex Preset</span>
                    <select
                      className="input"
                      value={draft.validationType || ''}
                      onChange={(e) => {
                        const key = e.target.value
                        if (!key) {
                          update({
                            validationType: '',
                            regexPattern: '',
                            regexMessage: '',
                          })
                          return
                        }
                        const preset = presets.find((p) => p.key === key)
                        if (preset) {
                          update({
                            validationType: key,
                            regexPattern: preset.pattern,
                            regexMessage: preset.message,
                          })
                        }
                      }}
                    >
                      <option value="">None — no preset</option>
                      {presets.map((p) => (
                        <option key={p.key} value={p.key}>
                          {p.label}
                        </option>
                      ))}
                    </select>
                  </label>

                  <label className="field compact">
                    <span>Regex Pattern</span>
                    <input
                      className="input mono"
                      value={draft.regexPattern}
                      onChange={(e) =>
                        update({ regexPattern: e.target.value })
                      }
                      placeholder="^\d{5}-\d{7}-\d$"
                    />
                  </label>

                  <label className="field compact">
                    <span>Custom Error Message</span>
                    <input
                      className="input"
                      value={draft.regexMessage}
                      onChange={(e) =>
                        update({ regexMessage: e.target.value })
                      }
                      placeholder="Enter valid CNIC (42101-1234567-1)"
                    />
                  </label>

                  <div className="form-row">
                    <label className="field compact">
                      <span>Min Length</span>
                      <input
                        className="input"
                        type="number"
                        min="0"
                        value={draft.minLength ?? ''}
                        onChange={(e) =>
                          update({
                            minLength: e.target.value
                              ? Number(e.target.value)
                              : null,
                          })
                        }
                        placeholder="—"
                      />
                    </label>

                    <label className="field compact">
                      <span>Max Length</span>
                      <input
                        className="input"
                        type="number"
                        min="0"
                        value={draft.maxLength ?? ''}
                        onChange={(e) =>
                          update({
                            maxLength: e.target.value
                              ? Number(e.target.value)
                              : null,
                          })
                        }
                        placeholder="—"
                      />
                    </label>
                  </div>

                  {draft.fieldType === 'number' && (
                    <div className="form-row">
                      <label className="field compact">
                        <span>Min Value</span>
                        <input
                          className="input"
                          type="number"
                          value={draft.minValue ?? ''}
                          onChange={(e) =>
                            update({
                              minValue: e.target.value
                                ? Number(e.target.value)
                                : null,
                            })
                          }
                          placeholder="—"
                        />
                      </label>

                      <label className="field compact">
                        <span>Max Value</span>
                        <input
                          className="input"
                          type="number"
                          value={draft.maxValue ?? ''}
                          onChange={(e) =>
                            update({
                              maxValue: e.target.value
                                ? Number(e.target.value)
                                : null,
                            })
                          }
                          placeholder="—"
                        />
                      </label>
                    </div>
                  )}
                </div>
              )}

              {/* ==================================================
                  SECTION 5 — TEST YOUR REGEX
              ================================================== */}
              {showValidationSection && draft.regexPattern && (
                <div className="section-block section-block--test">
                  <div className="designer-section-title">
                    Test Your Regex
                  </div>

                  <label className="field compact">
                    <span>Sample Value</span>
                    <input
                      className="input mono"
                      value={testValue}
                      onChange={(e) => setTestValue(e.target.value)}
                      placeholder="Type a value to test…"
                    />
                  </label>

                  {testValue && (
                    <div
                      className={`test-result ${
                        testResult === true ? 'ok' : 'fail'
                      }`}
                    >
                      {testResult === true &&
                        '✓ Valid — matches the pattern'}
                      {testResult === false &&
                        '✗ Invalid — does not match the pattern'}
                      {testResult === 'invalid' &&
                        '✗ Invalid regex pattern'}
                    </div>
                  )}

                  {!testValue && (
                    <small className="muted-text">
                      Type a value above to verify your regex pattern
                    </small>
                  )}
                </div>
              )}
            </div>

            {/* ---------- FOOTER ---------- */}
            <div className="modal-actions">
              <button className="btn btn-secondary" onClick={closeModal}>
                Cancel
              </button>
              <button
                className="btn btn-primary"
                onClick={save}
                disabled={saving}
              >
                {saving
                  ? 'Saving…'
                  : editingId
                  ? 'Save Changes'
                  : 'Add Field'}
              </button>
            </div>
          </div>
        </div>
      )}
    </>
  )
}