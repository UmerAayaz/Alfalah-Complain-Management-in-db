import { useEffect, useState } from 'react'
import { api } from '../api/client.js'
import PageHeader from '../components/PageHeader.jsx'
import ErrorBanner from '../components/ErrorBanner.jsx'

export default function NewComplaintPage({ navigate }) {
  const [categories, setCategories] = useState([])
  const [products, setProducts] = useState([])
  const [form, setForm] = useState({
    categoryId: '',
    productId: '',
    subject: '',
    description: '',
    priority: 1,
    autoReference: ''
  })
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const [files, setFiles] = useState([])

  // ============================================================
  // LOAD CATEGORIES (top-level only)
  // ============================================================
  useEffect(() => {
    api('/reference/categories')
      .then((items) => {
        setCategories(items)
        if (items.length)
          setForm((current) => ({ ...current, categoryId: items[0].id }))
      })
      .catch((err) => setError(err.message))
  }, [])

  // ============================================================
  // LOAD PRODUCTS when category changes
  // ============================================================
  useEffect(() => {
    if (!form.categoryId) {
      setProducts([])
      return
    }

    api(`/reference/products?categoryId=${form.categoryId}`)
      .then((items) => {
        const list = Array.isArray(items) ? items : []
        setProducts(list)
        setForm((current) => ({
          ...current,
          productId: list.length ? list[0].id : ''
        }))
      })
      .catch((err) => {
        console.error('Products load failed:', err)
        setProducts([])
      })
  }, [form.categoryId])

  // ============================================================
  // LOAD DEFAULT PRIORITY when category changes
  // ============================================================
  useEffect(() => {
  if (!form.categoryId) return

  const url = form.productId
    ? `/reference/workflow-defaults?categoryId=${form.categoryId}&productId=${form.productId}`
    : `/reference/workflow-defaults?categoryId=${form.categoryId}`

  api(url)
    .then((data) => {
      setForm((current) => ({
        ...current,
        priority: data?.defaultPriority ?? 1
      }))
    })
    .catch((err) => {
      console.error('Workflow defaults load failed:', err)
    })
}, [form.categoryId, form.productId])   // ← productId bhi add kiya

  // ============================================================
  // AUTO-GENERATE REFERENCE (client-side display only)
  // ============================================================
  useEffect(() => {
  if (form.autoReference) return
  const now = new Date()
  const pad = (n, len = 2) => String(n).padStart(len, '0')
  const ts = 
    now.getUTCFullYear() +
    pad(now.getUTCMonth() + 1) +
    pad(now.getUTCDate()) +
    pad(now.getUTCHours()) +
    pad(now.getUTCMinutes()) +
    pad(now.getUTCSeconds()) +
    pad(now.getUTCMilliseconds(), 3)
  setForm((current) => ({
    ...current,
    autoReference: ts
  }))
}, [])

  // ============================================================
  // SUBMIT
  // ============================================================
  const submit = async (e) => {
    e.preventDefault()
    setSaving(true)
    setError('')
    try {
      // 1. Complaint create karo
      const created = await api('/complaints', {
        method: 'POST',
        body: {
          categoryId: form.categoryId,
          productId: form.productId || null,
          subject: form.subject,
          description: form.description,
          customerReference: null,
          priority: Number(form.priority),
          autoReference: form.autoReference || null
        }
      })

      // 2. Attachments upload karo
      if (files.length > 0) {
        const formData = new FormData()
        files.forEach((f) => formData.append('files', f))

        await api(`/complaints/${created.id}/attachments`, {
          method: 'POST',
          body: formData
        })
      }

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
          {/* Related To — Category */}
          <label className="field">
            <span>Related to</span>
            <select
              className="input"
              value={form.categoryId}
              onChange={(e) =>
                setForm({ ...form, categoryId: e.target.value })
              }
              required
            >
              {categories.map((x) => (
                <option key={x.id} value={x.id}>
                  {x.name}
                </option>
              ))}
            </select>
          </label>

          {/* Product — filtered by category */}
          <label className="field">
            <span>Product</span>
            <select
              className="input"
              value={form.productId}
              onChange={(e) =>
                setForm({ ...form, productId: e.target.value })
              }
              required
              disabled={products.length === 0}
            >
              {products.length === 0 ? (
                <option value="">
                  No products available for this category
                </option>
              ) : (
                products.map((p) => (
                  <option key={p.id} value={p.id}>
                    {p.name}
                  </option>
                ))
              )}
            </select>
          </label>
        </div>

        <div className="form-grid two">
          {/* Priority — READ-ONLY (auto-fetched) */}
          <label className="field">
            <span>Priority (auto)</span>
            <input
              className="input"
              value={
                ['Low', 'Normal', 'High', 'Critical'][form.priority] || 'Normal'
              }
              readOnly
              disabled
              style={{ background: '#f9fafb', cursor: 'not-allowed' }}
            />
          </label>

          {/* Auto Reference — READ-ONLY */}
          <label className="field">
            <span>Reference (auto-generated)</span>
            <input
              className="input"
              value={form.autoReference}
              readOnly
              disabled
              style={{ background: '#f9fafb', cursor: 'not-allowed' }}
            />
          </label>
        </div>

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
            disabled={
              saving || !form.categoryId || !form.productId || products.length === 0
            }
          >
            {saving ? 'Logging…' : 'Log complaint'}
          </button>
        </div>
      </form>
    </>
  )
}