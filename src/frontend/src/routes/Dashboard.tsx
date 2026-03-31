import { useState } from "react";
import { Link } from "@tanstack/react-router";
import { useQuery } from "@tanstack/react-query";
import {
  LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, Legend,
} from "recharts";
import { apiFetch } from "../lib/api";
import { EmptyState } from "../components/EmptyState";
import type { Vehicle, FillUpPage, Stats } from "../lib/types";

const COLORS = ["#60a5fa", "#4ade80", "#f87171", "#a78bfa", "#fb923c"];

export function DashboardPage() {
  const [vehicleId, setVehicleId] = useState("");
  const { data: vehicles = [] } = useQuery({ queryKey: ["vehicles"], queryFn: () => apiFetch<Vehicle[]>("/vehicles") });

  const params = new URLSearchParams();
  if (vehicleId) params.set("vehicleId", vehicleId);

  const { data: stats } = useQuery({ queryKey: ["stats", vehicleId], queryFn: () => apiFetch<Stats>(`/stats?${params}`) });
  const { data: recentPage } = useQuery({ queryKey: ["fill-ups-recent", vehicleId], queryFn: () => apiFetch<FillUpPage>(`/fill-ups?pageSize=5&sortBy=date&sortDir=desc${vehicleId ? `&vehicleId=${vehicleId}` : ""}`) });
  const { data: chartPage } = useQuery({ queryKey: ["fill-ups-chart", vehicleId], queryFn: () => apiFetch<FillUpPage>(`/fill-ups?pageSize=100&sortBy=date&sortDir=asc${vehicleId ? `&vehicleId=${vehicleId}` : ""}`) });

  const chartData = (chartPage?.items ?? []).map((f) => ({ date: f.date, mpg: f.mpg, price: f.pricePerGallon, vehicle: f.vehicleLabel }));
  const vehicleNames = [...new Set(chartData.map((d) => d.vehicle))];
  const mpgData = chartData.filter((d) => d.mpg != null).map((d) => {
    const point: Record<string, string | number | null> = { date: d.date };
    vehicleNames.forEach((v) => { point[v] = d.vehicle === v ? d.mpg : null; });
    return point;
  });
  const priceData = chartData.map((d) => ({ date: d.date, price: d.price }));

  return (
    <div>
      <div className="mb-4 flex items-center justify-between">
        <h2 className="text-2xl font-semibold">Dashboard</h2>
        <select value={vehicleId} onChange={(e) => setVehicleId(e.target.value)} className="input w-48">
          <option value="">All Vehicles</option>
          {vehicles.map((v) => <option key={v.id} value={v.id}>{v.label}</option>)}
        </select>
      </div>

      {stats && (
        <div className="mb-6 grid grid-cols-2 gap-4 lg:grid-cols-4">
          <StatCard label="Total Spend" value={`$${stats.totalCost.toFixed(2)}`} />
          <StatCard label="Avg MPG" value={stats.avgMpg?.toFixed(1) ?? "\u2014"} />
          <StatCard label="Fill-Ups" value={String(stats.totalFillUps)} />
          <StatCard label="$/Mile" value={stats.costPerMile != null ? `$${stats.costPerMile.toFixed(2)}` : "\u2014"} />
        </div>
      )}

      {mpgData.length > 1 && (
        <div className="card mb-6 p-4">
          <h3 className="mb-2 text-sm font-medium text-text-secondary">MPG Over Time</h3>
          <ResponsiveContainer width="100%" height={250}>
            <LineChart data={mpgData}>
              <CartesianGrid strokeDasharray="3 3" stroke="var(--color-border)" />
              <XAxis dataKey="date" tick={{ fontSize: 11, fill: "var(--color-text-secondary)" }} />
              <YAxis tick={{ fontSize: 11, fill: "var(--color-text-secondary)" }} />
              <Tooltip contentStyle={{ backgroundColor: "var(--color-surface-raised)", border: "1px solid var(--color-border)", color: "var(--color-text-primary)" }} />
              <Legend />
              {vehicleNames.map((name, i) => <Line key={name} type="monotone" dataKey={name} stroke={COLORS[i % COLORS.length]} connectNulls dot={{ r: 3 }} />)}
            </LineChart>
          </ResponsiveContainer>
        </div>
      )}

      {priceData.length > 1 && (
        <div className="card mb-6 p-4">
          <h3 className="mb-2 text-sm font-medium text-text-secondary">Price per Gallon</h3>
          <ResponsiveContainer width="100%" height={250}>
            <LineChart data={priceData}>
              <CartesianGrid strokeDasharray="3 3" stroke="var(--color-border)" />
              <XAxis dataKey="date" tick={{ fontSize: 11, fill: "var(--color-text-secondary)" }} />
              <YAxis tick={{ fontSize: 11, fill: "var(--color-text-secondary)" }} domain={["auto", "auto"]} />
              <Tooltip contentStyle={{ backgroundColor: "var(--color-surface-raised)", border: "1px solid var(--color-border)", color: "var(--color-text-primary)" }} />
              <Line type="monotone" dataKey="price" stroke="#60a5fa" dot={{ r: 3 }} />
            </LineChart>
          </ResponsiveContainer>
        </div>
      )}

      {recentPage && recentPage.items.length > 0 && (
        <div className="card">
          <h3 className="border-b border-border px-4 py-2 text-sm font-medium text-text-secondary">Recent Fill-Ups</h3>
          <table className="table-striped w-full text-left text-sm">
            <tbody>
              {recentPage.items.map((f) => (
                <tr key={f.id} className="border-t border-border hover:bg-surface-hover">
                  <td className="px-4 py-2">{f.date}</td>
                  <td className="px-4 py-2">{f.vehicleLabel}</td>
                  <td className="px-4 py-2">{f.stationName}</td>
                  <td className="px-4 py-2">{f.gallons.toFixed(1)} gal</td>
                  <td className="px-4 py-2">${f.totalCost.toFixed(2)}</td>
                  <td className="px-4 py-2"><Link to="/fill-ups/$id" params={{ id: f.id }} className="link">View</Link></td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {(!stats || stats.totalFillUps === 0) && (
        <EmptyState
          icon={<svg xmlns="http://www.w3.org/2000/svg" className="h-8 w-8" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={1.5}><path strokeLinecap="round" strokeLinejoin="round" d="M12 4.5v15m7.5-7.5h-15" /></svg>}
          message="No fill-ups recorded yet."
        >
          <Link to="/fill-ups/new" className="link">Add your first fill-up</Link>
        </EmptyState>
      )}
    </div>
  );
}

function StatCard({ label, value }: { label: string; value: string }) {
  return <div className="stat-card"><p className="text-xs font-medium text-text-muted">{label}</p><p className="mt-1 text-2xl font-semibold">{value}</p></div>;
}
