/// <reference types="vite/client" />
// import.meta.env and side-effect imports of .css come from here.

// qrcode ships no types and has no @types package worth a dependency for one call: the MFA dialog
// renders one data URL and nothing else uses the library. Declared here rather than left implicitly
// any, so a wrong argument is still an error.
declare module 'qrcode' {
  export function toDataURL(
    text: string,
    options?: { margin?: number; width?: number },
  ): Promise<string>;
}
