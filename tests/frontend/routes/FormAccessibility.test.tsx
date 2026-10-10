import type { ReactNode } from "react";
import { describe, it, expect, vi } from "vitest";
import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { ToastProvider } from "@/components/Toast";
import NewFillUp from "@/routes/NewFillUp";
import EditFillUp from "@/routes/EditFillUp";
import Vehicles from "@/routes/Vehicles";
import YnabSettings from "@/routes/YnabSettings";
import YnabImports from "@/routes/YnabImports";
import FillUps from "@/routes/FillUps";
import ChangePassword from "@/routes/ChangePassword";

vi.mock("@tanstack/react-router", () => ({
  useNavigate: () => vi.fn(),
  useParams: () => ({ fillUpId: "fill-up-1" }),
  Link: ({ children, to }: { children: ReactNode; to: string }) => (
    <a href={to}>{children}</a>
  ),
}));

vi.mock("@/lib/api", () => ({
  apiFetch: vi.fn(() => Promise.resolve([])),
  mustResetPassword: () => false,
}));

const vehicle = {
  id: "vehicle-1", year: 2025, make: "Honda", model: "Civic",
  notes: null, octaneRating: 87, isActive: true, label: "2025 Honda Civic",
};

function renderForm(children: ReactNode) {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false, staleTime: Infinity } },
  });
  client.setQueryData(["vehicles"], [vehicle]);
  client.setQueryData(["vehicles", "all"], [vehicle]);
  client.setQueryData(["ynab-config"], {
    configured: true, enabled: true, planId: "plan-1", accountId: "account-1",
    categoryId: "category-1", maskedToken: "****",
  });
  client.setQueryData(["ynab-plans"], [{ id: "plan-1", name: "Budget" }]);
  client.setQueryData(["ynab-accounts", "plan-1"], []);
  client.setQueryData(["ynab-categories", "plan-1"], []);
  client.setQueryData(["ynab-accounts-cached"], []);
  client.setQueryData(["ynab-categories-cached"], []);
  client.setQueryData(["fill-up", "fill-up-1"], {
    id: "fill-up-1", vehicleId: vehicle.id, date: "2026-10-10",
    odometerMiles: 1000, gallons: 10, pricePerGallon: 3.5,
    stationName: "Station", notes: null, latitude: null, longitude: null,
    octaneRating: 87, stationAddress: null, receiptUrl: "/receipts/existing.pdf",
  });
  client.setQueryData(["fill-ups", 1, "", "", ""], { items: [], totalCount: 0 });
  client.setQueryData(["ynab-imports", 1, "pending"], {
    items: [{
      id: "import-1", date: "2026-10-10", payeeName: "Station",
      amountMilliunits: -35000, status: "pending", gallons: 10,
      pricePerGallon: 3.5, octaneRating: 87, odometerMiles: 1000,
      vehicleId: vehicle.id, vehicleName: vehicle.label, memo: null,
    }],
    totalCount: 1,
  });
  return render(
    <QueryClientProvider client={client}>
      <ToastProvider>{children}</ToastProvider>
    </QueryClientProvider>,
  );
}

// Check the rendered DOM, including composite inputs and implicit labels.
function auditLabels(container: HTMLElement) {
  const labels = container.querySelectorAll<HTMLLabelElement>("label");
  expect(labels.length).toBeGreaterThan(0);
  for (const label of labels) {
    expect(label.control, label.textContent ?? "").not.toBeNull();
    expect(label.control).toHaveAccessibleName(label.textContent?.trim());
  }
  for (const control of container.querySelectorAll("input, select, textarea")) {
    expect(control).toHaveAccessibleName();
  }
  const ids = [...container.querySelectorAll("[id]")].map((el) => el.id);
  expect(new Set(ids).size).toBe(ids.length);
}

describe("form accessibility", () => {
  it("names all new fill-up fields, calculated output, and GPS actions", async () => {
    const user = userEvent.setup();
    const { container } = renderForm(<NewFillUp />);
    auditLabels(container);
    expect(screen.getByLabelText("Gallons")).toHaveProperty("tagName", "OUTPUT");
    expect(screen.getByRole("group", { name: "GPS Location" })).toBeInTheDocument();

    await user.click(screen.getByText("Odometer (miles)"));
    expect(screen.getByLabelText("Odometer (miles)")).toHaveFocus();
    await user.type(screen.getByLabelText("Price/Gallon"), "3500");
    await user.type(screen.getByLabelText("Total Price"), "3500");
    expect(screen.getByLabelText("Gallons")).toHaveTextContent("10.000");
  });

  it("names all edit fill-up fields", () => {
    const { container } = renderForm(<EditFillUp />);
    auditLabels(container);
    expect(screen.getByRole("group", { name: "GPS Location" })).toBeInTheDocument();
    expect(screen.getByLabelText("Receipt (optional)")).toHaveAccessibleDescription(
      "A receipt is already attached — uploading a new one replaces it.",
    );
  });

  it("keeps add and edit vehicle labels attached to their own controls", async () => {
    const user = userEvent.setup();
    const { container } = renderForm(<Vehicles />);
    await user.click(screen.getByRole("button", { name: "Add Vehicle" }));
    await user.click(screen.getByRole("button", { name: "Edit" }));
    auditLabels(container);
    const forms = container.querySelectorAll("form");
    expect(forms).toHaveLength(2);
    const addMake = within(forms[0]).getByLabelText("Make");
    const editMake = within(forms[1]).getByLabelText("Make");
    await user.click(within(forms[1]).getByText("Make"));
    expect(editMake).toHaveFocus();
    expect(addMake).not.toHaveFocus();
  });

  it("names YNAB configuration, token, and sync toggle", async () => {
    const user = userEvent.setup();
    const { container } = renderForm(<YnabSettings />);
    await user.click(screen.getByRole("button", { name: "Update Token" }));
    auditLabels(container);
    expect(screen.getByRole("checkbox", { name: "Enable YNAB sync" })).toBeChecked();
    await user.click(screen.getByText("Enable YNAB sync"));
    expect(screen.getByRole("checkbox", { name: "Enable YNAB sync" })).not.toBeChecked();
  });

  it("names the YNAB import editor fields", async () => {
    const user = userEvent.setup();
    const { container } = renderForm(<YnabImports />);
    await user.click(screen.getByRole("button", { name: "Edit" }));
    auditLabels(container);
  });

  it("names vehicle and both date filters", () => {
    const { container } = renderForm(<FillUps />);
    auditLabels(container);
    expect(screen.getByLabelText("From date")).toHaveAttribute("type", "date");
    expect(screen.getByLabelText("To date")).toHaveAttribute("type", "date");
  });

  it("retains the existing password label associations", () => {
    const { container } = renderForm(<ChangePassword />);
    auditLabels(container);
  });
});
