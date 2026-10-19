import { Handle, Position } from '@xyflow/react'

const NODE_META = {
  0: {
    label: 'Start',
    cls: 'start',
    icon: (
      <svg
        width="18"
        height="18"
        viewBox="0 0 24 24"
        fill="currentColor"
        stroke="currentColor"
        strokeWidth="2.5"
        strokeLinecap="round"
        strokeLinejoin="round"
      >
        <polygon points="6 3 20 12 6 21 6 3" />
      </svg>
    ),
  },
  1: {
    label: 'Human Task',
    cls: 'human',
    icon: (
      <svg
        width="18"
        height="18"
        viewBox="0 0 24 24"
        fill="none"
        stroke="currentColor"
        strokeWidth="2"
        strokeLinecap="round"
        strokeLinejoin="round"
      >
        <circle cx="12" cy="8" r="4" />
        <path d="M6 21v-2a4 4 0 0 1 4-4h4a4 4 0 0 1 4 4v2" />
      </svg>
    ),
  },
  2: {
    label: 'End',
    cls: 'end',
    icon: (
      <svg
        width="18"
        height="18"
        viewBox="0 0 24 24"
        fill="none"
        stroke="currentColor"
        strokeWidth="3"
        strokeLinecap="round"
        strokeLinejoin="round"
      >
        <polyline points="20 6 9 17 4 12" />
      </svg>
    ),
  },
}

export default function WorkflowNode({ data, selected }) {
  const nodeType = Number(data.nodeType)
  const meta = NODE_META[nodeType] || NODE_META[1]
  const isHuman = nodeType === 1

  // INLINE STYLES — force colors regardless of CSS
  const typeStyle = isHuman
    ? { color: '#3b82f6' }
    : { color: '#6b7280' }

  const labelStyle = isHuman
    ? { color: '#2563eb' }
    : { color: '#111827' }

  const metaStyle = isHuman
    ? { color: '#60a5fa' }
    : { color: '#6b7280' }

  return (
    <div
      className={`wf-node wf-node--${meta.cls} ${
        selected ? 'wf-node--selected' : ''
      }`}
    >
      {nodeType !== 0 && (
        <Handle
          type="target"
          position={Position.Left}
          className="wf-handle wf-handle--target"
        />
      )}

      <div className="wf-node-icon">{meta.icon}</div>

      <div className="wf-node-content">
        <div className="wf-node-type" style={typeStyle}>
          {meta.label}
        </div>
        <div className="wf-node-label" style={labelStyle}>
          {data.label || 'Untitled'}
        </div>

        {isHuman && data.roleCode && (
          <div className="wf-node-meta" style={metaStyle}>
            <svg
              width="10"
              height="10"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              strokeWidth="2"
              strokeLinecap="round"
              strokeLinejoin="round"
            >
              <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2" />
              <circle cx="12" cy="7" r="4" />
            </svg>
            {data.roleCode}
          </div>
        )}

        {isHuman && data.slaHours && (
          <div className="wf-node-meta" style={metaStyle}>
            <svg
              width="10"
              height="10"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              strokeWidth="2"
              strokeLinecap="round"
              strokeLinejoin="round"
            >
              <circle cx="12" cy="12" r="10" />
              <polyline points="12 6 12 12 16 14" />
            </svg>
            {data.slaHours}h SLA
          </div>
        )}
      </div>

      {nodeType !== 2 && (
        <Handle
          type="source"
          position={Position.Right}
          className="wf-handle wf-handle--source"
        />
      )}
    </div>
  )
}