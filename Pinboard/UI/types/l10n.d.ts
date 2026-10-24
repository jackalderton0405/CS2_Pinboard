declare module "cs2/l10n" {
  export interface Localization {
  	translate: (id: string, fallback?: string | null) => string | null;
  }
  export function useLocalization(): Localization;
}
