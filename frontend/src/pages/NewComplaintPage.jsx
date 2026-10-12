import { useEffect, useState } from 'react'
import { api } from '../api/client.js'
import PageHeader from '../components/PageHeader.jsx'
import ErrorBanner from '../components/ErrorBanner.jsx'

export default function NewComplaintPage({ navigate }) {
  const [categories, setCategories] = useState([])
  const [form, setForm] = useState({
    categoryId: '',
    subject: '',
    description: '',
    customerReference: '',
    priority: 1
  })
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const [files, setFiles] = useState([])

  useEffect(() => {
    api('/reference/categories')
      .then((items) => {
        setCategories(items)
        if (items.length)
          setForm((current) => ({ ...current, categoryId: items[0].id }))
      })
      .catch((err) => setError(err.message))
  }, [])

  const submit = async (e) => {
  e.preventDefault()
  setSaving(true)
  setError('')
  try {
    // 1. Complaint create karo
    const created = await api('/complaints', {
      method: 'POST',
      body: { ...form, priority: Number(form.priority) }
    })

    // 2. Attachments upload karo (agar hain)
    if (files.length > 0) {
      const formData = new FormData()
      files.forEach((f) => formData.append('files', f))

      await api(`/complaints/${created.id}/attachments`, {
        method: 'POST',
        body: formData
      })
    }

    // 3. Navigate
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

      <form className="panel form-panel" onSubmit={submit}>
        <div className="form-grid two">
          <label className="field">
            <span>Related to</span>
            <select
              className="input"
              value={form.categoryId}
              onChange={(e) => setForm({ ...form, categoryId: e.target.value })}
              required
            >
              {categories.map((x) => (
                <option key={x.id} value={x.id}>
                  {x.name}
                </option>
              ))}
            </select>
          </label>
          <label className="field">
            <span>Priority</span>
            <select
              className="input"
              value={form.priority}
              onChange={(e) => setForm({ ...form, priority: e.target.value })}
            >
              <option value="0">Low</option>
              <option value="1">Normal</option>
              <option value="2">High</option>
              <option value="3">Critical</option>
            </select>
          </label>
        </div>

        <label className="field">
          <span>Customer / transaction reference</span>
          <input
            className="input"
            value={form.customerReference}
            onChange={(e) =>
              setForm({ ...form, customerReference: e.target.value })
            }
            placeholder="Optional customer ID, account reference or transaction ID"
          />
        </label>

        <label className="field">
          <span>Subject</span>
          <input
            className="input"
            value={form.subject}
            onChange={(e) => setForm({ ...form, subject: e.target.value })}
            placeholder="Short description of the complaint"
            required
            maxLength={300}
          />
        </label>

        <label className="field">
          <span>Description</span>
          <textarea
            className="input textarea"
            value={form.description}
            onChange={(e) => setForm({ ...form, description: e.target.value })}
            placeholder="Add the complaint details, customer impact and relevant context…"
            required
            rows={8}
          />
        </label>

        {/* ============ ATTACHMENTS ============ */}
        <div className="field" style={{ marginTop: 8 }}>
          <span>Attachments</span>

          <div className="attachment-option" style={{ marginTop: 8 }}>
            <input
              id="complaint-files"
              type="file"
              multiple
              style={{ display: 'none' }}
              onChange={(e) => {
                const newFiles = Array.from(e.target.files || [])
                setFiles((prev) => [...prev, ...newFiles])
                e.target.value = ''
              }}
            />

            <label
              htmlFor="complaint-files"
              className={files.length > 0 ? 'btn-success' : 'btn-secondary'}
              style={{ marginBottom: 0 }}
            >
              {files.length > 0 ? '✓ Files Attached' : '📎 Attach File'}
            </label>

            <span className="muted-text">
              Allowed: <strong>PDF, JPG, PNG</strong> · Max{' '}
              <strong>5 MB</strong> each
            </span>
          </div>

          {files.length === 0 ? (
            <span className="attachment-empty">No files attached yet.</span>
          ) : (
            <div className="attachment-list">
              {files.map((f, i) => (
                <div key={i} className="attachment-row">
                  <span className="attachment-check">✓</span>
                  <span className="attachment-name">
                    {f.name}
                    <span className="attachment-size">
                      ({(f.size / 1024).toFixed(1)} KB)
                    </span>
                  </span>
                  <button
                    type="button"
                    className="link-btn"
                    onClick={() =>
                      setFiles(files.filter((_, idx) => idx !== i))
                    }
                  >
                    Remove
                  </button>
                </div>
              ))}
            </div>
          )}
        </div>

        <div className="form-actions">
          <button
            type="button"
            className="btn btn-secondary"
            onClick={() => navigate('/complaints')}
          >
            Cancel
          </button>
          <button
            className="btn btn-primary"
            disabled={saving || !form.categoryId}
          >
            {saving ? 'Logging…' : 'Log complaint'}
          </button>
        </div>
      </form>
    </>
  )
}