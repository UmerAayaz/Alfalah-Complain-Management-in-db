import { useEffect, useMemo, useState } from 'react'
import { api } from '../api/client.js'
import { useDevUser } from '../auth/DevUserContext.jsx'
import PageHeader from '../components/PageHeader.jsx'
import ErrorBanner from '../components/ErrorBanner.jsx'
import EmptyState from '../components/EmptyState.jsx'
import { PriorityBadge, StatusBadge } from '../components/Badge.jsx'
import { complaintStatus, formatDate } from '../constants.js'
import Pagination from '../components/Pagination.jsx'
import usePagination from '../hooks/usePagination.js'

export default function ComplaintsPage({ navigate }) {
  const { userId } = useDevUser()

  const [complaints, setComplaints] = useState([])
  const [categories, setCategories] = useState([])
  const [filterStatus, setFilterStatus] = useState('all')
  const [filterCategory, setFilterCategory] = useState('all')
  const [query, setQuery] = useState('')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    let cancelled = false

    const loadComplaints = async () => {
      setLoading(true)

      try {
        const [c, cats] = await Promise.all([
          api('/complaints'),
          api('/reference/categories'),
        ])

        if (!cancelled) {
          setComplaints(Array.isArray(c) ? c : [])
          setCategories(Array.isArray(cats) ? cats : [])
          setError('')
        }
      } catch (err) {
        if (!cancelled) {
          setError(err.message)
        }
      } finally {
        if (!cancelled) {
          setLoading(false)
        }
      }
    }

    loadComplaints()

    return () => {
      cancelled = true
    }
  }, [userId])

  const filtered = useMemo(
    () =>
      complaints.filter((item) => {
        if (
          filterStatus !== 'all' &&
          String(item.status) !== filterStatus
        ) {
          return false
        }

        if (filterCategory !== 'all') {
          const selectedCategory = categories.find(
            (x) => String(x.id) === filterCategory
          )?.name

          if (item.category !== selectedCategory) {
            return false
          }
        }

        if (query.trim()) {
          const haystack =
            `${item.complaintNumber ?? ''} ${item.subject ?? ''} ${item.category ?? ''}`.toLowerCase()

          if (!haystack.includes(query.trim().toLowerCase())) {
            return false
          }
        }

        return true
      }),
    [complaints, categories, filterStatus, filterCategory, query]
  )

  const {
    page,
    setPage,
    pageSize,
    setPageSize,
    total,
    pageItems: pagedComplaints,
  } = usePagination(filtered, 10)

  return (
    <>
      <PageHeader
        eyebrow="COMPLAINTS"
        title="Complaint register"
        description="Search, filter and open every complaint logged in the platform."
        actions={
          <button
            className="btn btn-primary"
            onClick={() => navigate('/complaints/new')}
          >
            + Log complaint
          </button>
        }
      />

      <ErrorBanner message={error} />

      <section className="panel">
        <div className="filter-bar">
          <input
            className="input search-input"
            placeholder="Search complaint number, subject or category…"
            value={query}
            onChange={(e) => setQuery(e.target.value)}
          />

          <select
            className="input"
            value={filterCategory}
            onChange={(e) => {
              setFilterCategory(e.target.value)
              setPage(1)
            }}
          >
            <option value="all">All categories</option>

            {categories.map((cat) => (
              <option key={cat.id} value={String(cat.id)}>
                {cat.name}
              </option>
            ))}
          </select>

          <select
            className="input"
            value={filterStatus}
            onChange={(e) => {
              setFilterStatus(e.target.value)
              setPage(1)
            }}
          >
            <option value="all">All statuses</option>

            {Object.entries(complaintStatus).map(([value, label]) => (
              <option key={value} value={value}>
                {label}
              </option>
            ))}
          </select>
        </div>

        {loading ? (
          <div className="loading-block">
            Loading complaints…
          </div>
        ) : filtered.length === 0 ? (
          <EmptyState
            title="No matching complaints"
            description="Try a different filter or log a new complaint."
          />
        ) : (
          <>
            <div className="table-wrap">
              <table className="data-table">
                <thead>
                  <tr>
                    <th>Complaint</th>
                    <th>Category</th>
                    <th>Subject</th>
                    <th>Priority</th>
                    <th>Status</th>
                    <th>Created</th>
                    <th></th>
                  </tr>
                </thead>

                <tbody>
                  {pagedComplaints.map((item) => (
                    <tr key={item.id}>
                      <td>
                        <strong>{item.complaintNumber}</strong>
                      </td>

                      <td>{item.category}</td>

                      <td>{item.subject}</td>

                      <td>
                        <PriorityBadge priority={item.priority} />
                      </td>

                      <td>
                        <StatusBadge status={item.status} />
                      </td>

                      <td>{formatDate(item.createdAtUtc)}</td>

                      <td>
                        <button
                          className="link-button"
                          onClick={() =>
                            navigate(`/complaints/${item.id}`)
                          }
                        >
                          Open →
                        </button>
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
