import api from './client'
import type {
  Office, OfficeBudget, Employee, SnackBox, RefillRequest,
  ReimbursementClaim, Notification, MonthlyReport, SnackConsumption, HrClaim
} from '../types'

// Offices
export const getOffices = () => api.get<Office[]>('/offices').then(r => r.data)
export const getOfficeBudget = (id: number, year?: number, month?: number) =>
  api.get<OfficeBudget>(`/offices/${id}/budget`, { params: { year, month } }).then(r => r.data)

// Employees / Volunteers
export const getVolunteers = (officeId?: number) =>
  api.get<Employee[]>('/volunteers', { params: { officeId } }).then(r => r.data)
export const getAllEmployees = () =>
  api.get<Employee[]>('/volunteers/employees').then(r => r.data)
export const getVolunteerAssignments = (id: number) =>
  api.get<RefillRequest[]>(`/volunteers/${id}/assignments`).then(r => r.data)
export const updateAvailability = (id: number, data: { isAvailable: boolean; isOnLeave: boolean; isPresentToday: boolean }) =>
  api.patch<Employee>(`/volunteers/${id}/availability`, data).then(r => r.data)

// Snack Boxes
export const getSnackBoxes = (officeId?: number) =>
  api.get<SnackBox[]>('/snack-boxes', { params: { officeId } }).then(r => r.data)
export const reportEmpty = (id: number, estimatedCost: number, notes?: string) =>
  api.post<RefillRequest>(`/snack-boxes/${id}/report-empty`, { estimatedCost, notes }).then(r => r.data)

// Refills
export const getRefills = (status?: string, officeId?: number) =>
  api.get<RefillRequest[]>('/refills', { params: { status, officeId } }).then(r => r.data)
export const getRefill = (id: number) =>
  api.get<RefillRequest>(`/refills/${id}`).then(r => r.data)
export const acceptOffer = (id: number) =>
  api.post<RefillRequest>(`/refills/${id}/accept`).then(r => r.data)
export const declineOffer = (id: number) =>
  api.post<RefillRequest>(`/refills/${id}/decline`).then(r => r.data)
export const submitPurchase = (id: number, amount: number, receiptReference: string, description: string) =>
  api.post<RefillRequest>(`/refills/${id}/purchase`, { amount, receiptReference, description }).then(r => r.data)
export const confirmRefilled = (id: number) =>
  api.post<RefillRequest>(`/refills/${id}/confirm-refilled`).then(r => r.data)
export const cancelRefill = (id: number, reason: string) =>
  api.post<RefillRequest>(`/refills/${id}/cancel`, { reason }).then(r => r.data)
export const submitClaim = (id: number) =>
  api.post<ReimbursementClaim>(`/refills/${id}/submit-claim`).then(r => r.data)
export const processExpired = () =>
  api.post<{ processed: number }>('/refills/process-expired').then(r => r.data)

// Reimbursements
export const getReimbursements = () =>
  api.get<ReimbursementClaim[]>('/reimbursements').then(r => r.data)
export const retrySubmission = (id: number) =>
  api.post<ReimbursementClaim>(`/reimbursements/${id}/retry`).then(r => r.data)

// HR Demo
export const getHrClaims = () =>
  api.get<HrClaim[]>('/demo/hr/claims').then(r => r.data)
export const approveHrClaim = (externalId: string) =>
  api.post(`/demo/hr/claims/${externalId}/approve`).then(r => r.data)
export const rejectHrClaim = (externalId: string, rejectionReason?: string) =>
  api.post(`/demo/hr/claims/${externalId}/reject`, { rejectionReason }).then(r => r.data)
export const markHrClaimPaid = (externalId: string) =>
  api.post(`/demo/hr/claims/${externalId}/mark-paid`).then(r => r.data)

// Reports
export const getMonthlyReport = (year?: number, month?: number) =>
  api.get<MonthlyReport>('/reports/monthly', { params: { year, month } }).then(r => r.data)
export const getSnackConsumption = () =>
  api.get<SnackConsumption[]>('/reports/snack-consumption').then(r => r.data)

// Notifications
export const getNotifications = () =>
  api.get<Notification[]>('/notifications').then(r => r.data)
export const getEmployeeNotifications = (employeeId: number) =>
  api.get<Notification[]>(`/notifications/employee/${employeeId}`).then(r => r.data)
export const markNotificationRead = (id: number) =>
  api.patch(`/notifications/${id}/read`).then(r => r.data)
