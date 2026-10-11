import { useMemo, useState } from 'react'

// ============================================================
// HELPERS
// ============================================================

function displayValue(value) {
  if (value === null || value === undefined || value === '') return '—'
  if (Array.isArray(value)) return value.join(', ')
  if (typeof value === 'boolean') return value ? 'Yes' : 'No'
  if (typeof value === 'object') return JSON.stringify(value)
  return String(value)
}

function asAttachments(value) {
  if (!value) return []
  if (Array.isArray(value)) {
    return value.filter(
      (v) => v && typeof v === 'object' && v.attachmentId
    )
  }
  if (typeof value === 'object' && value.attachmentId) return [value]
  return []
}

function formatFileSize(bytes) {
  if (!bytes && bytes !== 0) return ''
  if (bytes < 1024) return `${bytes} B`
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
  return `${(bytes / (1024 * 1024)).toFixed(2)} MB`
}

// ============================================================
// VALIDATION FUNCTION
// ============================================================
function validateField(field, value) {
  const empty =
    value === null ||
    value === undefined ||
    value === '' ||
    (Array.isArray(value) && value.length === 0)

  // Required check (skip attachment — handled separately)
  if (
    field.isRequired &&
    field.fieldType !== 'attachment' &&
    empty
  ) {
    return 'This field is required'
  }

  if (empty) return ''

  // Length checks (strings only)
  if (typeof value === 'string') {
    if (field.minLength != null && value.length < field.minLength) {
      return `Minimum ${field.minLength} characters`
    }
    if (field.maxLength != null && value.length > field.maxLength) {
      return `Maximum ${field.maxLength} characters`
    }
  }

  // Number range
  if (field.fieldType === 'number') {
    const num = Number(value)
    if (field.minValue != null && num < field.minValue) {
      return `Minimum value is ${field.minValue}`
    }
    if (field.maxValue != null && num > field.maxValue) {
      return `Maximum value is ${field.maxValue}`
    }
  }

  // Regex check
  if (field.regexPattern && typeof value === 'string') {
    try {
      const regex = new RegExp(field.regexPattern)
      if (!regex.test(value)) {
        return field.regexMessage || 'Invalid format'
      }
    } catch (e) {
      // invalid regex on backend — skip
    }
  }

  return ''
}

