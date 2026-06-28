tsimport { describe, it, expect, vi, beforeEach } from "vitest";
import { dateUtils } from "../../utils/dateUtils";

describe("dateUtils", () => {
  describe("format", () => {
    it("formats a date string correctly", () => {
      const result = dateUtils.format("2025-01-15T00:00:00Z");
      expect(result).toContain("2025");
      expect(result).toContain("Jan");
    });

    it("formats a Date object correctly", () => {
      const date   = new Date("2025-06-01T00:00:00Z");
      const result = dateUtils.format(date);
      expect(result).toContain("2025");
    });
  });

  describe("timeAgo", () => {
    it('returns "just now" for very recent dates', () => {
      const recent = new Date(Date.now() - 5000);
      expect(dateUtils.timeAgo(recent)).toBe("just now");
    });

    it("returns minutes ago format", () => {
      const fiveMin = new Date(Date.now() - 5 * 60 * 1000);
      expect(dateUtils.timeAgo(fiveMin)).toBe("5m ago");
    });

    it("returns hours ago format", () => {
      const threeHrs = new Date(Date.now() - 3 * 60 * 60 * 1000);
      expect(dateUtils.timeAgo(threeHrs)).toBe("3h ago");
    });

    it("returns days ago format", () => {
      const twoDays = new Date(Date.now() - 2 * 24 * 60 * 60 * 1000);
      expect(dateUtils.timeAgo(twoDays)).toBe("2d ago");
    });
  });

  describe("isOverdue", () => {
    it("returns true for past dates", () => {
      const past = new Date(Date.now() - 86400000);
      expect(dateUtils.isOverdue(past)).toBe(true);
    });

    it("returns false for future dates", () => {
      const future = new Date(Date.now() + 86400000);
      expect(dateUtils.isOverdue(future)).toBe(false);
    });
  });
});
