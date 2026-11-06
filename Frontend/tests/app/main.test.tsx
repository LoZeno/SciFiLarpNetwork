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

    const root = createRoot.mock.results[0]?.value;
    expect({
      mountElement: createRoot.mock.calls[0]?.[0],
      rootCreated: root !== undefined,
      renderCount: root === undefined ? 0 : vi.mocked(root.render).mock.calls.length,
    }).toEqual({
      mountElement: document.getElementById('root'),
      rootCreated: true,
      renderCount: 1,
    });
  });

  it('fails clearly when the mount element is absent', async () => {
    document.body.innerHTML = '';

    await expect(import('../../src/main')).rejects.toThrow(
      'The application root element is missing.',
    );
  });
});
