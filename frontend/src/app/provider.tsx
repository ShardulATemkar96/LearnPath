import { Provider } from "react-redux";
import { ThemeProvider, CssBaseline } from "@mui/material";
import { BrowserRouter } from "react-router-dom";
import { store } from "./store";
import { getAppTheme } from "../theme";
import { ThemeModeProvider, useThemeMode } from "../context/ThemeModeContext";
import ErrorBoundary from "../components/common/ErrorBoundary/ErrorBoundary";

const ThemedApp = ({ children }: { children: React.ReactNode }) => {
  const { mode } = useThemeMode();
  const theme = getAppTheme(mode);
  return (
    <ThemeProvider theme={theme}>
      <CssBaseline />
      <BrowserRouter>
        <ErrorBoundary>
          {children}
        </ErrorBoundary>
      </BrowserRouter>
    </ThemeProvider>
  );
};

const AppProvider = ({ children }: { children: React.ReactNode }) => (
  <Provider store={store}>
    <ThemeModeProvider>
      <ThemedApp>{children}</ThemedApp>
    </ThemeModeProvider>
  </Provider>
);

export default AppProvider;