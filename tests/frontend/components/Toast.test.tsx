import { describe, it, expect, vi } from "vitest";
import { render, screen, act } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { ToastProvider, useToast } from "@/components/Toast";

function TestConsumer() {
  const { toast } = useToast();
  return (
    <div>
      <button onClick={() => toast("Hello")}>Show Info</button>
      <button onClick={() => toast("Success!", "success")}>Show Success</button>
      <button onClick={() => toast("Error!", "error")}>Show Error</button>
    </div>
  );
}

describe("Toast", () => {
  it("throws when useToast is used outside provider", () => {
    // Suppress console.error for expected error
    const spy = vi.spyOn(console, "error").mockImplementation(() => {});
    expect(() => render(<TestConsumer />)).toThrow(
      "useToast must be used within ToastProvider",
    );
    spy.mockRestore();
  });

  it("renders a toast when triggered", async () => {
    const user = userEvent.setup();
    render(
      <ToastProvider>
        <TestConsumer />
      </ToastProvider>,
    );

    await user.click(screen.getByText("Show Info"));
    expect(screen.getByText("Hello")).toBeInTheDocument();
  });

  it("applies correct color for success toasts", async () => {
    const user = userEvent.setup();
    render(
      <ToastProvider>
        <TestConsumer />
      </ToastProvider>,
    );

    await user.click(screen.getByText("Show Success"));
    const toast = screen.getByText("Success!").closest("div");
    expect(toast?.className).toContain("bg-green-600");
  });

  it("applies correct color for error toasts", async () => {
    const user = userEvent.setup();
    render(
      <ToastProvider>
        <TestConsumer />
      </ToastProvider>,
    );

    await user.click(screen.getByText("Show Error"));
    const toast = screen.getByText("Error!").closest("div");
    expect(toast?.className).toContain("bg-red-600");
  });

  it("dismisses toast on close button click", async () => {
    const user = userEvent.setup();
    render(
      <ToastProvider>
        <TestConsumer />
      </ToastProvider>,
    );

    await user.click(screen.getByText("Show Info"));
    expect(screen.getByText("Hello")).toBeInTheDocument();

    // Click the × close button
    const closeButton = screen.getByText("×");
    await user.click(closeButton);
    expect(screen.queryByText("Hello")).not.toBeInTheDocument();
  });

  it("auto-dismisses after 4 seconds", () => {
    vi.useFakeTimers();

    const { getByText, queryByText } = render(
      <ToastProvider>
        <TestConsumer />
      </ToastProvider>,
    );

    // Click button synchronously — fake timers are active
    act(() => {
      getByText("Show Info").click();
    });
    expect(getByText("Hello")).toBeInTheDocument();

    act(() => {
      vi.advanceTimersByTime(4000);
    });

    expect(queryByText("Hello")).not.toBeInTheDocument();
    vi.useRealTimers();
  });
});
