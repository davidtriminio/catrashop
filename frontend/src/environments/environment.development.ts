/**
 * Development environment. apiBaseUrl stays empty: the dev server proxies "/api" to the backend
 * (see proxy.conf.json → http://localhost:5227), so there is no CORS and no hard-coded host.
 */
export const environment = {
  production: false,
  apiBaseUrl: '',
};
