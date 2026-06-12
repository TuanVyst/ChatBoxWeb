import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  server: {
    host: '0.0.0.0', // Cho phép truy cập từ máy khác (LAN/Radmin VPN)
    port: 3000,
    proxy: {
      '/uploads': {
        target: 'http://localhost:5206',
        changeOrigin: true,
      },
      '/api': {
        target: 'http://localhost:5206',
        changeOrigin: true,
      },
      '/chathub': {
        target: 'http://localhost:5206',
        ws: true,
      },
    },
  },
})
