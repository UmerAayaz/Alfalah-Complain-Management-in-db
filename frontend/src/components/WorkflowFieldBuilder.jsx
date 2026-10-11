import { useState } from 'react'

const fieldTypes = [
  ['text', 'Text'],
  ['textarea', 'Text area'],
  ['number', 'Number'],
  ['date', 'Date'],
  ['dropdown', 'Dropdown'],
  ['radio', 'Radio buttons'],
  ['checkbox', 'Checkbox'],
  ['attachment', 'Attachment'],
]

const attachmentTypes = [
  ['pdf', 'PDF'],
  ['jpg', 'JPG'],
  ['png', 'PNG'],
  ['docx', 'DOCX'],
]

function makeFieldKey(label) {
  return label
    .trim()
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '_')
    .replace(/^_+|_+$/g, '')
}

function createEmptyField(displayOrder) {
  return {
    clientId: crypto.randomUUID(),
    fieldKey: '',
    label: '',
    fieldType: 'text',
    placeholder: '',
    isRequired: false,
    displayOrder,
    options: [],

    // Attachment settings
    allowMultiple: false,
    allowedFileTypes: ['pdf'],
    maxFileSizeMb: 5,
  }
}

export default function WorkflowFieldBuilder({
  fields = [],
  onChange,
  disabled = false,
}) {
  const [isModalOpen, setIsModalOpen] = useState(false)
  const [editingIndex, setEditingIndex] = useState(null)
  const [draftField, setDraftField] = useState(null)

  // --------------------------------------------------
  // OPEN ADD MODAL
  // --------------------------------------------------

  const openAddModal = () => {
    setEditingIndex(null)
    setDraftField(createEmptyField(fields.length))
    setIsModalOpen(true)
  }

  // --------------------------------------------------
  // OPEN EDIT MODAL
  // --------------------------------------------------

  const openEditModal = (field, index) => {
    setEditingIndex(index)

    setDraftField({
      ...field,
      options: [...(field.options || [])],

      allowMultiple: field.allowMultiple || false,

      allowedFileTypes: field.allowedFileTypes?.length
        ? [...field.allowedFileTypes]
        : ['pdf'],

      maxFileSizeMb: field.maxFileSizeMb || 5,
    })

    setIsModalOpen(true)
  }

  // --------------------------------------------------
  // CLOSE MODAL
  // --------------------------------------------------

  const closeModal = () => {
    setIsModalOpen(false)
    setEditingIndex(null)
    setDraftField(null)
  }

  // --------------------------------------------------
  // UPDATE MODAL FIELD
  // --------------------------------------------------

  const updateDraftField = (patch) => {
    setDraftField((current) => ({
      ...current,
      ...patch,
    }))
  }

  // --------------------------------------------------
  // SAVE ADD / EDIT
  // --------------------------------------------------

  const saveField = () => {
    if (!draftField) return

    const label = draftField.label.trim()

    if (!label) {
      window.alert('Field label is required.')
      return
    }

    const fieldKey =
      draftField.fieldKey.trim() || makeFieldKey(label)

    if (!fieldKey) {
      window.alert('Field key is required.')
      return
    }

    // Prevent duplicate field keys
    const duplicateKey = fields.some(
      (field, index) =>
        index !== editingIndex &&
        (field.fieldKey || '').trim().toLowerCase() ===
          fieldKey.toLowerCase()
    )

    if (duplicateKey) {
      window.alert(
        `A field with key "${fieldKey}" already exists in this step.`
      )
      return
    }

    // Dropdown / Radio validation
    if (
      (draftField.fieldType === 'dropdown' ||
        draftField.fieldType === 'radio') &&
      (!draftField.options || draftField.options.length === 0)
    ) {
      window.alert('Please add at least one option.')
      return
    }

    // Attachment validation
    if (draftField.fieldType === 'attachment') {
      if (
        !draftField.allowedFileTypes ||
        draftField.allowedFileTypes.length === 0
      ) {
        window.alert(
          'Please select at least one allowed file type.'
        )
        return
      }

      const maxSize = Number(draftField.maxFileSizeMb)

      if (!maxSize || maxSize <= 0) {
        window.alert(
          'Maximum file size must be greater than 0 MB.'
        )
        return
      }
    }

    const fieldToSave = {
      ...draftField,

      label,

      fieldKey,

      // attachment fields don't need a placeholder
      placeholder:
        draftField.fieldType === 'attachment'
          ? ''
          : draftField.placeholder?.trim() || '',

      options: draftField.options || [],

      allowMultiple:
        draftField.fieldType === 'attachment'
          ? !!draftField.allowMultiple
          : false,

      allowedFileTypes:
        draftField.fieldType === 'attachment'
          ? draftField.allowedFileTypes || []
          : [],

      maxFileSizeMb:
        draftField.fieldType === 'attachment'
          ? Number(draftField.maxFileSizeMb || 5)
          : null,
    }

    // EDIT EXISTING FIELD
    if (editingIndex !== null) {
      onChange(
        fields.map((field, index) =>
          index === editingIndex
            ? {
                ...fieldToSave,
                displayOrder: field.displayOrder ?? index,
              }
            : field
        )
      )
    }

    // ADD NEW FIELD
    else {
      onChange([
        ...fields,
        {
          ...fieldToSave,
          displayOrder: fields.length,
        },
      ])
    }

    closeModal()
  }

  // --------------------------------------------------
  // REMOVE FIELD
  // --------------------------------------------------

  const removeField = (index) => {
    onChange(
      fields
        .filter((_, fieldIndex) => fieldIndex !== index)
        .map((field, fieldIndex) => ({
          ...field,
          displayOrder: fieldIndex,
        }))
    )
  }

  // --------------------------------------------------
  // ATTACHMENT TYPE TOGGLE
  // --------------------------------------------------

  const toggleAttachmentType = (type) => {
    const current = draftField.allowedFileTypes || []
    const exists = current.includes(type)

    const updated = exists
      ? current.filter((item) => item !== type)
      : [...current, type]

    updateDraftField({
      allowedFileTypes: updated,
    })
  }

  // --------------------------------------------------
  // Determine if the current draft is an attachment field
  // --------------------------------------------------

  const isAttachmentField =
    draftField?.fieldType === 'attachment'

  return (
    <div className="workflow-field-builder">
      {/* <div className="designer-section-title">Step fields</div>

      <p className="muted-text">
        These fields will be editable by the person completing this
        step. Earlier step values will appear read-only.
      </p> */}

      {/* ---------------------------------------
          EXISTING FIELDS
      --------------------------------------- */}

      {/* {fields.length === 0 && (
        <div className="workflow-fields-empty">
          No fields added to this step.
        </div>
      )} */}

      {fields.map((field, index) => (
        <div
          className="workflow-field-summary"
          key={field.clientId || field.id || index}
        >
          <div className="workflow-field-summary-info">
            <strong>{field.label || 'Untitled field'}</strong>

            <div className="workflow-field-meta">
              {field.fieldType || 'text'}

              {field.isRequired ? ' • Required' : ' • Optional'}

              {field.fieldType === 'attachment' && (
                <>
                  {' • '}
                  {field.allowMultiple
                    ? 'Multiple files'
                    : 'Single file'}
                </>
              )}
            </div>

            <div className="workflow-field-key">
              {field.fieldKey}
            </div>
          </div>

          {!disabled && (
            <div className="workflow-field-actions">
              <button
                type="button"
                className="btn btn-secondary"
                onClick={() => openEditModal(field, index)}
              >
                Edit
              </button>

              <button
                type="button"
                className="btn btn-danger-soft"
                onClick={() => removeField(index)}
              >
                Remove
              </button>
            </div>
          )}
        </div>
      ))}
{/* 
      {!disabled && (
        <button
          type="button"
          className="btn btn-secondary"
          onClick={openAddModal}
        >
          + Add field
        </button>
      )} */}

      {/* ---------------------------------------
          ADD / EDIT FIELD MODAL
      --------------------------------------- */}

      {isModalOpen && draftField && (
        <div
          className="workflow-field-modal-backdrop"
          onMouseDown={(event) => {
            if (event.target === event.currentTarget) {
              closeModal()
            }
          }}
        >
          <div
            className="workflow-field-modal"
            role="dialog"
            aria-modal="true"
            aria-labelledby="workflow-field-modal-title"
          >
            {/* HEADER */}

            <div className="workflow-field-modal-header">
              <div>
                <h3
                  id="workflow-field-modal-title"
                  className="workflow-field-modal-title"
                >
                  {editingIndex !== null
                    ? 'Edit field'
                    : 'Add field'}
                </h3>

                <p className="muted-text">
                  Configure the field for this workflow step.
                </p>
              </div>

              <button
                type="button"
                className="workflow-field-modal-close"
                onClick={closeModal}
                aria-label="Close"
              >
                ×
              </button>
            </div>

            {/* BODY */}

            <div className="workflow-field-modal-body">
              {/* FIELD LABEL */}

              <label className="field compact">
                <span>Field label</span>

                <input
                  className="input"
                  autoFocus
                  value={draftField.label || ''}
                  onChange={(event) => {
                    const oldGeneratedKey = makeFieldKey(
                      draftField.label || ''
                    )

                    const label = event.target.value

                    const shouldUpdateKey =
                      !draftField.fieldKey ||
                      draftField.fieldKey === oldGeneratedKey

                    updateDraftField({
                      label,
                      ...(shouldUpdateKey
                        ? { fieldKey: makeFieldKey(label) }
                        : {}),
                    })
                  }}
                  placeholder="e.g. CNIC Number"
                />
              </label>

              {/* FIELD KEY */}

              <label className="field compact">
                <span>Field key</span>

                <input
                  className="input"
                  value={draftField.fieldKey || ''}
                  onChange={(event) =>
                    updateDraftField({
                      fieldKey: event.target.value,
                    })
                  }
                  placeholder="e.g. cnic_number"
                />
              </label>

              {/* FIELD TYPE */}

              <label className="field compact">
                <span>Field type</span>

                <select
                  className="input"
                  value={draftField.fieldType || 'text'}
                  onChange={(event) => {
                    const fieldType = event.target.value

                    updateDraftField({
                      fieldType,

                      options:
                        fieldType === 'dropdown' ||
                        fieldType === 'radio'
                          ? draftField.options || []
                          : [],

                      // when switching to attachment, clear placeholder
                      placeholder:
                        fieldType === 'attachment'
                          ? ''
                          : draftField.placeholder || '',

                      allowMultiple:
                        fieldType === 'attachment'
                          ? draftField.allowMultiple || false
                          : false,

                      allowedFileTypes:
                        fieldType === 'attachment'
                          ? draftField.allowedFileTypes || ['pdf']
                          : [],

                      maxFileSizeMb:
                        fieldType === 'attachment'
                          ? draftField.maxFileSizeMb || 5
                          : null,
                    })
                  }}
                >
                  {fieldTypes.map(([value, label]) => (
                    <option key={value} value={value}>
                      {label}
                    </option>
                  ))}
                </select>
              </label>

              {/* PLACEHOLDER — hidden for attachment fields */}

              {!isAttachmentField && (
                <label className="field compact">
                  <span>Placeholder</span>

                  <input
                    className="input"
                    value={draftField.placeholder || ''}
                    onChange={(event) =>
                      updateDraftField({
                        placeholder: event.target.value,
                      })
                    }
                    placeholder="e.g. Enter CNIC number"
                  />
                </label>
              )}

              {/* ---------------------------------
                  ATTACHMENT SETTINGS
              --------------------------------- */}

              {isAttachmentField && (
                <div className="attachment-settings">
                  <div className="designer-section-title">
                    Attachment settings
                  </div>

                  {/* ALLOW MULTIPLE */}

                  <label className="checkbox-field">
                    <input
                      type="checkbox"
                      checked={!!draftField.allowMultiple}
                      onChange={(event) =>
                        updateDraftField({
                          allowMultiple: event.target.checked,
                        })
                      }
                    />

                    Allow multiple files
                  </label>

                  {/* ALLOWED TYPES */}

                  <div className="field compact">
                    <span>Allowed file types</span>

                    <div
                      style={{
                        display: 'flex',
                        flexDirection: 'column',
                        gap: '8px',
                        marginTop: '8px',
                      }}
                    >
                      {attachmentTypes.map(([type, label]) => (
                        <label
                          key={type}
                          className="checkbox-field"
                        >
                          <input
                            type="checkbox"
                            checked={(
                              draftField.allowedFileTypes || []
                            ).includes(type)}
                            onChange={() =>
                              toggleAttachmentType(type)
                            }
                          />

                          {label}
                        </label>
                      ))}
                    </div>
                  </div>

                  {/* MAX FILE SIZE */}

                  <label className="field compact">
                    <span>Maximum file size (MB)</span>

                    <input
                      className="input"
                      type="number"
                      min="1"
                      step="1"
                      value={draftField.maxFileSizeMb ?? 5}
                      onChange={(event) =>
                        updateDraftField({
                          maxFileSizeMb: event.target.value,
                        })
                      }
                    />

                    <small className="muted-text">
                      Default maximum file size is 5 MB.
                    </small>
                  </label>
                </div>
              )}

              {/* ---------------------------------
                  DROPDOWN / RADIO OPTIONS
              --------------------------------- */}

              {(draftField.fieldType === 'dropdown' ||
                draftField.fieldType === 'radio') && (
                <label className="field compact">
                  <span>Options — one per line</span>

                  <textarea
                    className="input textarea"
                    rows={5}
                    value={(draftField.options || []).join('\n')}
                    onChange={(event) =>
                      updateDraftField({
                        options: event.target.value
                          .split('\n')
                          .map((x) => x.trim())
                          .filter(Boolean),
                      })
                    }
                    placeholder={'Option 1\nOption 2\nOption 3'}
                  />
                </label>
              )}

              {/* REQUIRED */}

              <label className="checkbox-field">
                <input
                  type="checkbox"
                  checked={!!draftField.isRequired}
                  onChange={(event) =>
                    updateDraftField({
                      isRequired: event.target.checked,
                    })
                  }
                />

                Required field
              </label>
            </div>

            {/* FOOTER */}

            <div className="workflow-field-modal-footer">
              <button
                type="button"
                className="btn btn-secondary"
                onClick={closeModal}
              >
                Cancel
              </button>

              <button
                type="button"
                className="btn btn-primary"
                onClick={saveField}
              >
                {editingIndex !== null
                  ? 'Save changes'
                  : 'Add field'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}