import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import {
  AlertCircle, RefreshCw, UserCheck, ShoppingCart,
  Package, FileText, CheckCircle, DollarSign, ArrowRight, PlayCircle
} from 'lucide-react'
import { getRefills, getReimbursements, getSnackBoxes } from '../api/endpoints'
import { LoadingSpinner, PageHeader } from '../components/Feedback'
import StatusBadge from '../components/StatusBadge'

const WORKFLOW_STEPS = [
  {
    id: 1, icon: AlertCircle, color: 'bg-red-500', label: 'Empty Snack Box',
    description: 'An employee notices a snack box is empty and reports it via the Snack Counters page.',
    action: 'Report Empty', link: '/snack-counters', statuses: ['Empty'],
  },
  {
    id: 2, icon: RefreshCw, color: 'bg-blue-500', label: 'Refill Requested',
    description: 'A refill request is created with an estimated cost. The system immediately searches for an eligible volunteer.',
    action: 'View Requests', link: '/refills', statuses: ['Requested'],
  },
  {
    id: 3, icon: UserCheck, color: 'bg-amber-500', label: 'Volunteer Offered',
    description: 'The system selects the most eligible volunteer (fewest recent refills, present today, not on leave) and sends an offer. The volunteer has 2 hours to respond.',
    action: 'My Assignments', link: '/my-assignments', statuses: ['Offered'],
  },
  {
    id: 4, icon: CheckCircle, color: 'bg-indigo-500', label: 'Assignment Accepted',
    description: 'The volunteer accepts the offer. Budget is reserved atomically. A 24-hour deadline is set. If declined, the next eligible volunteer is offered.',
    action: 'Accept Offer', link: '/my-assignments', statuses: ['Assigned'],
  },
  {
    id: 5, icon: ShoppingCart, color: 'bg-purple-500', label: 'Purchase Recorded',
    description: 'The volunteer buys the snacks and records the actual amount with a receipt reference. If over budget, it is flagged but not blocked.',
    action: 'Record Purchase', link: '/my-assignments', statuses: ['Purchased'],
  },
  {
    id: 6, icon: Package, color: 'bg-teal-500', label: 'Snack Box Refilled',
    description: 'The volunteer confirms the snacks have been placed in the counter. The snack box status returns to Available.',
    action: 'Confirm Refilled', link: '/my-assignments', statuses: ['Refilled'],
  },
  {
    id: 7, icon: FileText, color: 'bg-orange-500', label: 'HR Claim Submitted',
    description: 'A reimbursement claim is submitted to the HR system with idempotency protection. The claim gets an external HR ID.',
    action: 'Submit Claim', link: '/my-assignments', statuses: ['ClaimSubmitted'],
  },
  {
    id: 8, icon: CheckCircle, color: 'bg-emerald-500', label: 'HR Approved',
    description: 'The HR system (simulated) approves the claim. In production this would be an external payroll system.',
    action: 'HR System', link: '/reimbursements', statuses: ['Approved'],
  },
  {
    id: 9, icon: DollarSign, color: 'bg-green-600', label: 'HR Paid',
    description: 'HR marks the claim as paid. The reimbursement claim status becomes Paid.',
    action: 'Mark Paid', link: '/reimbursements', statuses: ['Paid'],
  },
  {
    id: 10, icon: CheckCircle, color: 'bg-slate-700', label: 'Reimbursed',
    description: 'The refill request reaches its final state: Reimbursed. The full lifecycle is complete.',
    action: 'View Reports', link: '/reports', statuses: ['Reimbursed'],
  },
]

