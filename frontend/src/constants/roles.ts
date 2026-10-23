export const ROLES = {
  ADMIN: "Admin",
  STUDENT: "Student",
} as const;

export type Role = (typeof ROLES)[keyof typeof ROLES];