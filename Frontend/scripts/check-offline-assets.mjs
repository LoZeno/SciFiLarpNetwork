import { error, log } from 'node:console';
import { readdir, readFile } from 'node:fs/promises';
import { dirname, extname, join } from 'node:path';
import process from 'node:process';
import { fileURLToPath, pathToFileURL } from 'node:url';

const frontendDirectory = dirname(dirname(fileURLToPath(import.meta.url)));
const assetDirectories = ['src', 'public'].map((directory) => join(frontendDirectory, directory));
const assetExtensions = new Set(['.css', '.htm', '.html', '.js', '.jsx', '.ts', '.tsx']);
const resourceTagPattern = /<(?:script|img|source|video|audio|iframe|embed|object|link)\b[^>]*>/gi;
const assetAttributePattern =
  /\b(?:src|href|poster|data|srcset)\s*=\s*(?:"([^"]*)"|'([^']*)'|([^\s>]+))/gi;
const remoteUrlPattern = /^(?:https?:)?\/\//i;
const cssRemoteUrlPattern =
  /(?:@import\s+(?:url\(\s*)?|url\(\s*)["']?((?:https?:)?\/\/[^)"'\s;]+)/gi;

export function findExternalAssetReferences(content) {
  const references = [];

  for (const match of content.matchAll(resourceTagPattern)) {
    const tag = match[0];
    const tagName = /^<([a-z]+)/i.exec(tag)?.[1]?.toLowerCase();
    const isLink = tagName === 'link';
    const isResourceTag = tagName !== undefined && !isLink;

    for (const attribute of tag.matchAll(assetAttributePattern)) {
      const value = attribute[1] ?? attribute[2] ?? attribute[3] ?? '';
      const candidates = attribute[0].toLowerCase().startsWith('srcset')
        ? value.split(',').map((candidate) => candidate.trim().split(/\s+/, 1)[0] ?? '')
        : [value.trim()];

      for (const candidate of candidates) {
        if (
          remoteUrlPattern.test(candidate) &&
          (isResourceTag || (isLink && /^href\s*=/i.test(attribute[0])))
        ) {
          references.push(candidate);
        }
      }
    }
  }

  for (const match of content.matchAll(cssRemoteUrlPattern)) {
    references.push(match[1]);
  }

  return references;
}

async function findFiles(directory) {
  let entries;

  try {
    entries = await readdir(directory, { withFileTypes: true });
  } catch (error) {
    if (error.code === 'ENOENT') {
      return [];
    }

    throw error;
  }

  const nestedFiles = await Promise.all(
    entries.map(async (entry) => {
      const path = join(directory, entry.name);

      if (entry.isDirectory()) {
        return findFiles(path);
      }

      return assetExtensions.has(extname(entry.name).toLowerCase()) ? [path] : [];
    }),
  );

  return nestedFiles.flat();
}

export async function checkOfflineAssets() {
  const files = (await Promise.all(assetDirectories.map(findFiles))).flat();
  const violations = [];

  for (const file of files) {
    const content = await readFile(file, 'utf8');

    for (const reference of findExternalAssetReferences(content)) {
      violations.push(`${file}: ${reference}`);
    }
  }

  return { filesChecked: files.length, violations };
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  const { filesChecked, violations } = await checkOfflineAssets();

  if (violations.length > 0) {
    error('External frontend assets are not allowed:');
    for (const violation of violations) {
      error(`- ${violation}`);
    }
    process.exitCode = 1;
  } else {
    log(`Checked ${filesChecked} frontend HTML/CSS/JS asset file(s); no external assets found.`);
  }
}
