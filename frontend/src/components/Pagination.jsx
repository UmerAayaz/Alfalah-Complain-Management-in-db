export default function Pagination({
  page,
  pageSize,
  total,
  onPageChange,
  onPageSizeChange,
  pageSizeOptions = [10, 25, 50, 100],
}) {
  const totalPages = Math.max(1, Math.ceil(total / pageSize))
  const start = total === 0 ? 0 : (page - 1) * pageSize + 1
  const end = Math.min(page * pageSize, total)

  const go = (p) => {
    const clamped = Math.min(Math.max(1, p), totalPages)
    if (clamped !== page) onPageChange(clamped)
  }

  // Build page number list (with ellipsis)
  const pages = []
  const addPage = (n) => pages.push(n)
  const addEllipsis = () => pages.push('...')

  if (totalPages <= 7) {
    for (let i = 1; i <= totalPages; i++) addPage(i)
  } else {
    addPage(1)
    if (page > 3) addEllipsis()
    for (let i = Math.max(2, page - 1); i <= Math.min(totalPages - 1, page + 1); i++) {
      addPage(i)
    }
    if (page < totalPages - 2) addEllipsis()
    addPage(totalPages)
  }

  return (
    <div className="pagination-bar">
      <div className="pagination-info">
        Showing <strong>{start}</strong>–<strong>{end}</strong> of <strong>{total}</strong>
      </div>

      <div className="pagination-controls">
        <button
          className="pagination-btn"
          onClick={() => go(1)}
          disabled={page === 1}
          title="First"
        >
          «
        </button>
        <button
          className="pagination-btn"
          onClick={() => go(page - 1)}
          disabled={page === 1}
          title="Previous"
        >
          ‹
        </button>

        {pages.map((p, i) =>
          p === '...' ? (
            <span key={`e-${i}`} className="pagination-ellipsis">
              …
            </span>
          ) : (
            <button
              key={p}
              className={`pagination-btn ${p === page ? 'active' : ''}`}
              onClick={() => go(p)}
            >
              {p}
            </button>
          )
        )}

        <button
          className="pagination-btn"
          onClick={() => go(page + 1)}
          disabled={page === totalPages}
          title="Next"
        >
          ›
        </button>
        <button
          className="pagination-btn"
          onClick={() => go(totalPages)}
          disabled={page === totalPages}
          title="Last"
        >
          »
        </button>
      </div>

      <div className="pagination-size">
        <label>
          Rows per page:{' '}
          <select
            className="pagination-select"
            value={pageSize}
            onChange={(e) => {
              onPageSizeChange(Number(e.target.value))
              onPageChange(1)
            }}
          >
            {pageSizeOptions.map((n) => (
              <option key={n} value={n}>
                {n}
              </option>
            ))}
          </select>
        </label>
      </div>
    </div>
  )
}