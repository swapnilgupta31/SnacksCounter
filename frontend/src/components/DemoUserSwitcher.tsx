import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { getAllEmployees } from '../api/endpoints'
import { getDemoUserId, setDemoUserId } from '../api/client'
import { User } from 'lucide-react'

export default function DemoUserSwitcher() {
  const [userId, setUserId] = useState(getDemoUserId())
  const { data: employees } = useQuery({ queryKey: ['employees'], queryFn: getAllEmployees })

  const current = employees?.find(e => e.id === userId)

  const handleChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    const id = parseInt(e.target.value, 10)
    setDemoUserId(id)
    setUserId(id)
    window.location.reload()
  }

  return (
    <div className="flex items-center gap-2 bg-yellow-50 border border-yellow-200 rounded-lg px-3 py-1.5">
      <User size={14} className="text-yellow-600" />
      <span className="text-xs text-yellow-700 font-medium">Demo User:</span>
      <select
        value={userId}
        onChange={handleChange}
        className="text-xs border-0 bg-transparent text-yellow-800 font-semibold focus:ring-0 cursor-pointer"
      >
        {employees?.map(e => (
          <option key={e.id} value={e.id}>
            {e.name} ({e.role})
          </option>
        ))}
      </select>
      {current && (
        <span className="text-xs text-yellow-600">
          @ {current.currentOfficeName}
        </span>
      )}
    </div>
  )
}
