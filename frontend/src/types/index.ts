export interface Office {
  id: number
  name: string
  currency: string
  monthlyBudget: number
  timeZoneId: string
}

export interface OfficeBudget {
  officeId: number
  officeName: string
  currency: string
  monthlyBudget: number
  actualSpending: number
  activeReservations: number
  availableBudget: number
  budgetMonth: string
}

export interface Employee {
  id: number
  name: string
  email: string
  homeOfficeId: number
  homeOfficeName: string
  currentOfficeId: number
  currentOfficeName: string
  payrollCurrency: string
  isVolunteer: boolean
  isAvailable: boolean
  isOnLeave: boolean
  isPresentToday: boolean
  role: string
}

export interface SnackBox {
  id: number
  officeId: number
  officeName: string
  name: string
  category: string
  availabilityStatus: string
  lastRefilledAt: string | null
  activeRefillId: number | null
}

export interface Purchase {
  id: number
  refillRequestId: number
  employeeId: number
  employeeName: string
  amount: number
  currency: string
  purchasedAt: string
  receiptReference: string | null
  description: string
  isOverBudget: boolean
}

export interface ReimbursementClaim {
  id: number
  refillRequestId: number
  employeeId: number
  employeeName: string
  externalClaimId: string | null
  amount: number
  currency: string
  status: string
  createdAt: string
  submittedAt: string | null
  approvedAt: string | null
  paidAt: string | null
  failureReason: string | null
}

export interface RefillRequest {
  id: number
  snackBoxId: number
  snackBoxName: string
  snackCategory: string
  officeId: number
  officeName: string
  status: string
  assignedVolunteerId: number | null
  assignedVolunteerName: string | null
  estimatedCost: number
  currency: string
  reportedByEmployeeId: number
  reportedByName: string
  createdAt: string
  offerExpiresAt: string | null
  assignedAt: string | null
  assignmentDeadline: string | null
  completedAt: string | null
  notes: string | null
  cancellationReason: string | null
  purchase: Purchase | null
  reimbursementClaim: ReimbursementClaim | null
}

export interface Notification {
  id: number
  recipientEmployeeId: number | null
  recipientName: string | null
  refillRequestId: number | null
  type: string
  message: string
  isRead: boolean
  sentAt: string
}

export interface MonthlyReport {
  year: number
  month: number
  oslo: OfficeReport
  bengaluru: OfficeReport
  demoExchangeRateNokToInr: number
  osloSpendingInInr: number
  combinedTotalInr: number
  exchangeRateLabel: string
}

export interface OfficeReport {
  officeId: number
  officeName: string
  currency: string
  monthlyBudget: number
  actualSpending: number
  activeReservations: number
  availableBudget: number
  completedRefills: number
  spendingByCategory: CategorySpending[]
  reimbursementBreakdown: ReimbursementStatusCount[]
}

export interface CategorySpending {
  category: string
  totalSpending: number
  refillCount: number
}

export interface ReimbursementStatusCount {
  status: string
  count: number
  totalAmount: number
}

export interface SnackConsumption {
  snackBoxId: number
  snackBoxName: string
  category: string
  officeId: number
  officeName: string
  refillCount: number
  lastRefilledAt: string | null
}

export interface HrClaim {
  externalClaimId: string
  localClaimId: number
  employeeName: string
  amount: number
  currency: string
  status: string
  submittedAt: string
  approvedAt: string | null
  paidAt: string | null
  rejectionReason: string | null
}

export interface ApiError {
  code: string
  message: string
}
