export interface User {
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
  avatarUrl?: string;
  bio?: string;
  roles: string[];
}