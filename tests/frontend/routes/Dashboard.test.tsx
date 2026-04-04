import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import type { ReactNode } from "react";

const mockApiFetch = vi.fn();

// Mock matchMedia for ThemeProvider
Object.defineProperty(window, "matchMedia", {
  writable: true,
  value: vi.fn().mockImplementation((query: string) => ({
    matches: false,
    media: query,
    onchange: null,
    addListener: vi.fn(),
    removeListener: vi.fn(),
    addEventListener: vi.fn(),
    removeEventListener: vi.fn(),
    dispatchEvent: vi.fn(),
  })),
});

vi.mock("@tanstack/react-router", () => ({
  Link: ({ children, to, ...props }: { children: ReactNode; to: string; [k: string]: unknown }) => (
    <a href={to} {...props}>{children}</a>
  ),
}));

vi.mock("@/lib/api", () => ({
  apiFetch: (...args: unknown[]) => mockApiFetch(...args),
}));

// Mock recharts to avoid jsdom SVG rendering issues
vi.mock("recharts", () => ({
  LineChart: ({ children }: { children: ReactNode }) => <div data-testid="line-chart">{children}</div>,
  Line: () => <div />,
  XAxis: () => <div />,
  YAxis: () => <div />,
  CartesianGrid: () => <div />,
  Tooltip: () => <div />,
  ResponsiveContainer: ({ children }: { children: ReactNode }) => <div>{children}</div>,
}));

const { default: Dashboard } = await import("@/routes/Dashboard");
const { ThemeProvider } = await import("@/components/ThemeProvider");

function renderDashboard() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false, gcTime: 0 } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <ThemeProvider>
        <Dashboard />
      </ThemeProvider>
    </QueryClientProvider>,
  );
}

const stats = {
  totalFillUps: 15,
  totalGallons: 180.5,
  totalCost: 523.75,
  totalMiles: 4500,
  avgMpg: 28.3,
  avgPricePerGallon: 2.9,
  avgCostPerFillUp: 34.92,
  costPerMile: 0.12,
};

const recentFillUps = {
  items: [
    {
      id: "1",
      vehicleId: "v1",
      vehicleLabel: "2020 Honda Civic",
      date: "2024-01-15",
      odometerMiles: 50000,
      gallons: 12.5,
      pricePerGallon: 3.29,
      totalCost: 41.13,
      octaneRating: 87,
      stationName: "Shell",
      stationAddress: null,
      latitude: null,
      longitude: null,
      receiptUrl: null,
      tripMiles: 350,
      mpg: 28.0,
      costPerMile: 0.12,
      paperlessSyncStatus: "none",
      paperlessSyncedAt: null,
      paperlessSyncError: null,
      ynabSyncStatus: "none",
      ynabAccountId: null,
      ynabAccountName: null,
      ynabCategoryId: null,
      ynabCategoryName: null,
      notes: null,
      createdAt: "2024-01-15T10:00:00Z",
    },
  ],
  page: 1,
  pageSize: 5,
  totalCount: 1,
};

const vehicles = [
  {
    id: "v1",
    year: 2020,
    make: "Honda",
    model: "Civic",
    notes: null,
    octaneRating: 87,
    isActive: true,
    label: "2020 Honda Civic",
    createdAt: "2023-01-01",
    updatedAt: "2023-01-01",
  },
];

describe("Dashboard", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("shows loading spinner while data is fetching", () => {
    mockApiFetch.mockReturnValue(new Promise(() => {}));
    renderDashboard();

    expect(document.querySelector(".animate-spin")).toBeInTheDocument();
  });

  it("renders stat cards when data loads", async () => {
    mockApiFetch.mockImplementation((url: string) => {
      if (url.includes("/stats")) return Promise.resolve(stats);
      if (url.includes("/vehicles")) return Promise.resolve(vehicles);
      return Promise.resolve({ items: [] });
    });

    renderDashboard();

    await waitFor(() => {
      expect(screen.getByText("Dashboard")).toBeInTheDocument();
    });

    expect(screen.getByText("Total Fill-Ups")).toBeInTheDocument();
    expect(screen.getByText("15")).toBeInTheDocument();
    expect(screen.getByText("Avg MPG")).toBeInTheDocument();
    expect(screen.getByText("28.3")).toBeInTheDocument();
    expect(screen.getByText("$523.75")).toBeInTheDocument();
    expect(screen.getByText("$0.12")).toBeInTheDocument();
  });

  it("renders recent fill-ups table", async () => {
    mockApiFetch.mockImplementation((url: string) => {
      if (url.includes("/stats")) return Promise.resolve(stats);
      if (url.includes("/vehicles")) return Promise.resolve(vehicles);
      if (url.includes("sortDir=desc")) return Promise.resolve(recentFillUps);
      return Promise.resolve({ items: [] });
    });

    renderDashboard();

    await waitFor(() => {
      expect(screen.getByText("Recent Fill-Ups")).toBeInTheDocument();
    });

    expect(screen.getByText("Shell")).toBeInTheDocument();
    expect(screen.getByText("12.500")).toBeInTheDocument();
    expect(screen.getByText("$41.13")).toBeInTheDocument();
  });

  it("shows 'No fill-ups yet.' when no data", async () => {
    mockApiFetch.mockImplementation((url: string) => {
      if (url.includes("/stats"))
        return Promise.resolve({
          totalFillUps: 0,
          totalGallons: 0,
          totalCost: 0,
          totalMiles: 0,
          avgMpg: null,
          avgPricePerGallon: null,
          avgCostPerFillUp: null,
          costPerMile: null,
        });
      if (url.includes("/vehicles")) return Promise.resolve([]);
      return Promise.resolve({ items: [] });
    });

    renderDashboard();

    await waitFor(() => {
      expect(screen.getByText("No fill-ups yet.")).toBeInTheDocument();
    });
  });

  it("shows '--' for null stat values", async () => {
    mockApiFetch.mockImplementation((url: string) => {
      if (url.includes("/stats"))
        return Promise.resolve({
          ...stats,
          avgMpg: null,
          costPerMile: null,
        });
      if (url.includes("/vehicles")) return Promise.resolve([]);
      return Promise.resolve({ items: [] });
    });

    renderDashboard();

    await waitFor(() => {
      expect(screen.getByText("Dashboard")).toBeInTheDocument();
    });

    // Null avgMpg and costPerMile render as "--"
    const dashes = screen.getAllByText("--");
    expect(dashes.length).toBeGreaterThanOrEqual(2);
  });
});