export default function WorkflowDemo() {
  const { data: refills, isLoading: loadingRefills } = useQuery({ queryKey: ['refills'], queryFn: () => getRefills() })
  const { data: boxes } = useQuery({ queryKey: ['snack-boxes'], queryFn: () => getSnackBoxes() })
  const { data: claims } = useQuery({ queryKey: ['reimbursements'], queryFn: getReimbursements })

  const getStepData = (step: typeof WORKFLOW_STEPS[0]) => {
    if (step.statuses.includes('Empty')) {
      const empty = boxes?.filter(b => b.availabilityStatus === 'Empty') ?? []
      return { count: empty.length, items: empty.slice(0, 3).map(b => `${b.name} (${b.officeName})`) }
    }
    if (step.statuses.includes('Approved') || step.statuses.includes('Paid')) {
      const matching = claims?.filter(c => step.statuses.includes(c.status)) ?? []
      return { count: matching.length, items: matching.slice(0, 3).map(c => `${c.employeeName} — ${c.amount} ${c.currency}`) }
    }
    const matching = refills?.filter(r => step.statuses.includes(r.status)) ?? []
    return { count: matching.length, items: matching.slice(0, 3).map(r => `#${r.id} ${r.snackBoxName} (${r.officeName})`) }
  }

  return (
    <div>
      <PageHeader
        title="Workflow Demo"
        subtitle="Complete end-to-end snack refill lifecycle — ~5 minute walkthrough"
      />

      <div className="bg-indigo-50 border border-indigo-200 rounded-xl p-4 mb-8 text-sm text-indigo-800">
        <div className="flex items-start gap-2">
          <PlayCircle size={16} className="flex-shrink-0 mt-0.5" />
          <div>
            <p className="font-semibold mb-1">How to use this demo</p>
            <ol className="list-decimal list-inside space-y-0.5 text-xs text-indigo-700">
              <li>Use the <strong>Demo User</strong> selector in the top bar to switch between employees</li>
              <li>Follow each step below — click the action link to navigate to the relevant page</li>
              <li>Live counts show real backend data for each workflow stage</li>
              <li>All actions call real backend APIs — no mock data</li>
            </ol>
          </div>
        </div>
      </div>

      {loadingRefills ? <LoadingSpinner /> : (
        <div className="relative">
          {/* Vertical line */}
          <div className="absolute left-6 top-8 bottom-8 w-0.5 bg-slate-200 hidden lg:block" />

          <div className="space-y-4">
            {WORKFLOW_STEPS.map((step, idx) => {
              const { count, items } = getStepData(step)
              const Icon = step.icon
              return (
                <div key={step.id} className="relative flex gap-4 lg:gap-6">
                  {/* Step circle */}
                  <div className={`flex-shrink-0 w-12 h-12 rounded-full ${step.color} flex items-center justify-center shadow-sm z-10`}>
                    <Icon size={18} className="text-white" />
                  </div>

                  {/* Content */}
                  <div className="flex-1 bg-white rounded-xl border border-slate-200 shadow-sm p-5 mb-2">
                    <div className="flex items-start justify-between gap-3 flex-wrap">
                      <div className="flex-1">
                        <div className="flex items-center gap-2 mb-1">
                          <span className="text-xs font-bold text-slate-400">STEP {step.id}</span>
                          <span className="text-xs text-slate-300">·</span>
                          <h3 className="font-bold text-slate-900 text-sm">{step.label}</h3>
                        </div>
                        <p className="text-sm text-slate-600 mb-3">{step.description}</p>

                        {/* Live data */}
                        <div className="flex items-center gap-2 flex-wrap">
                          <span className={`px-2.5 py-1 rounded-full text-xs font-bold ${count > 0 ? 'bg-indigo-100 text-indigo-700' : 'bg-slate-100 text-slate-500'}`}>
                            {count} currently in this state
                          </span>
                          {items.map((item, i) => (
                            <span key={i} className="text-xs text-slate-500 bg-slate-50 px-2 py-0.5 rounded-full border border-slate-200">
                              {item}
                            </span>
                          ))}
                        </div>
                      </div>

                      <Link
                        to={step.link}
                        className="flex items-center gap-1.5 px-3 py-2 text-xs font-medium text-indigo-600 border border-indigo-200 rounded-lg hover:bg-indigo-50 transition-colors flex-shrink-0"
                      >
                        {step.action} <ArrowRight size={12} />
                      </Link>
                    </div>
                  </div>
                </div>
              )
            })}
          </div>
        </div>
      )}

      {/* Quick scenario guide */}
      <div className="mt-8 bg-slate-900 rounded-2xl p-6 text-white">
        <h3 className="font-bold mb-4 flex items-center gap-2">
          <PlayCircle size={16} className="text-indigo-400" />
          Quick Demo Scenario (Bengaluru)
        </h3>
        <ol className="space-y-2 text-sm text-slate-300 list-decimal list-inside">
          <li>Switch demo user to <strong className="text-white">Divya Bhat (Admin)</strong> — Bengaluru</li>
          <li>Go to <Link to="/snack-counters" className="text-indigo-400 hover:text-indigo-300">Snack Counters</Link> → find an empty box → click <strong className="text-white">Request Refill</strong></li>
          <li>Enter estimated cost (e.g. 800 INR) → Submit</li>
          <li>Go to <Link to="/refills" className="text-indigo-400 hover:text-indigo-300">Refill Requests</Link> → verify the new request appears with status <strong className="text-white">Offered</strong></li>
          <li>Switch demo user to the <strong className="text-white">assigned volunteer</strong> shown in the request</li>
          <li>Go to <Link to="/my-assignments" className="text-indigo-400 hover:text-indigo-300">My Assignments</Link> → Accept the offer</li>
          <li>Record a purchase: amount 750 INR, receipt RCPT-DEMO-001, description "Snack refill"</li>
          <li>Confirm the snack box is refilled</li>
          <li>Submit the reimbursement claim</li>
          <li>Go to <Link to="/reimbursements" className="text-indigo-400 hover:text-indigo-300">Reimbursements</Link> → HR System tab → Approve → Mark Paid</li>
          <li>Go to <Link to="/budgets" className="text-indigo-400 hover:text-indigo-300">Budgets</Link> → verify Bengaluru spending updated</li>
          <li>Go to <Link to="/reports" className="text-indigo-400 hover:text-indigo-300">Reports</Link> → verify purchase in monthly report</li>
        </ol>
      </div>
    </div>
  )
}
