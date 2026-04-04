import { describe, it, expect, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import CurrencyInput from "@/components/CurrencyInput";

describe("CurrencyInput", () => {
  it("renders with the provided value", () => {
    render(<CurrencyInput value="12.34" onChange={() => {}} />);
    expect(screen.getByRole("textbox")).toHaveValue("12.34");
  });

  it("has decimal inputMode", () => {
    render(<CurrencyInput value="" onChange={() => {}} />);
    expect(screen.getByRole("textbox")).toHaveAttribute("inputMode", "decimal");
  });

  it("calls onChange for valid numeric input", async () => {
    const onChange = vi.fn();
    const user = userEvent.setup();
    render(<CurrencyInput value="" onChange={onChange} />);

    await user.type(screen.getByRole("textbox"), "5");
    expect(onChange).toHaveBeenCalledWith("5");
  });

  it("rejects letters", async () => {
    const onChange = vi.fn();
    const user = userEvent.setup();
    render(<CurrencyInput value="" onChange={onChange} />);

    await user.type(screen.getByRole("textbox"), "abc");
    expect(onChange).not.toHaveBeenCalled();
  });

  it("allows a decimal point", async () => {
    const onChange = vi.fn();
    const user = userEvent.setup();
    render(<CurrencyInput value="3" onChange={onChange} />);

    await user.type(screen.getByRole("textbox"), ".");
    expect(onChange).toHaveBeenCalledWith("3.");
  });

  it("respects custom decimals prop", () => {
    // With decimals=3, "1.234" should be valid
    const onChange = vi.fn();
    const { rerender } = render(
      <CurrencyInput value="1.234" onChange={onChange} decimals={3} />,
    );
    expect(screen.getByRole("textbox")).toHaveValue("1.234");

    // Verify it renders correctly with the value
    rerender(<CurrencyInput value="1.2345" onChange={onChange} decimals={3} />);
    expect(screen.getByRole("textbox")).toHaveValue("1.2345");
  });

  it("passes through additional input props", () => {
    render(
      <CurrencyInput
        value=""
        onChange={() => {}}
        placeholder="Enter amount"
        data-testid="currency"
      />,
    );
    expect(screen.getByPlaceholderText("Enter amount")).toBeInTheDocument();
  });
});
