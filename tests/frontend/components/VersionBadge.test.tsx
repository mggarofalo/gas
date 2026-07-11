import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";

const mockApiFetch = vi.fn();

vi.mock("@/lib/api", () => ({
  apiFetch: (...args: unknown[]) => mockApiFetch(...args),
}));

const { default: VersionBadge } = await import("@/components/VersionBadge");

function renderBadge() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false, gcTime: 0 } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <VersionBadge />
    </QueryClientProvider>,
  );
}

beforeEach(() => {
  mockApiFetch.mockReset();
});

describe("VersionBadge", () => {
  it("shows the release version from /health", async () => {
    mockApiFetch.mockResolvedValue({
      status: "Healthy",
      version: "1.28.0",
      commit: "abc1234",
    });

    renderBadge();

    await waitFor(() =>
      expect(screen.getByText("Gas Tracker v1.28.0")).toBeInTheDocument(),
    );
    expect(mockApiFetch).toHaveBeenCalledWith("/health");
    expect(screen.getByText("Gas Tracker v1.28.0")).toHaveAttribute(
      "title",
      "Commit abc1234",
    );
  });

  it("shows dev without a v prefix for local builds", async () => {
    mockApiFetch.mockResolvedValue({ status: "Healthy", version: "dev", commit: null });

    renderBadge();

    await waitFor(() =>
      expect(screen.getByText("Gas Tracker dev")).toBeInTheDocument(),
    );
    expect(screen.getByText("Gas Tracker dev")).not.toHaveAttribute("title");
  });

  it("renders nothing when the health call fails", async () => {
    mockApiFetch.mockRejectedValue(new Error("network down"));

    const { container } = renderBadge();

    await waitFor(() => expect(mockApiFetch).toHaveBeenCalled());
    expect(container).toBeEmptyDOMElement();
  });
});
