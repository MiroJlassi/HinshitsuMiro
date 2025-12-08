# Use Node.js 22 Linux base image
FROM node:22-slim

# Install Chrome/Chromium dependencies (required for Playwright)
RUN apt-get update && apt-get install -y \
  wget gnupg \
  libnss3 \
  libatk1.0-0 \
  libatk-bridge2.0-0 \
  libcups2 \
  libxdamage1 \
  libxrandr2 \
  libgbm-dev \
  libasound2 \
  fonts-liberation \
  libgtk-3-0 \
  chromium \
  --no-install-recommends \
  && rm -rf /var/lib/apt/lists/*

# Create app directory
WORKDIR /app

# Install dependencies first
COPY package*.json ./
RUN npm install

# Copy app source code
COPY . .

# Set Chrome path for Playwright
ENV CHROME_PATH=/usr/bin/chromium

# Expose port
EXPOSE 4000

CMD ["node", "server.js"]
