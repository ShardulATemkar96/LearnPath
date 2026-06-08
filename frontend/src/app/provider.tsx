import { Provider } from "react-redux";
import { ThemeProvider, CssBaseline } from "@mui/material";
import { BrowserRouter } from "react-router-dom";
import { store } from "./store";
import { theme } from "../theme";
import ErrorBoundary from "../components/common/ErrorBoundary/ErrorBoundary";

const AppProvider = ({ children }: { children: React.ReactNode }) => (
  <Provider store={store}>
    <ThemeProvider theme={theme}>
      <CssBaseline />
      <BrowserRouter>
        <ErrorBoundary>
          {children}
        </ErrorBoundary>
      </BrowserRouter>
    </ThemeProvider>
  </Provider>
);

export default AppProvider;