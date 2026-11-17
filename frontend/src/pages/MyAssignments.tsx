import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { toast } from 'sonner'
import { CheckCircle, XCircle, ShoppingCart, Package, FileText, Clock, AlertTriangle } from 'lucide-react'
import {
  getVolunteerAssignments, acceptOffer, declineOffer,
  submitPurchase, confirmRefilled, submitClaim
} from '../api/endpoints'
import { getDemoUserId } from '../api/client'
import { LoadingSpinner, ErrorMessage, EmptyState, PageHeader } from '../components/Feedback'
import StatusBadge from '../components/StatusBadge'
import Modal from '../components/Modal'
import ConfirmDialog from '../components/ConfirmDialog'
import type { RefillRequest } from '../types'

const purchaseSchema = z.object({
  amount: z.number({ invalid_type_error: 'Required' }).positive('Must be positive'),
  receiptReference: z.string().min(1, 'Receipt reference is required'),
  description: z.string().min(3, 'Description required'),
})
type PurchaseForm = z.infer<typeof purchaseSchema>

function AssignmentCard({ refill, onAction }: { refill: RefillRequest; onAction: (action: string, r: RefillRequest) => void }) {
  const isOverdue = refill.assignmentDeadline && new Date(refill.assignmentDeadline) < new Date()
  const offerExpiring = refill.offerExpiresAt && new Date(refill.offerExpiresAt) < new Date(Date.now() + 30 * 60 * 1000)

  return (
    <div className={`bg-white rounded-xl border shadow-sm p-5 flex flex-col gap-4 ${
      isOverdue ? 'border-red-200' : offerExpiring ? 'border-amber-200' : 'border-slate-200'
    }`}>
      <div className="flex items-start justify-between gap-3">
        <div>
          <p className="font-semibold text-slate-900">{refill.snackBoxName}</p>
          <p className="text-xs text-slate-400 mt-0.5">{refill.snackCategory} · {refill.officeName} · #{refill.id}</p>
        </div>
        <StatusBadge status={refill.status} size="md" />
      </div>

      <div className="grid grid-cols-2 gap-2 text-xs text-slate-600">
        <div><span className="text-slate-400">Est. Cost</span><p className="font-medium">{refill.estimatedCost} {refill.currency}</p></div>
        {refill.purchase && <div><span className="text-slate-400">Actual</span><p className={`font-medium ${refill.purchase.isOverBudget ? 'text-red-600' : ''}`}>{refill.purchase.amount} {refill.purchase.currency}</p></div>}
        {refill.offerExpiresAt && refill.status === 'Offered' && (
          <div className="col-span-2">
            <span className="text-slate-400">Offer expires</span>
            <p className={`font-medium ${offerExpiring ? 'text-amber-600' : ''}`}>
              {new Date(refill.offerExpiresAt).toLocaleString()}
              {offerExpiring && ' ⚠ Expiring soon'}
            </p>
          </div>
        )}
        {refill.assignmentDeadline && refill.status === 'Assigned' && (
          <div className="col-span-2">
            <span className="text-slate-400">Deadline</span>
            <p className={`font-medium ${isOverdue ? 'text-red-600' : ''}`}>
              {new Date(refill.assignmentDeadline).toLocaleString()}
              {isOverdue && ' ⚠ OVERDUE'}
            </p>
          </div>
        )}
        {refill.completedAt && <div><span className="text-slate-400">Completed</span><p className="font-medium">{new Date(refill.completedAt).toLocaleDateString()}</p></div>}
      </div>

      {refill.purchase?.receiptReference && (
        <div className="bg-slate-50 rounded-lg px-3 py-2 text-xs">
          <span className="text-slate-400">Receipt: </span>
          <span className="font-medium text-slate-700">{refill.purchase.receiptReference}</span>
          <span className="text-slate-400 ml-2">— {refill.purchase.description}</span>
        </div>
      )}

      {refill.reimbursementClaim && (
        <div className="bg-indigo-50 rounded-lg px-3 py-2 text-xs">
          <span className="text-slate-500">Claim {refill.reimbursementClaim.externalClaimId ?? `#${refill.reimbursementClaim.id}`}: </span>
          <StatusBadge status={refill.reimbursementClaim.status} />
          {refill.reimbursementClaim.failureReason && (
            <p className="text-red-600 mt-1">{refill.reimbursementClaim.failureReason}</p>
          )}
        </div>
      )}

      {/* Action buttons */}
      <div className="flex flex-wrap gap-2 mt-auto">
        {refill.status === 'Offered' && (
          <>
            <button onClick={() => onAction('accept', refill)}
              className="flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium text-white bg-emerald-600 rounded-lg hover:bg-emerald-700 transition-colors">
              <CheckCircle size={13} /> Accept
            </button>
            <button onClick={() => onAction('decline', refill)}
              className="flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium text-slate-700 bg-slate-100 rounded-lg hover:bg-slate-200 transition-colors">
              <XCircle size={13} /> Decline
            </button>
          </>
        )}
        {refill.status === 'Assigned' && (
          <button onClick={() => onAction('purchase', refill)}
            className="flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium text-white bg-indigo-600 rounded-lg hover:bg-indigo-700 transition-colors">
            <ShoppingCart size={13} /> Record Purchase
          </button>
        )}
        {refill.status === 'Purchased' && (
          <button onClick={() => onAction('confirm', refill)}
            className="flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium text-white bg-teal-600 rounded-lg hover:bg-teal-700 transition-colors">
            <Package size={13} /> Confirm Refilled
          </button>
        )}
        {refill.status === 'Refilled' && !refill.reimbursementClaim && (
          <button onClick={() => onAction('claim', refill)}
            className="flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium text-white bg-purple-600 rounded-lg hover:bg-purple-700 transition-colors">
            <FileText size={13} /> Submit Claim
          </button>
        )}
      </div>
    </div>
  )
}

