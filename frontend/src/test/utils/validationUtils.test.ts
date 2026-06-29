import { describe, it, expect } from "vitest";
import { validationUtils } from "../../utils/validationUtils";

describe("validationUtils", () => {
  describe("isEmail", () => {
    it("accepts valid emails", () => {
      expect(validationUtils.isEmail("user@example.com")).toBe(true);
      expect(validationUtils.isEmail("test.user+tag@domain.org")).toBe(true);
    });

    it("rejects invalid emails", () => {
      expect(validationUtils.isEmail("notanemail")).toBe(false);
      expect(validationUtils.isEmail("missing@")).toBe(false);
      expect(validationUtils.isEmail("@nodomain.com")).toBe(false);
      expect(validationUtils.isEmail("")).toBe(false);
    });
  });

  describe("isStrongPassword", () => {
    it("accepts strong passwords", () => {
      expect(validationUtils.isStrongPassword("Password1")).toBe(true);
      expect(validationUtils.isStrongPassword("SecurePass99!")).toBe(true);
    });

    it("rejects weak passwords", () => {
      expect(validationUtils.isStrongPassword("short1A")).toBe(false);
      expect(validationUtils.isStrongPassword("nouppercase1")).toBe(false);
      expect(validationUtils.isStrongPassword("NoNumbers")).toBe(false);
    });
  });

  describe("isNotEmpty", () => {
    it("returns true for non-empty strings", () => {
      expect(validationUtils.isNotEmpty("hello")).toBe(true);
      expect(validationUtils.isNotEmpty("  content  ")).toBe(true);
    });

    it("returns false for empty or whitespace strings", () => {
      expect(validationUtils.isNotEmpty("")).toBe(false);
      expect(validationUtils.isNotEmpty("   ")).toBe(false);
    });
  });

  describe("maxLength", () => {
    it("returns true when within limit", () => {
      expect(validationUtils.maxLength("hello", 10)).toBe(true);
    });

    it("returns false when exceeds limit", () => {
      expect(validationUtils.maxLength("toolongstring", 5)).toBe(false);
    });
  });
});
