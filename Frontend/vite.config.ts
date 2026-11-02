import { defineConfig, loadEnv } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig(({ mode }) => {
  const environment = loadEnv(mode, process.cwd(), '');
  const appBasePath = environment.VITE_APP_BASE_PATH || '/';
  const apiBasePath = environment.VITE_API_BASE_PATH || '/api';
  const backendOrigin = environment.VITE_BACKEND_ORIGIN || 'http://localhost:5000';
  const apiProxy = {
    [apiBasePath]: {
      target: backendOrigin,
      changeOrigin: false,
    },
  };

  return {
    base: appBasePath,
    plugins: [react()],
    server: {
      proxy: apiProxy,
    },
    preview: {
      proxy: apiProxy,
    },
  };
});
