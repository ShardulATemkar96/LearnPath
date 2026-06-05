import { createTheme } from "@mui/material/styles";
import { palette } from "./palette";
import { typography } from "./typography";

export const theme = createTheme({
  palette,
  typography,
  shape: { borderRadius: 12 },
  spacing: 8,
  components: {
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
          boxShadow: "0 2px 16px rgba(0,0,0,0.06)",
        },
      },
    },
    MuiTextField: {
      styleOverrides: {
        root: { "& .MuiOutlinedInput-root": { borderRadius: 8 } },
      },
    },
  },
});