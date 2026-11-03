import { useState } from 'react';
import { useLocale } from '../i18n/LocaleProvider';
import { QrCapture } from './QrCapture';
import type { QrScanner } from './qrScanner';

export function QrCaptureTestPage({ scanner }: { readonly scanner?: QrScanner }) {
  const { t } = useLocale();
  const [decodedValue, setDecodedValue] = useState('');

  return (
    <main className="qr-capture-test">
      <QrCapture onScan={setDecodedValue} {...(scanner === undefined ? {} : { scanner })} />
      <label className="field qr-capture-test-result">
        <span>{t('scanner.testResult')}</span>
        <textarea aria-label={t('scanner.testResult')} readOnly value={decodedValue} />
      </label>
      <button
        className="button button-secondary qr-capture-test-clear"
        type="button"
        disabled={decodedValue.length === 0}
        onClick={() => setDecodedValue('')}
      >
        {t('scanner.clearTestResult')}
      </button>
    </main>
  );
}
