export const ROLES = {
  ADMIN: "Admin",
  INSTRUCTOR: "Instructor",
  STUDENT: "Student",
} as const;

export type Role = (typeof ROLES)[keyof typeof ROLES];