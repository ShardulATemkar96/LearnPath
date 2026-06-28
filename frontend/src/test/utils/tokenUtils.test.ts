tsimport { describe, it, expect, beforeEach } from "vitest";
import { tokenUtils } from "../../utils/tokenUtils";

describe("tokenUtils", () => {
  beforeEach(() => {
    localStorage.clear();
  });

  it("setTokens stores both tokens", () => {
    tokenUtils.setTokens("access123", "refresh456");
    expect(tokenUtils.getAccessToken()).toBe("access123");
    expect(tokenUtils.getRefreshToken()).toBe("refresh456");
  });

  it("clearTokens removes both tokens", () => {
    tokenUtils.setTokens("access123", "refresh456");
    tokenUtils.clearTokens();
    expect(tokenUtils.getAccessToken()).toBeNull();
    expect(tokenUtils.getRefreshToken()).toBeNull();
  });

  it("getAccessToken returns null when not set", () => {
    expect(tokenUtils.getAccessToken()).toBeNull();
  });

  it("isTokenExpired returns true for invalid token", () => {
    expect(tokenUtils.isTokenExpired("not.a.token")).toBe(true);
  });

  it("isTokenExpired returns true for expired token", () => {
    const expiredToken = [
      "eyJhbGciOiJIUzI1NiJ9",
      btoa(JSON.stringify({ exp: 1 })),
      "signature",
    ].join(".");
    expect(tokenUtils.isTokenExpired(expiredToken)).toBe(true);
  });

  it("isTokenExpired returns false for future token", () => {
    const futureExp = Math.floor(Date.now() / 1000) + 3600;
    const futureToken = [
      "eyJhbGciOiJIUzI1NiJ9",
      btoa(JSON.stringify({ exp: futureExp })),
      "signature",
    ].join(".");
    expect(tokenUtils.isTokenExpired(futureToken)).toBe(false);
  });
});
