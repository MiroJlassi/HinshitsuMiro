# Use Node.js 22 Linux base image
FROM node:22-slim

# Install Chrome dependencies (required for Lighthouse/Chrome)
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
  --no-install-recommends

# Install Chromium
RUN apt-get install -y chromium

# Create app directory
WORKDIR /app

# Install dependencies first
COPY package*.json ./
RUN npm install

# Copy app source code
COPY . .

# Set Chrome path for chrome-launcher
ENV CHROME_PATH=/usr/bin/chromium

# Expose port
EXPOSE 3000

CMD ["node", "server.js"]
