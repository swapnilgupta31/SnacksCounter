import { useState } from 'react'
import { NavLink, Outlet, useNavigate } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import {
  LayoutDashboard, Package, RefreshCw, UserCheck, Users,
  DollarSign, CreditCard, BarChart2, Bell, PlayCircle,
  ChevronLeft, ChevronRight, Menu, Building2, ChevronDown
} from 'lucide-react'
import { getAllEmployees, getOffices, getNotifications } from '../api/endpoints'
import { getDemoUserId, setDemoUserId } from '../api/client'

const navItems = [
  { to: '/', icon: LayoutDashboard, label: 'Dashboard', end: true },
  { to: '/snack-counters', icon: Package, label: 'Snack Counters' },
  { to: '/refills', icon: RefreshCw, label: 'Refill Requests' },
  { to: '/my-assignments', icon: UserCheck, label: 'My Assignments' },
  { to: '/volunteers', icon: Users, label: 'Volunteers' },
  { to: '/budgets', icon: DollarSign, label: 'Budgets' },
  { to: '/reimbursements', icon: CreditCard, label: 'Reimbursements' },
  { to: '/reports', icon: BarChart2, label: 'Reports & Analytics' },
  { to: '/notifications', icon: Bell, label: 'Notifications' },
  { to: '/demo', icon: PlayCircle, label: 'Workflow Demo' },
]

