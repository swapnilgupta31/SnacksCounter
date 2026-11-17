import { Routes, Route } from 'react-router-dom'
import AppLayout from './layouts/AppLayout'
import Dashboard from './pages/Dashboard'
import SnackCounters from './pages/SnackCounters'
import RefillRequests from './pages/RefillRequests'
import MyAssignments from './pages/MyAssignments'
import Volunteers from './pages/Volunteers'
import Budgets from './pages/Budgets'
import Reimbursements from './pages/Reimbursements'
import Reports from './pages/Reports'
import Notifications from './pages/Notifications'
import WorkflowDemo from './pages/WorkflowDemo'
import NotFound from './pages/NotFound'

export default function App() {
  return (
    <Routes>
      <Route path="/" element={<AppLayout />}>
        <Route index element={<Dashboard />} />
        <Route path="snack-counters" element={<SnackCounters />} />
        <Route path="refills" element={<RefillRequests />} />
        <Route path="my-assignments" element={<MyAssignments />} />
        <Route path="volunteers" element={<Volunteers />} />
        <Route path="budgets" element={<Budgets />} />
        <Route path="reimbursements" element={<Reimbursements />} />
        <Route path="reports" element={<Reports />} />
        <Route path="notifications" element={<Notifications />} />
        <Route path="demo" element={<WorkflowDemo />} />
        <Route path="*" element={<NotFound />} />
      </Route>
    </Routes>
  )
}
