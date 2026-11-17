import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { CreditCard, CheckCircle, XCircle, DollarSign, Eye, RefreshCw } from 'lucide-react'
import {
  getReimbursements, getHrClaims, approveHrClaim,
  rejectHrClaim, markHrClaimPaid, retrySubmission
} from '../api/endpoints'
import { LoadingSpinner, ErrorMessage, EmptyState, PageHeader, SkeletonRow } from '../components/Feedback'
import StatusBadge from '../components/StatusBadge'
import Modal from '../components/Modal'
import ConfirmDialog from '../components/ConfirmDialog'
import type { ReimbursementClaim, HrClaim } from '../types'

const CLAIM_STATUSES = ['PendingSubmission', 'Submitted', 'Approved', 'Rejected', 'Paid', 'SubmissionFailed']

function ClaimDetail({ claim }: { claim: ReimbursementClaim }) {
  const timeline = [
    { label: 'Claim Created', date: claim.createdAt, done: true },
    { label: 'Submitted to HR', date: claim.submittedAt, done: !!claim.submittedAt },
    { label: 'Approved', date: claim.approvedAt, done: !!claim.approvedAt },
    { label: 'Paid', date: claim.paidAt, done: !!claim.paidAt },
  ]
  return (
    <div className="space-y-4">
      <div className="grid grid-cols-2 gap-3 text-sm">
        <div><span className="text-slate-500">Employee</span><p className="font-medium">{claim.employeeName}</p></div>
        <div><span className="text-slate-500">Amount</span><p className="font-medium">{claim.amount} {claim.currency}</p></div>
        <div><span className="text-slate-500">Status</span><div className="mt-0.5"><StatusBadge status={claim.status} size="md" /></div></div>
        <div><span className="text-slate-500">External ID</span><p className="font-medium font-mono text-xs">{claim.externalClaimId ?? '—'}</p></div>
        {claim.failureReason && <div className="col-span-2"><span className="text-slate-500">Failure Reason</span><p className="font-medium text-red-600">{claim.failureReason}</p></div>}
      </div>
      <div>
        <p className="font-semibold text-slate-700 text-sm mb-3">Timeline</p>
        <div className="space-y-2">
          {timeline.map((step, i) => (
            <div key={i} className="flex items-center gap-3">
              <div className={`w-5 h-5 rounded-full flex items-center justify-center flex-shrink-0 text-xs font-bold ${step.done ? 'bg-indigo-500 text-white' : 'bg-slate-200 text-slate-400'}`}>
                {step.done ? '✓' : i + 1}
              </div>
              <span className={`text-sm ${step.done ? 'text-slate-900 font-medium' : 'text-slate-400'}`}>{step.label}</span>
              {step.date && <span className="text-xs text-slate-400 ml-auto">{new Date(step.date).toLocaleString()}</span>}
            </div>
          ))}
        </div>
      </div>
    </div>
  )
}

