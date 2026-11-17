import { Link } from 'react-router-dom'
import { Home, AlertCircle } from 'lucide-react'

export default function NotFound() {
  return (
    <div className="flex flex-col items-center justify-center py-24 text-center">
      <div className="w-16 h-16 bg-slate-100 rounded-2xl flex items-center justify-center mb-4">
        <AlertCircle size={28} className="text-slate-400" />
      </div>
      <h1 className="text-2xl font-bold text-slate-900 mb-2">Page Not Found</h1>
      <p className="text-slate-500 text-sm mb-6">The page you're looking for doesn't exist.</p>
      <Link to="/" className="flex items-center gap-2 px-4 py-2 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 transition-colors text-sm font-medium">
        <Home size={14} /> Back to Dashboard
      </Link>
    </div>
  )
}
