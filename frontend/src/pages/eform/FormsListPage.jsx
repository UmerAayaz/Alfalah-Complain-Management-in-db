import PageHeader from '../../components/PageHeader.jsx'

export default function FormsListPage({ navigate }) {
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

      <section className="panel">
        <p style={{ padding: 20, color: '#6b7280' }}>
          No forms yet. Click "New Form" to create one.
        </p>
      </section>
    </>
  )
}