export default function AppLayout() {
  const [collapsed, setCollapsed] = useState(false)
  const [mobileOpen, setMobileOpen] = useState(false)
  const navigate = useNavigate()

  const { data: employees } = useQuery({ queryKey: ['employees'], queryFn: getAllEmployees })
  const { data: offices } = useQuery({ queryKey: ['offices'], queryFn: getOffices })
  const { data: notifications } = useQuery({ queryKey: ['notifications'], queryFn: getNotifications, refetchInterval: 30_000 })

  const userId = getDemoUserId()
  const currentUser = employees?.find(e => e.id === userId)
  const unreadCount = notifications?.filter(n => !n.isRead).length ?? 0

  const handleUserChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    setDemoUserId(parseInt(e.target.value, 10))
    window.location.reload()
  }

  return (
    <div className="flex h-screen bg-slate-50 overflow-hidden">
      {/* Mobile overlay */}
      {mobileOpen && (
        <div className="fixed inset-0 bg-black/40 z-20 lg:hidden" onClick={() => setMobileOpen(false)} />
      )}

      {/* Sidebar */}
      <aside className={`
        fixed lg:relative z-30 flex flex-col h-full bg-slate-900 text-white transition-all duration-300
        ${collapsed ? 'w-16' : 'w-60'}
        ${mobileOpen ? 'translate-x-0' : '-translate-x-full lg:translate-x-0'}
      `}>
        {/* Logo */}
        <div className={`flex items-center gap-3 px-4 py-4 border-b border-slate-700 min-h-[64px] ${collapsed ? 'justify-center' : ''}`}>
          <div className="w-8 h-8 bg-indigo-500 rounded-lg flex items-center justify-center flex-shrink-0">
            <span className="text-white font-bold text-sm">SF</span>
          </div>
          {!collapsed && (
            <div className="overflow-hidden">
              <p className="font-bold text-white text-sm leading-tight">SnackFlow</p>
              <p className="text-slate-400 text-xs leading-tight">Framsikt Automation</p>
            </div>
          )}
        </div>

        {/* Nav */}
        <nav className="flex-1 py-3 overflow-y-auto">
          {navItems.map(({ to, icon: Icon, label, end }) => (
            <NavLink
              key={to}
              to={to}
              end={end}
              className={({ isActive }) =>
                `flex items-center gap-3 mx-2 px-3 py-2.5 rounded-lg text-sm transition-colors mb-0.5 ${
                  isActive
                    ? 'bg-indigo-600 text-white font-medium'
                    : 'text-slate-300 hover:bg-slate-800 hover:text-white'
                } ${collapsed ? 'justify-center' : ''}`
              }
              title={collapsed ? label : undefined}
            >
              <Icon size={16} className="flex-shrink-0" />
              {!collapsed && <span className="truncate">{label}</span>}
            </NavLink>
          ))}
        </nav>

        {/* Collapse toggle */}
        <div className="p-3 border-t border-slate-700">
          {!collapsed && <p className="text-xs text-slate-500 mb-2 px-1">Demo Mode — Not production auth</p>}
          <button
            onClick={() => setCollapsed(c => !c)}
            className="hidden lg:flex items-center justify-center w-full py-1.5 rounded-lg text-slate-400 hover:bg-slate-800 hover:text-white transition-colors"
          >
            {collapsed ? <ChevronRight size={16} /> : <ChevronLeft size={16} />}
          </button>
        </div>
      </aside>

      {/* Main */}
      <div className="flex-1 flex flex-col min-w-0 overflow-hidden">
        {/* Top bar */}
        <header className="bg-white border-b border-slate-200 px-4 lg:px-6 py-3 flex items-center gap-3 min-h-[64px] flex-shrink-0">
          <button
            className="lg:hidden p-2 rounded-lg text-slate-500 hover:bg-slate-100"
            onClick={() => setMobileOpen(o => !o)}
          >
            <Menu size={18} />
          </button>

          {/* Office selector */}
          <div className="flex items-center gap-2 bg-slate-50 border border-slate-200 rounded-lg px-3 py-1.5">
            <Building2 size={14} className="text-slate-500" />
            <select
              className="text-sm bg-transparent border-0 text-slate-700 font-medium focus:ring-0 cursor-pointer pr-1"
              defaultValue=""
              onChange={e => {
                const val = e.target.value
                if (val === 'oslo') navigate('/snack-counters?office=1')
                else if (val === 'bengaluru') navigate('/snack-counters?office=2')
                else navigate('/')
              }}
            >
              <option value="">All Offices</option>
              {offices?.map(o => (
                <option key={o.id} value={o.name.toLowerCase()}>{o.name}</option>
              ))}
            </select>
          </div>

          <div className="flex-1" />

          {/* Notifications */}
          <button
            className="relative p-2 rounded-lg text-slate-500 hover:bg-slate-100 transition-colors"
            onClick={() => navigate('/notifications')}
          >
            <Bell size={18} />
            {unreadCount > 0 && (
              <span className="absolute -top-0.5 -right-0.5 w-4 h-4 bg-red-500 text-white text-xs rounded-full flex items-center justify-center font-bold">
                {unreadCount > 9 ? '9+' : unreadCount}
              </span>
            )}
          </button>

          {/* Demo user switcher */}
          <div className="flex items-center gap-2 bg-amber-50 border border-amber-200 rounded-lg px-3 py-1.5">
            <div className="w-6 h-6 bg-indigo-500 rounded-full flex items-center justify-center flex-shrink-0">
              <span className="text-white text-xs font-bold">
                {currentUser?.name?.charAt(0) ?? '?'}
              </span>
            </div>
            <div className="hidden sm:block">
              <select
                value={userId}
                onChange={handleUserChange}
                className="text-xs bg-transparent border-0 text-amber-800 font-semibold focus:ring-0 cursor-pointer max-w-[140px]"
              >
                {employees?.map(e => (
                  <option key={e.id} value={e.id}>
                    {e.name} ({e.role})
                  </option>
                ))}
              </select>
              {currentUser && (
                <p className="text-xs text-amber-600 leading-none">{currentUser.currentOfficeName}</p>
              )}
            </div>
            <ChevronDown size={12} className="text-amber-600 hidden sm:block" />
          </div>
        </header>

        {/* Page content */}
        <main className="flex-1 overflow-y-auto p-4 lg:p-6">
          <Outlet />
        </main>
      </div>
    </div>
  )
}
