import PageHeader from '../../components/PageHeader.jsx'

export default function FieldLibraryPage({ navigate }) {
  return (
    <>
      <PageHeader
        eyebrow="E-FORM BUILDER"
        title="Field Library"
        description="Reusable fields available across all forms"
      />

      <section className="panel">
        <p style={{ padding: 20, color: '#6b7280' }}>
          Field library coming soon.
        </p>
      </section>
    </>
  )
}