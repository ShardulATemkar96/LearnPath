export const validationUtils = {
  isEmail: (value: string): boolean =>
    /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value),

  isStrongPassword: (value: string): boolean =>
    value.length >= 8 &&
    /[A-Z]/.test(value) &&
    /[0-9]/.test(value),

  isUrl: (value: string): boolean => {
    try { new URL(value); return true; }
    catch { return false; }
  },

  isNotEmpty: (value: string): boolean =>
    value.trim().length > 0,

  maxLength: (value: string, max: number): boolean =>
    value.length <= max,
};
