export function LoadingSpinner({ message = 'Loading...' }: { message?: string }) {
  return (
    <div className="flex items-center justify-center py-16">
      <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-indigo-600 mr-3" />
      <span className="text-slate-500 text-sm">{message}</span>
    </div>
  )
}

export function ErrorMessage({ message }: { message: string }) {
  return (
    <div className="rounded-xl bg-red-50 border border-red-200 p-4 text-red-700 text-sm">
      <strong className="font-semibold">Error: </strong>{message}
    </div>
  )
}

export function EmptyState({ message, icon }: { message: string; icon?: React.ReactNode }) {
  return (
    <div className="text-center py-16 text-slate-400">
      {icon && <div className="flex justify-center mb-3 opacity-40">{icon}</div>}
      <p className="text-sm">{message}</p>
    </div>
  )
}

export function SkeletonRow({ cols = 5 }: { cols?: number }) {
  return (
    <tr>
      {Array.from({ length: cols }).map((_, i) => (
        <td key={i} className="px-4 py-3">
          <div className="h-4 bg-slate-200 rounded animate-pulse" style={{ width: `${60 + (i * 13) % 40}%` }} />
        </td>
      ))}
    </tr>
  )
}

export function CardSkeleton() {
  return (
    <div className="bg-white rounded-xl border border-slate-200 p-5 animate-pulse">
      <div className="h-3 bg-slate-200 rounded w-1/3 mb-3" />
      <div className="h-7 bg-slate-200 rounded w-1/2 mb-2" />
      <div className="h-3 bg-slate-200 rounded w-2/3" />
    </div>
  )
}

export function PageHeader({ title, subtitle, actions }: { title: string; subtitle?: string; actions?: React.ReactNode }) {
  return (
    <div className="flex items-start justify-between mb-6 flex-wrap gap-3">
      <div>
        <h1 className="text-xl font-bold text-slate-900">{title}</h1>
        {subtitle && <p className="text-sm text-slate-500 mt-0.5">{subtitle}</p>}
      </div>
      {actions && <div className="flex items-center gap-2 flex-wrap">{actions}</div>}
    </div>
  )
}
