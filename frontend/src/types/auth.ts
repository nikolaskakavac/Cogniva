export interface CurrentUser {
  id: string
  email: string
  firstName: string
  lastName: string
}

export interface AuthResponse {
  token: string
  expiresAt: string
  user: CurrentUser
}

export interface LoginRequest {
  email: string
  password: string
}

export interface RegisterRequest extends LoginRequest {
  firstName: string
  lastName: string
}

export interface ApiProblem {
  title?: string
  detail?: string
  errors?: Record<string, string[]>
}
