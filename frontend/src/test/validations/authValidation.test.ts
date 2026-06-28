tsimport { describe, it, expect } from "vitest";
import { validateLogin, validateRegister } from "../../validations/authValidation";

describe("validateLogin", () => {
  it("returns no errors for valid credentials", () => {
    const errors = validateLogin("user@test.com", "Password1");
    expect(Object.keys(errors)).toHaveLength(0);
  });

  it("returns email error for empty email", () => {
    const errors = validateLogin("", "Password1");
    expect(errors.email).toBeDefined();
  });

  it("returns email error for invalid email format", () => {
    const errors = validateLogin("notanemail", "Password1");
    expect(errors.email).toBeDefined();
  });

  it("returns password error for empty password", () => {
    const errors = validateLogin("user@test.com", "");
    expect(errors.password).toBeDefined();
  });

  it("returns password error for short password", () => {
    const errors = validateLogin("user@test.com", "short");
    expect(errors.password).toBeDefined();
  });
});

describe("validateRegister", () => {
  const valid = {
    firstName:       "John",
    lastName:        "Doe",
    email:           "john@test.com",
    password:        "Password1",
    confirmPassword: "Password1",
  };

  it("returns no errors for valid registration data", () => {
    const errors = validateRegister(
      valid.firstName, valid.lastName, valid.email,
      valid.password, valid.confirmPassword
    );
    expect(Object.keys(errors)).toHaveLength(0);
  });

  it("returns firstName error when empty", () => {
    const errors = validateRegister(
      "", valid.lastName, valid.email,
      valid.password, valid.confirmPassword
    );
    expect(errors.firstName).toBeDefined();
  });

  it("returns confirmPassword error when passwords dont match", () => {
    const errors = validateRegister(
      valid.firstName, valid.lastName, valid.email,
      valid.password, "DifferentPass1"
    );
    expect(errors.confirmPassword).toBeDefined();
    expect(errors.confirmPassword).toContain("match");
  });

  it("returns password error for missing uppercase", () => {
    const errors = validateRegister(
      valid.firstName, valid.lastName, valid.email,
      "password1", "password1"
    );
    expect(errors.password).toBeDefined();
    expect(errors.password).toContain("uppercase");
  });

  it("returns password error for missing number", () => {
    const errors = validateRegister(
      valid.firstName, valid.lastName, valid.email,
      "PasswordOnly", "PasswordOnly"
    );
    expect(errors.password).toBeDefined();
    expect(errors.password).toContain("number");
  });
});
