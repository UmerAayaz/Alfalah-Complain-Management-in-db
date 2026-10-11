import { useEffect, useState } from 'react'
import { api } from '../../api/client.js'
import PageHeader from '../../components/PageHeader.jsx'
import ErrorBanner from '../../components/ErrorBanner.jsx'
import './eform.css'
import Pagination from '../../components/Pagination.jsx'
import usePagination from '../../hooks/usePagination.js'

export default function FormsListPage({ navigate }) {
  const [forms, setForms] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
    const {
    page,
    setPage,
    pageSize,
    setPageSize,
    total,
    pageItems: pagedForms,
  } = usePagination(forms, 10)

  const loadForms = async () => {
    setLoading(true)
    try {
      const data = await api('/forms')
      setForms(Array.isArray(data) ? data : [])
      setError('')
    } catch (err) {
      console.error('LOAD FAILED:', err)
      setError(err.message)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    loadForms()
  }, [])

  const publish = async (id) => {
    if (
      !window.confirm(
        'Publish this form? It will become available for workflows.'
      )
    )
      return
    try {
      await api(`/forms/${id}/publish`, { method: 'POST' })
      await loadForms()
    } catch (err) {
      setError(err.message)
    }
  }

  const createNewVersion = async (id, currentVersion) => {
    if (
      !window.confirm(
        `Create new version (v${currentVersion + 1})? A draft copy will be created for editing.`
      )
    )
      return

    try {
      const newForm = await api(`/forms/${id}/new-version`, { method: 'POST' })
      navigate(`/forms/${newForm.id}`)
    } catch (err) {
      setError(err.message)
    }
  }

  return (
    <>
      <PageHeader
        eyebrow="E-FORM BUILDER"
        title="Forms"
        description="Create and manage reusable forms"
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
              + New Form
            </button>
          </div>
        }
      />

      <ErrorBanner message={error} />

<section className="panel">
  {loading ? (
    <div className="loading-block">Loading forms…</div>
  ) : forms.length === 0 ? (
    <div className="inline-empty" style={{ padding: 24 }}>
      No forms yet. Click "New Form" to create one.
    </div>
  ) : (
    <>
      <table className="data-table">
        <thead>
          <tr>
            <th>Name</th>
            <th>Version</th>
            <th>Fields</th>
            <th>Status</th>
            <th>Published</th>
            <th style={{ width: 220 }}>Actions</th>
          </tr>
        </thead>

        <tbody>
          {pagedForms.map((f) => (
            <tr key={f.id}>
              <td>
                <strong>{f.name}</strong>
              </td>

              <td>v{f.version}</td>

              <td>{f.fieldCount ?? 0}</td>

              <td>
                <span
                  className={`badge ${
                    f.status === 'Published'
                      ? 'badge-success'
                      : f.status === 'Archived'
                      ? 'badge-type'
                      : 'badge-info'
                  }`}
                >
                  {f.status}
                </span>
              </td>

              <td>
                {f.publishedAtUtc
                  ? new Date(f.publishedAtUtc).toLocaleDateString()
                  : '—'}
              </td>

              <td>
                {/* DRAFT */}
                {f.status === 'Draft' && (
                  <>
                    <button
                      className="btn btn-secondary btn-sm"
                      onClick={() => navigate(`/forms/${f.id}`)}
                    >
                      Edit
                    </button>

                    <button
                      className="btn btn-primary btn-sm"
                      style={{ marginLeft: 6 }}
                      onClick={() => publish(f.id)}
                    >
                      Publish
                    </button>
                  </>
                )}

                {/* PUBLISHED */}
                {f.status === 'Published' && (
                  <>
                    <button
                      className="btn btn-secondary btn-sm"
                      onClick={() => navigate(`/forms/${f.id}`)}
                    >
                      View
                    </button>

                    <button
                      className="btn btn-primary btn-sm"
                      style={{ marginLeft: 6 }}
                      onClick={() =>
                        createNewVersion(f.id, f.version)
                      }
                    >
                      New Version
                    </button>
                  </>
                )}

                {/* ARCHIVED */}
                {f.status === 'Archived' && (
                  <button
                    className="btn btn-secondary btn-sm"
                    onClick={() => navigate(`/forms/${f.id}`)}
                  >
                    View
                  </button>
                )}
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
    </>
  )
}