import { describe, it, expect } from "vitest";
import { render, screen } from "@testing-library/react";
import EmptyState from "@/components/EmptyState";

describe("EmptyState", () => {
  it("renders the title", () => {
    render(<EmptyState title="No items" />);
    expect(screen.getByText("No items")).toBeInTheDocument();
  });

  it("renders optional message", () => {
    render(<EmptyState title="No items" message="Try adding one" />);
    expect(screen.getByText("Try adding one")).toBeInTheDocument();
  });

  it("does not render message when not provided", () => {
    const { container } = render(<EmptyState title="No items" />);
    const paragraphs = container.querySelectorAll("p");
    expect(paragraphs).toHaveLength(0);
  });

  it("renders optional action", () => {
    render(<EmptyState title="No items" action={<button>Add</button>} />);
    expect(screen.getByRole("button", { name: "Add" })).toBeInTheDocument();
  });

  it("does not render action wrapper when not provided", () => {
    const { container } = render(<EmptyState title="No items" />);
    // Only the svg and h3 should be inside, no action div
    const headings = container.querySelectorAll("h3");
    expect(headings).toHaveLength(1);
  });
});
