import axios from 'axios'
import type { ApiProblem } from '../types/auth'

export function getApiErrorMessage(error: unknown): string {
  if (!axios.isAxiosError<ApiProblem>(error)) {
    return 'Something went wrong. Please try again.'
  }

  const problem = error.response?.data
  const validationMessage = problem?.errors
    ? Object.values(problem.errors).flat()[0]
    : undefined

  return validationMessage ?? problem?.detail ?? problem?.title ?? 'Unable to complete the request.'
}
