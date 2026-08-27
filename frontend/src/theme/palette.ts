import { PaletteOptions } from "@mui/material";

export const lightPalette: PaletteOptions = {
  mode: "light",
  primary: {
    main: "#6C63FF",
    light: "#9D97FF",
    dark: "#4B44CC",
    contrastText: "#FFFFFF",
  },
  secondary: {
    main: "#FF6584",
    light: "#FF92A8",
    dark: "#CC3D60",
    contrastText: "#FFFFFF",
  },
  background: {
    default: "#F7F8FC",
    paper: "#FFFFFF",
  },
  text: {
    primary: "#1A1D2E",
    secondary: "#6B7280",
  },
  divider: "rgba(26,29,46,0.08)",
  success: { main: "#22C55E" },
  warning: { main: "#F59E0B" },
  error: { main: "#EF4444" },
  info: { main: "#3B82F6" },
};

export const darkPalette: PaletteOptions = {
  mode: "dark",
  primary: {
    main: "#7C75FF",
    light: "#9D97FF",
    dark: "#4B44CC",
    contrastText: "#FFFFFF",
  },
  secondary: {
    main: "#FF6584",
    light: "#FF92A8",
    dark: "#CC3D60",
    contrastText: "#FFFFFF",
  },
  background: {
    default: "#0F111A",
    paper: "#1A1D2E",
  },
  text: {
    primary: "#F1F5F9",
    secondary: "#94A3B8",
  },
  divider: "rgba(255,255,255,0.08)",
  success: { main: "#22C55E" },
  warning: { main: "#F59E0B" },
  error: { main: "#F87171" },
  info: { main: "#60A5FA" },
};

// Backward compat
export const palette = lightPalette;