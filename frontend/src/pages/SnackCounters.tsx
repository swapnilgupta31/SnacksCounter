import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useSearchParams } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { toast } from 'sonner'
import { Package, AlertCircle, CheckCircle, Clock, Search, Filter } from 'lucide-react'
import { getSnackBoxes, getOffices, reportEmpty } from '../api/endpoints'
import { LoadingSpinner, ErrorMessage, EmptyState, PageHeader } from '../components/Feedback'
import StatusBadge from '../components/StatusBadge'
import ConfirmDialog from '../components/ConfirmDialog'
import type { SnackBox } from '../types'

const reportSchema = z.object({
  estimatedCost: z.number({ invalid_type_error: 'Required' }).positive('Must be positive'),
  notes: z.string().optional(),
})
type ReportForm = z.infer<typeof reportSchema>

function AvailabilityIcon({ status }: { status: string }) {
  if (status === 'Available') return <CheckCircle size={14} className="text-emerald-500" />
  if (status === 'Empty') return <AlertCircle size={14} className="text-red-500" />
  return <Clock size={14} className="text-amber-500" />
}

export default function SnackCounters() {
  const [searchParams] = useSearchParams()
  const qc = useQueryClient()
  const [officeFilter, setOfficeFilter] = useState<string>(searchParams.get('office') ?? '')
  const [categoryFilter, setCategoryFilter] = useState('')
  const [statusFilter, setStatusFilter] = useState('')
  const [search, setSearch] = useState('')
  const [reportTarget, setReportTarget] = useState<SnackBox | null>(null)

  const { data: offices } = useQuery({ queryKey: ['offices'], queryFn: getOffices })
  const { data: boxes, isLoading, error } = useQuery({
    queryKey: ['snack-boxes', officeFilter],
    queryFn: () => getSnackBoxes(officeFilter ? parseInt(officeFilter) : undefined),
  })

  const { register, handleSubmit, reset, formState: { errors } } = useForm<ReportForm>({
    resolver: zodResolver(reportSchema),
  })

  const reportMutation = useMutation({
    mutationFn: (data: ReportForm) =>
      reportEmpty(reportTarget!.id, data.estimatedCost, data.notes),
    onSuccess: () => {
      toast.success(`Refill request created for ${reportTarget?.name}`)
      qc.invalidateQueries({ queryKey: ['snack-boxes'] })
      qc.invalidateQueries({ queryKey: ['refills'] })
      setReportTarget(null)
      reset()
    },
    onError: (err: any) => {
      const msg = err?.response?.data?.message ?? err?.message ?? 'Failed to report empty'
      if (err?.response?.status === 409) {
        toast.error('A refill request already exists for this snack box.')
      } else {
        toast.error(msg)
      }
      setReportTarget(null)
      reset()
    },
  })

  const categories = [...new Set(boxes?.map(b => b.category) ?? [])]

  const filtered = boxes?.filter(b => {
    if (categoryFilter && b.category !== categoryFilter) return false
    if (statusFilter && b.availabilityStatus !== statusFilter) return false
    if (search && !b.name.toLowerCase().includes(search.toLowerCase())) return false
    return true
  }) ?? []

  const osloBoxes = filtered.filter(b => b.officeName === 'Oslo')
  const bengaluruBoxes = filtered.filter(b => b.officeName === 'Bengaluru')

  if (isLoading) return <LoadingSpinner />
  if (error) return <ErrorMessage message="Failed to load snack boxes" />

  return (
    <div>
      <PageHeader
        title="Snack Counters"
        subtitle={`${boxes?.length ?? 0} snack boxes across all offices`}
      />

      {/* Filters */}
      <div className="flex flex-wrap gap-3 mb-6">
        <div className="relative">
          <Search size={14} className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" />
          <input
            value={search}
            onChange={e => setSearch(e.target.value)}
            placeholder="Search snacks…"
            className="pl-8 pr-3 py-2 text-sm border border-slate-200 rounded-lg bg-white focus:outline-none focus:ring-2 focus:ring-indigo-300 w-48"
          />
        </div>
        <select value={officeFilter} onChange={e => setOfficeFilter(e.target.value)}
          className="border border-slate-200 rounded-lg px-3 py-2 text-sm bg-white focus:outline-none focus:ring-2 focus:ring-indigo-300">
          <option value="">All Offices</option>
          {offices?.map(o => <option key={o.id} value={o.id}>{o.name}</option>)}
        </select>
        <select value={categoryFilter} onChange={e => setCategoryFilter(e.target.value)}
          className="border border-slate-200 rounded-lg px-3 py-2 text-sm bg-white focus:outline-none focus:ring-2 focus:ring-indigo-300">
          <option value="">All Categories</option>
          {categories.map(c => <option key={c} value={c}>{c}</option>)}
        </select>
        <select value={statusFilter} onChange={e => setStatusFilter(e.target.value)}
          className="border border-slate-200 rounded-lg px-3 py-2 text-sm bg-white focus:outline-none focus:ring-2 focus:ring-indigo-300">
          <option value="">All Statuses</option>
          <option value="Available">Available</option>
          <option value="Empty">Empty</option>
          <option value="RefillPending">Refill Pending</option>
        </select>
        {(search || officeFilter || categoryFilter || statusFilter) && (
          <button onClick={() => { setSearch(''); setOfficeFilter(''); setCategoryFilter(''); setStatusFilter('') }}
            className="text-sm text-slate-500 hover:text-slate-700 flex items-center gap-1">
            <Filter size={13} /> Clear
          </button>
        )}
      </div>

      {filtered.length === 0 ? (
        <EmptyState message="No snack boxes match your filters" icon={<Package size={40} />} />
      ) : (
        <div className="space-y-8">
          {[{ label: 'Oslo', items: osloBoxes, currency: 'NOK' }, { label: 'Bengaluru', items: bengaluruBoxes, currency: 'INR' }]
            .filter(g => g.items.length > 0)
            .map(group => (
              <div key={group.label}>
                <h2 className="text-sm font-semibold text-slate-700 mb-3 flex items-center gap-2">
                  <span className="w-2 h-2 rounded-full bg-indigo-500 inline-block" />
                  {group.label} Office — {group.items.length} boxes
                </h2>
                <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
                  {group.items.map(box => (
                    <div key={box.id} className={`bg-white rounded-xl border shadow-sm p-4 flex flex-col gap-3 transition-shadow hover:shadow-md ${
                      box.availabilityStatus === 'Empty' ? 'border-red-200' :
                      box.availabilityStatus === 'RefillPending' ? 'border-amber-200' :
                      'border-slate-200'
                    }`}>
                      <div className="flex items-start justify-between gap-2">
                        <div className="min-w-0">
                          <p className="font-semibold text-slate-900 text-sm truncate">{box.name}</p>
                          <p className="text-xs text-slate-400 mt-0.5">{box.category}</p>
                        </div>
                        <div className="flex items-center gap-1 flex-shrink-0">
                          <AvailabilityIcon status={box.availabilityStatus} />
                          <StatusBadge status={box.availabilityStatus} />
                        </div>
                      </div>

                      <div className="text-xs text-slate-500 space-y-1">
                        <div className="flex justify-between">
                          <span>Last refilled</span>
                          <span className="text-slate-700">
                            {box.lastRefilledAt ? new Date(box.lastRefilledAt).toLocaleDateString() : 'Never'}
                          </span>
                        </div>
                        {box.activeRefillId && (
                          <div className="flex justify-between">
                            <span>Active refill</span>
                            <span className="text-indigo-600 font-medium">#{box.activeRefillId}</span>
                          </div>
                        )}
                      </div>

                      {box.availabilityStatus === 'Available' && !box.activeRefillId && (
                        <button
                          onClick={() => setReportTarget(box)}
                          className="mt-auto w-full py-1.5 text-xs font-medium text-red-600 border border-red-200 rounded-lg hover:bg-red-50 transition-colors"
                        >
                          Report Empty
                        </button>
                      )}
                      {box.availabilityStatus === 'Empty' && !box.activeRefillId && (
                        <button
                          onClick={() => setReportTarget(box)}
                          className="mt-auto w-full py-1.5 text-xs font-medium text-white bg-red-500 rounded-lg hover:bg-red-600 transition-colors"
                        >
                          Request Refill
                        </button>
                      )}
                      {box.activeRefillId && (
                        <div className="mt-auto text-xs text-center text-amber-600 bg-amber-50 rounded-lg py-1.5 font-medium">
                          Refill in progress
                        </div>
                      )}
                    </div>
                  ))}
                </div>
              </div>
            ))}
        </div>
      )}

      {/* Report Empty Dialog */}
      <ConfirmDialog
        open={!!reportTarget}
        title={`Report Empty: ${reportTarget?.name}`}
        message={`This will create a refill request and notify eligible volunteers at ${reportTarget?.officeName}.`}
        confirmLabel={reportMutation.isPending ? 'Submitting…' : 'Submit Request'}
        onConfirm={handleSubmit(data => reportMutation.mutate(data))}
        onCancel={() => { setReportTarget(null); reset() }}
      >
        <div className="space-y-3">
          <div>
            <label className="block text-xs font-medium text-slate-700 mb-1">
              Estimated Cost ({reportTarget?.officeName === 'Oslo' ? 'NOK' : 'INR'}) *
            </label>
            <input
              type="number"
              step="0.01"
              {...register('estimatedCost', { valueAsNumber: true })}
              placeholder={reportTarget?.officeName === 'Oslo' ? 'e.g. 350' : 'e.g. 800'}
              className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-300"
            />
            {errors.estimatedCost && <p className="text-xs text-red-500 mt-1">{errors.estimatedCost.message}</p>}
          </div>
          <div>
            <label className="block text-xs font-medium text-slate-700 mb-1">Notes (optional)</label>
            <textarea
              {...register('notes')}
              rows={2}
              placeholder="Any additional notes…"
              className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-300 resize-none"
            />
          </div>
        </div>
      </ConfirmDialog>
    </div>
  )
}
