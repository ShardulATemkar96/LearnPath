import { defineConfig } from "vitest/config";
import react from "@vitejs/plugin-react";
import path from "path";

export default defineConfig({
  plugins: [react()],
  test: {
    globals:     true,
    environment: "jsdom",
    setupFiles:  "./src/test/setup.ts",
    coverage: {
      reporter:  ["text", "json", "html"],
      exclude:   [
        "node_modules/",
        "src/test/",
        "**/*.d.ts",
        "src/main.tsx",
      ],
    },
  },
  resolve: {
    alias: { "@": path.resolve(__dirname, "./src") },
  },
});
