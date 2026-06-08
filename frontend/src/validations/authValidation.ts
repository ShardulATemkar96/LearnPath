export interface LoginFormErrors {
  email?: string;
  password?: string;
}

export interface RegisterFormErrors {
  firstName?: string;
  lastName?: string;
  email?: string;
  password?: string;
  confirmPassword?: string;
}

export const validateLogin = (email: string, password: string): LoginFormErrors => {
  const errors: LoginFormErrors = {};
  if (!email) errors.email = "Email is required.";
  else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) errors.email = "Enter a valid email.";
  if (!password) errors.password = "Password is required.";
  else if (password.length < 8) errors.password = "Minimum 8 characters.";
  return errors;
};

export const validateRegister = (
  firstName: string,
  lastName: string,
  email: string,
  password: string,
  confirmPassword: string
): RegisterFormErrors => {
  const errors: RegisterFormErrors = {};
  if (!firstName.trim()) errors.firstName = "First name is required.";
  if (!lastName.trim()) errors.lastName = "Last name is required.";
  if (!email) errors.email = "Email is required.";
  else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) errors.email = "Enter a valid email.";
  if (!password) errors.password = "Password is required.";
  else if (password.length < 8) errors.password = "Minimum 8 characters.";
  else if (!/[A-Z]/.test(password)) errors.password = "Must contain an uppercase letter.";
  else if (!/[0-9]/.test(password)) errors.password = "Must contain a number.";
  if (!confirmPassword) errors.confirmPassword = "Please confirm your password.";
  else if (confirmPassword !== password) errors.confirmPassword = "Passwords do not match.";
  return errors;
};