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

      <div className="mb-4 grid grid-cols-1 gap-3 sm:flex sm:flex-wrap">
        <select value={vehicleId} onChange={(e) => { setVehicleId(e.target.value); setPage(1); }} className="input sm:w-48">
          <option value="">All Vehicles</option>
          {vehicles.map((v) => <option key={v.id} value={v.id}>{v.label}</option>)}
        </select>
        <input type="date" value={startDate} onChange={(e) => { setStartDate(e.target.value); setPage(1); }} className="input sm:w-auto" />
        <input type="date" value={endDate} onChange={(e) => { setEndDate(e.target.value); setPage(1); }} className="input sm:w-auto" />
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
          {/* Desktop table */}
          <div className="card hidden overflow-x-auto md:block">
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
                  <th className="px-3 py-2 font-medium text-text-secondary" title="Paperless sync status">Sync</th>
                </tr>
              </thead>
              <tbody>
                {data.items.map((f) => (
                  <tr key={f.id} onClick={() => navigate({ to: "/fill-ups/$id", params: { id: f.id } })} className="cursor-pointer border-t border-border hover:bg-accent-subtle">
                    <td className="whitespace-nowrap px-3 py-2">{f.date}</td>
                    <td className="whitespace-nowrap px-3 py-2">{f.vehicleLabel}</td>
                    <td className="whitespace-nowrap px-3 py-2">{f.stationName}</td>
                    <td className="whitespace-nowrap px-3 py-2">{f.gallons.toFixed(3)}</td>
                    <td className="whitespace-nowrap px-3 py-2">${f.pricePerGallon.toFixed(3)}</td>
                    <td className="whitespace-nowrap px-3 py-2">${f.totalCost.toFixed(2)}</td>
                    <td className="whitespace-nowrap px-3 py-2">{f.odometerMiles.toLocaleString()}</td>
                    <td className="whitespace-nowrap px-3 py-2">{f.mpg?.toFixed(1) ?? "\u2014"}</td>
                    <td className="whitespace-nowrap px-3 py-2"><SyncIcon status={f.paperlessSyncStatus} /></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {/* Mobile card layout */}
          <div className="space-y-3 md:hidden">
            {data.items.map((f) => (
              <div
                key={f.id}
                onClick={() => navigate({ to: "/fill-ups/$id", params: { id: f.id } })}
                className="fill-up-card cursor-pointer active:bg-surface-hover"
              >
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-1.5">
                    <span className="font-medium">{f.date}</span>
                    <SyncIcon status={f.paperlessSyncStatus} />
                  </div>
                  <span className="text-sm font-semibold">${f.totalCost.toFixed(2)}</span>
                </div>
                <div className="fill-up-card-row">
                  <span className="fill-up-card-label">{f.vehicleLabel}</span>
                  <span>{f.stationName}</span>
                </div>
                <div className="fill-up-card-row">
                  <span className="fill-up-card-label">{f.gallons.toFixed(3)} gal @ ${f.pricePerGallon.toFixed(3)}</span>
                  <span>{f.mpg?.toFixed(1) ?? "\u2014"} mpg</span>
                </div>
                <div className="fill-up-card-row text-xs text-text-muted">
                  <span>{f.odometerMiles.toLocaleString()} mi</span>
                </div>
              </div>
            ))}
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
  return <th onClick={onClick} className="cursor-pointer select-none whitespace-nowrap px-3 py-2 font-medium text-text-secondary hover:text-accent-text">{children}</th>;
}

function SyncIcon({ status }: { status: string }) {
  if (status === "none") return null;
  if (status === "pending") return <span title="Syncing to Paperless" className="inline-block h-2.5 w-2.5 rounded-full bg-yellow-400" />;
  if (status === "synced") return <svg aria-label="Synced to Paperless" role="img" className="inline-block h-4 w-4 text-green-500" viewBox="0 0 20 20" fill="currentColor"><path fillRule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clipRule="evenodd" /></svg>;
  if (status === "failed") return <svg aria-label="Paperless sync failed" role="img" className="inline-block h-4 w-4 text-red-500" viewBox="0 0 20 20" fill="currentColor"><path fillRule="evenodd" d="M8.257 3.099c.765-1.36 2.722-1.36 3.486 0l5.58 9.92c.75 1.334-.213 2.98-1.742 2.98H4.42c-1.53 0-2.493-1.646-1.743-2.98l5.58-9.92zM11 13a1 1 0 11-2 0 1 1 0 012 0zm-1-8a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1z" clipRule="evenodd" /></svg>;
  return null;
}
