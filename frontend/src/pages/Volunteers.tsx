import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { Users, CheckCircle, XCircle, Info } from 'lucide-react'
import { getVolunteers, getOffices, updateAvailability, getAllEmployees } from '../api/endpoints'
import { LoadingSpinner, ErrorMessage, EmptyState, PageHeader, SkeletonRow } from '../components/Feedback'
import type { Employee } from '../types'

function AvailBadge({ ok }: { ok: boolean }) {
  return ok
    ? <CheckCircle size={14} className="text-emerald-500" />
    : <XCircle size={14} className="text-red-400" />
}

export default function Volunteers() {
  const qc = useQueryClient()
  const [officeFilter, setOfficeFilter] = useState('')
  const [showAll, setShowAll] = useState(false)
  const [editId, setEditId] = useState<number | null>(null)
  const [editState, setEditState] = useState({ isAvailable: true, isOnLeave: false, isPresentToday: true })

  const { data: offices } = useQuery({ queryKey: ['offices'], queryFn: getOffices })
  const { data: volunteers, isLoading: loadingVols } = useQuery({
    queryKey: ['volunteers', officeFilter],
    queryFn: () => getVolunteers(officeFilter ? parseInt(officeFilter) : undefined),
  })
  const { data: allEmployees, isLoading: loadingAll } = useQuery({
    queryKey: ['employees'],
    queryFn: getAllEmployees,
    enabled: showAll,
  })

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: number; data: typeof editState }) => updateAvailability(id, data),
    onSuccess: () => {
      toast.success('Availability updated')
      qc.invalidateQueries({ queryKey: ['volunteers'] })
      qc.invalidateQueries({ queryKey: ['employees'] })
      setEditId(null)
    },
    onError: () => toast.error('Failed to update availability'),
  })

  const isLoading = showAll ? loadingAll : loadingVols
  const people: Employee[] = (showAll ? allEmployees : volunteers) ?? []

  const filtered = officeFilter
    ? people.filter(e => e.currentOfficeId === parseInt(officeFilter))
    : people

  return (
    <div>
      <PageHeader
        title="Volunteers"
        subtitle="Manage volunteer availability and view assignments"
      />

      {/* Info panel */}
      <div className="bg-indigo-50 border border-indigo-200 rounded-xl p-4 mb-6 text-sm text-indigo-800">
        <div className="flex items-start gap-2">
          <Info size={16} className="flex-shrink-0 mt-0.5" />
          <div>
            <p className="font-semibold mb-1">Volunteer Selection Algorithm</p>
            <ul className="space-y-0.5 text-xs text-indigo-700 list-disc list-inside">
              <li>Must be marked as a volunteer, available, not on leave, and present today</li>
              <li>Must not already be assigned to an active refill at the same office</li>
              <li>Visiting employees may volunteer at their current office</li>
              <li>Selection prioritizes fewest refills in the last 30 days, then oldest last-assigned date</li>
              <li>Tie-breaking uses lowest employee ID for determinism</li>
            </ul>
          </div>
        </div>
      </div>

      {/* Filters */}
      <div className="flex flex-wrap gap-3 mb-5">
        <select value={officeFilter} onChange={e => setOfficeFilter(e.target.value)}
          className="border border-slate-200 rounded-lg px-3 py-2 text-sm bg-white focus:outline-none focus:ring-2 focus:ring-indigo-300">
          <option value="">All Offices</option>
          {offices?.map(o => <option key={o.id} value={o.id}>{o.name}</option>)}
        </select>
        <label className="flex items-center gap-2 text-sm text-slate-600 cursor-pointer">
          <input type="checkbox" checked={showAll} onChange={e => setShowAll(e.target.checked)}
            className="rounded border-slate-300 text-indigo-600 focus:ring-indigo-300" />
          Show all employees
        </label>
      </div>

      {isLoading ? <LoadingSpinner /> : filtered.length === 0 ? (
        <EmptyState message="No volunteers found" icon={<Users size={40} />} />
      ) : (
        <div className="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden">
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead className="bg-slate-50 border-b border-slate-200">
                <tr>
                  {['Name', 'Role', 'Home Office', 'Current Office', 'Available', 'On Leave', 'Present', 'Volunteer', 'Actions'].map(h => (
                    <th key={h} className="px-4 py-3 text-left text-xs font-semibold text-slate-500 uppercase tracking-wide whitespace-nowrap">{h}</th>
                  ))}
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-50">
                {filtered.map(e => (
                  <tr key={e.id} className="hover:bg-slate-50 transition-colors">
                    <td className="px-4 py-3">
                      <div className="flex items-center gap-2">
                        <div className="w-7 h-7 bg-indigo-100 rounded-full flex items-center justify-center flex-shrink-0">
                          <span className="text-indigo-700 text-xs font-bold">{e.name.charAt(0)}</span>
                        </div>
                        <div>
                          <p className="font-medium text-slate-900">{e.name}</p>
                          <p className="text-xs text-slate-400">{e.email}</p>
                        </div>
                      </div>
                    </td>
                    <td className="px-4 py-3">
                      <span className={`px-2 py-0.5 rounded-full text-xs font-medium ${
                        e.role === 'Admin' ? 'bg-purple-100 text-purple-700' :
                        e.role === 'Volunteer' ? 'bg-indigo-100 text-indigo-700' :
                        'bg-slate-100 text-slate-600'
                      }`}>{e.role}</span>
                    </td>
                    <td className="px-4 py-3 text-slate-600 whitespace-nowrap">{e.homeOfficeName}</td>
                    <td className="px-4 py-3 whitespace-nowrap">
                      <span className={e.currentOfficeName !== e.homeOfficeName ? 'text-amber-600 font-medium' : 'text-slate-600'}>
                        {e.currentOfficeName}
                        {e.currentOfficeName !== e.homeOfficeName && ' (visiting)'}
                      </span>
                    </td>
                    <td className="px-4 py-3"><AvailBadge ok={e.isAvailable} /></td>
                    <td className="px-4 py-3">
                      {e.isOnLeave ? <span className="text-xs text-amber-600 font-medium">On Leave</span> : <AvailBadge ok={!e.isOnLeave} />}
                    </td>
                    <td className="px-4 py-3"><AvailBadge ok={e.isPresentToday} /></td>
                    <td className="px-4 py-3">
                      {e.isVolunteer ? <CheckCircle size={14} className="text-emerald-500" /> : <span className="text-xs text-slate-400">—</span>}
                    </td>
                    <td className="px-4 py-3">
                      {editId === e.id ? (
                        <div className="flex items-center gap-2">
                          <label className="flex items-center gap-1 text-xs cursor-pointer">
                            <input type="checkbox" checked={editState.isAvailable} onChange={ev => setEditState(s => ({ ...s, isAvailable: ev.target.checked }))} className="rounded" />
                            Avail
                          </label>
                          <label className="flex items-center gap-1 text-xs cursor-pointer">
                            <input type="checkbox" checked={editState.isOnLeave} onChange={ev => setEditState(s => ({ ...s, isOnLeave: ev.target.checked }))} className="rounded" />
                            Leave
                          </label>
                          <label className="flex items-center gap-1 text-xs cursor-pointer">
                            <input type="checkbox" checked={editState.isPresentToday} onChange={ev => setEditState(s => ({ ...s, isPresentToday: ev.target.checked }))} className="rounded" />
                            Present
                          </label>
                          <button onClick={() => updateMutation.mutate({ id: e.id, data: editState })}
                            className="text-xs text-white bg-indigo-600 px-2 py-1 rounded hover:bg-indigo-700">Save</button>
                          <button onClick={() => setEditId(null)} className="text-xs text-slate-500 hover:text-slate-700">✕</button>
                        </div>
                      ) : (
                        <button
                          onClick={() => { setEditId(e.id); setEditState({ isAvailable: e.isAvailable, isOnLeave: e.isOnLeave, isPresentToday: e.isPresentToday }) }}
                          className="text-xs text-indigo-600 hover:text-indigo-800 font-medium"
                        >
                          Edit
                        </button>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </div>
  )
}
