import axios from 'axios';

let accessToken: string | null = null;

export const setAccessToken = (token: string | null) => {
  accessToken = token;
};

export const getAccessToken = () => accessToken;

const client = axios.create({
  baseURL: '/api',
  withCredentials: true,
});

client.interceptors.request.use((config) => {
  if (accessToken) {
    config.headers.Authorization = `Bearer ${accessToken}`;
  }
  return config;
});

// Refresh tokens are single-use (rotated on every call, and the server lets only
// one request use a token). So concurrent callers in this tab share one request,
// tabs take turns through a Web Lock (the next tab then sends the cookie the
// previous one just got), and a failed refresh is tried once more.
let refreshPromise: Promise<string> | null = null;

const REFRESH_LOCK = 'giftcircles-refresh';
const RETRY_DELAY_MS = 300;

const postRefresh = () =>
  axios
    .post<{ accessToken: string }>('/api/auth/refresh', {}, { withCredentials: true })
    .then(({ data }) => data.accessToken);

const acrossTabs = <T,>(work: () => Promise<T>): Promise<T> =>
  typeof navigator !== 'undefined' && navigator.locks
    ? navigator.locks.request(REFRESH_LOCK, work)
    : work();

const refreshWithRetry = async (): Promise<string> => {
  try {
    return await acrossTabs(postRefresh);
  } catch {
    // Another tab (without Web Locks) may have just rotated the cookie.
    await new Promise((resolve) => setTimeout(resolve, RETRY_DELAY_MS));
    return acrossTabs(postRefresh);
  }
};

export const refreshAccessToken = (): Promise<string> => {
  refreshPromise ??= refreshWithRetry()
    .then((token) => {
      setAccessToken(token);
      return token;
    })
    .finally(() => {
      refreshPromise = null;
    });
  return refreshPromise;
};

client.interceptors.response.use(
  (res) => res,
  async (error) => {
    const original = error.config;

    if (error.response?.status === 401 && !original._retry) {
      original._retry = true;

      try {
        const token = await refreshAccessToken();
        original.headers.Authorization = `Bearer ${token}`;
        return client(original);
      } catch {
        setAccessToken(null);
        window.location.href = '/login';
        return Promise.reject(error);
      }
    }

    return Promise.reject(error);
  }
);

export default client;
