import { defineConfig } from 'vitest/config';

export default defineConfig({
  test: {
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
