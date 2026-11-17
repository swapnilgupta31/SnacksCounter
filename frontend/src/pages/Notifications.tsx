import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { Bell, CheckCheck, Filter } from 'lucide-react'
import { getNotifications, markNotificationRead } from '../api/endpoints'
import { LoadingSpinner, ErrorMessage, EmptyState, PageHeader } from '../components/Feedback'

const TYPE_COLORS: Record<string, string> = {
  AssignmentOffer: 'bg-blue-100 text-blue-700',
  OfferAccepted: 'bg-emerald-100 text-emerald-700',
  OfferDeclined: 'bg-slate-100 text-slate-600',
  OfferExpired: 'bg-amber-100 text-amber-700',
  PurchaseCompleted: 'bg-indigo-100 text-indigo-700',
  ClaimSubmitted: 'bg-purple-100 text-purple-700',
  ClaimApproved: 'bg-teal-100 text-teal-700',
  ClaimPaid: 'bg-emerald-100 text-emerald-700',
  ClaimRejected: 'bg-red-100 text-red-700',
  ClaimSubmissionFailed: 'bg-red-100 text-red-700',
  BudgetWarning: 'bg-amber-100 text-amber-700',
  NewRefillRequest: 'bg-blue-100 text-blue-700',
  NoVolunteerAvailable: 'bg-red-100 text-red-700',
}

export default function Notifications() {
  const qc = useQueryClient()
  const [typeFilter, setTypeFilter] = useState('')
  const [showUnreadOnly, setShowUnreadOnly] = useState(false)

  const { data: notifications, isLoading, error } = useQuery({
    queryKey: ['notifications'],
    queryFn: getNotifications,
    refetchInterval: 15_000,
  })

  const markReadMutation = useMutation({
    mutationFn: (id: number) => markNotificationRead(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['notifications'] }),
    onError: () => toast.error('Failed to mark as read'),
  })

  const types = [...new Set(notifications?.map(n => n.type) ?? [])]

  const filtered = notifications?.filter(n => {
    if (typeFilter && n.type !== typeFilter) return false
    if (showUnreadOnly && n.isRead) return false
    return true
  }) ?? []

  const unreadCount = notifications?.filter(n => !n.isRead).length ?? 0

  return (
    <div>
      <PageHeader
        title="Notifications"
        subtitle={`${unreadCount} unread notification${unreadCount !== 1 ? 's' : ''}`}
      />

      {/* Filters */}
      <div className="flex flex-wrap gap-3 mb-5">
        <select value={typeFilter} onChange={e => setTypeFilter(e.target.value)}
          className="border border-slate-200 rounded-lg px-3 py-2 text-sm bg-white focus:outline-none focus:ring-2 focus:ring-indigo-300">
          <option value="">All Types</option>
          {types.map(t => <option key={t} value={t}>{t}</option>)}
        </select>
        <label className="flex items-center gap-2 text-sm text-slate-600 cursor-pointer">
          <input type="checkbox" checked={showUnreadOnly} onChange={e => setShowUnreadOnly(e.target.checked)}
            className="rounded border-slate-300 text-indigo-600 focus:ring-indigo-300" />
          Unread only
        </label>
      </div>

      {error ? <ErrorMessage message="Failed to load notifications" /> :
      isLoading ? <LoadingSpinner /> :
      filtered.length === 0 ? (
        <EmptyState message="No notifications" icon={<Bell size={40} />} />
      ) : (
        <div className="space-y-2">
          {filtered.map(n => (
            <div key={n.id} className={`bg-white rounded-xl border shadow-sm p-4 flex items-start gap-4 transition-colors ${
              !n.isRead ? 'border-indigo-200 bg-indigo-50/30' : 'border-slate-200'
            }`}>
              <div className="flex-shrink-0 mt-0.5">
                <div className={`w-2 h-2 rounded-full mt-1.5 ${!n.isRead ? 'bg-indigo-500' : 'bg-transparent'}`} />
              </div>
              <div className="flex-1 min-w-0">
                <div className="flex items-center gap-2 mb-1 flex-wrap">
                  <span className={`px-2 py-0.5 rounded-full text-xs font-medium ${TYPE_COLORS[n.type] ?? 'bg-slate-100 text-slate-600'}`}>
                    {n.type}
                  </span>
                  {n.recipientName && (
                    <span className="text-xs text-slate-500">→ {n.recipientName}</span>
                  )}
                  {n.refillRequestId && (
                    <span className="text-xs text-slate-400">Refill #{n.refillRequestId}</span>
                  )}
                </div>
                <p className="text-sm text-slate-700">{n.message}</p>
                <p className="text-xs text-slate-400 mt-1">{new Date(n.sentAt).toLocaleString()}</p>
              </div>
              {!n.isRead && (
                <button
                  onClick={() => markReadMutation.mutate(n.id)}
                  className="flex-shrink-0 p-1.5 rounded-lg text-slate-400 hover:bg-indigo-100 hover:text-indigo-600 transition-colors"
                  title="Mark as read"
                >
                  <CheckCheck size={14} />
                </button>
              )}
            </div>
          ))}
        </div>
      )}
    </div>
  )
}
