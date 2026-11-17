import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { BarChart, Bar, XAxis, YAxis, Tooltip, ResponsiveContainer, CartesianGrid } from 'recharts'
import { DollarSign, TrendingUp, AlertTriangle, Info } from 'lucide-react'
import { getOfficeBudget, getOffices, getMonthlyReport } from '../api/endpoints'
import { LoadingSpinner, ErrorMessage, PageHeader } from '../components/Feedback'
import BudgetBar from '../components/BudgetBar'

export default function Budgets() {
  const now = new Date()
  const [year, setYear] = useState(now.getFullYear())
  const [month, setMonth] = useState(now.getMonth() + 1)

  const { data: offices } = useQuery({ queryKey: ['offices'], queryFn: getOffices })
  const osloId = offices?.find(o => o.name === 'Oslo')?.id
  const bengaluruId = offices?.find(o => o.name === 'Bengaluru')?.id

  const { data: osloBudget, isLoading: loadingOslo } = useQuery({
    queryKey: ['budget', osloId, year, month],
    queryFn: () => getOfficeBudget(osloId!, year, month),
    enabled: !!osloId,
  })
  const { data: bengaluruBudget, isLoading: loadingBengaluru } = useQuery({
    queryKey: ['budget', bengaluruId, year, month],
    queryFn: () => getOfficeBudget(bengaluruId!, year, month),
    enabled: !!bengaluruId,
  })
  const { data: report } = useQuery({
    queryKey: ['monthly-report', year, month],
    queryFn: () => getMonthlyReport(year, month),
  })

  const isLoading = loadingOslo || loadingBengaluru

  const months = Array.from({ length: 12 }, (_, i) => ({
    value: i + 1,
    label: new Date(2000, i).toLocaleString('default', { month: 'long' }),
  }))

  const categoryChartData = [
    ...(report?.oslo.spendingByCategory.map(c => ({ name: c.category, Oslo: c.totalSpending })) ?? []),
  ].reduce((acc: any[], item) => {
    const existing = acc.find(a => a.name === item.name)
    if (existing) { existing.Oslo = item.Oslo; return acc }
    return [...acc, item]
  }, [])

  const bengaluruCats = report?.bengaluru.spendingByCategory ?? []
  bengaluruCats.forEach(c => {
    const existing = categoryChartData.find(a => a.name === c.category)
    if (existing) existing.Bengaluru = c.totalSpending
    else categoryChartData.push({ name: c.category, Bengaluru: c.totalSpending })
  })

  return (
    <div>
      <PageHeader
        title="Budget Management"
        subtitle="Monthly budget tracking for Oslo and Bengaluru offices"
        actions={
          <div className="flex gap-2">
            <select value={month} onChange={e => setMonth(parseInt(e.target.value))}
              className="border border-slate-200 rounded-lg px-3 py-2 text-sm bg-white focus:outline-none focus:ring-2 focus:ring-indigo-300">
              {months.map(m => <option key={m.value} value={m.value}>{m.label}</option>)}
            </select>
            <select value={year} onChange={e => setYear(parseInt(e.target.value))}
              className="border border-slate-200 rounded-lg px-3 py-2 text-sm bg-white focus:outline-none focus:ring-2 focus:ring-indigo-300">
              {[now.getFullYear() - 1, now.getFullYear(), now.getFullYear() + 1].map(y => (
                <option key={y} value={y}>{y}</option>
              ))}
            </select>
          </div>
        }
      />

      {/* Currency note */}
      <div className="bg-amber-50 border border-amber-200 rounded-xl p-3 mb-6 flex items-start gap-2 text-xs text-amber-800">
        <Info size={14} className="flex-shrink-0 mt-0.5" />
        <span>Oslo budget is tracked in <strong>NOK</strong>. Bengaluru budget is tracked in <strong>INR</strong>. These are separate budgets in different currencies and are never directly added together.</span>
      </div>

      {isLoading ? <LoadingSpinner /> : (
        <div className="space-y-6">
          {/* Budget cards */}
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
            {[osloBudget, bengaluruBudget].filter(Boolean).map(b => b && (
              <div key={b.officeId} className="bg-white rounded-xl border border-slate-200 shadow-sm p-6">
                <div className="flex items-center justify-between mb-4">
                  <div>
                    <h2 className="font-bold text-slate-900">{b.officeName}</h2>
                    <p className="text-xs text-slate-400">{b.budgetMonth}</p>
                  </div>
                  <div className="p-2.5 bg-indigo-50 rounded-xl">
                    <DollarSign size={18} className="text-indigo-600" />
                  </div>
                </div>

                <div className="space-y-3 mb-5">
                  <div className="flex justify-between text-sm">
                    <span className="text-slate-500">Monthly Limit</span>
                    <span className="font-bold text-slate-900">{b.monthlyBudget.toLocaleString()} {b.currency}</span>
                  </div>
                  <div className="flex justify-between text-sm">
                    <span className="text-slate-500">Actual Spending</span>
                    <span className="font-semibold text-indigo-600">{b.actualSpending.toLocaleString()} {b.currency}</span>
                  </div>
                  <div className="flex justify-between text-sm">
                    <span className="text-slate-500">Active Reservations</span>
                    <span className="font-semibold text-amber-600">{b.activeReservations.toLocaleString()} {b.currency}</span>
                  </div>
                  <div className="flex justify-between text-sm border-t border-slate-100 pt-3">
                    <span className="text-slate-700 font-semibold">Available Balance</span>
                    <span className={`font-bold text-lg ${b.availableBudget < b.monthlyBudget * 0.15 ? 'text-red-600' : b.availableBudget < b.monthlyBudget * 0.3 ? 'text-amber-600' : 'text-emerald-600'}`}>
                      {b.availableBudget.toLocaleString()} {b.currency}
                    </span>
                  </div>
                </div>

                <BudgetBar spent={b.actualSpending} reserved={b.activeReservations} total={b.monthlyBudget} currency={b.currency} />

                {b.availableBudget < b.monthlyBudget * 0.15 && (
                  <div className="mt-4 flex items-center gap-2 text-xs text-red-600 bg-red-50 rounded-lg px-3 py-2">
                    <AlertTriangle size={13} />
                    Budget is below 15% — consider reviewing upcoming refills
                  </div>
                )}

                <div className="mt-4 text-xs text-slate-400">
                  <p>Typical purchase range: {b.currency === 'NOK' ? '300–1,200 NOK' : '500–3,000 INR'} (informational only)</p>
                </div>
              </div>
            ))}
          </div>

          {/* Combined report note */}
          {report && (
            <div className="bg-white rounded-xl border border-slate-200 shadow-sm p-6">
              <div className="flex items-center gap-2 mb-4">
                <TrendingUp size={16} className="text-indigo-600" />
                <h3 className="font-semibold text-slate-900">Cross-Office Summary ({report.year}-{String(report.month).padStart(2, '0')})</h3>
              </div>
              <div className="grid grid-cols-2 lg:grid-cols-4 gap-4 text-sm mb-4">
                <div className="bg-slate-50 rounded-lg p-3">
                  <p className="text-slate-500 text-xs">Oslo Spending</p>
                  <p className="font-bold text-slate-900">{report.oslo.actualSpending.toLocaleString()} NOK</p>
                </div>
                <div className="bg-slate-50 rounded-lg p-3">
                  <p className="text-slate-500 text-xs">Bengaluru Spending</p>
                  <p className="font-bold text-slate-900">{report.bengaluru.actualSpending.toLocaleString()} INR</p>
                </div>
                <div className="bg-indigo-50 rounded-lg p-3">
                  <p className="text-slate-500 text-xs">Oslo in INR (illustrative)</p>
                  <p className="font-bold text-indigo-700">{report.osloSpendingInInr.toLocaleString()} INR</p>
                </div>
                <div className="bg-indigo-50 rounded-lg p-3">
                  <p className="text-slate-500 text-xs">Combined (illustrative)</p>
                  <p className="font-bold text-indigo-700">{report.combinedTotalInr.toLocaleString()} INR</p>
                </div>
              </div>
              <p className="text-xs text-amber-700 bg-amber-50 rounded-lg px-3 py-2">
                ⚠ {report.exchangeRateLabel} — Rate used: 1 NOK = {report.demoExchangeRateNokToInr} INR. This is an illustrative demo rate, not a live market rate.
              </p>
            </div>
          )}

          {/* Category spending chart */}
          {categoryChartData.length > 0 && (
            <div className="bg-white rounded-xl border border-slate-200 shadow-sm p-6">
              <h3 className="font-semibold text-slate-900 mb-1 text-sm">Spending by Category</h3>
              <p className="text-xs text-slate-400 mb-4">Each office in its own currency</p>
              <ResponsiveContainer width="100%" height={220}>
                <BarChart data={categoryChartData}>
                  <CartesianGrid strokeDasharray="3 3" stroke="#f1f5f9" />
                  <XAxis dataKey="name" tick={{ fontSize: 11 }} />
                  <YAxis tick={{ fontSize: 11 }} />
                  <Tooltip formatter={(v) => v != null ? Number(v).toLocaleString() : ''} />
                  <Bar dataKey="Oslo" name="Oslo (NOK)" fill="#6366f1" radius={[3, 3, 0, 0]} />
                  <Bar dataKey="Bengaluru" name="Bengaluru (INR)" fill="#f59e0b" radius={[3, 3, 0, 0]} />
                </BarChart>
              </ResponsiveContainer>
            </div>
          )}
        </div>
      )}
    </div>
  )
}