export default function MyAssignments() {
  const qc = useQueryClient()
  const userId = getDemoUserId()
  const [purchaseTarget, setPurchaseTarget] = useState<RefillRequest | null>(null)
  const [confirmTarget, setConfirmTarget] = useState<RefillRequest | null>(null)
  const [claimTarget, setClaimTarget] = useState<RefillRequest | null>(null)
  const [declineTarget, setDeclineTarget] = useState<RefillRequest | null>(null)

  const { data: assignments, isLoading, error } = useQuery({
    queryKey: ['assignments', userId],
    queryFn: () => getVolunteerAssignments(userId),
    refetchInterval: 15_000,
  })

  const { register, handleSubmit, reset, formState: { errors } } = useForm<PurchaseForm>({
    resolver: zodResolver(purchaseSchema),
  })

  const invalidate = () => {
    qc.invalidateQueries({ queryKey: ['assignments', userId] })
    qc.invalidateQueries({ queryKey: ['refills'] })
    qc.invalidateQueries({ queryKey: ['snack-boxes'] })
  }

  const acceptMutation = useMutation({
    mutationFn: (id: number) => acceptOffer(id),
    onSuccess: () => { toast.success('Assignment accepted!'); invalidate() },
    onError: (err: any) => {
      if (err?.response?.status === 409) toast.error('This offer was already taken by another volunteer.')
      else toast.error(err?.response?.data?.message ?? 'Failed to accept')
    },
  })

  const declineMutation = useMutation({
    mutationFn: (id: number) => declineOffer(id),
    onSuccess: () => { toast.info('Offer declined. Another volunteer will be notified.'); invalidate(); setDeclineTarget(null) },
    onError: (err: any) => toast.error(err?.response?.data?.message ?? 'Failed to decline'),
  })

  const purchaseMutation = useMutation({
    mutationFn: (data: PurchaseForm) => submitPurchase(purchaseTarget!.id, data.amount, data.receiptReference, data.description),
    onSuccess: () => { toast.success('Purchase recorded!'); invalidate(); setPurchaseTarget(null); reset() },
    onError: (err: any) => toast.error(err?.response?.data?.message ?? 'Failed to record purchase'),
  })

  const confirmMutation = useMutation({
    mutationFn: (id: number) => confirmRefilled(id),
    onSuccess: () => { toast.success('Refill confirmed! Snack box is now available.'); invalidate(); setConfirmTarget(null) },
    onError: (err: any) => toast.error(err?.response?.data?.message ?? 'Failed to confirm'),
  })

  const claimMutation = useMutation({
    mutationFn: (id: number) => submitClaim(id),
    onSuccess: () => { toast.success('Reimbursement claim submitted to HR!'); invalidate(); setClaimTarget(null) },
    onError: (err: any) => toast.error(err?.response?.data?.message ?? 'Failed to submit claim'),
  })

  const handleAction = (action: string, refill: RefillRequest) => {
    if (action === 'accept') acceptMutation.mutate(refill.id)
    else if (action === 'decline') setDeclineTarget(refill)
    else if (action === 'purchase') setPurchaseTarget(refill)
    else if (action === 'confirm') setConfirmTarget(refill)
    else if (action === 'claim') setClaimTarget(refill)
  }

  if (isLoading) return <LoadingSpinner />
  if (error) return <ErrorMessage message="Failed to load assignments" />

  const offered = assignments?.filter(r => r.status === 'Offered') ?? []
  const active = assignments?.filter(r => ['Assigned', 'Purchased'].includes(r.status)) ?? []
  const completed = assignments?.filter(r => ['Refilled', 'ClaimSubmitted', 'Reimbursed'].includes(r.status)) ?? []
  const other = assignments?.filter(r => ['Cancelled', 'Requested'].includes(r.status)) ?? []

  return (
    <div>
      <PageHeader
        title="My Assignments"
        subtitle="Your volunteer refill assignments and workflow actions"
      />

      {assignments?.length === 0 && (
        <EmptyState message="No assignments yet. You'll be notified when a refill is offered to you." icon={<Package size={40} />} />
      )}

      {offered.length > 0 && (
        <section className="mb-8">
          <h2 className="text-sm font-semibold text-amber-700 mb-3 flex items-center gap-2">
            <Clock size={14} /> Pending Offers ({offered.length})
          </h2>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            {offered.map(r => <AssignmentCard key={r.id} refill={r} onAction={handleAction} />)}
          </div>
        </section>
      )}

      {active.length > 0 && (
        <section className="mb-8">
          <h2 className="text-sm font-semibold text-indigo-700 mb-3 flex items-center gap-2">
            <AlertTriangle size={14} /> Active Assignments ({active.length})
          </h2>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            {active.map(r => <AssignmentCard key={r.id} refill={r} onAction={handleAction} />)}
          </div>
        </section>
      )}

      {completed.length > 0 && (
        <section className="mb-8">
          <h2 className="text-sm font-semibold text-emerald-700 mb-3 flex items-center gap-2">
            <CheckCircle size={14} /> Completed ({completed.length})
          </h2>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            {completed.map(r => <AssignmentCard key={r.id} refill={r} onAction={handleAction} />)}
          </div>
        </section>
      )}

      {other.length > 0 && (
        <section>
          <h2 className="text-sm font-semibold text-slate-500 mb-3">Other ({other.length})</h2>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            {other.map(r => <AssignmentCard key={r.id} refill={r} onAction={handleAction} />)}
          </div>
        </section>
      )}

      {/* Purchase Modal */}
      <Modal open={!!purchaseTarget} title={`Record Purchase — ${purchaseTarget?.snackBoxName}`} onClose={() => { setPurchaseTarget(null); reset() }}>
        <form onSubmit={handleSubmit(data => purchaseMutation.mutate(data))} className="space-y-4">
          <div>
            <label className="block text-xs font-medium text-slate-700 mb-1">
              Actual Amount ({purchaseTarget?.currency}) *
            </label>
            <input type="number" step="0.01" {...register('amount', { valueAsNumber: true })}
              placeholder={purchaseTarget?.officeName === 'Oslo' ? 'e.g. 340' : 'e.g. 750'}
              className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-300" />
            {errors.amount && <p className="text-xs text-red-500 mt-1">{errors.amount.message}</p>}
          </div>
          <div>
            <label className="block text-xs font-medium text-slate-700 mb-1">Receipt Reference *</label>
            <input {...register('receiptReference')} placeholder="e.g. RCPT-2024-042"
              className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-300" />
            {errors.receiptReference && <p className="text-xs text-red-500 mt-1">{errors.receiptReference.message}</p>}
            <p className="text-xs text-slate-400 mt-1">Enter your receipt number or reference. Physical receipts are kept separately.</p>
          </div>
          <div>
            <label className="block text-xs font-medium text-slate-700 mb-1">Description *</label>
            <input {...register('description')} placeholder="e.g. Digestive Biscuits refill"
              className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-300" />
            {errors.description && <p className="text-xs text-red-500 mt-1">{errors.description.message}</p>}
          </div>
          <div className="flex gap-3 justify-end pt-2">
            <button type="button" onClick={() => { setPurchaseTarget(null); reset() }}
              className="px-4 py-2 text-sm text-slate-700 bg-slate-100 rounded-lg hover:bg-slate-200 transition-colors">
              Cancel
            </button>
            <button type="submit" disabled={purchaseMutation.isPending}
              className="px-4 py-2 text-sm text-white bg-indigo-600 rounded-lg hover:bg-indigo-700 disabled:opacity-50 transition-colors">
              {purchaseMutation.isPending ? 'Saving…' : 'Record Purchase'}
            </button>
          </div>
        </form>
      </Modal>

      {/* Confirm Refilled Dialog */}
      <ConfirmDialog
        open={!!confirmTarget}
        title="Confirm Snack Box Refilled"
        message={`Confirm that you have placed the purchased snacks in ${confirmTarget?.snackBoxName}? This will mark the box as Available.`}
        confirmLabel="Confirm Refilled"
        onConfirm={() => confirmMutation.mutate(confirmTarget!.id)}
        onCancel={() => setConfirmTarget(null)}
      />

      {/* Submit Claim Dialog */}
      <ConfirmDialog
        open={!!claimTarget}
        title="Submit Reimbursement Claim"
        message={`Submit HR reimbursement claim for ${claimTarget?.purchase?.amount} ${claimTarget?.purchase?.currency}? This will be sent to the HR system.`}
        confirmLabel="Submit to HR"
        onConfirm={() => claimMutation.mutate(claimTarget!.id)}
        onCancel={() => setClaimTarget(null)}
      />

      {/* Decline Dialog */}
      <ConfirmDialog
        open={!!declineTarget}
        title="Decline Assignment Offer"
        message={`Decline the refill offer for ${declineTarget?.snackBoxName}? Another eligible volunteer will be offered the assignment.`}
        confirmLabel="Decline"
        danger
        onConfirm={() => declineMutation.mutate(declineTarget!.id)}
        onCancel={() => setDeclineTarget(null)}
      />
    </div>
  )
}
