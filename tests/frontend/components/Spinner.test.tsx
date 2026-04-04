import { describe, it, expect } from "vitest";
import { render } from "@testing-library/react";
import Spinner from "@/components/Spinner";

describe("Spinner", () => {
  it("renders with default classes", () => {
    const { container } = render(<Spinner />);
    const wrapper = container.firstElementChild as HTMLElement;
    expect(wrapper).toHaveClass("flex", "items-center", "justify-center");
  });

  it("applies custom className", () => {
    const { container } = render(<Spinner className="mt-8" />);
    const wrapper = container.firstElementChild as HTMLElement;
    expect(wrapper.className).toContain("mt-8");
  });

  it("renders the spinning element", () => {
    const { container } = render(<Spinner />);
    const spinner = container.querySelector(".animate-spin");
    expect(spinner).toBeInTheDocument();
  });
});