export default function Reimbursements() {
  const qc = useQueryClient()
  const [statusFilter, setStatusFilter] = useState('')
  const [detailClaim, setDetailClaim] = useState<ReimbursementClaim | null>(null)
  const [rejectTarget, setRejectTarget] = useState<HrClaim | null>(null)
  const [rejectReason, setRejectReason] = useState('')
  const [activeTab, setActiveTab] = useState<'claims' | 'hr'>('claims')

  const { data: claims, isLoading, error } = useQuery({ queryKey: ['reimbursements'], queryFn: getReimbursements })
  const { data: hrClaims, isLoading: loadingHr } = useQuery({
    queryKey: ['hr-claims'],
    queryFn: getHrClaims,
    enabled: activeTab === 'hr',
  })

  const invalidate = () => {
    qc.invalidateQueries({ queryKey: ['reimbursements'] })
    qc.invalidateQueries({ queryKey: ['hr-claims'] })
    qc.invalidateQueries({ queryKey: ['refills'] })
  }

  const approveMutation = useMutation({
    mutationFn: (externalId: string) => approveHrClaim(externalId),
    onSuccess: () => { toast.success('Claim approved'); invalidate() },
    onError: () => toast.error('Failed to approve'),
  })
  const rejectMutation = useMutation({
    mutationFn: ({ externalId, reason }: { externalId: string; reason?: string }) => rejectHrClaim(externalId, reason),
    onSuccess: () => { toast.info('Claim rejected'); invalidate(); setRejectTarget(null); setRejectReason('') },
    onError: () => toast.error('Failed to reject'),
  })
  const paidMutation = useMutation({
    mutationFn: (externalId: string) => markHrClaimPaid(externalId),
    onSuccess: () => { toast.success('Claim marked as paid'); invalidate() },
    onError: () => toast.error('Failed to mark paid'),
  })
  const retryMutation = useMutation({
    mutationFn: (id: number) => retrySubmission(id),
    onSuccess: () => { toast.success('Claim resubmitted'); invalidate() },
    onError: () => toast.error('Failed to retry'),
  })

  const filtered = claims?.filter(c => !statusFilter || c.status === statusFilter) ?? []

  return (
    <div>
      <PageHeader title="Reimbursements" subtitle="HR reimbursement claims for volunteer purchases" />

      {/* Tabs */}
      <div className="flex gap-1 mb-6 bg-slate-100 rounded-xl p-1 w-fit">
        {(['claims', 'hr'] as const).map(tab => (
          <button key={tab} onClick={() => setActiveTab(tab)}
            className={`px-4 py-2 text-sm font-medium rounded-lg transition-colors ${activeTab === tab ? 'bg-white text-slate-900 shadow-sm' : 'text-slate-500 hover:text-slate-700'}`}>
            {tab === 'claims' ? 'Reimbursement Claims' : 'HR System (Demo)'}
          </button>
        ))}
      </div>

      {activeTab === 'claims' && (
        <>
          <div className="flex gap-3 mb-5">
            <select value={statusFilter} onChange={e => setStatusFilter(e.target.value)}
              className="border border-slate-200 rounded-lg px-3 py-2 text-sm bg-white focus:outline-none focus:ring-2 focus:ring-indigo-300">
              <option value="">All Statuses</option>
              {CLAIM_STATUSES.map(s => <option key={s} value={s}>{s}</option>)}
            </select>
          </div>

          {error ? <ErrorMessage message="Failed to load claims" /> : (
            <div className="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden">
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead className="bg-slate-50 border-b border-slate-200">
                    <tr>
                      {['Claim ID', 'Employee', 'Refill #', 'Amount', 'Status', 'Submitted', 'Approved', 'Paid', 'Actions'].map(h => (
                        <th key={h} className="px-4 py-3 text-left text-xs font-semibold text-slate-500 uppercase tracking-wide whitespace-nowrap">{h}</th>
                      ))}
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-50">
                    {isLoading ? Array.from({ length: 4 }).map((_, i) => <SkeletonRow key={i} cols={9} />) :
                    filtered.length === 0 ? (
                      <tr><td colSpan={9} className="py-12 text-center text-slate-400 text-sm">No claims found</td></tr>
                    ) : filtered.map(c => (
                      <tr key={c.id} className="hover:bg-slate-50 transition-colors">
                        <td className="px-4 py-3 font-mono text-xs text-slate-500">{c.externalClaimId ?? `#${c.id}`}</td>
                        <td className="px-4 py-3 font-medium text-slate-900">{c.employeeName}</td>
                        <td className="px-4 py-3 text-slate-500">#{c.refillRequestId}</td>
                        <td className="px-4 py-3 font-semibold text-slate-900">{c.amount} {c.currency}</td>
                        <td className="px-4 py-3"><StatusBadge status={c.status} /></td>
                        <td className="px-4 py-3 text-xs text-slate-400">{c.submittedAt ? new Date(c.submittedAt).toLocaleDateString() : '—'}</td>
                        <td className="px-4 py-3 text-xs text-slate-400">{c.approvedAt ? new Date(c.approvedAt).toLocaleDateString() : '—'}</td>
                        <td className="px-4 py-3 text-xs text-slate-400">{c.paidAt ? new Date(c.paidAt).toLocaleDateString() : '—'}</td>
                        <td className="px-4 py-3">
                          <div className="flex items-center gap-2">
                            <button onClick={() => setDetailClaim(c)} className="p-1.5 rounded-lg text-slate-400 hover:bg-indigo-50 hover:text-indigo-600 transition-colors" title="Details">
                              <Eye size={14} />
                            </button>
                            {c.status === 'SubmissionFailed' && (
                              <button onClick={() => retryMutation.mutate(c.id)} className="p-1.5 rounded-lg text-slate-400 hover:bg-amber-50 hover:text-amber-600 transition-colors" title="Retry">
                                <RefreshCw size={14} />
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
        </>
      )}

      {activeTab === 'hr' && (
        <div>
          <div className="bg-amber-50 border border-amber-200 rounded-xl p-3 mb-5 text-xs text-amber-800">
            This is the simulated HR system. In production, HR approval would happen in an external payroll system. These actions are for demo purposes only.
          </div>
          {loadingHr ? <LoadingSpinner /> : !hrClaims?.length ? (
            <EmptyState message="No HR claims yet. Submit a reimbursement claim first." icon={<CreditCard size={40} />} />
          ) : (
            <div className="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden">
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead className="bg-slate-50 border-b border-slate-200">
                    <tr>
                      {['External ID', 'Employee', 'Amount', 'Status', 'Submitted', 'Actions'].map(h => (
                        <th key={h} className="px-4 py-3 text-left text-xs font-semibold text-slate-500 uppercase tracking-wide whitespace-nowrap">{h}</th>
                      ))}
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-50">
                    {hrClaims.map(c => (
                      <tr key={c.externalClaimId} className="hover:bg-slate-50 transition-colors">
                        <td className="px-4 py-3 font-mono text-xs text-slate-500">{c.externalClaimId}</td>
                        <td className="px-4 py-3 font-medium text-slate-900">{c.employeeName}</td>
                        <td className="px-4 py-3 font-semibold">{c.amount} {c.currency}</td>
                        <td className="px-4 py-3"><StatusBadge status={c.status} /></td>
                        <td className="px-4 py-3 text-xs text-slate-400">{new Date(c.submittedAt).toLocaleDateString()}</td>
                        <td className="px-4 py-3">
                          <div className="flex items-center gap-2">
                            {c.status === 'Submitted' && (
                              <>
                                <button onClick={() => approveMutation.mutate(c.externalClaimId)}
                                  className="flex items-center gap-1 px-2 py-1 text-xs text-white bg-emerald-600 rounded-lg hover:bg-emerald-700 transition-colors">
                                  <CheckCircle size={11} /> Approve
                                </button>
                                <button onClick={() => setRejectTarget(c)}
                                  className="flex items-center gap-1 px-2 py-1 text-xs text-white bg-red-500 rounded-lg hover:bg-red-600 transition-colors">
                                  <XCircle size={11} /> Reject
                                </button>
                              </>
                            )}
                            {c.status === 'Approved' && (
                              <button onClick={() => paidMutation.mutate(c.externalClaimId)}
                                className="flex items-center gap-1 px-2 py-1 text-xs text-white bg-indigo-600 rounded-lg hover:bg-indigo-700 transition-colors">
                                <DollarSign size={11} /> Mark Paid
                              </button>
                            )}
                            {c.status === 'Paid' && <span className="text-xs text-emerald-600 font-medium">✓ Complete</span>}
                            {c.status === 'Rejected' && <span className="text-xs text-red-500">{c.rejectionReason ?? 'Rejected'}</span>}
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          )}
        </div>
      )}

      <Modal open={!!detailClaim} title={`Claim ${detailClaim?.externalClaimId ?? `#${detailClaim?.id}`}`} onClose={() => setDetailClaim(null)}>
        {detailClaim && <ClaimDetail claim={detailClaim} />}
      </Modal>

      <ConfirmDialog
        open={!!rejectTarget}
        title="Reject HR Claim"
        message={`Reject claim ${rejectTarget?.externalClaimId} for ${rejectTarget?.employeeName}?`}
        confirmLabel="Reject Claim"
        danger
        onConfirm={() => rejectMutation.mutate({ externalId: rejectTarget!.externalClaimId, reason: rejectReason || undefined })}
        onCancel={() => { setRejectTarget(null); setRejectReason('') }}
      >
        <input value={rejectReason} onChange={e => setRejectReason(e.target.value)}
          placeholder="Rejection reason (optional)"
          className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-300" />
      </ConfirmDialog>
    </div>
  )
}
