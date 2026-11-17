import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { RefreshCw, Search, Eye, XCircle } from 'lucide-react'
import { getRefills, getOffices, cancelRefill, processExpired, getRefill } from '../api/endpoints'
import { LoadingSpinner, ErrorMessage, PageHeader, SkeletonRow } from '../components/Feedback'
import StatusBadge from '../components/StatusBadge'
import Modal from '../components/Modal'
import ConfirmDialog from '../components/ConfirmDialog'
import type { RefillRequest } from '../types'

const STATUSES = ['Requested', 'Offered', 'Assigned', 'Purchased', 'Refilled', 'ClaimSubmitted', 'Reimbursed', 'Cancelled']

function RefillDetail({ refill }: { refill: RefillRequest }) {
  const timeline = [
    { label: 'Requested', date: refill.createdAt, done: true },
    { label: 'Offered', date: refill.offerExpiresAt, done: ['Offered', 'Assigned', 'Purchased', 'Refilled', 'ClaimSubmitted', 'Reimbursed'].includes(refill.status) },
    { label: 'Assigned', date: refill.assignedAt, done: ['Assigned', 'Purchased', 'Refilled', 'ClaimSubmitted', 'Reimbursed'].includes(refill.status) },
    { label: 'Purchased', date: refill.purchase?.purchasedAt ?? null, done: ['Purchased', 'Refilled', 'ClaimSubmitted', 'Reimbursed'].includes(refill.status) },
    { label: 'Refilled', date: refill.completedAt, done: ['Refilled', 'ClaimSubmitted', 'Reimbursed'].includes(refill.status) },
    { label: 'Claim Submitted', date: refill.reimbursementClaim?.submittedAt ?? null, done: ['ClaimSubmitted', 'Reimbursed'].includes(refill.status) },
    { label: 'Reimbursed', date: refill.reimbursementClaim?.paidAt ?? null, done: refill.status === 'Reimbursed' },
  ]

  return (
    <div className="space-y-5">
      <div className="grid grid-cols-2 gap-3 text-sm">
        <div><span className="text-slate-500">Snack Box</span><p className="font-medium">{refill.snackBoxName}</p></div>
        <div><span className="text-slate-500">Category</span><p className="font-medium">{refill.snackCategory}</p></div>
        <div><span className="text-slate-500">Office</span><p className="font-medium">{refill.officeName}</p></div>
        <div><span className="text-slate-500">Status</span><div className="mt-0.5"><StatusBadge status={refill.status} size="md" /></div></div>
        <div><span className="text-slate-500">Reported By</span><p className="font-medium">{refill.reportedByName}</p></div>
        <div><span className="text-slate-500">Volunteer</span><p className="font-medium">{refill.assignedVolunteerName ?? '—'}</p></div>
        <div><span className="text-slate-500">Estimated Cost</span><p className="font-medium">{refill.estimatedCost} {refill.currency}</p></div>
        {refill.purchase && <div><span className="text-slate-500">Actual Cost</span><p className="font-medium text-indigo-600">{refill.purchase.amount} {refill.purchase.currency}{refill.purchase.isOverBudget && <span className="ml-1 text-xs text-red-500">⚠ Over budget</span>}</p></div>}
        {refill.assignmentDeadline && <div><span className="text-slate-500">Deadline</span><p className="font-medium">{new Date(refill.assignmentDeadline).toLocaleString()}</p></div>}
        {refill.notes && <div className="col-span-2"><span className="text-slate-500">Notes</span><p className="font-medium">{refill.notes}</p></div>}
        {refill.cancellationReason && <div className="col-span-2"><span className="text-slate-500">Cancellation Reason</span><p className="font-medium text-red-600">{refill.cancellationReason}</p></div>}
      </div>

      {refill.purchase && (
        <div className="bg-slate-50 rounded-xl p-4 text-sm">
          <p className="font-semibold text-slate-700 mb-2">Purchase Details</p>
          <div className="grid grid-cols-2 gap-2">
            <div><span className="text-slate-500">Amount</span><p className="font-medium">{refill.purchase.amount} {refill.purchase.currency}</p></div>
            <div><span className="text-slate-500">Receipt Ref</span><p className="font-medium">{refill.purchase.receiptReference ?? '—'}</p></div>
            <div className="col-span-2"><span className="text-slate-500">Description</span><p className="font-medium">{refill.purchase.description}</p></div>
          </div>
        </div>
      )}

      {refill.reimbursementClaim && (
        <div className="bg-indigo-50 rounded-xl p-4 text-sm">
          <p className="font-semibold text-slate-700 mb-2">Reimbursement Claim</p>
          <div className="grid grid-cols-2 gap-2">
            <div><span className="text-slate-500">Claim ID</span><p className="font-medium">{refill.reimbursementClaim.externalClaimId ?? `#${refill.reimbursementClaim.id}`}</p></div>
            <div><span className="text-slate-500">Status</span><div className="mt-0.5"><StatusBadge status={refill.reimbursementClaim.status} /></div></div>
            <div><span className="text-slate-500">Amount</span><p className="font-medium">{refill.reimbursementClaim.amount} {refill.reimbursementClaim.currency}</p></div>
            {refill.reimbursementClaim.failureReason && <div className="col-span-2"><span className="text-slate-500">Failure</span><p className="font-medium text-red-600">{refill.reimbursementClaim.failureReason}</p></div>}
          </div>
        </div>
      )}

      <div>
        <p className="font-semibold text-slate-700 text-sm mb-3">Status Timeline</p>
        <div className="space-y-2">
          {timeline.map((step, i) => (
            <div key={i} className="flex items-center gap-3">
              <div className={`w-5 h-5 rounded-full flex items-center justify-center flex-shrink-0 text-xs font-bold ${
                step.done ? 'bg-indigo-500 text-white' : 'bg-slate-200 text-slate-400'
              }`}>
                {step.done ? '✓' : i + 1}
              </div>
              <span className={`text-sm ${step.done ? 'text-slate-900 font-medium' : 'text-slate-400'}`}>{step.label}</span>
              {step.date && <span className="text-xs text-slate-400 ml-auto">{new Date(step.date).toLocaleDateString()}</span>}
            </div>
          ))}
        </div>
      </div>
    </div>
  )
}

