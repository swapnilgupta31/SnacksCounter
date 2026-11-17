interface BadgeProps {
  status: string
  size?: 'sm' | 'md'
}

const statusColors: Record<string, string> = {
  Available: 'bg-green-100 text-green-800',
  Empty: 'bg-red-100 text-red-800',
  RefillPending: 'bg-yellow-100 text-yellow-800',
  Requested: 'bg-gray-100 text-gray-700',
  Offered: 'bg-blue-100 text-blue-800',
  Assigned: 'bg-indigo-100 text-indigo-800',
  Purchased: 'bg-purple-100 text-purple-800',
  Refilled: 'bg-teal-100 text-teal-800',
  ClaimSubmitted: 'bg-orange-100 text-orange-800',
  Reimbursed: 'bg-green-100 text-green-800',
  Cancelled: 'bg-gray-100 text-gray-500',
  PendingSubmission: 'bg-gray-100 text-gray-700',
  Submitted: 'bg-blue-100 text-blue-800',
  Approved: 'bg-teal-100 text-teal-800',
  Rejected: 'bg-red-100 text-red-800',
  Paid: 'bg-green-100 text-green-800',
  SubmissionFailed: 'bg-red-100 text-red-800',
}

export default function StatusBadge({ status, size = 'sm' }: BadgeProps) {
  const color = statusColors[status] || 'bg-gray-100 text-gray-700'
  const padding = size === 'sm' ? 'px-2 py-0.5 text-xs' : 'px-3 py-1 text-sm'
  return (
    <span className={`inline-flex items-center rounded-full font-medium ${padding} ${color}`}>
      {status}
    </span>
  )
}
