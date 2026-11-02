import { readdir, readFile } from "node:fs/promises";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const directory = dirname(fileURLToPath(import.meta.url));
const localeFiles = (await readdir(directory))
  .filter((file) => /^[a-z]{2}(?:-[A-Z]{2})?\.json$/.test(file))
  .sort();
const requiredLocales = ["en.json", "it.json"];

for (const locale of requiredLocales) {
  if (!localeFiles.includes(locale)) {
    throw new Error(`Required locale resource is missing: ${locale}`);
  }
}

const resources = new Map(
  await Promise.all(
    localeFiles.map(async (file) => [
      file,
      JSON.parse(await readFile(join(directory, file), "utf8")),
    ]),
  ),
);
const english = resources.get("en.json");

if (english === null || typeof english !== "object" || Array.isArray(english)) {
  throw new Error("The English locale resource must be a JSON object.");
}

const englishKeys = Object.keys(english).sort();
const keyPattern = /^[a-z][a-zA-Z0-9]*(?:\.[a-z][a-zA-Z0-9]*)+$/;

if (englishKeys.length === 0) {
  throw new Error("The English locale resource must contain translation keys.");
}

for (const key of englishKeys) {
  if (!keyPattern.test(key)) {
    throw new Error(
      `English locale contains a non-namespaced translation key: "${key}".`,
    );
  }
}

for (const [file, resource] of resources) {
  if (
    resource === null ||
    typeof resource !== "object" ||
    Array.isArray(resource)
  ) {
    throw new Error(`${file} must contain a JSON object.`);
  }

  const keys = Object.keys(resource).sort();
  const missing = englishKeys.filter((key) => !Object.hasOwn(resource, key));
  const extra = keys.filter((key) => !Object.hasOwn(english, key));

  if (missing.length > 0 || extra.length > 0) {
    throw new Error(
      `${file} keys differ from en.json. Missing: ${missing.join(", ") || "none"}; extra: ${extra.join(", ") || "none"}.`,
    );
  }

  for (const [key, value] of Object.entries(resource)) {
    if (typeof value !== "string" || value.trim().length === 0) {
      throw new Error(
        `${file} contains a missing or empty translation for "${key}".`,
      );
    }
  }
}

console.log(
  `Validated ${localeFiles.length} locale resources with ${englishKeys.length} keys.`,
);
