import { describe, it, expect } from "vitest";
import { render, screen } from "@testing-library/react";
import Loader from "../../components/common/Loader/Loader";
import { ThemeProvider } from "@mui/material";
import { theme } from "../../theme";

const renderLoader = (props = {}) =>
  render(
    <ThemeProvider theme={theme}>
      <Loader {...props} />
    </ThemeProvider>
  );

describe("Loader", () => {
  it("renders without crashing", () => {
    renderLoader();
    expect(document.body).toBeTruthy();
  });

  it("renders with fullScreen prop", () => {
    const { container } = renderLoader({ fullScreen: true });
    expect(container.firstChild).toBeTruthy();
  });
});
