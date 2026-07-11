/**
 * Production environment. The API is expected on the same origin (served behind a reverse proxy),
 * so requests use relative "/api/..." paths. Override apiBaseUrl for a split-origin deployment.
 */
export const environment = {
  production: true,
  apiBaseUrl: '',
};
