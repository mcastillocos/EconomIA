declare const __APP_VERSION__: string;
declare const __GIT_HASH__: string;
declare const __BUILD_DATE__: string;

declare module 'virtual:app-meta' {
  export const APP_VERSION: string;
  export const GIT_HASH: string;
  export const BUILD_DATE: string;
}
