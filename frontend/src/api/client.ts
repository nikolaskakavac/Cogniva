import axios from 'axios'

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5080'
export const authTokenStorageKey = 'cogniva.auth.token'
export const unauthorizedEventName = 'cogniva:unauthorized'

export const apiClient = axios.create({
  baseURL: apiBaseUrl,
  headers: {
    'Content-Type': 'application/json',
  },
  timeout: 15_000,
})

apiClient.interceptors.request.use((config) => {
  const token = sessionStorage.getItem(authTokenStorageKey)

  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }

  return config
})

apiClient.interceptors.response.use(
  (response) => response,
  (error: unknown) => {
    if (axios.isAxiosError(error) && error.response?.status === 401) {
      sessionStorage.removeItem(authTokenStorageKey)
      window.dispatchEvent(new Event(unauthorizedEventName))
    }

    return Promise.reject(error)
  },
)
