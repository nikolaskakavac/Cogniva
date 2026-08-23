import {
  createContext,
  type PropsWithChildren,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
} from 'react'
import {
  apiClient,
  authTokenStorageKey,
  unauthorizedEventName,
} from '../api/client'
import type {
  AuthResponse,
  CurrentUser,
  LoginRequest,
  RegisterRequest,
} from '../types/auth'

interface AuthContextValue {
  user: CurrentUser | null
  token: string | null
  isAuthenticated: boolean
  loading: boolean
  login: (request: LoginRequest) => Promise<void>
  register: (request: RegisterRequest) => Promise<void>
  logout: () => void
}

const AuthContext = createContext<AuthContextValue | null>(null)

export function AuthProvider({ children }: PropsWithChildren) {
  const [token, setToken] = useState<string | null>(() =>
    sessionStorage.getItem(authTokenStorageKey),
  )
  const [user, setUser] = useState<CurrentUser | null>(null)
  const [loading, setLoading] = useState(true)

  const clearSession = useCallback(() => {
    sessionStorage.removeItem(authTokenStorageKey)
    setToken(null)
    setUser(null)
  }, [])

  const establishSession = useCallback((response: AuthResponse) => {
    sessionStorage.setItem(authTokenStorageKey, response.token)
    setToken(response.token)
    setUser(response.user)
  }, [])

  useEffect(() => {
    const handleUnauthorized = () => clearSession()
    window.addEventListener(unauthorizedEventName, handleUnauthorized)
    return () => window.removeEventListener(unauthorizedEventName, handleUnauthorized)
  }, [clearSession])

  useEffect(() => {
    let active = true

    async function restoreSession() {
      if (!token) {
        setLoading(false)
        return
      }

      try {
        const response = await apiClient.get<CurrentUser>('/api/auth/me')
        if (active) setUser(response.data)
      } catch {
        if (active) clearSession()
      } finally {
        if (active) setLoading(false)
      }
    }

    void restoreSession()
    return () => {
      active = false
    }
  }, [clearSession, token])

  const login = useCallback(
    async (request: LoginRequest) => {
      const response = await apiClient.post<AuthResponse>('/api/auth/login', request)
      establishSession(response.data)
    },
    [establishSession],
  )

  const register = useCallback(
    async (request: RegisterRequest) => {
      const response = await apiClient.post<AuthResponse>('/api/auth/register', request)
      establishSession(response.data)
    },
    [establishSession],
  )

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      token,
      isAuthenticated: Boolean(token && user),
      loading,
      login,
      register,
      logout: clearSession,
    }),
    [clearSession, loading, login, register, token, user],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const context = useContext(AuthContext)
  if (!context) throw new Error('useAuth must be used within AuthProvider.')
  return context
}
