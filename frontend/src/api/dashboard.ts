import { apiClient } from './client'
import type { DashboardData } from '../types/dashboard'

export async function getDashboard() {
  const response = await apiClient.get<DashboardData>('/api/dashboard')
  return response.data
}
