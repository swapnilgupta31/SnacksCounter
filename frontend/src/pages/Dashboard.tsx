import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { BarChart, Bar, XAxis, YAxis, Tooltip, ResponsiveContainer, PieChart, Pie, Cell } from 'recharts'
import {
  Package, RefreshCw, Clock, CheckCircle, AlertCircle,
  DollarSign, TrendingUp, Users, ArrowRight
} from 'lucide-react'
import {
  getRefills, getSnackBoxes, getReimbursements,
  getOfficeBudget, getOffices, getVolunteers
} from '../api/endpoints'
import { CardSkeleton } from '../components/Feedback'
import StatusBadge from '../components/StatusBadge'

const PIE_COLORS = ['#6366f1', '#f59e0b', '#10b981', '#ef4444', '#8b5cf6', '#06b6d4']

function KpiCard({ label, value, icon: Icon, color, sub }: {
  label: string; value: number | string; icon: any; color: string; sub?: string
}) {
  return (
    <div className="bg-white rounded-xl border border-slate-200 p-5 flex items-start gap-4 shadow-sm">
      <div className={`p-3 rounded-xl ${color} flex-shrink-0`}>
        <Icon size={18} className="text-white" />
      </div>
      <div className="min-w-0">
        <p className="text-2xl font-bold text-slate-900 leading-tight">{value}</p>
        <p className="text-xs text-slate-500 mt-0.5 leading-tight">{label}</p>
        {sub && <p className="text-xs text-slate-400 mt-0.5">{sub}</p>}
      </div>
    </div>
  )
}

