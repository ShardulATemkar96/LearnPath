import { describe, it, expect } from "vitest";
import { screen } from "@testing-library/react";
import { renderWithProviders } from "../renderWithProviders";
import StatCard from "../../components/dashboard/StatCard/StatCard";
import { CheckCircleRounded } from "@mui/icons-material";

describe("StatCard", () => {
  const defaultProps = {
    title:    "Test Metric",
    value:    42,
    icon:     <CheckCircleRounded />,
    gradient: "linear-gradient(135deg, #6C63FF, #9D97FF)",
  };

  it("renders title and value", () => {
    renderWithProviders(<StatCard {...defaultProps} />);
    expect(screen.getByText("Test Metric")).toBeInTheDocument();
    expect(screen.getByText("42")).toBeInTheDocument();
  });

  it("renders subtitle when provided", () => {
    renderWithProviders(
      <StatCard {...defaultProps} subtitle="This week" />
    );
    expect(screen.getByText("This week")).toBeInTheDocument();
  });

  it("renders positive trend", () => {
    renderWithProviders(
      <StatCard
        {...defaultProps}
        trend={{ value: 15, label: "this week" }}
      />
    );
    expect(screen.getByText(/\+15%/)).toBeInTheDocument();
  });

  it("renders negative trend", () => {
    renderWithProviders(
      <StatCard
        {...defaultProps}
        trend={{ value: -5, label: "this week" }}
      />
    );
    expect(screen.getByText(/-5%/)).toBeInTheDocument();
  });
});
