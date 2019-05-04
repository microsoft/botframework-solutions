FROM node:alpine AS builder
WORKDIR /var/build/
ADD . /var/build/
RUN npm ci
RUN npm run bootstrap -- --ci
RUN npm run build
RUN rm -r /var/build/packages/server/node_modules
RUN rm -r /var/build/packages/server/src

FROM node:alpine
LABEL name="web" version="1.0.0"
EXPOSE 80
ENTRYPOINT node .
WORKDIR /var/www/app/
COPY --from=builder /var/build/packages/server/ /var/www/app/
RUN npm ci --ignore-scripts --production
COPY --from=builder /var/build/packages/app/build/ /var/www/app/_site/
