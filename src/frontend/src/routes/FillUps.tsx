import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Link } from "@tanstack/react-router";
import { apiFetch } from "@/lib/api";
import type { FillUpPage, Vehicle } from "@/lib/types";
import Spinner from "@/components/Spinner";
import EmptyState from "@/components/EmptyState";

export default function FillUps() {
  const [page, setPage] = useState(1);
  const [vehicleId, setVehicleId] = useState("");
  const [dateFrom, setDateFrom] = useState("");
  const [dateTo, setDateTo] = useState("");
  const pageSize = 15;

  const params = new URLSearchParams({
    page: page.toString(),
    pageSize: pageSize.toString(),
    sortBy: "date",
    sortDir: "desc",
  });
  if (vehicleId) params.set("vehicleId", vehicleId);
  if (dateFrom) params.set("dateFrom", dateFrom);
  if (dateTo) params.set("dateTo", dateTo);

  const { data: vehicles } = useQuery({
    queryKey: ["vehicles"],
    queryFn: () => apiFetch<Vehicle[]>("/api/vehicles"),
  });

  const { data, isLoading } = useQuery({
    queryKey: ["fill-ups", page, vehicleId, dateFrom, dateTo],
    queryFn: () => apiFetch<FillUpPage>(`/api/fill-ups?${params}`),
  });

  const totalPages = data ? Math.ceil(data.totalCount / pageSize) : 0;

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <h1 className="text-2xl font-bold text-gray-900 dark:text-gray-100">Fill-Ups</h1>
        <Link
          to="/fill-ups/new"
          className="w-full rounded-lg bg-blue-600 dark:bg-blue-500 px-4 py-2 text-center text-sm font-medium text-white shadow-sm dark:shadow-gray-900/30 hover:bg-blue-700 dark:hover:bg-blue-600 sm:w-auto"
        >
          New Fill-Up
        </Link>
      </div>

      {/* Filters */}
      <div className="flex flex-col gap-3 sm:flex-row sm:flex-wrap">
        <select
          value={vehicleId}
          onChange={(e) => {
            setVehicleId(e.target.value);
            setPage(1);
          }}
          className="rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 px-3 py-2 text-sm text-gray-900 dark:text-gray-100 focus:border-blue-500 focus:ring-1 focus:ring-blue-500 focus:outline-none"
        >
          <option value="">All Vehicles</option>
          {vehicles?.map((v) => (
            <option key={v.id} value={v.id}>
              {v.label}
            </option>
          ))}
        </select>

        <input
          type="date"
          value={dateFrom}
          onChange={(e) => {
            setDateFrom(e.target.value);
            setPage(1);
          }}
          placeholder="From"
          className="rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 px-3 py-2 text-sm text-gray-900 dark:text-gray-100 focus:border-blue-500 focus:ring-1 focus:ring-blue-500 focus:outline-none"
        />
        <input
          type="date"
          value={dateTo}
          onChange={(e) => {
            setDateTo(e.target.value);
            setPage(1);
          }}
          placeholder="To"
          className="rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 px-3 py-2 text-sm text-gray-900 dark:text-gray-100 focus:border-blue-500 focus:ring-1 focus:ring-blue-500 focus:outline-none"
        />

        {(vehicleId || dateFrom || dateTo) && (
          <button
            onClick={() => {
              setVehicleId("");
              setDateFrom("");
              setDateTo("");
              setPage(1);
            }}
            className="rounded-lg border border-gray-300 dark:border-gray-600 px-3 py-2 text-sm text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700"
          >
            Clear Filters
          </button>
        )}
      </div>

      {isLoading ? (
        <Spinner className="mt-10" />
      ) : !data || data.items.length === 0 ? (
        <EmptyState
          title="No fill-ups found"
          message="Record your first fill-up to get started."
          action={
            <Link
              to="/fill-ups/new"
              className="rounded-lg bg-blue-600 dark:bg-blue-500 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700 dark:hover:bg-blue-600"
            >
              New Fill-Up
            </Link>
          }
        />
      ) : (
        <>
          {/* Desktop table */}
          <div className="hidden overflow-x-auto rounded-xl bg-white dark:bg-gray-800 shadow-sm dark:shadow-gray-900/30 md:block">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-gray-200 dark:border-gray-700 text-left text-gray-500 dark:text-gray-400">
                  <th className="p-4 font-medium">Date</th>
                  <th className="p-4 font-medium">Vehicle</th>
                  <th className="p-4 font-medium">Station</th>
                  <th className="p-4 text-right font-medium">Odometer</th>
                  <th className="p-4 text-right font-medium">Gallons</th>
                  <th className="p-4 text-right font-medium">$/Gal</th>
                  <th className="p-4 text-right font-medium">Total</th>
                  <th className="p-4 text-right font-medium">MPG</th>
                </tr>
              </thead>
              <tbody className="text-gray-900 dark:text-gray-100">
                {data.items.map((f) => (
                  <tr key={f.id} className="border-b border-gray-200 dark:border-gray-700 last:border-0 hover:bg-gray-50 dark:hover:bg-gray-700/50">
                    <td className="p-4">
                      <Link
                        to="/fill-ups/$fillUpId"
                        params={{ fillUpId: f.id }}
                        className="text-blue-600 hover:underline dark:text-blue-400"
                      >
                        {new Date(f.date).toLocaleDateString()}
                      </Link>
                    </td>
                    <td className="p-4">{f.vehicleLabel}</td>
                    <td className="max-w-[200px] truncate p-4">{f.stationName}</td>
                    <td className="p-4 text-right">{f.odometerMiles.toLocaleString()}</td>
                    <td className="p-4 text-right">{f.gallons.toFixed(3)}</td>
                    <td className="p-4 text-right">${f.pricePerGallon.toFixed(3)}</td>
                    <td className="p-4 text-right">${f.totalCost.toFixed(2)}</td>
                    <td className="p-4 text-right">{f.mpg?.toFixed(1) ?? "--"}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {/* Mobile cards */}
          <div className="space-y-3 md:hidden">
            {data.items.map((f) => (
              <Link
                key={f.id}
                to="/fill-ups/$fillUpId"
                params={{ fillUpId: f.id }}
                className="block rounded-xl bg-white dark:bg-gray-800 p-4 shadow-sm dark:shadow-gray-900/30"
              >
                <div className="flex items-start justify-between">
                  <div>
                    <p className="font-medium text-gray-900 dark:text-gray-100">
                      {new Date(f.date).toLocaleDateString()}
                    </p>
                    <p className="text-sm text-gray-500 dark:text-gray-400">{f.vehicleLabel}</p>
                    <p className="text-sm text-gray-500 dark:text-gray-400">{f.stationName}</p>
                  </div>
                  <div className="text-right">
                    <p className="font-semibold text-gray-900 dark:text-gray-100">${f.totalCost.toFixed(2)}</p>
                    <p className="text-sm text-gray-500 dark:text-gray-400">{f.gallons.toFixed(3)} gal</p>
                    {f.mpg && (
                      <p className="text-sm text-gray-500 dark:text-gray-400">{f.mpg.toFixed(1)} mpg</p>
                    )}
                  </div>
                </div>
              </Link>
            ))}
          </div>

          {/* Pagination */}
          {totalPages > 1 && (
            <div className="flex items-center justify-between">
              <p className="text-sm text-gray-500 dark:text-gray-400">
                {data.totalCount} fill-up{data.totalCount !== 1 ? "s" : ""}
              </p>
              <div className="flex gap-2">
                <button
                  disabled={page <= 1}
                  onClick={() => setPage((p) => p - 1)}
                  className="rounded-lg border border-gray-200 dark:border-gray-600 px-3 py-1.5 text-sm text-gray-700 dark:text-gray-300 disabled:opacity-50"
                >
                  Previous
                </button>
                <span className="flex items-center px-3 text-sm text-gray-500 dark:text-gray-400">
                  {page} / {totalPages}
                </span>
                <button
                  disabled={page >= totalPages}
                  onClick={() => setPage((p) => p + 1)}
                  className="rounded-lg border border-gray-200 dark:border-gray-600 px-3 py-1.5 text-sm text-gray-700 dark:text-gray-300 disabled:opacity-50"
                >
                  Next
                </button>
              </div>
            </div>
          )}
        </>
      )}
    </div>
  );
}
