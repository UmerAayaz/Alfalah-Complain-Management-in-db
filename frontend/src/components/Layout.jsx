import { useState } from 'react'
import { useDevUser } from '../auth/DevUserContext.jsx'
import './Layout.css'

// ============================================================
// ICONS — inline SVG (no dependency)
// ============================================================
const Icons = {
  dashboard: (
    <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <rect x="3" y="3" width="7" height="9" />
      <rect x="14" y="3" width="7" height="5" />
      <rect x="14" y="12" width="7" height="9" />
      <rect x="3" y="16" width="7" height="5" />
    </svg>
  ),
  complaints: (
    <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" />
      <polyline points="14 2 14 8 20 8" />
      <line x1="9" y1="15" x2="15" y2="15" />
      <line x1="9" y1="18" x2="13" y2="18" />
    </svg>
  ),
  queue: (
    <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <path d="M9 11l3 3L22 4" />
      <path d="M21 12v7a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11" />
    </svg>
  ),
  workflows: (
    <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <circle cx="5" cy="6" r="3" />
      <circle cx="19" cy="6" r="3" />
      <circle cx="12" cy="18" r="3" />
      <path d="M8 6h8" />
      <path d="M12 9v6" />
      <path d="M5 9v3a3 3 0 0 0 3 3h1" />
      <path d="M19 9v3a3 3 0 0 1-3 3h-1" />
    </svg>
  ),
  forms: (
    <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <rect x="3" y="3" width="18" height="18" rx="2" />
      <line x1="7" y1="8" x2="17" y2="8" />
      <line x1="7" y1="12" x2="17" y2="12" />
      <line x1="7" y1="16" x2="13" y2="16" />
    </svg>
  ),
}

// ============================================================
// MENU CONFIG — hardcoded with role filter
// ============================================================
const MENU = [
  {
    key: 'dashboard',
    label: 'Dashboard',
    path: '/dashboard',
    icon: Icons.dashboard,
    roles: null, // null = everyone
  },
  {
    key: 'complaints',
    label: 'Complaints',
    path: '/complaints',
    icon: Icons.complaints,
    roles: null,
  },
  {
    key: 'queue',
    label: 'My Working Queue',
    path: '/tasks',
    icon: Icons.queue,
    roles: null,
  },
  {
    key: 'workflows',
    label: 'Workflow Designer',
    path: '/workflows',
    icon: Icons.workflows,
    roles: ['DeptAdmin'],
  },
  {
    key: 'forms',
    label: 'E-Form Builder',
    path: '/forms',
    icon: Icons.forms,
    roles: ['DeptAdmin'],
  },
]

// ============================================================
// LAYOUT
// ============================================================
export default function Layout({ route, navigate, children }) {
  const { me, roles, devUsers, userId, switchUser, loading } = useDevUser()
  const [collapsed, setCollapsed] = useState(false)

  // ---- isActive — is a function declaration so it is hoisted ----
  function isActive(path) {
    if (path === '/tasks') {
      return route === '/tasks' || route === '/my-queue'
    }
    return route === path || route.startsWith(path + '/')
  }

  // ---- Visible menu items (role filter) ----
  const visibleItems = MENU.filter((item) => {
    if (!item.roles) return true
    return item.roles.some((r) => roles?.has?.(r))
  })

  // ---- Dynamic page title ----
  const currentMenuItem = MENU.find((m) => isActive(m.path))
  const pageTitle = currentMenuItem?.label || 'Complaint Management Unit'

  // ---- Short roles label (e.g. "DeptAdmin" or "DeptAdmin +2") ----
  const rolesLabel = (() => {
    const list =
      me?.memberships?.map((x) => x.roleCode) ||
      (roles ? [...roles] : [])

    if (list.length === 0) return 'No role'
    if (list.length === 1) return list[0]
    return `${list[0]} +${list.length - 1}`
  })()

  // ---- User avatar initial ----
  const userInitial = (me?.displayName || 'U').charAt(0).toUpperCase()

  return (
    <div className={`app-layout ${collapsed ? 'sidebar-collapsed' : ''}`}>
      {/* ============================================================
          SIDEBAR
      ============================================================ */}
      <aside className="app-sidebar">
        <div className="app-sidebar__brand">
          <div className="app-sidebar__logo">A</div>
          {!collapsed && (
            <div className="app-sidebar__brand-text">
              <div className="app-sidebar__brand-title">Bank Alfalah</div>
              <div className="app-sidebar__brand-sub">Complaint Management</div>
            </div>
          )}
        </div>

        <nav className="app-sidebar__nav">
          {visibleItems.map((item) => (
            <button
              key={item.key}
              className={`app-sidebar__item ${isActive(item.path) ? 'active' : ''}`}
              onClick={() => navigate(item.path)}
              title={collapsed ? item.label : undefined}
            >
              <span className="app-sidebar__icon">{item.icon}</span>
              {!collapsed && (
                <span className="app-sidebar__label">{item.label}</span>
              )}
            </button>
          ))}
        </nav>

        <div className="app-sidebar__footer">
          <button
            className="app-sidebar__collapse"
            onClick={() => setCollapsed((c) => !c)}
            aria-label="Toggle sidebar"
          >
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round">
              <polyline
                points={collapsed ? '9 18 15 12 9 6' : '15 18 9 12 15 6'}
              />
            </svg>
            {!collapsed && (
              <span className="app-sidebar__collapse-label">Collapse</span>
            )}
          </button>
        </div>
      </aside>

      {/* ============================================================
          MAIN — HEADER + CONTENT
      ============================================================ */}
      <div className="app-main">
        <header className="app-header">
          <div className="app-header__left">
            <h2 className="app-header__title">{pageTitle}</h2>
          </div>

          <div className="app-header__right">
            <div className="user-switcher-wrap">
              {/* Avatar */}
              <div className="user-avatar">{userInitial}</div>

              {/* Name + roles */}
              <div className="user-meta">
                <strong>
                  {loading
                    ? 'Loading…'
                    : me?.displayName || 'Unknown user'}
                </strong>
                <span>{rolesLabel}</span>
              </div>

              {/* Switcher dropdown */}
              <select
                className="user-switcher"
                value={userId || ''}
                onChange={(e) => switchUser(e.target.value)}
                aria-label="Switch development user"
              >
                {devUsers?.map((user) => (
                  <option key={user.id} value={user.id}>
                    {user.name}
                    {user.roleCode ? ` · ${user.roleCode}` : ''}
                  </option>
                ))}
              </select>
            </div>
          </div>
        </header>

        <main className="app-content">
          <div className="app-content__inner">{children}</div>
        </main>
      </div>
    </div>
  )
}