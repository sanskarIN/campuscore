FROM node:24-alpine AS build
WORKDIR /app
COPY src/CampusCore.Web/package.json ./
RUN npm install --no-audit --no-fund
COPY src/CampusCore.Web/ ./
ARG VITE_API_BASE_URL=/
ARG VITE_APP_VERSION=0.1.0
ENV VITE_API_BASE_URL=${VITE_API_BASE_URL} \
    VITE_APP_VERSION=${VITE_APP_VERSION}
RUN npm run build

FROM nginx:1.29-alpine AS runtime
RUN mkdir -p /var/cache/nginx/client_temp \
        /var/cache/nginx/proxy_temp \
        /var/cache/nginx/fastcgi_temp \
        /var/cache/nginx/uwsgi_temp \
        /var/cache/nginx/scgi_temp \
    && touch /var/run/nginx.pid \
    && chown -R nginx:nginx /var/cache/nginx /var/run/nginx.pid /usr/share/nginx/html
COPY --chown=nginx:nginx src/CampusCore.Web/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build --chown=nginx:nginx /app/dist /usr/share/nginx/html
USER nginx
EXPOSE 8080
CMD ["nginx", "-g", "daemon off;"]
