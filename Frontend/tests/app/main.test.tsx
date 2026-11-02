import { beforeEach, describe, expect, it, vi } from 'vitest';

vi.mock('react-dom/client', () => ({
  createRoot: vi.fn(() => ({ render: vi.fn() })),
}));
vi.mock('../../src/app/App', () => ({ App: () => null }));
vi.mock('../../src/styles.css', () => ({}));

describe('SPA entry point', () => {
  beforeEach(() => {
    vi.resetModules();
  });

  it('mounts the application into the root element', async () => {
    document.body.innerHTML = '<div id="root"></div>';
    const reactDom = await import('react-dom/client');
    const createRoot = vi.mocked(reactDom.createRoot);

    await import('../../src/main');

    expect(createRoot).toHaveBeenCalledWith(document.getElementById('root'));
    const root = createRoot.mock.results[0]?.value;
    expect(root).toBeDefined();
    if (root === undefined) {
      throw new Error('The SPA did not create its React root.');
    }

    expect(vi.mocked(root.render)).toHaveBeenCalledOnce();
  });

  it('fails clearly when the mount element is absent', async () => {
    document.body.innerHTML = '';

    await expect(import('../../src/main')).rejects.toThrow(
      'The application root element is missing.',
    );
  });
});
