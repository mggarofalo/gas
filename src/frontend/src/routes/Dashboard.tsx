import { useQuery } from "@tanstack/react-query";
import { Link } from "@tanstack/react-router";
import {
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
} from "recharts";
import { apiFetch } from "@/lib/api";
import type { Stats, FillUp, Vehicle } from "@/lib/types";
import Spinner from "@/components/Spinner";

function fmt$(n: number | null | undefined): string {
  if (n == null) return "--";
  return `$${n.toFixed(2)}`;
}

function fmtNum(n: number | null | undefined, decimals = 1): string {
  if (n == null) return "--";
  return n.toFixed(decimals);
}

export default function Dashboard() {
  const { data: stats, isLoading: statsLoading } = useQuery({
    queryKey: ["stats"],
    queryFn: () => apiFetch<Stats>("/api/stats"),
  });

  const { data: vehicles } = useQuery({
    queryKey: ["vehicles"],
    queryFn: () => apiFetch<Vehicle[]>("/api/vehicles"),
  });

  const { data: recentFillUps, isLoading: fillUpsLoading } = useQuery({
    queryKey: ["fill-ups", "recent"],
    queryFn: () =>
      apiFetch<{ items: FillUp[] }>(
        "/api/fill-ups?page=1&pageSize=5&sortBy=date&sortDir=desc"
      ).then((r) => r.items),
  });

  // Fetch more fill-ups for charts (last 20)
  const { data: chartFillUps } = useQuery({
    queryKey: ["fill-ups", "chart"],
    queryFn: () =>
      apiFetch<{ items: FillUp[] }>(
        "/api/fill-ups?page=1&pageSize=20&sortBy=date&sortDir=asc"
      ).then((r) => r.items),
  });

  const mpgData = (chartFillUps ?? [])
    .filter((f) => f.mpg != null)
    .map((f) => ({
      date: new Date(f.date).toLocaleDateString("en-US", { month: "short", day: "numeric" }),
      mpg: f.mpg!,
    }));

  const priceData = (chartFillUps ?? []).map((f) => ({
    date: new Date(f.date).toLocaleDateString("en-US", { month: "short", day: "numeric" }),
    price: f.pricePerGallon,
  }));

  if (statsLoading || fillUpsLoading) {
    return <Spinner className="mt-20" />;
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-gray-900">Dashboard</h1>
        <Link
          to="/fill-ups/new"
          className="rounded-lg bg-blue-600 px-4 py-2 text-sm font-medium text-white shadow-sm hover:bg-blue-700"
        >
          New Fill-Up
        </Link>
      </div>

      {/* Stats cards */}
      <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
        <StatCard label="Total Fill-Ups" value={stats?.totalFillUps?.toString() ?? "--"} />
        <StatCard label="Total Gallons" value={fmtNum(stats?.totalGallons, 1)} />
        <StatCard label="Total Cost" value={fmt$(stats?.totalCost)} />
        <StatCard label="Total Miles" value={fmtNum(stats?.totalMiles, 0)} />
        <StatCard label="Avg MPG" value={fmtNum(stats?.avgMpg)} />
        <StatCard label="Avg Price/Gal" value={fmt$(stats?.avgPricePerGallon)} />
        <StatCard label="Avg Cost/Fill-Up" value={fmt$(stats?.avgCostPerFillUp)} />
        <StatCard label="Cost/Mile" value={fmt$(stats?.costPerMile)} />
      </div>

      {/* Charts */}
      <div className="grid gap-6 lg:grid-cols-2">
        {mpgData.length > 1 && (
          <div className="rounded-xl bg-white p-6 shadow-sm">
            <h2 className="mb-4 text-lg font-semibold text-gray-900">MPG Trend</h2>
            <ResponsiveContainer width="100%" height={250}>
              <LineChart data={mpgData}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey="date" tick={{ fontSize: 12 }} />
                <YAxis tick={{ fontSize: 12 }} />
                <Tooltip />
                <Line
                  type="monotone"
                  dataKey="mpg"
                  stroke="#2563eb"
                  strokeWidth={2}
                  dot={{ r: 3 }}
                />
              </LineChart>
            </ResponsiveContainer>
          </div>
        )}

        {priceData.length > 1 && (
          <div className="rounded-xl bg-white p-6 shadow-sm">
            <h2 className="mb-4 text-lg font-semibold text-gray-900">Price per Gallon</h2>
            <ResponsiveContainer width="100%" height={250}>
              <LineChart data={priceData}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey="date" tick={{ fontSize: 12 }} />
                <YAxis tick={{ fontSize: 12 }} tickFormatter={(v) => `$${v}`} />
                <Tooltip formatter={(value) => [`$${Number(value).toFixed(3)}`, "Price"]} />
                <Line
                  type="monotone"
                  dataKey="price"
                  stroke="#16a34a"
                  strokeWidth={2}
                  dot={{ r: 3 }}
                />
              </LineChart>
            </ResponsiveContainer>
          </div>
        )}
      </div>

      {/* Recent fill-ups */}
      <div className="rounded-xl bg-white p-6 shadow-sm">
        <div className="mb-4 flex items-center justify-between">
          <h2 className="text-lg font-semibold text-gray-900">Recent Fill-Ups</h2>
          <Link to="/fill-ups" className="text-sm font-medium text-blue-600 hover:text-blue-700">
            View all
          </Link>
        </div>

        {recentFillUps && recentFillUps.length > 0 ? (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b text-left text-gray-500">
                  <th className="pb-2 pr-4 font-medium">Date</th>
                  <th className="pb-2 pr-4 font-medium">Vehicle</th>
                  <th className="pb-2 pr-4 font-medium">Station</th>
                  <th className="pb-2 pr-4 text-right font-medium">Gallons</th>
                  <th className="pb-2 pr-4 text-right font-medium">Total</th>
                  <th className="pb-2 text-right font-medium">MPG</th>
                </tr>
              </thead>
              <tbody>
                {recentFillUps.map((f) => (
                  <tr key={f.id} className="border-b last:border-0">
                    <td className="py-2 pr-4">
                      <Link
                        to="/fill-ups/$fillUpId"
                        params={{ fillUpId: f.id }}
                        className="text-blue-600 hover:underline"
                      >
                        {new Date(f.date).toLocaleDateString()}
                      </Link>
                    </td>
                    <td className="py-2 pr-4">{f.vehicleLabel}</td>
                    <td className="py-2 pr-4 max-w-[150px] truncate">{f.stationName}</td>
                    <td className="py-2 pr-4 text-right">{f.gallons.toFixed(3)}</td>
                    <td className="py-2 pr-4 text-right">{fmt$(f.totalCost)}</td>
                    <td className="py-2 text-right">{fmtNum(f.mpg)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ) : (
          <p className="text-sm text-gray-500">No fill-ups yet.</p>
        )}
      </div>

      {/* Vehicles summary */}
      {vehicles && vehicles.length > 0 && (
        <div className="rounded-xl bg-white p-6 shadow-sm">
          <h2 className="mb-4 text-lg font-semibold text-gray-900">Vehicles</h2>
          <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
            {vehicles
              .filter((v) => v.isActive)
              .map((v) => (
                <div key={v.id} className="rounded-lg border p-4">
                  <p className="font-medium text-gray-900">{v.label}</p>
                  {v.octaneRating && (
                    <p className="text-sm text-gray-500">{v.octaneRating} octane</p>
                  )}
                </div>
              ))}
          </div>
        </div>
      )}
    </div>
  );
}

function StatCard({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-xl bg-white p-4 shadow-sm">
      <p className="text-sm text-gray-500">{label}</p>
      <p className="mt-1 text-2xl font-bold text-gray-900">{value}</p>
    </div>
  );
}
