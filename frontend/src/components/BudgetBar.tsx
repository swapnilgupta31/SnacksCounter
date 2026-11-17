interface BudgetBarProps {
  spent: number
  reserved: number
  total: number
  currency: string
}

export default function BudgetBar({ spent, reserved, total, currency }: BudgetBarProps) {
  const spentPct = Math.min(100, (spent / total) * 100)
  const reservedPct = Math.min(100 - spentPct, (reserved / total) * 100)
  const usedPct = spentPct + reservedPct
  const color = usedPct > 90 ? 'bg-red-500' : usedPct > 70 ? 'bg-amber-500' : 'bg-indigo-500'

  return (
    <div>
      <div className="flex justify-between text-xs text-slate-500 mb-1">
        <span>{Math.round(usedPct)}% utilized</span>
        <span>{total.toLocaleString()} {currency} limit</span>
      </div>
      <div className="w-full bg-slate-100 rounded-full h-2.5 overflow-hidden">
        <div className="h-full flex">
          <div className={`${color} transition-all`} style={{ width: `${spentPct}%` }} />
          <div className="bg-amber-300 transition-all" style={{ width: `${reservedPct}%` }} />
        </div>
      </div>
      <div className="flex gap-4 mt-1.5 text-xs text-slate-500">
        <span className="flex items-center gap-1"><span className="w-2 h-2 rounded-full bg-indigo-500 inline-block" />Spent</span>
        <span className="flex items-center gap-1"><span className="w-2 h-2 rounded-full bg-amber-300 inline-block" />Reserved</span>
        <span className="flex items-center gap-1"><span className="w-2 h-2 rounded-full bg-slate-200 inline-block" />Available</span>
      </div>
    </div>
  )
}
