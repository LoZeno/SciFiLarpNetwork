import { lazy, Suspense } from 'react';
import { Route, Routes } from 'react-router-dom';
import { App } from './App';

const QrCaptureTestPage = lazy(() =>
  import('../scanning/QrCaptureTestPage').then((module) => ({
    default: module.QrCaptureTestPage,
  })),
);

export function AppRouter() {
  return (
    <Routes>
      <Route
        path="/qr-capture-test"
        element={
          <Suspense fallback={null}>
            <QrCaptureTestPage />
          </Suspense>
        }
      />
      <Route path="*" element={<App />} />
    </Routes>
  );
}
