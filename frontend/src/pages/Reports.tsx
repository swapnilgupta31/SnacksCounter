import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import {
  BarChart, Bar, XAxis, YAxis, Tooltip, ResponsiveContainer,
  CartesianGrid, PieChart, Pie, Cell, Legend
} from 'recharts'
import { Download, BarChart2, Info } from 'lucide-react'
import { getMonthlyReport, getSnackConsumption } from '../api/endpoints'
import { LoadingSpinner, ErrorMessage, PageHeader } from '../components/Feedback'

const COLORS = ['#6366f1', '#f59e0b', '#10b981', '#ef4444', '#8b5cf6', '#06b6d4', '#f97316']

function exportCsv(data: any[], filename: string) {
  if (!data.length) return
  const keys = Object.keys(data[0])
  const csv = [keys.join(','), ...data.map(row => keys.map(k => JSON.stringify(row[k] ?? '')).join(','))].join('\n')
  const blob = new Blob([csv], { type: 'text/csv' })
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url; a.download = filename; a.click()
  URL.revokeObjectURL(url)
}

export default function Reports() {
  const now = new Date()
  const [year, setYear] = useState(now.getFullYear())
  const [month, setMonth] = useState(now.getMonth() + 1)

  const { data: report, isLoading, error } = useQuery({
    queryKey: ['monthly-report', year, month],
    queryFn: () => getMonthlyReport(year, month),
  })
  const { data: consumption } = useQuery({
    queryKey: ['snack-consumption'],
    queryFn: getSnackConsumption,
  })

  const months = Array.from({ length: 12 }, (_, i) => ({
    value: i + 1,
    label: new Date(2000, i).toLocaleString('default', { month: 'long' }),
  }))

  const osloCatData = report?.oslo.spendingByCategory ?? []
  const bengaluruCatData = report?.bengaluru.spendingByCategory ?? []

  const reimbBreakdown = [
    ...(report?.oslo.reimbursementBreakdown.map(r => ({ ...r, office: 'Oslo' })) ?? []),
    ...(report?.bengaluru.reimbursementBreakdown.map(r => ({ ...r, office: 'Bengaluru' })) ?? []),
  ]

  const consumptionByOffice = (officeName: string) =>
    consumption?.filter(c => c.officeName === officeName).slice(0, 8) ?? []

  return (
    <div>
      <PageHeader
        title="Reports & Analytics"
        subtitle="Monthly spending, refill frequency, and reimbursement analytics"
        actions={
          <div className="flex gap-2">
            <select value={month} onChange={e => setMonth(parseInt(e.target.value))}
              className="border border-slate-200 rounded-lg px-3 py-2 text-sm bg-white focus:outline-none focus:ring-2 focus:ring-indigo-300">
              {months.map(m => <option key={m.value} value={m.value}>{m.label}</option>)}
            </select>
            <select value={year} onChange={e => setYear(parseInt(e.target.value))}
              className="border border-slate-200 rounded-lg px-3 py-2 text-sm bg-white focus:outline-none focus:ring-2 focus:ring-indigo-300">
              {[now.getFullYear() - 1, now.getFullYear()].map(y => <option key={y} value={y}>{y}</option>)}
            </select>
            {report && (
              <button
                onClick={() => exportCsv([
                  { office: 'Oslo', currency: 'NOK', budget: report.oslo.monthlyBudget, spent: report.oslo.actualSpending, available: report.oslo.availableBudget, completedRefills: report.oslo.completedRefills },
                  { office: 'Bengaluru', currency: 'INR', budget: report.bengaluru.monthlyBudget, spent: report.bengaluru.actualSpending, available: report.bengaluru.availableBudget, completedRefills: report.bengaluru.completedRefills },
                ], `snackflow-report-${year}-${String(month).padStart(2, '0')}.csv`)}
                className="flex items-center gap-2 px-3 py-2 text-sm bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 transition-colors"
              >
                <Download size={14} /> Export CSV
              </button>
            )}
          </div>
        }
      />

      {error ? <ErrorMessage message="Failed to load report" /> : isLoading ? <LoadingSpinner /> : report && (
        <div className="space-y-6">
          {/* Summary cards */}
          <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
            {[
              { label: 'Oslo Spending', value: `${report.oslo.actualSpending.toLocaleString()} NOK`, sub: `of ${report.oslo.monthlyBudget.toLocaleString()} NOK budget` },
              { label: 'Bengaluru Spending', value: `${report.bengaluru.actualSpending.toLocaleString()} INR`, sub: `of ${report.bengaluru.monthlyBudget.toLocaleString()} INR budget` },
              { label: 'Oslo Refills', value: report.oslo.completedRefills, sub: 'completed this month' },
              { label: 'Bengaluru Refills', value: report.bengaluru.completedRefills, sub: 'completed this month' },
            ].map(card => (
              <div key={card.label} className="bg-white rounded-xl border border-slate-200 shadow-sm p-4">
                <p className="text-xs text-slate-500 mb-1">{card.label}</p>
                <p className="text-xl font-bold text-slate-900">{card.value}</p>
                <p className="text-xs text-slate-400 mt-0.5">{card.sub}</p>
              </div>
            ))}
          </div>

          {/* Exchange rate note */}
          <div className="bg-amber-50 border border-amber-200 rounded-xl p-3 flex items-start gap-2 text-xs text-amber-800">
            <Info size={14} className="flex-shrink-0 mt-0.5" />
            <span>{report.exchangeRateLabel} — 1 NOK = {report.demoExchangeRateNokToInr} INR (illustrative). Oslo in INR: {report.osloSpendingInInr.toLocaleString()} INR. Combined: {report.combinedTotalInr.toLocaleString()} INR.</span>
          </div>

          {/* Category spending charts */}
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
            {[
              { title: 'Oslo — Spending by Category (NOK)', data: osloCatData, color: '#6366f1' },
              { title: 'Bengaluru — Spending by Category (INR)', data: bengaluruCatData, color: '#f59e0b' },
            ].map(chart => (
              <div key={chart.title} className="bg-white rounded-xl border border-slate-200 shadow-sm p-5">
                <h3 className="font-semibold text-slate-900 text-sm mb-4">{chart.title}</h3>
                {chart.data.length === 0 ? (
                  <p className="text-slate-400 text-sm py-8 text-center">No spending data for this period</p>
                ) : (
                  <ResponsiveContainer width="100%" height={200}>
                    <BarChart data={chart.data}>
                      <CartesianGrid strokeDasharray="3 3" stroke="#f1f5f9" />
                      <XAxis dataKey="category" tick={{ fontSize: 11 }} />
                      <YAxis tick={{ fontSize: 11 }} />
                      <Tooltip formatter={(v) => v != null ? Number(v).toLocaleString() : ''} />
                      <Bar dataKey="totalSpending" name="Spending" fill={chart.color} radius={[3, 3, 0, 0]} />
                    </BarChart>
                  </ResponsiveContainer>
                )}
              </div>
            ))}
          </div>

          {/* Reimbursement breakdown */}
          {reimbBreakdown.length > 0 && (
            <div className="bg-white rounded-xl border border-slate-200 shadow-sm p-5">
              <h3 className="font-semibold text-slate-900 text-sm mb-4">Reimbursement Status Breakdown</h3>
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead className="bg-slate-50">
                    <tr>
                      {['Office', 'Status', 'Count', 'Total Amount'].map(h => (
                        <th key={h} className="px-4 py-2 text-left text-xs font-semibold text-slate-500 uppercase">{h}</th>
                      ))}
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-50">
                    {reimbBreakdown.map((r, i) => (
                      <tr key={i} className="hover:bg-slate-50">
                        <td className="px-4 py-2 text-slate-600">{r.office}</td>
                        <td className="px-4 py-2"><span className="px-2 py-0.5 rounded-full text-xs bg-slate-100 text-slate-700">{r.status}</span></td>
                        <td className="px-4 py-2 font-medium">{r.count}</td>
                        <td className="px-4 py-2 font-medium">{r.totalAmount.toLocaleString()}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          )}

          {/* Snack consumption / refill frequency */}
          {consumption && consumption.length > 0 && (
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
              {['Oslo', 'Bengaluru'].map(office => {
                const data = consumptionByOffice(office)
                return data.length > 0 ? (
                  <div key={office} className="bg-white rounded-xl border border-slate-200 shadow-sm p-5">
                    <h3 className="font-semibold text-slate-900 text-sm mb-1">{office} — Refill Frequency</h3>
                    <p className="text-xs text-slate-400 mb-4">Estimated consumption indicator (refill count, not exact units)</p>
                    <ResponsiveContainer width="100%" height={200}>
                      <BarChart data={data} layout="vertical">
                        <XAxis type="number" tick={{ fontSize: 10 }} />
                        <YAxis dataKey="snackBoxName" type="category" tick={{ fontSize: 10 }} width={100} />
                        <Tooltip />
                        <Bar dataKey="refillCount" name="Refill Count" fill="#6366f1" radius={[0, 3, 3, 0]} />
                      </BarChart>
                    </ResponsiveContainer>
                  </div>
                ) : null
              })}
            </div>
          )}
        </div>
      )}
    </div>
  )
}
