import { useEffect, useMemo, useState } from 'react'
import { api } from '../api/client.js'
import { useDevUser } from '../auth/DevUserContext.jsx'
import PageHeader from '../components/PageHeader.jsx'
import ErrorBanner from '../components/ErrorBanner.jsx'
import EmptyState from '../components/EmptyState.jsx'
import { formatDate, relativeSla } from '../constants.js'
import WorkflowTaskFields from '../components/WorkflowTaskFields.jsx'

export default function MyTasksPage({ navigate }) {
  const { userId, roles } = useDevUser()

  const [tasks, setTasks] = useState([])
  const [selected, setSelected] = useState(null)

  const [actions, setActions] = useState([])
  const [selectedActionIndex, setSelectedActionIndex] = useState(0)

  const [assignees, setAssignees] = useState([])
  const [assigneeId, setAssigneeId] = useState('')

  const [comment, setComment] = useState('')
  const [error, setError] = useState('')
  const [successMessage, setSuccessMessage] = useState('')

  const [saving, setSaving] = useState(false)
  const [loading, setLoading] = useState(true)

  const [reassigning, setReassigning] = useState(false)
  const [reassignUsers, setReassignUsers] = useState([])
  const [reassignUserId, setReassignUserId] = useState('')

  const [taskForm, setTaskForm] = useState(null)

  // fieldValues: { [fieldId]: any }
  //   - normal fields       -> string / number / boolean
  //   - attachment fields   -> array of File objects
  const [fieldValues, setFieldValues] = useState({})

  // Complaint-level attachments (read-only preview + download)
  const [attachments, setAttachments] = useState([])
  const [attachmentsLoading, setAttachmentsLoading] = useState(false)

  // =========================================================
  // LOAD MY WORK QUEUE
  // =========================================================

  const load = async () => {
    setLoading(true)

    try {
      const data = await api('/workflow-tasks/my-bucket')

      console.log('MY TASKS:', data)

      setTasks(Array.isArray(data) ? data : [])
      setError('')
    } catch (err) {
      console.error('FAILED TO LOAD TASKS:', err)
      setError(err.message)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    load()
  }, [userId])

  // =========================================================
  // CURRENT SELECTED WORKFLOW ACTION
  // =========================================================

  const selectedAction = actions[selectedActionIndex] || null

  // =========================================================
  // LOAD POSSIBLE ASSIGNEES FOR NEXT WORKFLOW STEP
  // =========================================================

  useEffect(() => {
    if (
      !selected ||
      !selectedAction ||
      !selectedAction.targetRoleCode ||
      selectedAction.targetNodeType !== 1
    ) {
      setAssignees([])
      setAssigneeId('')
      return
    }

    let cancelled = false

    const loadAssignees = async () => {
      try {
        setAssignees([])
        setAssigneeId('')

        console.log('LOADING ASSIGNEES:', {
          taskId: selected.id,
          departmentId: selected.departmentId,
          roleCode: selectedAction.targetRoleCode,
          requiresAssignee: selectedAction.requiresAssignee,
          targetNodeType: selectedAction.targetNodeType,
        })

        const users = await api(
          `/reference/users?departmentId=${selected.departmentId}&roleCode=${encodeURIComponent(
            selectedAction.targetRoleCode
          )}`
        )

        console.log('ASSIGNEES RETURNED:', users)

        if (cancelled) return

        const userList = Array.isArray(users) ? users : []

        setAssignees(userList)

        if (selectedAction.requiresAssignee && userList.length > 0) {
          setAssigneeId(userList[0].id)
        } else {
          setAssigneeId('')
        }
      } catch (err) {
        if (cancelled) return

        console.error('FAILED TO LOAD ASSIGNEES:', err)

        setAssignees([])
        setAssigneeId('')
        setError(err.message)
      }
    }

    loadAssignees()

    return () => {
      cancelled = true
    }
  }, [
    selected?.id,
    selected?.departmentId,
    selectedActionIndex,
    selectedAction?.targetNodeId,
    selectedAction?.targetRoleCode,
    selectedAction?.targetNodeType,
    selectedAction?.requiresAssignee,
  ])

  // =========================================================
  // OPEN TASK
  // =========================================================

  const openTask = async (task) => {
    setSelected(task)

    setActions([])
    setSelectedActionIndex(0)

    setAssignees([])
    setAssigneeId('')

    setComment('')
    setTaskForm(null)
    setFieldValues({})

    setReassigning(false)
    setReassignUsers([])
    setReassignUserId('')

    setError('')
    setSuccessMessage('')

    // Reset attachments
    setAttachments([])
    setAttachmentsLoading(true)

    try {
      console.log('OPENING TASK:', task)

      const [availableActions, form, complaintAttachments] = await Promise.all([
        api(`/workflow-tasks/${task.id}/actions`),
        api(`/workflow-tasks/${task.id}/form`),
        api(`/complaints/${task.complaintId}/attachments`).catch((err) => {
          console.error('Failed to load complaint attachments:', err)
          return []
        }),
      ])

      setActions(Array.isArray(availableActions) ? availableActions : [])
      setTaskForm(form)
      setAttachments(
        Array.isArray(complaintAttachments) ? complaintAttachments : []
      )
    } catch (err) {
      console.error('FAILED TO LOAD TASK ACTIONS:', err)
      setError(err.message)
    } finally {
      setAttachmentsLoading(false)
    }
  }

  // =========================================================
  // COMPLETE / ADVANCE TASK
  // =========================================================

  const complete = async () => {
    if (!selected || !selectedAction) return

    if (selectedAction.requiresAssignee && !assigneeId) {
      setError(`Select a ${selectedAction.targetRoleCode} before continuing.`)
      return
    }

    setSaving(true)
    setError('')

    try {
      const formData = new FormData()

      formData.append('outcomeKey', selectedAction.outcomeKey || '')

      if (assigneeId) {
        formData.append('nextAssigneeUserId', assigneeId)
      }

      formData.append('comment', comment.trim() || '')

      const fieldAnswers = []
      let attachmentCount = 0

      taskForm?.currentFields?.forEach((field) => {
        const isAttachment = field.fieldType?.toLowerCase() === 'attachment'

        if (isAttachment) {
          const raw = fieldValues[field.id]
          const files = Array.isArray(raw) ? raw : raw ? [raw] : []

          files.forEach((file) => {
            formData.append(`files_${field.id}`, file, file.name)
          })

          if (files.length > 0) attachmentCount += files.length

          fieldAnswers.push({ fieldId: field.id, value: null })
          return
        }

        fieldAnswers.push({
          fieldId: field.id,
          value: fieldValues[field.id] ?? null,
        })
      })

      formData.append('fieldAnswers', JSON.stringify(fieldAnswers))

      console.log(
        'COMPLETE PAYLOAD DEBUG:',
        JSON.stringify(
          {
            fieldAnswers,
            currentFields: taskForm?.currentFields,
            attachmentCount,
          },
          null,
          2
        )
      )

      await api(`/workflow-tasks/${selected.id}/complete`, {
        method: 'POST',
        body: formData,
      })

      setSuccessMessage(
        attachmentCount > 0
          ? `Task completed successfully with ${attachmentCount} attachment(s).`
          : 'Task completed successfully.'
      )
      setTimeout(() => setSuccessMessage(''), 4000)

      setSelected(null)
      setActions([])
      setAssignees([])
      setAssigneeId('')
      setComment('')
      setTaskForm(null)
      setFieldValues({})
      setAttachments([])

      await load()
    } catch (err) {
      console.error('FAILED TO COMPLETE TASK:', err)
      setError(err.message)
    } finally {
      setSaving(false)
    }
  }

  // =========================================================
  // START REASSIGNMENT
  // =========================================================

  const startReassign = async () => {
    if (!selected || !selected.assignedRoleCode) return

    setReassigning(true)
    setReassignUsers([])
    setReassignUserId('')
    setError('')

    try {
      console.log('LOADING REASSIGNMENT USERS:', {
        departmentId: selected.departmentId,
        roleCode: selected.assignedRoleCode,
      })

      const users = await api(
        `/reference/users?departmentId=${selected.departmentId}&roleCode=${encodeURIComponent(
          selected.assignedRoleCode
        )}`
      )

      console.log('REASSIGNMENT USERS RETURNED:', users)

      const userList = Array.isArray(users) ? users : []
      setReassignUsers(userList)

      if (userList.length > 0) {
        setReassignUserId(userList[0].id)
      }
    } catch (err) {
      console.error('FAILED TO LOAD REASSIGNMENT USERS:', err)
      setError(err.message)
    }
  }

  // =========================================================
  // REASSIGN CURRENT TASK
  // =========================================================

  const reassign = async () => {
    if (!selected || !reassignUserId) return

    setSaving(true)
    setError('')

    try {
      console.log('REASSIGNING TASK:', {
        taskId: selected.id,
        userId: reassignUserId,
      })

      await api(`/workflow-tasks/${selected.id}/reassign`, {
        method: 'POST',
        body: {
          userId: reassignUserId,
          comment: comment.trim() || null,
        },
      })

      setSuccessMessage('Task reassigned successfully.')
      setTimeout(() => setSuccessMessage(''), 4000)

      setSelected(null)
      setReassigning(false)
      setReassignUsers([])
      setReassignUserId('')
      setComment('')

      await load()
    } catch (err) {
      console.error('FAILED TO REASSIGN TASK:', err)
      setError(err.message)
    } finally {
      setSaving(false)
    }
  }

  // =========================================================
  // OVERDUE COUNT
  // =========================================================

  const overdue = useMemo(() => {
    const now = new Date()
    return tasks.filter(
      (task) => task.dueAtUtc && new Date(task.dueAtUtc) < now
    ).length
  }, [tasks])

  // =========================================================
  // PAGE
  // =========================================================

  return (
    <>
      <PageHeader
        eyebrow="WORK QUEUE"
        title="My assigned work"
        description={`${tasks.length} open task${
          tasks.length === 1 ? '' : 's'
        } · ${overdue} overdue`}
      />

      <ErrorBanner message={error} />

      {successMessage && (
        <div className="success-banner">✓ {successMessage}</div>
      )}

      {/* =====================================================
          TASK LIST
      ====================================================== */}

      {loading ? (
        <div className="loading-block">Loading your queue…</div>
      ) : tasks.length === 0 ? (
        <section className="panel">
          <EmptyState
            title="Your queue is clear"
            description="New workflow tasks assigned to you or your role will appear here."
          />
        </section>
      ) : (
        <div className="task-grid">
          {tasks.map((task) => {
            const sla = relativeSla(task.dueAtUtc)

            return (
              <article
                className={`task-card ${
                  sla.state === 'danger' ? 'task-overdue' : ''
                }`}
                key={task.id}
              >
                <div className="task-card-top">
                  <span className="step-pill">{task.nodeName}</span>
                  <span className={`sla-chip sla-${sla.state}`}>
                    {sla.text}
                  </span>
                </div>

                <button
                  className="task-title-link"
                  onClick={() => navigate(`/complaints/${task.complaintId}`)}
                >
                  {task.complaintNumber}
                </button>

                <h3>{task.complaintSubject}</h3>

                <div className="task-details">
                  <div>
                    <span>Queue</span>
                    <strong>{task.assignedRoleCode || '—'}</strong>
                  </div>
                  <div>
                    <span>Assigned to</span>
                    <strong>{task.assignedToUserName || 'Role queue'}</strong>
                  </div>
                  <div>
                    <span>Opened</span>
                    <strong>{formatDate(task.openedAtUtc)}</strong>
                  </div>
                  <div>
                    <span>Due</span>
                    <strong>{formatDate(task.dueAtUtc)}</strong>
                  </div>
                </div>

                <button
                  className="btn btn-primary full-width"
                  onClick={() => openTask(task)}
                >
                  Open task
                </button>
              </article>
            )
          })}
        </div>
      )}

      {/* =====================================================
          TASK MODAL
      ====================================================== */}

      {selected && (
        <div
          className="modal-backdrop"
          onMouseDown={(event) => {
            if (event.target === event.currentTarget) {
              setSelected(null)
            }
          }}
        >
          <div className="modal">
            <div className="modal-header">
              <div>
                <div className="eyebrow">{selected.complaintNumber}</div>
                <h2>{selected.nodeName}</h2>
                <p>{selected.complaintSubject}</p>
              </div>

              <button
                className="icon-button"
                onClick={() => setSelected(null)}
              >
                ×
              </button>
            </div>

            <ErrorBanner message={error} />

            {/* ============ COMPLAINT ATTACHMENTS ============ */}
            {!reassigning && (
              <div className="field">
                <span>Complaint attachments ({attachments.length})</span>

                {attachmentsLoading ? (
                  <small className="muted-text">Loading attachments…</small>
                ) : attachments.length === 0 ? (
                  <span className="attachment-empty">
                    No attachments on this complaint.
                  </span>
                ) : (
                  <div className="attachment-list" style={{ marginTop: 8 }}>
                    {attachments.map((a) => {
                      const downloadUrl = `/api/complaints/attachments/${a.id}/download`
                      const previewUrl = `${downloadUrl}?inline=true`

                      return (
                        <div key={a.id} className="attachment-row">
                          <span className="attachment-check">✓</span>
                          <span className="attachment-name">
                            {a.fileName}
                            <span className="attachment-size">
                              ({(a.sizeBytes / 1024).toFixed(1)} KB)
                            </span>
                          </span>
                          <a
                            href={previewUrl}
                            target="_blank"
                            rel="noopener noreferrer"
                            className="view-btn"
                          >
                            Preview
                          </a>
                          <a href={downloadUrl} className="view-btn" download>
                            Download
                          </a>
                        </div>
                      )
                    })}
                  </div>
                )}
              </div>
            )}

            {!reassigning ? (
              <>
                {/* ACTION SELECTION */}

                <div className="field">
                  <span>Choose action</span>

                  <div className="action-choice-list">
                    {actions.map((action, index) => (
                      <button
                        key={`${action.targetNodeId}-${index}`}
                        type="button"
                        className={`action-choice ${
                          selectedActionIndex === index ? 'selected' : ''
                        }`}
                        onClick={() => {
                          setSelectedActionIndex(index)
                          setAssignees([])
                          setAssigneeId('')
                          setError('')
                        }}
                      >
                        <strong>{action.label}</strong>
                        <span>
                          Next: {action.targetNodeName}
                          {action.targetRoleCode
                            ? ` · ${action.targetRoleCode}`
                            : ''}
                        </span>
                      </button>
                    ))}

                    {actions.length === 0 && (
                      <div className="inline-empty">
                        No available transition was returned for this step.
                      </div>
                    )}
                  </div>
                </div>

                {/* NEXT USER ASSIGNMENT */}

                {selectedAction?.targetNodeType === 1 &&
                  selectedAction.targetRoleCode && (
                    <label className="field">
                      <span>
                        {selectedAction.requiresAssignee
                          ? `Assign specific ${selectedAction.targetRoleCode}`
                          : `Optional direct assignment (${selectedAction.targetRoleCode})`}
                      </span>

                      <select
                        className="input"
                        value={assigneeId}
                        onChange={(event) => setAssigneeId(event.target.value)}
                        required={selectedAction.requiresAssignee}
                      >
                        {assignees.length === 0 && (
                          <option value="">
                            No {selectedAction.targetRoleCode} users available
                          </option>
                        )}

                        {!selectedAction.requiresAssignee &&
                          assignees.length > 0 && (
                            <option value="">Send to role queue</option>
                          )}

                        {assignees.map((user) => (
                          <option key={user.id} value={user.id}>
                            {user.displayName} · {user.employeeCode}
                          </option>
                        ))}
                      </select>

                      {selectedAction.requiresAssignee &&
                        assignees.length === 0 && (
                          <small style={{ color: '#b42318', marginTop: '6px' }}>
                            No active {selectedAction.targetRoleCode} users were
                            returned for this department.
                          </small>
                        )}
                    </label>
                  )}

                {/* DYNAMIC FIELDS */}

                <WorkflowTaskFields
                  form={taskForm}
                  values={fieldValues}
                  onChange={setFieldValues}
                />

                {/* COMMENT */}

                <label className="field">
                  <span>Comment</span>

                  <textarea
                    className="input textarea"
                    rows={4}
                    value={comment}
                    onChange={(event) => setComment(event.target.value)}
                    placeholder="Add an optional action note…"
                  />
                </label>

                {/* ACTION BUTTONS */}

                <div className="modal-actions">
                  {(roles.has('TeamLead') || roles.has('UnitHead')) &&
                    selected.assignedRoleCode && (
                      <button
                        className="btn btn-secondary"
                        onClick={startReassign}
                      >
                        Reassign current step
                      </button>
                    )}

                  <div className="modal-actions-right">
                    <button
                      className="btn btn-secondary"
                      onClick={() => setSelected(null)}
                    >
                      Cancel
                    </button>

                    <button
                      className="btn btn-primary"
                      disabled={
                        saving ||
                        !selectedAction ||
                        (selectedAction.requiresAssignee && !assigneeId)
                      }
                      onClick={complete}
                    >
                      {saving
                        ? 'Processing…'
                        : selectedAction?.label || 'Complete'}
                    </button>
                  </div>
                </div>
              </>
            ) : (
              <>
                {/* REASSIGNMENT VIEW */}

                <div className="notice-box">
                  Reassign this <strong>{selected.assignedRoleCode}</strong> task
                  without advancing the workflow.
                </div>

                <label className="field">
                  <span>Assign to</span>

                  <select
                    className="input"
                    value={reassignUserId}
                    onChange={(event) => setReassignUserId(event.target.value)}
                  >
                    {reassignUsers.length === 0 && (
                      <option value="">No users available</option>
                    )}

                    {reassignUsers.map((user) => (
                      <option key={user.id} value={user.id}>
                        {user.displayName} · {user.employeeCode}
                      </option>
                    ))}
                  </select>
                </label>

                <label className="field">
                  <span>Comment</span>

                  <textarea
                    className="input textarea"
                    rows={3}
                    value={comment}
                    onChange={(event) => setComment(event.target.value)}
                  />
                </label>

                <div className="modal-actions">
                  <button
                    className="btn btn-secondary"
                    onClick={() => setReassigning(false)}
                  >
                    ← Back
                  </button>

                  <button
                    className="btn btn-primary"
                    onClick={reassign}
                    disabled={saving || !reassignUserId}
                  >
                    {saving ? 'Reassigning…' : 'Reassign task'}
                  </button>
                </div>
              </>
            )}
          </div>
        </div>
      )}
    </>
  )
}