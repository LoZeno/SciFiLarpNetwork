export function findExternalAssetReferences(content: string): string[];
export function checkOfflineAssets(): Promise<{
  filesChecked: number;
  violations: string[];
}>;
