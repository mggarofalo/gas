import { useState } from "react";
import { useParams, useNavigate } from "@tanstack/react-router";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { apiFetch } from "../lib/api";
import type { FillUp } from "../lib/types";

export function FillUpDetailPage() {
  const { id } = useParams({ from: "/fill-ups/$id" });
  const navigate = useNavigate();
  const qc = useQueryClient();
  const [confirmDelete, setConfirmDelete] = useState(false);

  const { data: fillUp, isLoading } = useQuery({
    queryKey: ["fill-up", id],
    queryFn: () => apiFetch<FillUp>(`/fill-ups/${id}`),
  });

  const deleteMut = useMutation({
    mutationFn: () => apiFetch<void>(`/fill-ups/${id}`, { method: "DELETE" }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["fill-ups"] });
      navigate({ to: "/fill-ups" });
    },
  });

  if (isLoading) return <p className="text-gray-500">Loading...</p>;
  if (!fillUp) return <p className="text-gray-500">Fill-up not found.</p>;

  return (
    <div className="mx-auto max-w-xl">
      <div className="mb-4 flex items-center justify-between">
        <h2 className="text-2xl font-semibold">Fill-Up Detail</h2>
        <div className="flex gap-2">
          <button
            onClick={() => navigate({ to: "/fill-ups" })}
            className="rounded border px-3 py-1.5 text-sm hover:bg-gray-50"
          >
            Back
          </button>
          {!confirmDelete ? (
            <button
              onClick={() => setConfirmDelete(true)}
              className="rounded bg-red-600 px-3 py-1.5 text-sm text-white hover:bg-red-700"
            >
              Delete
            </button>
          ) : (
            <div className="flex gap-1">
              <button
                onClick={() => deleteMut.mutate()}
                className="rounded bg-red-600 px-3 py-1.5 text-sm text-white hover:bg-red-700"
              >
                Confirm Delete
              </button>
              <button
                onClick={() => setConfirmDelete(false)}
                className="rounded border px-3 py-1.5 text-sm hover:bg-gray-50"
              >
                Cancel
              </button>
            </div>
          )}
        </div>
      </div>

      <div className="space-y-4 rounded border bg-white p-6">
        <Row label="Vehicle" value={fillUp.vehicleLabel} />
        <Row label="Date" value={fillUp.date} />
        <Row label="Station" value={fillUp.stationName} />
        {fillUp.stationAddress && <Row label="Address" value={fillUp.stationAddress} />}
        <Row label="Odometer" value={`${fillUp.odometerMiles.toLocaleString()} mi`} />

        <div className="grid grid-cols-3 gap-4">
          <Row label="Gallons" value={fillUp.gallons.toFixed(3)} />
          <Row label="$/Gallon" value={`$${fillUp.pricePerGallon.toFixed(3)}`} />
          <Row label="Total" value={`$${fillUp.totalCost.toFixed(2)}`} />
        </div>

        {fillUp.tripMiles != null && (
          <div className="grid grid-cols-3 gap-4">
            <Row label="Trip Miles" value={fillUp.tripMiles.toLocaleString()} />
            <Row label="MPG" value={fillUp.mpg?.toFixed(1) ?? "\u2014"} />
            <Row label="$/Mile" value={fillUp.costPerMile != null ? `$${fillUp.costPerMile.toFixed(2)}` : "\u2014"} />
          </div>
        )}

        {(fillUp.latitude != null && fillUp.longitude != null) && (
          <Row label="GPS" value={`${fillUp.latitude}, ${fillUp.longitude}`} />
        )}

        {fillUp.notes && <Row label="Notes" value={fillUp.notes} />}

        {/* Receipt */}
        {fillUp.receiptUrl && (
          <div>
            <span className="text-xs font-medium text-gray-500">Receipt</span>
            <img
              src={fillUp.receiptUrl}
              alt="Receipt"
              className="mt-1 max-h-64 rounded border object-contain"
            />
          </div>
        )}

        {/* Sync status */}
        {fillUp.paperlessSyncStatus !== "none" && (
          <SyncBadge status={fillUp.paperlessSyncStatus} fillUpId={fillUp.id} />
        )}
      </div>
    </div>
  );
}

function Row({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <span className="text-xs font-medium text-gray-500">{label}</span>
      <p className="text-sm">{value}</p>
    </div>
  );
}

function SyncBadge({ status, fillUpId }: { status: string; fillUpId: string }) {
  const qc = useQueryClient();
  const resyncMut = useMutation({
    mutationFn: () => apiFetch<void>(`/fill-ups/${fillUpId}/resync`, { method: "POST" }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["fill-up", fillUpId] }),
  });

  const colors: Record<string, string> = {
    pending: "bg-yellow-100 text-yellow-800",
    synced: "bg-green-100 text-green-800",
    failed: "bg-red-100 text-red-800",
  };

  const labels: Record<string, string> = {
    pending: "Syncing...",
    synced: "Synced to Paperless",
    failed: "Sync failed",
  };

  return (
    <div className="flex items-center gap-2">
      <span className={`rounded px-2 py-0.5 text-xs font-medium ${colors[status] ?? ""}`}>
        {labels[status] ?? status}
      </span>
      {status === "failed" && (
        <button
          onClick={() => resyncMut.mutate()}
          disabled={resyncMut.isPending}
          className="text-xs text-blue-600 hover:underline"
        >
          Retry
        </button>
      )}
    </div>
  );
}
