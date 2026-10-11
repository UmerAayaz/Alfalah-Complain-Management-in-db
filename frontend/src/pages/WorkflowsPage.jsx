import { useEffect, useState } from 'react'
import { api } from '../api/client.js'
import { useDevUser } from '../auth/DevUserContext.jsx'
import PageHeader from '../components/PageHeader.jsx'
import ErrorBanner from '../components/ErrorBanner.jsx'
import EmptyState from '../components/EmptyState.jsx'
import { WorkflowStatusBadge } from '../components/Badge.jsx'
import { formatDate } from '../constants.js'
import Pagination from '../components/Pagination.jsx'
import usePagination from '../hooks/usePagination.js'

export default function WorkflowsPage({ navigate }) {
  const { userId, roles } = useDevUser()
  const [workflows, setWorkflows] = useState([])
  const [categories, setCategories] = useState([])
  const [error, setError] = useState('')
  const [busyId, setBusyId] = useState('')
  const {
    page,
    setPage,
    pageSize,
    setPageSize,
    total,
    pageItems: pagedWorkflows,
  } = usePagination(workflows, 10)

  const load = async () => {
    try {
      const [items, cats] = await Promise.all([api('/workflow-definitions'), api('/reference/categories')])
      setWorkflows(items)
      setCategories(cats)
      setError('')
    } catch (err) { setError(err.message) }
  }
  useEffect(() => { load() }, [userId])

  if (!roles.has('DeptAdmin') && !roles.has('UnitHead'))
    return (
      <section className="panel">
        <ErrorBanner message="Unit Head or Department Admin access is required for the workflow designer." />
      </section>
    )

  const publish = async (id) => {
    setBusyId(id)
    try { await api(`/workflow-definitions/${id}/publish`, { method: 'POST' }); await load() }
    catch (err) { setError(err.message) }
    finally { setBusyId('') }
  }

  const newVersion = async (id) => {
    setBusyId(id)
    try {
      const created = await api(`/workflow-definitions/${id}/new-version`, { method: 'POST' })
      navigate(`/workflows/${created.id}/designer`)
    } catch (err) { setError(err.message) }
    finally { setBusyId('') }
  }

  const categoryName = (id) => categories.find((x) => x.id === id)?.name || 'Unknown category'

  return (
    <>
      <PageHeader
        eyebrow="AUTOMATION"
        title="Workflow library"

        actions={<button className="btn btn-primary" onClick={() => navigate('/workflows/new')}>+ Create workflow</button>}
      />
      <ErrorBanner message={error} />
      <section className="panel">
        {workflows.length === 0 ? (
          <EmptyState
            title="No workflows yet"
            description="Create the first workflow in the visual designer."
          />
        ) : (
          <>
            <div className="table-wrap">
              <table className="data-table">
                <thead>
                  <tr>
                    <th>Workflow</th>
                    <th>Category</th>
                    <th>Version</th>
                    <th>Status</th>
                    <th>Published</th>
                    <th>Actions</th>
                  </tr>
                </thead>

                <tbody>
                  {pagedWorkflows.map((item) => (
                    <tr key={item.id}>
                      <td>
                        <strong>{item.name}</strong>
                      </td>

                      <td>{categoryName(item.categoryId)}</td>

                      <td>v{item.version}</td>

                      <td>
                        <WorkflowStatusBadge status={item.status} />
                      </td>

                      <td>{formatDate(item.publishedAtUtc)}</td>

                      <td>
                        <div className="table-actions">
                          <button
                            className="link-button"
                            onClick={() =>
                              navigate(`/workflows/${item.id}/designer`)
                            }
                          >
                            {item.status === 0 ? 'Edit' : 'View'}
                          </button>

                          {item.status === 0 && (
                            <button
                              className="link-button"
                              disabled={busyId === item.id}
                              onClick={() => publish(item.id)}
                            >
                              Publish
                            </button>
                          )}

                          {item.status !== 0 && (
                            <button
                              className="link-button"
                              disabled={busyId === item.id}
                              onClick={() => newVersion(item.id)}
                            >
                              New version
                            </button>
                          )}
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

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