// ============================================================
// COMPONENT
// ============================================================
export default function WorkflowTaskFields({
  form,
  values,
  onChange,
}) {
  if (!form) return null

  // Track validation errors per field
  const [errors, setErrors] = useState({})

  const setValue = (fieldId, value) => {
    onChange({
      ...values,
      [fieldId]: value,
    })

    // Validate on change
    const field = form.currentFields?.find((f) => f.id === fieldId)
    if (field) {
      const err = validateField(field, value)
      setErrors((prev) => ({ ...prev, [fieldId]: err }))
    }
  }

  return (
    <div className="workflow-task-form">
      {/* ==========================================================
          PREVIOUS STEPS
      ========================================================== */}
      {form.previousSteps?.map((step) => (
        <section
          className="previous-step-fields"
          key={step.workflowTaskId}
        >
          <div className="designer-section-title">
            {step.nodeName}
          </div>

          <small>
            Completed by {step.submittedBy || '—'}
          </small>

          {step.fields.map((field) => {
            const isAttachment =
              field.fieldType?.toLowerCase() === 'attachment'

            const attachments = isAttachment
              ? asAttachments(field.value)
              : []

            return (
              <label
                className="field"
                key={`${step.workflowTaskId}-${field.fieldKey}`}
              >
                <span>{field.label}</span>

                {isAttachment ? (
                  attachments.length > 0 ? (
                    <div className="prev-attachment-list">
                      {attachments.map((att) => (
                        <div
                          className="prev-attachment-row"
                          key={att.attachmentId}
                        >
                          <span className="attachment-check">✓</span>
                          <span className="attachment-name">
                            {att.fileName}
                          </span>
                          <a
                            className="view-btn"
                            href={`/api/workflow-tasks/attachments/${att.attachmentId}/view`}
                            target="_blank"
                            rel="noreferrer"
                          >
                            View
                          </a>
                          <a
                            className="download-btn"
                            href={`/api/workflow-tasks/attachments/${att.attachmentId}/download`}
                          >
                            Download
                          </a>
                        </div>
                      ))}
                    </div>
                  ) : (
                    <input className="input" value="—" readOnly />
                  )
                ) : (
                  <input
                    className="input"
                    value={displayValue(field.value)}
                    readOnly
                  />
                )}
              </label>
            )
          })}
        </section>
      ))}

      {/* ==========================================================
          CURRENT STEP
      ========================================================== */}
      {form.currentFields?.length > 0 && (
        <section className="current-step-fields">
          <div className="designer-section-title">
            {form.nodeName}
          </div>

          {form.currentFields.map((field) => {
            const value = values[field.id] ?? ''
            const error = errors[field.id] || ''
            const hasError = !!error

            /* ============================================================
               ATTACHMENT
            ============================================================ */
            if (field.fieldType === 'attachment') {
              const acceptTypes = (field.allowedFileTypes || [])
                .flatMap((type) => {
                  switch (type.toLowerCase()) {
                    case 'pdf': return ['.pdf']
                    case 'jpg': return ['.jpg', '.jpeg']
                    case 'jpeg': return ['.jpg', '.jpeg']
                    case 'png': return ['.png']
                    case 'docx': return ['.docx']
                    default: return [`.${type}`]
                  }
                })
                .join(',')

              const selectedFiles = Array.isArray(value)
                ? value
                : value
                  ? [value]
                  : []

              const inputId = `file-${field.id}`

              const handleFilesChosen = (event) => {
                const incoming = Array.from(event.target.files || [])
                const maxSizeMb = Number(field.maxFileSizeMb || 5)
                const maxSizeBytes = maxSizeMb * 1024 * 1024

                const valid = []
                const invalid = []

                incoming.forEach((file) => {
                  if (file.size <= maxSizeBytes) valid.push(file)
                  else invalid.push(file.name)
                })

                if (invalid.length > 0) {
                  window.alert(
                    `These files exceed max size of ${maxSizeMb} MB:\n\n${invalid.join('\n')}`
                  )
                }

                if (field.allowMultiple) {
                  const merged = [...selectedFiles]
                  valid.forEach((f) => {
                    const dup = merged.some(
                      (m) => m.name === f.name && m.size === f.size
                    )
                    if (!dup) merged.push(f)
                  })
                  setValue(field.id, merged)
                } else {
                  setValue(field.id, valid.length > 0 ? [valid[0]] : [])
                }

                event.target.value = ''
              }

              const removeFile = (index) => {
                const updated = selectedFiles.filter(
                  (_, i) => i !== index
                )
                setValue(field.id, updated)
              }

              const hasFiles = selectedFiles.length > 0

              return (
                <div className="field" key={field.id}>
                  <span>
                    {field.label}
                    {field.isRequired ? ' *' : ''}
                  </span>

                  <input
                    id={inputId}
                    type="file"
                    style={{ display: 'none' }}
                    multiple={field.allowMultiple || false}
                    accept={acceptTypes || undefined}
                    onChange={handleFilesChosen}
                  />

                  <div className="attachment-option">
                    <label
                      htmlFor={inputId}
                      className={`btn ${hasFiles ? 'btn-success' : 'btn-secondary'}`}
                    >
                      {hasFiles ? '✓ Change file' : 'Choose file'}
                    </label>

                    <small className="muted-text">
                      Allowed:{' '}
                      {field.allowedFileTypes?.length > 0
                        ? field.allowedFileTypes.join(', ').toUpperCase()
                        : 'any file type'}
                      {' • '}
                      Max: {field.maxFileSizeMb || 5} MB
                      {field.allowMultiple
                        ? ' • Multiple allowed'
                        : ' • Single file'}
                    </small>
                  </div>

                  {hasFiles && (
                    <div className="attachment-banner">
                      ✓ File{selectedFiles.length > 1 ? 's' : ''} attached
                    </div>
                  )}

                  {hasFiles ? (
                    <div className="attachment-list">
                      {selectedFiles.map((file, index) => (
                        <div
                          className="attachment-row"
                          key={`${file.name}-${index}`}
                        >
                          <span className="attachment-check">✓</span>
                          <span className="attachment-name">
                            {file.name}
                            <span className="attachment-size">
                              {' '}
                              ({formatFileSize(file.size)})
                            </span>
                          </span>
                          <a
                            className="view-btn"
                            href={URL.createObjectURL(file)}
                            target="_blank"
                            rel="noreferrer"
                          >
                            Preview
                          </a>
                          <button
                            type="button"
                            className="link-btn"
                            onClick={() => removeFile(index)}
                          >
                            Remove
                          </button>
                        </div>
                      ))}
                    </div>
                  ) : (
                    <small className="attachment-empty">
                      No file selected yet.
                    </small>
                  )}
                </div>
              )
            }

            /* ============================================================
               TEXTAREA
            ============================================================ */
            if (field.fieldType === 'textarea') {
              return (
                <label className="field" key={field.id}>
                  <span>
                    {field.label}
                    {field.isRequired ? ' *' : ''}
                  </span>

                  <textarea
                    className={`input textarea ${hasError ? 'input-error' : ''}`}
                    value={value}
                    placeholder={field.placeholder || ''}
                    onChange={(e) =>
                      setValue(field.id, e.target.value)
                    }
                  />

                  {hasError && (
                    <div className="field-error">⚠ {error}</div>
                  )}
                </label>
              )
            }

            /* ============================================================
               DROPDOWN / RADIO
            ============================================================ */
            if (
              field.fieldType === 'dropdown' ||
              field.fieldType === 'radio'
            ) {
              return (
                <label className="field" key={field.id}>
                  <span>
                    {field.label}
                    {field.isRequired ? ' *' : ''}
                  </span>

                  <select
                    className={`input ${hasError ? 'input-error' : ''}`}
                    value={value}
                    onChange={(e) =>
                      setValue(field.id, e.target.value)
                    }
                  >
                    <option value="">Select...</option>
                    {(field.options || []).map((option) => (
                      <option key={option} value={option}>
                        {option}
                      </option>
                    ))}
                  </select>

                  {hasError && (
                    <div className="field-error">⚠ {error}</div>
                  )}
                </label>
              )
            }

            /* ============================================================
               CHECKBOX
            ============================================================ */
            if (field.fieldType === 'checkbox') {
              return (
                <label className="checkbox-field" key={field.id}>
                  <input
                    type="checkbox"
                    checked={!!value}
                    onChange={(e) =>
                      setValue(field.id, e.target.checked)
                    }
                  />
                  {field.label}
                  {field.isRequired ? ' *' : ''}
                </label>
              )
            }

            /* ============================================================
               TEXT / NUMBER / DATE (with regex validation)
            ============================================================ */
            return (
              <label className="field" key={field.id}>
                <span>
                  {field.label}
                  {field.isRequired ? ' *' : ''}
                </span>

                <input
                  className={`input ${hasError ? 'input-error' : ''}`}
                  type={
                    field.fieldType === 'number'
                      ? 'number'
                      : field.fieldType === 'date'
                        ? 'date'
                        : 'text'
                  }
                  value={value}
                  placeholder={field.placeholder || ''}
                  onChange={(e) =>
                    setValue(field.id, e.target.value)
                  }
                />

                {hasError && (
                  <div className="field-error">⚠ {error}</div>
                )}

                {!hasError && field.regexMessage && value && (
                  <small className="field-hint">
                    Format: {field.regexMessage}
                  </small>
                )}
              </label>
            )
          })}
        </section>
      )}
    </div>
  )
}