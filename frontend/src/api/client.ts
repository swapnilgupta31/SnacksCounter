import axios from 'axios'

const BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000/api'

// Demo user state — stored in localStorage for persistence across refreshes
export const getDemoUserId = (): number => {
  return parseInt(localStorage.getItem('demoUserId') || '1', 10)
}

export const setDemoUserId = (id: number) => {
  localStorage.setItem('demoUserId', String(id))
}

const api = axios.create({
  baseURL: BASE_URL,
  headers: { 'Content-Type': 'application/json' }
})

// Attach demo user header to every request
api.interceptors.request.use(config => {
  config.headers['X-Demo-User-Id'] = getDemoUserId()
  return config
})

export default api
