import PageHeader from '../../components/PageHeader.jsx'

export default function FormBuilderPage({ formId, isNew, navigate }) {
  return (
    <>
      <PageHeader
        eyebrow="E-FORM BUILDER"
        title={isNew ? 'Create Form' : 'Edit Form'}
        description="Build your form by adding fields from the library"
      />

      <section className="panel">
        <p style={{ padding: 20, color: '#6b7280' }}>
          Form builder coming soon.
        </p>
      </section>
    </>
  )
}