export default function RefillRequests() {
  const qc = useQueryClient()
  const [statusFilter, setStatusFilter] = useState('')
  const [officeFilter, setOfficeFilter] = useState<string>('')
  const [search, setSearch] = useState('')
  const [detailId, setDetailId] = useState<number | null>(null)
  const [cancelTarget, setCancelTarget] = useState<RefillRequest | null>(null)
  const [cancelReason, setCancelReason] = useState('')

  const { data: offices } = useQuery({ queryKey: ['offices'], queryFn: getOffices })
  const { data: refills, isLoading, error } = useQuery({
    queryKey: ['refills', statusFilter, officeFilter],
    queryFn: () => getRefills(statusFilter || undefined, officeFilter ? parseInt(officeFilter) : undefined),
  })
  const { data: detailRefill } = useQuery({
    queryKey: ['refill', detailId],
    queryFn: () => getRefill(detailId!),
    enabled: !!detailId,
  })

  const cancelMutation = useMutation({
    mutationFn: ({ id, reason }: { id: number; reason: string }) => cancelRefill(id, reason),
    onSuccess: () => {
      toast.success('Refill request cancelled')
      qc.invalidateQueries({ queryKey: ['refills'] })
      qc.invalidateQueries({ queryKey: ['snack-boxes'] })
      setCancelTarget(null)
      setCancelReason('')
    },
    onError: (err: any) => toast.error(err?.response?.data?.message ?? 'Failed to cancel'),
  })

  const processMutation = useMutation({
    mutationFn: processExpired,
    onSuccess: data => {
      toast.success(`Processed ${data.processed} expired offer(s)`)
      qc.invalidateQueries({ queryKey: ['refills'] })
    },
    onError: () => toast.error('Failed to process expired offers'),
  })

  const filtered = refills?.filter(r => {
    if (search && !r.snackBoxName.toLowerCase().includes(search.toLowerCase()) && !String(r.id).includes(search)) return false
    return true
  }) ?? []

  return (
    <div>
      <PageHeader
        title="Refill Requests"
        subtitle={`${refills?.length ?? 0} total requests`}
        actions={
          <button
            onClick={() => processMutation.mutate()}
            disabled={processMutation.isPending}
            className="flex items-center gap-2 px-3 py-2 text-sm bg-slate-100 border border-slate-200 rounded-lg hover:bg-slate-200 disabled:opacity-50 transition-colors"
          >
            <RefreshCw size={14} className={processMutation.isPending ? 'animate-spin' : ''} />
            Process Expired
          </button>
        }
      />

      <div className="flex flex-wrap gap-3 mb-5">
        <div className="relative">
          <Search size={14} className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" />
          <input value={search} onChange={e => setSearch(e.target.value)} placeholder="Search…"
            className="pl-8 pr-3 py-2 text-sm border border-slate-200 rounded-lg bg-white focus:outline-none focus:ring-2 focus:ring-indigo-300 w-40" />
        </div>
        <select value={statusFilter} onChange={e => setStatusFilter(e.target.value)}
          className="border border-slate-200 rounded-lg px-3 py-2 text-sm bg-white focus:outline-none focus:ring-2 focus:ring-indigo-300">
          <option value="">All Statuses</option>
          {STATUSES.map(s => <option key={s} value={s}>{s}</option>)}
        </select>
        <select value={officeFilter} onChange={e => setOfficeFilter(e.target.value)}
          className="border border-slate-200 rounded-lg px-3 py-2 text-sm bg-white focus:outline-none focus:ring-2 focus:ring-indigo-300">
          <option value="">All Offices</option>
          {offices?.map(o => <option key={o.id} value={o.id}>{o.name}</option>)}
        </select>
      </div>

      {error ? <ErrorMessage message="Failed to load refill requests" /> : (
        <div className="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden">
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead className="bg-slate-50 border-b border-slate-200">
                <tr>
                  {['#', 'Snack Box', 'Office', 'Status', 'Volunteer', 'Est. Cost', 'Actual', 'Created', 'Deadline', 'Actions'].map(h => (
                    <th key={h} className="px-4 py-3 text-left text-xs font-semibold text-slate-500 uppercase tracking-wide whitespace-nowrap">{h}</th>
                  ))}
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-50">
                {isLoading ? (
                  Array.from({ length: 5 }).map((_, i) => <SkeletonRow key={i} cols={10} />)
                ) : filtered.length === 0 ? (
                  <tr><td colSpan={10} className="py-12 text-center text-slate-400 text-sm">No refill requests found</td></tr>
                ) : filtered.map(r => (
                  <tr key={r.id} className="hover:bg-slate-50 transition-colors">
                    <td className="px-4 py-3 text-slate-400 font-mono text-xs">#{r.id}</td>
                    <td className="px-4 py-3">
                      <p className="font-medium text-slate-900">{r.snackBoxName}</p>
                      <p className="text-xs text-slate-400">{r.snackCategory}</p>
                    </td>
                    <td className="px-4 py-3 text-slate-600 whitespace-nowrap">{r.officeName}</td>
                    <td className="px-4 py-3"><StatusBadge status={r.status} /></td>
                    <td className="px-4 py-3 text-slate-600 whitespace-nowrap">{r.assignedVolunteerName ?? '—'}</td>
                    <td className="px-4 py-3 text-slate-600 whitespace-nowrap">{r.estimatedCost} {r.currency}</td>
                    <td className="px-4 py-3 whitespace-nowrap">
                      {r.purchase ? (
                        <span className={r.purchase.isOverBudget ? 'text-red-600 font-medium' : 'text-slate-600'}>
                          {r.purchase.amount} {r.purchase.currency}
                          {r.purchase.isOverBudget && <span className="ml-1 text-xs">⚠</span>}
                        </span>
                      ) : '—'}
                    </td>
                    <td className="px-4 py-3 text-slate-400 text-xs whitespace-nowrap">{new Date(r.createdAt).toLocaleDateString()}</td>
                    <td className="px-4 py-3 text-xs whitespace-nowrap">
                      {r.assignmentDeadline ? (
                        <span className={new Date(r.assignmentDeadline) < new Date() ? 'text-red-500 font-medium' : 'text-slate-400'}>
                          {new Date(r.assignmentDeadline).toLocaleDateString()}
                        </span>
                      ) : '—'}
                    </td>
                    <td className="px-4 py-3">
                      <div className="flex items-center gap-2">
                        <button onClick={() => setDetailId(r.id)}
                          className="p-1.5 rounded-lg text-slate-400 hover:bg-indigo-50 hover:text-indigo-600 transition-colors" title="View details">
                          <Eye size={14} />
                        </button>
                        {['Requested', 'Offered', 'Assigned'].includes(r.status) && (
                          <button onClick={() => setCancelTarget(r)}
                            className="p-1.5 rounded-lg text-slate-400 hover:bg-red-50 hover:text-red-600 transition-colors" title="Cancel">
                            <XCircle size={14} />
                          </button>
                        )}
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      <Modal open={!!detailId} title={`Refill Request #${detailId}`} onClose={() => setDetailId(null)} wide>
        {detailRefill ? <RefillDetail refill={detailRefill} /> : <LoadingSpinner />}
      </Modal>

      <ConfirmDialog
        open={!!cancelTarget}
        title="Cancel Refill Request"
        message={`Cancel refill for ${cancelTarget?.snackBoxName}? This will release any budget reservation.`}
        confirmLabel="Cancel Request"
        danger
        onConfirm={() => cancelMutation.mutate({ id: cancelTarget!.id, reason: cancelReason || 'Cancelled by admin' })}
        onCancel={() => { setCancelTarget(null); setCancelReason('') }}
      >
        <input
          value={cancelReason}
          onChange={e => setCancelReason(e.target.value)}
          placeholder="Reason for cancellation (optional)"
          className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-300"
        />
      </ConfirmDialog>
    </div>
  )
}
