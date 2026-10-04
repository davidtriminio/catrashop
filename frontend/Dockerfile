# syntax=docker/dockerfile:1
# Build context: ./frontend.  Builds both SPAs (customer + admin) and serves them with nginx.
FROM node:22-alpine AS build
WORKDIR /app
RUN corepack enable
COPY package.json pnpm-lock.yaml ./
RUN pnpm install --frozen-lockfile
COPY . .
RUN pnpm exec ng build commercehub-web && pnpm exec ng build commercehub-admin

FROM nginx:1.27-alpine AS web
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist/commercehub-web/browser /usr/share/nginx/html/web
COPY --from=build /app/dist/commercehub-admin/browser /usr/share/nginx/html/admin
EXPOSE 80
