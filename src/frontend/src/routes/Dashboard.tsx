import { useState } from "react";
import { Link } from "@tanstack/react-router";
import { useQuery } from "@tanstack/react-query";
import {
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  Legend,
} from "recharts";
import { apiFetch } from "../lib/api";
import type { Vehicle, FillUpPage, Stats } from "../lib/types";

const COLORS = ["#2563eb", "#16a34a", "#dc2626", "#9333ea", "#ea580c"];

export function DashboardPage() {
  const [vehicleId, setVehicleId] = useState("");

  const { data: vehicles = [] } = useQuery({
    queryKey: ["vehicles"],
    queryFn: () => apiFetch<Vehicle[]>("/vehicles"),
  });

  const params = new URLSearchParams();
  if (vehicleId) params.set("vehicleId", vehicleId);

  const { data: stats } = useQuery({
    queryKey: ["stats", vehicleId],
    queryFn: () => apiFetch<Stats>(`/stats?${params}`),
  });

  const { data: recentPage } = useQuery({
    queryKey: ["fill-ups-recent", vehicleId],
    queryFn: () =>
      apiFetch<FillUpPage>(
        `/fill-ups?pageSize=5&sortBy=date&sortDir=desc${vehicleId ? `&vehicleId=${vehicleId}` : ""}`,
      ),
  });

  // Fetch more data for charts
  const { data: chartPage } = useQuery({
    queryKey: ["fill-ups-chart", vehicleId],
    queryFn: () =>
      apiFetch<FillUpPage>(
        `/fill-ups?pageSize=100&sortBy=date&sortDir=asc${vehicleId ? `&vehicleId=${vehicleId}` : ""}`,
      ),
  });

  const chartData = (chartPage?.items ?? []).map((f) => ({
    date: f.date,
    mpg: f.mpg,
    price: f.pricePerGallon,
    vehicle: f.vehicleLabel,
  }));

  // Group by vehicle for multi-line charts
  const vehicleNames = [...new Set(chartData.map((d) => d.vehicle))];

  const mpgData = chartData
    .filter((d) => d.mpg != null)
    .map((d) => {
      const point: Record<string, string | number | null> = { date: d.date };
      vehicleNames.forEach((v) => {
        point[v] = d.vehicle === v ? d.mpg : null;
      });
      return point;
    });

  const priceData = chartData.map((d) => ({
    date: d.date,
    price: d.price,
  }));

  return (
    <div>
      <div className="mb-4 flex items-center justify-between">
        <h2 className="text-2xl font-semibold">Dashboard</h2>
        <select
          value={vehicleId}
          onChange={(e) => setVehicleId(e.target.value)}
          className="input w-48"
        >
          <option value="">All Vehicles</option>
          {vehicles.map((v) => (
            <option key={v.id} value={v.id}>{v.label}</option>
          ))}
        </select>
      </div>

      {/* Stat Cards */}
      {stats && (
        <div className="mb-6 grid grid-cols-2 gap-4 lg:grid-cols-4">
          <StatCard label="Total Spend" value={`$${stats.totalCost.toFixed(2)}`} />
          <StatCard label="Avg MPG" value={stats.avgMpg?.toFixed(1) ?? "\u2014"} />
          <StatCard label="Fill-Ups" value={String(stats.totalFillUps)} />
          <StatCard label="$/Mile" value={stats.costPerMile != null ? `$${stats.costPerMile.toFixed(2)}` : "\u2014"} />
        </div>
      )}

      {/* Charts */}
      {mpgData.length > 1 && (
        <div className="mb-6 rounded border bg-white p-4">
          <h3 className="mb-2 text-sm font-medium text-gray-600">MPG Over Time</h3>
          <ResponsiveContainer width="100%" height={250}>
            <LineChart data={mpgData}>
              <CartesianGrid strokeDasharray="3 3" />
              <XAxis dataKey="date" tick={{ fontSize: 11 }} />
              <YAxis tick={{ fontSize: 11 }} />
              <Tooltip />
              <Legend />
              {vehicleNames.map((name, i) => (
                <Line
                  key={name}
                  type="monotone"
                  dataKey={name}
                  stroke={COLORS[i % COLORS.length]}
                  connectNulls
                  dot={{ r: 3 }}
                />
              ))}
            </LineChart>
          </ResponsiveContainer>
        </div>
      )}

      {priceData.length > 1 && (
        <div className="mb-6 rounded border bg-white p-4">
          <h3 className="mb-2 text-sm font-medium text-gray-600">Price per Gallon</h3>
          <ResponsiveContainer width="100%" height={250}>
            <LineChart data={priceData}>
              <CartesianGrid strokeDasharray="3 3" />
              <XAxis dataKey="date" tick={{ fontSize: 11 }} />
              <YAxis tick={{ fontSize: 11 }} domain={["auto", "auto"]} />
              <Tooltip />
              <Line type="monotone" dataKey="price" stroke="#2563eb" dot={{ r: 3 }} />
            </LineChart>
          </ResponsiveContainer>
        </div>
      )}

      {/* Recent Fill-Ups */}
      {recentPage && recentPage.items.length > 0 && (
        <div className="rounded border bg-white">
          <h3 className="border-b px-4 py-2 text-sm font-medium text-gray-600">
            Recent Fill-Ups
          </h3>
          <table className="w-full text-left text-sm">
            <tbody>
              {recentPage.items.map((f) => (
                <tr key={f.id} className="border-t hover:bg-blue-50">
                  <td className="px-4 py-2">{f.date}</td>
                  <td className="px-4 py-2">{f.vehicleLabel}</td>
                  <td className="px-4 py-2">{f.stationName}</td>
                  <td className="px-4 py-2">{f.gallons.toFixed(1)} gal</td>
                  <td className="px-4 py-2">${f.totalCost.toFixed(2)}</td>
                  <td className="px-4 py-2">
                    <Link
                      to="/fill-ups/$id"
                      params={{ id: f.id }}
                      className="text-blue-600 hover:underline"
                    >
                      View
                    </Link>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {(!stats || stats.totalFillUps === 0) && (
        <div className="rounded border bg-white p-8 text-center text-gray-500">
          <p className="mb-2">No fill-ups recorded yet.</p>
          <Link
            to="/fill-ups/new"
            className="text-blue-600 hover:underline"
          >
            Add your first fill-up
          </Link>
        </div>
      )}
    </div>
  );
}

function StatCard({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded border bg-white p-4">
      <p className="text-xs font-medium text-gray-500">{label}</p>
      <p className="mt-1 text-2xl font-semibold">{value}</p>
    </div>
  );
}