export default function Dashboard() {
  const now = new Date()
  const { data: offices, isLoading: loadingOffices } = useQuery({ queryKey: ['offices'], queryFn: getOffices })
  const { data: refills, isLoading: loadingRefills } = useQuery({ queryKey: ['refills'], queryFn: () => getRefills() })
  const { data: boxes, isLoading: loadingBoxes } = useQuery({ queryKey: ['snack-boxes'], queryFn: () => getSnackBoxes() })
  const { data: claims } = useQuery({ queryKey: ['reimbursements'], queryFn: getReimbursements })
  const { data: volunteers } = useQuery({ queryKey: ['volunteers'], queryFn: () => getVolunteers() })

  const osloId = offices?.find(o => o.name === 'Oslo')?.id
  const bengaluruId = offices?.find(o => o.name === 'Bengaluru')?.id

  const { data: osloBudget } = useQuery({
    queryKey: ['budget', osloId, now.getFullYear(), now.getMonth() + 1],
    queryFn: () => getOfficeBudget(osloId!, now.getFullYear(), now.getMonth() + 1),
    enabled: !!osloId
  })
  const { data: bengaluruBudget } = useQuery({
    queryKey: ['budget', bengaluruId, now.getFullYear(), now.getMonth() + 1],
    queryFn: () => getOfficeBudget(bengaluruId!, now.getFullYear(), now.getMonth() + 1),
    enabled: !!bengaluruId
  })

  const isLoading = loadingOffices || loadingRefills || loadingBoxes

  if (isLoading) {
    return (
      <div>
        <div className="mb-6">
          <div className="h-6 bg-slate-200 rounded w-40 animate-pulse mb-2" />
          <div className="h-4 bg-slate-200 rounded w-64 animate-pulse" />
        </div>
        <div className="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
          {Array.from({ length: 8 }).map((_, i) => <CardSkeleton key={i} />)}
        </div>
      </div>
    )
  }

  const emptyBoxes = boxes?.filter(b => b.availabilityStatus === 'Empty').length ?? 0
  const activeRefills = refills?.filter(r => !['Cancelled', 'Reimbursed'].includes(r.status)).length ?? 0
  const pendingOffers = refills?.filter(r => r.status === 'Offered').length ?? 0
  const completedRefills = refills?.filter(r => r.status === 'Reimbursed' || r.status === 'Refilled').length ?? 0
  const pendingClaims = claims?.filter(c => !['Paid', 'Rejected'].includes(c.status)).length ?? 0
  const availableVols = volunteers?.filter(v => v.isAvailable && !v.isOnLeave && v.isPresentToday).length ?? 0

  // Status distribution for pie chart
  const statusCounts: Record<string, number> = {}
  refills?.forEach(r => { statusCounts[r.status] = (statusCounts[r.status] ?? 0) + 1 })
  const pieData = Object.entries(statusCounts).map(([name, value]) => ({ name, value }))

  // Budget bar chart data
  const budgetData = [
    osloBudget && { name: 'Oslo (NOK)', budget: osloBudget.monthlyBudget, spent: osloBudget.actualSpending, reserved: osloBudget.activeReservations },
    bengaluruBudget && { name: 'Bengaluru (INR)', budget: bengaluruBudget.monthlyBudget, spent: bengaluruBudget.actualSpending, reserved: bengaluruBudget.activeReservations },
  ].filter(Boolean) as { name: string; budget: number; spent: number; reserved: number }[]

  const recentRefills = [...(refills ?? [])].sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()).slice(0, 6)

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-xl font-bold text-slate-900">Dashboard</h1>
        <p className="text-sm text-slate-500 mt-0.5">
          Overview of snack counter operations — Oslo & Bengaluru
        </p>
      </div>

      {/* KPI Grid */}
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
        <KpiCard label="Total Snack Boxes" value={boxes?.length ?? 0} icon={Package} color="bg-indigo-500" />
        <KpiCard label="Empty Boxes" value={emptyBoxes} icon={AlertCircle} color="bg-red-500" sub="Need refilling" />
        <KpiCard label="Active Refills" value={activeRefills} icon={RefreshCw} color="bg-blue-500" />
        <KpiCard label="Pending Offers" value={pendingOffers} icon={Clock} color="bg-amber-500" sub="Awaiting volunteer" />
        <KpiCard label="Completed Refills" value={completedRefills} icon={CheckCircle} color="bg-emerald-500" />
        <KpiCard label="Pending Claims" value={pendingClaims} icon={DollarSign} color="bg-purple-500" sub="HR reimbursements" />
        <KpiCard label="Available Volunteers" value={availableVols} icon={Users} color="bg-teal-500" sub="Present today" />
        <KpiCard
          label="Oslo Budget Used"
          value={osloBudget ? `${Math.round(((osloBudget.actualSpending + osloBudget.activeReservations) / osloBudget.monthlyBudget) * 100)}%` : '—'}
          icon={TrendingUp}
          color="bg-slate-600"
          sub={osloBudget ? `${osloBudget.actualSpending.toLocaleString()} / ${osloBudget.monthlyBudget.toLocaleString()} NOK` : ''}
        />
      </div>

      {/* Charts row */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Budget chart */}
        <div className="bg-white rounded-xl border border-slate-200 p-5 shadow-sm">
          <h3 className="font-semibold text-slate-900 mb-1 text-sm">Monthly Budget Utilization</h3>
          <p className="text-xs text-slate-400 mb-4">Spent vs Reserved vs Total (own currency)</p>
          {budgetData.length > 0 ? (
            <ResponsiveContainer width="100%" height={200}>
              <BarChart data={budgetData} barGap={4}>
                <XAxis dataKey="name" tick={{ fontSize: 11 }} />
                <YAxis tick={{ fontSize: 11 }} />
                <Tooltip formatter={(v) => v != null ? Number(v).toLocaleString() : ''} />
                <Bar dataKey="budget" name="Budget Limit" fill="#e2e8f0" radius={[3, 3, 0, 0]} />
                <Bar dataKey="spent" name="Spent" fill="#6366f1" radius={[3, 3, 0, 0]} />
                <Bar dataKey="reserved" name="Reserved" fill="#f59e0b" radius={[3, 3, 0, 0]} />
              </BarChart>
            </ResponsiveContainer>
          ) : <p className="text-slate-400 text-sm py-8 text-center">Loading budget data…</p>}
        </div>

        {/* Refill status pie */}
        <div className="bg-white rounded-xl border border-slate-200 p-5 shadow-sm">
          <h3 className="font-semibold text-slate-900 mb-1 text-sm">Refill Requests by Status</h3>
          <p className="text-xs text-slate-400 mb-4">All-time distribution</p>
          {pieData.length > 0 ? (
            <ResponsiveContainer width="100%" height={200}>
              <PieChart>
                <Pie data={pieData} cx="50%" cy="50%" outerRadius={70} dataKey="value" label={({ name, percent }) => `${name} ${Math.round((percent ?? 0) * 100)}%`} labelLine={false} fontSize={10}>
                  {pieData.map((_, i) => <Cell key={i} fill={PIE_COLORS[i % PIE_COLORS.length]} />)}
                </Pie>
                <Tooltip />
              </PieChart>
            </ResponsiveContainer>
          ) : <p className="text-slate-400 text-sm py-8 text-center">No refill data yet</p>}
        </div>
      </div>

      {/* Budget detail cards */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {[osloBudget, bengaluruBudget].filter(Boolean).map(b => b && (
          <div key={b.officeId} className="bg-white rounded-xl border border-slate-200 p-5 shadow-sm">
            <div className="flex items-center justify-between mb-4">
              <div>
                <h3 className="font-semibold text-slate-900 text-sm">{b.officeName} — {b.budgetMonth}</h3>
                <p className="text-xs text-slate-400">Monthly budget overview</p>
              </div>
              <Link to="/budgets" className="text-xs text-indigo-600 hover:text-indigo-800 flex items-center gap-1">
                Details <ArrowRight size={12} />
              </Link>
            </div>
            <div className="space-y-2 text-sm mb-4">
              <div className="flex justify-between">
                <span className="text-slate-500">Monthly Limit</span>
                <span className="font-semibold">{b.monthlyBudget.toLocaleString()} {b.currency}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-slate-500">Actual Spending</span>
                <span className="font-semibold text-indigo-600">{b.actualSpending.toLocaleString()} {b.currency}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-slate-500">Active Reservations</span>
                <span className="font-semibold text-amber-600">{b.activeReservations.toLocaleString()} {b.currency}</span>
              </div>
              <div className="flex justify-between border-t border-slate-100 pt-2">
                <span className="text-slate-700 font-medium">Available</span>
                <span className={`font-bold ${b.availableBudget < b.monthlyBudget * 0.2 ? 'text-red-600' : 'text-emerald-600'}`}>
                  {b.availableBudget.toLocaleString()} {b.currency}
                </span>
              </div>
            </div>
            <div className="w-full bg-slate-100 rounded-full h-2 overflow-hidden">
              <div className="h-full flex">
                <div className="bg-indigo-500 transition-all" style={{ width: `${Math.min(100, (b.actualSpending / b.monthlyBudget) * 100)}%` }} />
                <div className="bg-amber-400 transition-all" style={{ width: `${Math.min(100 - (b.actualSpending / b.monthlyBudget) * 100, (b.activeReservations / b.monthlyBudget) * 100)}%` }} />
              </div>
            </div>
            <p className="text-xs text-slate-400 mt-1">
              {Math.round(((b.actualSpending + b.activeReservations) / b.monthlyBudget) * 100)}% utilized
            </p>
          </div>
        ))}
      </div>

      {/* Recent activity */}
      <div className="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden">
        <div className="flex items-center justify-between px-5 py-4 border-b border-slate-100">
          <h3 className="font-semibold text-slate-900 text-sm">Recent Refill Activity</h3>
          <Link to="/refills" className="text-xs text-indigo-600 hover:text-indigo-800 flex items-center gap-1">
            View all <ArrowRight size={12} />
          </Link>
        </div>
        {recentRefills.length === 0 ? (
          <p className="text-slate-400 text-sm p-5">No refill activity yet</p>
        ) : (
          <div className="divide-y divide-slate-50">
            {recentRefills.map(r => (
              <div key={r.id} className="flex items-center justify-between px-5 py-3 hover:bg-slate-50 transition-colors">
                <div className="flex items-center gap-3 min-w-0">
                  <div className="w-8 h-8 bg-indigo-50 rounded-lg flex items-center justify-center flex-shrink-0">
                    <RefreshCw size={14} className="text-indigo-500" />
                  </div>
                  <div className="min-w-0">
                    <p className="text-sm font-medium text-slate-900 truncate">{r.snackBoxName}</p>
                    <p className="text-xs text-slate-400">{r.officeName} · #{r.id}</p>
                  </div>
                </div>
                <div className="flex items-center gap-3 flex-shrink-0">
                  <span className="text-xs text-slate-400 hidden sm:block">
                    {new Date(r.createdAt).toLocaleDateString()}
                  </span>
                  <StatusBadge status={r.status} />
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  )
}
