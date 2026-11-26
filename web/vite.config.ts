import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      // The browser only talks to Vite (localhost:5173). Vite forwards every /api/... request to the .NET API,
      // so for the browser everything comes from one origin and CORS never applies
      '/api': {
        target: 'https://localhost:7179',
        secure: false, // Node does not trust the API's development certificate
      },
    },
  },
})
