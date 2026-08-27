import { createTheme, Theme } from "@mui/material/styles";
import { lightPalette, darkPalette } from "./palette";
import { typography } from "./typography";

const getComponentOverrides = (mode: "light" | "dark") => ({
  MuiButton: {
    styleOverrides: {
      root: {
        borderRadius: 8,
        padding: "10px 24px",
        boxShadow: "none",
        "&:hover": { boxShadow: "0 4px 12px rgba(108,99,255,0.25)" },
      },
    },
  },
  MuiCard: {
    styleOverrides: {
      root: {
        borderRadius: 16,
        boxShadow: mode === "light" ? "0 2px 16px rgba(0,0,0,0.06)" : "0 4px 24px rgba(0,0,0,0.35)",
        border: mode === "dark" ? "1px solid rgba(255,255,255,0.06)" : "none",
      },
    },
  },
  MuiTextField: {
    styleOverrides: {
      root: { "& .MuiOutlinedInput-root": { borderRadius: 8 } },
    },
  },
  MuiPaper: {
    styleOverrides: {
      root: {
        backgroundImage: "none",
      },
    },
  },
  MuiDivider: {
    styleOverrides: {
      root: {
        borderColor: mode === "light" ? "rgba(26,29,46,0.08)" : "rgba(255,255,255,0.08)",
      },
    },
  },
  MuiChip: {
    styleOverrides: {
      root: {
        borderColor: mode === "dark" ? "rgba(255,255,255,0.12)" : undefined,
      },
    },
  },
});

export const getAppTheme = (mode: "light" | "dark"): Theme =>
  createTheme({
    palette: mode === "light" ? lightPalette : darkPalette,
    typography,
    shape: { borderRadius: 12 },
    spacing: 8,
    components: getComponentOverrides(mode) as any,
  });

// Backward compat — default light theme
export const theme = getAppTheme("light");