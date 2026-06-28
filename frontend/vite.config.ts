import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import path from "path";

export default defineConfig({
  plugins: [react()],

  resolve: {
    alias: {
      "@": path.resolve(__dirname, "./src"),
    },
  },

  build: {
    outDir: "dist",
    sourcemap: false,
    minify: "esbuild",
    target: "es2020",
    rollupOptions: {
      output: {
        manualChunks: {
          "vendor-react": ["react", "react-dom", "react-router-dom"],
          "vendor-redux": ["@reduxjs/toolkit", "react-redux"],
          "vendor-mui": ["@mui/material", "@mui/icons-material", "@emotion/react", "@emotion/styled"],
          "vendor-charts": ["recharts"],
          "vendor-axios": ["axios"],
        },
      },
    },
    chunkSizeWarningLimit: 1000,
  },

  server: {
    port: 5173,
    proxy: {
      "/api": {
        target: "http://localhost:5000",
        changeOrigin: true,
      },
    },
  },

  preview: {
    port: 4173,
  },
});
