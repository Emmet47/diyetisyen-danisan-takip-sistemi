# syntax=docker/dockerfile:1
# Web paneli imajı. Derleme bağlamı web/ klasörüdür.

# ---------- 1. aşama: React uygulamasını derle ----------
FROM node:26-slim AS build
WORKDIR /app

# Önce yalnızca paket listesi: bağımlılıklar değişmedikçe npm ci önbellekten gelir.
COPY package.json package-lock.json ./
RUN npm ci

COPY . .
RUN npm run build

# ---------- 2. aşama: derlenmiş dosyaları nginx ile sun ----------
FROM nginx:stable-alpine AS final
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
