import { ReactElement } from "react";
import { render, RenderOptions } from "@testing-library/react";
import { Provider } from "react-redux";
import { ThemeProvider, CssBaseline } from "@mui/material";
import { MemoryRouter } from "react-router-dom";
import { configureStore } from "@reduxjs/toolkit";
import authReducer         from "../redux/slices/authSlice";
import dashboardReducer    from "../redux/slices/dashboardSlice";
import pathReducer         from "../redux/slices/pathSlice";
import classroomReducer    from "../redux/slices/classroomSlice";
import analyticsReducer    from "../redux/slices/analyticsSlice";
import notificationReducer from "../redux/slices/notificationSlice";
import communityReducer    from "../redux/slices/communitySlice";
import { theme }           from "../theme";
import type { RootState }  from "../redux/store";

interface ExtendedRenderOptions extends Omit<RenderOptions, "queries"> {
  preloadedState?: Partial<RootState>;
  route?:          string;
}

export const renderWithProviders = (
  ui: ReactElement,
  {
    preloadedState = {},
    route         = "/",
    ...renderOptions
  }: ExtendedRenderOptions = {}
) => {
  const store = configureStore({
    reducer: {
      auth:          authReducer,
      dashboard:     dashboardReducer,
      paths:         pathReducer,
      classrooms:    classroomReducer,
      analytics:     analyticsReducer,
      notifications: notificationReducer,
      community:     communityReducer,
    },
    preloadedState,
  });

  const Wrapper = ({ children }: { children: React.ReactNode }) => (
    <Provider store={store}>
      <ThemeProvider theme={theme}>
        <CssBaseline />
        <MemoryRouter initialEntries={[route]}>
          {children}
        </MemoryRouter>
      </ThemeProvider>
    </Provider>
  );

  return {
    store,
    ...render(ui, { wrapper: Wrapper, ...renderOptions }),
  };
};
