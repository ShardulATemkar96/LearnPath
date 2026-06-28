import { describe, it, expect } from "vitest";
import { render, screen } from "@testing-library/react";
import EmptyState from "../../components/common/EmptyState/EmptyState";
import { ThemeProvider } from "@mui/material";
import { theme } from "../../theme";

describe("EmptyState", () => {
  it("renders title correctly", () => {
    render(
      <ThemeProvider theme={theme}>
        <EmptyState title="No data found" />
      </ThemeProvider>
    );
    expect(screen.getByText("No data found")).toBeInTheDocument();
  });

  it("renders description when provided", () => {
    render(
      <ThemeProvider theme={theme}>
        <EmptyState
          title="No data"
          description="Try adding some items"
        />
      </ThemeProvider>
    );
    expect(screen.getByText("Try adding some items")).toBeInTheDocument();
  });

  it("does not render description when not provided", () => {
    render(
      <ThemeProvider theme={theme}>
        <EmptyState title="No data" />
      </ThemeProvider>
    );
    expect(screen.queryByText("Try adding some items")).toBeNull();
  });
});
