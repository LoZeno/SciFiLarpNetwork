import { describe, expect, it } from 'vitest';

import { findExternalAssetReferences } from '../scripts/check-offline-assets.mjs';

describe('findExternalAssetReferences', () => {
  it('finds remote resources and stylesheets in HTML', () => {
    const html = `
      <script src="https://cdn.example.test/app.js"></script>
      <link rel="stylesheet" href="//fonts.example.test/site.css">
      <a href="https://example.test/page">External navigation is allowed</a>
    `;

    expect(findExternalAssetReferences(html)).toEqual([
      'https://cdn.example.test/app.js',
      '//fonts.example.test/site.css',
    ]);
  });

  it('finds remote CSS imports and URLs but allows local assets', () => {
    const css = `
      @import url("https://cdn.example.test/theme.css");
      .icon { background-image: url("/assets/icon.svg"); }
      .font { src: url(//fonts.example.test/font.woff2); }
    `;

    expect(findExternalAssetReferences(css)).toEqual([
      'https://cdn.example.test/theme.css',
      '//fonts.example.test/font.woff2',
    ]);
  });
});
