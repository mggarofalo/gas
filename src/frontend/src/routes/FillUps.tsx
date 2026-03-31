import { useState } from "react";
import { Link, useNavigate } from "@tanstack/react-router";
import { useQuery } from "@tanstack/react-query";
import { apiFetch } from "../lib/api";
import { Spinner } from "../components/Spinner";
import { EmptyState } from "../components/EmptyState";
import type { Vehicle, FillUpPage } from "../lib/types";

export function FillUpsPage() {
  const navigate = useNavigate();
  const [vehicleId, setVehicleId] = useState("");
  const [startDate, setStartDate] = useState("");
  const [endDate, setEndDate] = useState("");
  const [page, setPage] = useState(1);
  const [sortBy, setSortBy] = useState("date");
  const [sortDir, setSortDir] = useState("desc");
  const pageSize = 25;

  const { data: vehicles = [] } = useQuery({ queryKey: ["vehicles"], queryFn: () => apiFetch<Vehicle[]>("/vehicles") });

  const params = new URLSearchParams();
  if (vehicleId) params.set("vehicleId", vehicleId);
  if (startDate) params.set("startDate", startDate);
  if (endDate) params.set("endDate", endDate);
  params.set("page", String(page));
  params.set("pageSize", String(pageSize));
  params.set("sortBy", sortBy);
  params.set("sortDir", sortDir);

  const { data, isLoading } = useQuery({ queryKey: ["fill-ups", vehicleId, startDate, endDate, page, sortBy, sortDir], queryFn: () => apiFetch<FillUpPage>(`/fill-ups?${params}`) });
  const totalPages = data ? Math.ceil(data.totalCount / pageSize) : 0;

  const toggleSort = (col: string) => {
    if (sortBy === col) setSortDir((d) => (d === "asc" ? "desc" : "asc"));
    else { setSortBy(col); setSortDir("desc"); }
    setPage(1);
  };
  const sortIndicator = (col: string) => sortBy === col ? (sortDir === "asc" ? " \u25b2" : " \u25bc") : "";

  return (
    <div>
      <div className="mb-4 flex items-center justify-between">
        <h2 className="text-2xl font-semibold">Fill-Up History</h2>
        <Link to="/fill-ups/new" className="btn-primary">Add Fill-Up</Link>
      </div>

      <div className="mb-4 flex flex-wrap gap-3">
        <select value={vehicleId} onChange={(e) => { setVehicleId(e.target.value); setPage(1); }} className="input w-48">
          <option value="">All Vehicles</option>
          {vehicles.map((v) => <option key={v.id} value={v.id}>{v.label}</option>)}
        </select>
        <input type="date" value={startDate} onChange={(e) => { setStartDate(e.target.value); setPage(1); }} className="input w-auto" />
        <input type="date" value={endDate} onChange={(e) => { setEndDate(e.target.value); setPage(1); }} className="input w-auto" />
      </div>

      {isLoading ? <Spinner /> : !data || data.items.length === 0 ? (
        <EmptyState
          icon={<svg xmlns="http://www.w3.org/2000/svg" className="h-8 w-8" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={1.5}><path strokeLinecap="round" strokeLinejoin="round" d="M19.5 14.25v-2.625a3.375 3.375 0 0 0-3.375-3.375h-1.5A1.125 1.125 0 0 1 13.5 7.125v-1.5a3.375 3.375 0 0 0-3.375-3.375H8.25m2.25 0H5.625c-.621 0-1.125.504-1.125 1.125v17.25c0 .621.504 1.125 1.125 1.125h12.75c.621 0 1.125-.504 1.125-1.125V11.25a9 9 0 0 0-9-9Z" /></svg>}
          message="No fill-ups found."
        >
          <Link to="/fill-ups/new" className="link">Add a fill-up</Link>
        </EmptyState>
      ) : (
        <>
          <div className="card overflow-x-auto">
            <table className="table-striped w-full text-left text-sm">
              <thead className="bg-surface-hover/50">
                <tr>
                  <Th onClick={() => toggleSort("date")}>Date{sortIndicator("date")}</Th>
                  <th className="px-3 py-2 font-medium text-text-secondary">Vehicle</th>
                  <th className="px-3 py-2 font-medium text-text-secondary">Station</th>
                  <Th onClick={() => toggleSort("gallons")}>Gallons{sortIndicator("gallons")}</Th>
                  <th className="px-3 py-2 font-medium text-text-secondary">$/Gal</th>
                  <Th onClick={() => toggleSort("total")}>Total{sortIndicator("total")}</Th>
                  <Th onClick={() => toggleSort("odometer")}>Odometer{sortIndicator("odometer")}</Th>
                  <th className="px-3 py-2 font-medium text-text-secondary">MPG</th>
                </tr>
              </thead>
              <tbody>
                {data.items.map((f) => (
                  <tr key={f.id} onClick={() => navigate({ to: "/fill-ups/$id", params: { id: f.id } })} className="cursor-pointer border-t border-border hover:bg-accent-subtle">
                    <td className="px-3 py-2">{f.date}</td>
                    <td className="px-3 py-2">{f.vehicleLabel}</td>
                    <td className="px-3 py-2">{f.stationName}</td>
                    <td className="px-3 py-2">{f.gallons.toFixed(3)}</td>
                    <td className="px-3 py-2">${f.pricePerGallon.toFixed(3)}</td>
                    <td className="px-3 py-2">${f.totalCost.toFixed(2)}</td>
                    <td className="px-3 py-2">{f.odometerMiles.toLocaleString()}</td>
                    <td className="px-3 py-2">{f.mpg?.toFixed(1) ?? "\u2014"}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <div className="mt-3 flex items-center justify-between text-sm text-text-secondary">
            <span>{data.totalCount} fill-ups</span>
            <div className="flex gap-2">
              <button onClick={() => setPage((p) => Math.max(1, p - 1))} disabled={page <= 1} className="btn-outline disabled:opacity-40">Prev</button>
              <span className="px-2 py-1">{page} / {totalPages}</span>
              <button onClick={() => setPage((p) => Math.min(totalPages, p + 1))} disabled={page >= totalPages} className="btn-outline disabled:opacity-40">Next</button>
            </div>
          </div>
        </>
      )}
    </div>
  );
}

function Th({ onClick, children }: { onClick: () => void; children: React.ReactNode }) {
  return <th onClick={onClick} className="cursor-pointer select-none px-3 py-2 font-medium text-text-secondary hover:text-accent-text">{children}</th>;
}
