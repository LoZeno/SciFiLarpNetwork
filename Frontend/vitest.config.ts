import { defineConfig } from 'vitest/config';
import react from '@vitejs/plugin-react';

export default defineConfig({
  plugins: [react()],
  test: {
    environment: 'jsdom',
    coverage: {
      provider: 'v8',
      include: ['src/**/*.{ts,tsx}'],
      exclude: ['src/**/*.d.ts'],
      reporter: ['text', 'lcov', 'html'],
      reportsDirectory: 'coverage',
      thresholds: {
        lines: 80,
        statements: 80,
      },
    },
  },
});
