import { describe, it, expect, vi } from "vitest";
import { useState } from "react";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import CurrencyInput from "@/components/CurrencyInput";

/** Renders the input as a controlled field so typing accumulates like it does in a form. */
function Harness({
  decimals,
  initial = "",
  onValue,
}: {
  decimals?: number;
  initial?: string;
  onValue?: (v: string) => void;
}) {
  const [value, setValue] = useState(initial);
  return (
    <CurrencyInput
      value={value}
      decimals={decimals}
      onChange={(v) => {
        setValue(v);
        onValue?.(v);
      }}
    />
  );
}

describe("CurrencyInput", () => {
  it("renders the stored value padded to the required decimals", () => {
    render(<CurrencyInput value="12.3" onChange={() => {}} />);
    expect(screen.getByRole("textbox")).toHaveValue("12.30");
  });

  it("uses the numeric keypad on mobile", () => {
    render(<CurrencyInput value="" onChange={() => {}} />);
    expect(screen.getByRole("textbox")).toHaveAttribute("inputMode", "numeric");
  });

  it("progressively masks three decimals as digits are typed", async () => {
    const user = userEvent.setup();
    const onValue = vi.fn();
    render(<Harness decimals={3} onValue={onValue} />);
    const input = screen.getByRole("textbox");

    await user.type(input, "3");
    expect(input).toHaveValue("0.003");
    await user.type(input, "4");
    expect(input).toHaveValue("0.034");
    await user.type(input, "5");
    expect(input).toHaveValue("0.345");
    await user.type(input, "9");
    expect(input).toHaveValue("3.459");
    expect(onValue).toHaveBeenLastCalledWith("3.459");
  });

  it("progressively masks two decimals with thousands separators", async () => {
    const user = userEvent.setup();
    const onValue = vi.fn();
    render(<Harness decimals={2} onValue={onValue} />);
    const input = screen.getByRole("textbox");

    await user.type(input, "123456");
    expect(input).toHaveValue("1,234.56");
    expect(onValue).toHaveBeenLastCalledWith("1234.56");
  });

  it("masks whole numbers with commas for the odometer", async () => {
    const user = userEvent.setup();
    const onValue = vi.fn();
    render(<Harness decimals={0} onValue={onValue} />);
    const input = screen.getByRole("textbox");

    await user.type(input, "123456");
    expect(input).toHaveValue("123,456");
    expect(onValue).toHaveBeenLastCalledWith("123456");
  });

  it("drops a digit from the right on backspace", async () => {
    const user = userEvent.setup();
    render(<Harness decimals={2} initial="1234.56" />);
    const input = screen.getByRole("textbox");
    expect(input).toHaveValue("1,234.56");

    await user.click(input);
    await user.keyboard("{Backspace}");
    expect(input).toHaveValue("123.45");
    await user.keyboard("{Backspace}");
    expect(input).toHaveValue("12.34");
  });

  it("empties out once every digit is deleted", async () => {
    const user = userEvent.setup();
    const onValue = vi.fn();
    render(<Harness decimals={2} initial="0.05" onValue={onValue} />);
    const input = screen.getByRole("textbox");

    await user.click(input);
    await user.keyboard("{Backspace}");
    expect(input).toHaveValue("");
    expect(onValue).toHaveBeenLastCalledWith("");
  });

  it("ignores letters and stray punctuation", async () => {
    const user = userEvent.setup();
    render(<Harness decimals={2} />);
    const input = screen.getByRole("textbox");

    await user.type(input, "a.b$c");
    expect(input).toHaveValue("");

    await user.type(input, "4a2");
    expect(input).toHaveValue("0.42");
  });

  it("appends at the end when tapped mid-number", async () => {
    const user = userEvent.setup();
    render(<Harness decimals={2} initial="12.34" />);
    const input = screen.getByRole("textbox") as HTMLInputElement;

    input.setSelectionRange(1, 1); // as if the tap landed between digits
    await user.click(input);
    await user.type(input, "5");

    expect(input).toHaveValue("123.45");
    expect(input.selectionStart).toBe(input.value.length);
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
