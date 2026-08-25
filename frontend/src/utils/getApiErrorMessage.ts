import axios from 'axios'
import type { ApiProblem } from '../types/auth'

export function getApiErrorMessage(error: unknown): string {
  if (!axios.isAxiosError<ApiProblem>(error)) {
    return 'Došlo je do greške. Pokušajte ponovo.'
  }

  if (error.code === 'ECONNABORTED') {
    return 'AI model nije završio odgovor u predviđenom vremenu. Proverite da li Ollama radi i pokušajte ponovo.'
  }

  if (!error.response) {
    return 'Nije moguće povezati se sa serverom. Proverite da li je backend pokrenut.'
  }

  const problem = error.response?.data
  const validationMessage = problem?.errors
    ? Object.values(problem.errors).flat()[0]
    : undefined

  return validationMessage ?? problem?.detail ?? problem?.title ?? 'Zahtev nije moguće izvršiti.'
}
