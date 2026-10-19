dotnet ef database update --project .\src\BankingPlatform.Infrastructure --startup-project .\src\BankingPlatform.Api



$env:ASPNETCORE_ENVIRONMENT="Development"
dotnet run --project .\src\BankingPlatform.Api --urls http://localhost:5080


$env:ASPNETCORE_ENVIRONMENT="Development"
dotnet run --project .\src\BankingPlatform.WorkflowHost --urls http://localhost:5081


dotnet --version
node -v
npm -v
git --version



dotnet tool install --global dotnet-ef --version 8.0.0
dotnet tool update --global dotnet-ef --version 8.0.0

dotnet restore .\BankingPlatform.slnx

Dotnet ef migrations add NewSchema --project src/BankingPlatform.Infrastructure
--startup-project src/BankingPlatform.Api
dotnet ef database update --project .\src\BankingPlatform.Infrastructure --startup-project .\src\BankingPlatform.Api

$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --project .\src\BankingPlatform.Api --urls http://localhost:5080

$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --project .\src\BankingPlatform.WorkflowHost --urls http://localhost:5081

cd C:\Projects\Alfalah-Complain-Management-in-db\frontend
npm install
npm run dev

D:\Umer\Workflow\Alfalah-Complain-Management-in-db-main\frontend\src\pages\eform\FieldLibraryPage.jsx

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
                </div>

                {/* Only render the row/field if field type is NOT attachment */}
                {draft.fieldType !== 'attachment' && (
                  <div className="form-row">
                    <label className="field compact">
                      <span>Placeholder</span>
                      <input
                        className="input"
                        value={draft.placeholder}
                        onChange={(e) =>
                          update({ placeholder: e.target.value })
                        }
                        placeholder="e.g. 42101-111111-1"
                      />
                    </label>
                  </div>
                )}
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
                      className={`test-result ${testResult === true ? 'ok' : 'fail'
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

D:\Umer\Workflow\Alfalah-Complain-Management-in-db-main\frontend\src\pages\NewComplaintPage.jsx





import { useEffect, useState } from 'react'
import { api } from '../api/client.js'
import PageHeader from '../components/PageHeader.jsx'
import ErrorBanner from '../components/ErrorBanner.jsx'

export default function NewComplaintPage({ navigate }) {
  const [categories, setCategories] = useState([])
  const [form, setForm] = useState({ categoryId: '', subject: '', description: '', customerReference: '', priority: 1 })
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const [cnic, setCnic] = useState('');
  const [isCnicSubmitted, setIsCnicSubmitted] = useState(false);

  useEffect(() => {
    api('/reference/categories')
      .then((items) => {
        setCategories(items)
        if (items.length) setForm((current) => ({ ...current, categoryId: items[0].id }))
      })
      .catch((err) => setError(err.message))
  }, [])


  const handleCnicSubmit = (e) => {
    e.preventDefault();
    if (cnic.trim().length > 0) {
      // Simulated database/API lookup based on CNIC string
      setCustomerDetails({
        name: "Muhammad Ali",
        phone: "+92 300 1234567",
        email: "m.ali@example.com"
      });
      setIsCnicSubmitted(true);
    }
  };

  const [customerDetails, setCustomerDetails] = useState({
    name: '',
    phone: '',
    email: ''
  });

  const submit = async (e) => {
    e.preventDefault()
    setSaving(true)
    setError('')
    try {
      const created = await api('/complaints', { method: 'POST', body: { ...form, priority: Number(form.priority) } })
      navigate(`/complaints/${created.id}`)
    } catch (err) {
      setError(err.message)
    } finally {
      setSaving(false)
    }
  }

  return (
    <>
      <PageHeader
        eyebrow="NEW COMPLAINT"
        title="Log a complaint"
        description="The selected category determines which published workflow starts automatically."
      />
      <ErrorBanner message={error} />

      {/* BLOCK 1: Always Visible CNIC Entry Form */}
      <form className="panel form-panel" onSubmit={handleCnicSubmit} style={{ marginBottom: '1.5rem' }}>
        <div className="designer-section-title" style={{ marginBottom: '1rem', fontWeight: 'bold' }}>
          Identity Verification
        </div>
        <label className="field">
          <span>CNIC Number <span className="req">*</span></span>
          <input
            className="input"
            type="text"
            value={cnic}
            onChange={(e) => setCnic(e.target.value)}
            placeholder="e.g. 42101-111111-1"
            required
            maxLength={15}
            disabled={isCnicSubmitted} // Locks field once details are loaded
          />
        </label>

        {!isCnicSubmitted && (
          <div className="form-actions">
            <button type="button" className="btn btn-secondary" onClick={() => navigate('/complaints')}>
              Cancel
            </button>
            <button type="submit" className="btn btn-primary">
              Verify CNIC
            </button>
          </div>
        )}
      </form>

      {/* BLOCK 2: Appears below CNIC form upon successful verification */}
      {isCnicSubmitted && (
        <>
          {/* Profile Details Block */}
          <div className="panel data-panel" style={{ width: '920px', padding: '1.5rem', marginBottom: '1.5rem', backgroundColor: '#f9f9f9', borderRadius: '4px', border: '1px solid #e3e3e3' }}>
            <div className="designer-section-title" style={{ marginBottom: '1rem', fontWeight: 'bold', fontSize: '1.1rem' }}>
              Associated Customer Record
            </div>
            <div className="form-grid two" style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem' }}>
              <div>
                <span style={{ display: 'block', fontSize: '0.85rem', color: '#666' }}>Full Name</span>
                <strong style={{ fontSize: '1rem' }}>{customerDetails.name}</strong>
              </div>
              <div>
                <span style={{ display: 'block', fontSize: '0.85rem', color: '#666' }}>Phone Number</span>
                <strong style={{ fontSize: '1rem' }}>{customerDetails.phone}</strong>
              </div>
              <div style={{ gridColumn: 'span 2' }}>
                <span style={{ display: 'block', fontSize: '0.85rem', color: '#666' }}>Email Address</span>
                <strong style={{ fontSize: '1rem' }}>{customerDetails.email}</strong>
              </div>
            </div>
            <div style={{ marginTop: '1rem', textAlign: 'right' }}>
              <button
                type="button"
                className="btn btn-link"
                style={{ fontSize: '0.85rem', color: '#0056b3', background: 'none', border: 'none', cursor: 'pointer', padding: 0 }}
                onClick={() => setIsCnicSubmitted(false)}
              >
                Change CNIC / Reset
              </button>
            </div>
          </div>

          {/* BLOCK 3: The Primary Complaint Entry Form */}
          <form className="panel form-panel" onSubmit={submit}>
            <div className="designer-section-title" style={{ marginBottom: '1rem', fontWeight: 'bold' }}>
              Complaint Details
            </div>
            <div className="form-grid two">
              <label className="field">
                <span>Related to</span>
                <select className="input" value={form.categoryId} onChange={(e) => setForm({ ...form, categoryId: e.target.value })} required>
                  {categories.map((x) => <option key={x.id} value={x.id}>{x.name}</option>)}
                </select>
              </label>
              <label className="field">
                <span>Priority</span>
                <select className="input" value={form.priority} onChange={(e) => setForm({ ...form, priority: e.target.value })}>
                  <option value="0">Low</option>
                  <option value="1">Normal</option>
                  <option value="2">High</option>
                  <option value="3">Critical</option>
                </select>
              </label>
            </div>
            <label className="field">
              <span>Customer / transaction reference</span>
              <input className="input" value={form.customerReference} onChange={(e) => setForm({ ...form, customerReference: e.target.value })} placeholder="Optional customer ID, account reference or transaction ID" />
            </label>
            <label className="field">
              <span>Subject</span>
              <input className="input" value={form.subject} onChange={(e) => setForm({ ...form, subject: e.target.value })} placeholder="Short description of the complaint" required maxLength={300} />
            </label>
            <label className="field">
              <span>Description</span>
              <textarea className="input textarea" value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })} placeholder="Add the complaint details, customer impact and relevant context…" required rows={8} />
            </label>
            <div className="form-actions">
              <button type="button" className="btn btn-secondary" onClick={() => navigate('/complaints')}>
                Cancel
              </button>
              <button className="btn btn-primary" disabled={saving || !form.categoryId}>
                {saving ? 'Logging…' : 'Log complaint'}
              </button>
            </div>
          </form>
        </>
      )}
    </>
  )
}




USE [BankingPlatform2];
GO

DECLARE @alfaMallId UNIQUEIDENTIFIER = 
    (SELECT Id FROM ComplaintCategories WHERE Code = 'ALFA_MALL');
DECLARE @atmId UNIQUEIDENTIFIER = 
    (SELECT Id FROM ComplaintCategories WHERE Code = 'ATM');
DECLARE @txnId UNIQUEIDENTIFIER = 
    (SELECT Id FROM ComplaintCategories WHERE Code = 'TRANSACTION');

-- Alfa Mall products
INSERT INTO Products (Id, CategoryId, Name, IsActive, CreatedAtUtc)
VALUES 
  (NEWID(), @alfaMallId, 'Voucher Issue',       1, GETUTCDATE()),
  (NEWID(), @alfaMallId, 'Points Not Credited', 1, GETUTCDATE()),
  (NEWID(), @alfaMallId, 'Refund Delayed',      1, GETUTCDATE()),
  (NEWID(), @alfaMallId, 'Product Quality',     1, GETUTCDATE());

-- ATM products
INSERT INTO Products (Id, CategoryId, Name, IsActive, CreatedAtUtc)
VALUES 
  (NEWID(), @atmId, 'Cash Not Dispensed', 1, GETUTCDATE()),
  (NEWID(), @atmId, 'Card Retained',      1, GETUTCDATE()),
  (NEWID(), @atmId, 'Wrong Balance',      1, GETUTCDATE());

-- Transaction products
INSERT INTO Products (Id, CategoryId, Name, IsActive, CreatedAtUtc)
VALUES 
  (NEWID(), @txnId, 'Failed Transaction',  1, GETUTCDATE()),
  (NEWID(), @txnId, 'Pending Transaction', 1, GETUTCDATE()),
  (NEWID(), @txnId, 'Reversal Issue',      1, GETUTCDATE());

PRINT 'Inserted.';
GO
///////////////////////////////////////////////////////////
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BankingPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLabelOverrideFromFormField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LabelOverride",
                table: "FormFields");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LabelOverride",
                table: "FormFields",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }
    }
}



