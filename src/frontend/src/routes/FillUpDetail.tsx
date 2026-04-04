import { useState, useEffect } from "react";
import { useParams, useNavigate } from "@tanstack/react-router";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { apiFetch } from "../lib/api";
import { Spinner } from "../components/Spinner";
import { EmptyState } from "../components/EmptyState";
import type { FillUp } from "../lib/types";

export function FillUpDetailPage() {
  const { id } = useParams({ from: "/authenticated/fill-ups/$id" });
  const navigate = useNavigate();
  const qc = useQueryClient();
  const [confirmDelete, setConfirmDelete] = useState(false);

  const { data: fillUp, isLoading } = useQuery({ queryKey: ["fill-up", id], queryFn: () => apiFetch<FillUp>(`/fill-ups/${id}`) });
  const deleteMut = useMutation({
    mutationFn: () => apiFetch<void>(`/fill-ups/${id}`, { method: "DELETE" }),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ["fill-ups"] }); navigate({ to: "/fill-ups" }); },
  });

  if (isLoading) return <Spinner />;
  if (!fillUp) return <EmptyState message="Fill-up not found." />;

  return (
    <div className="mx-auto max-w-xl">
      <div className="mb-4 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <h2 className="text-2xl font-semibold">Fill-Up Detail</h2>
        <div className="flex gap-2">
          <button onClick={() => navigate({ to: "/fill-ups" })} className="btn-outline">Back</button>
          <button onClick={() => navigate({ to: "/fill-ups/$id/edit", params: { id } })} className="btn-primary">Edit</button>
          {!confirmDelete ? (
            <button onClick={() => setConfirmDelete(true)} className="btn-danger">Delete</button>
          ) : (
            <div className="flex gap-1">
              <button onClick={() => deleteMut.mutate()} className="btn-danger">Confirm</button>
              <button onClick={() => setConfirmDelete(false)} className="btn-outline">Cancel</button>
            </div>
          )}
        </div>
      </div>

      <div className="card space-y-4 p-4 sm:p-6">
        <Row label="Vehicle" value={fillUp.vehicleLabel} />
        <Row label="Date" value={fillUp.date} />
        <Row label="Station" value={fillUp.stationName} />
        {fillUp.stationAddress && <Row label="Address" value={fillUp.stationAddress} />}
        <Row label="Odometer" value={`${fillUp.odometerMiles.toLocaleString()} mi`} />
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
          <Row label="Gallons" value={fillUp.gallons.toFixed(3)} />
          <Row label="$/Gallon" value={`$${fillUp.pricePerGallon.toFixed(3)}`} />
          <Row label="Total" value={`$${fillUp.totalCost.toFixed(2)}`} />
        </div>
        {fillUp.octaneRating != null && <Row label="Octane" value={String(fillUp.octaneRating)} />}
        {fillUp.tripMiles != null && (
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
            <Row label="Trip Miles" value={fillUp.tripMiles.toLocaleString()} />
            <Row label="MPG" value={fillUp.mpg?.toFixed(1) ?? "\u2014"} />
            <Row label="$/Mile" value={fillUp.costPerMile != null ? `$${fillUp.costPerMile.toFixed(2)}` : "\u2014"} />
          </div>
        )}
        {fillUp.latitude != null && fillUp.longitude != null && <Row label="GPS" value={`${fillUp.latitude}, ${fillUp.longitude}`} />}
        {fillUp.notes && <Row label="Notes" value={fillUp.notes} />}
        {fillUp.receiptUrl && (
          <div><span className="text-xs font-medium text-text-muted">Receipt</span><img src={fillUp.receiptUrl} alt="Receipt" className="mt-1 max-h-64 rounded border border-border object-contain" /></div>
        )}
        {fillUp.paperlessSyncStatus !== "none" && <SyncBadge label="Paperless" status={fillUp.paperlessSyncStatus} syncedAt={fillUp.paperlessSyncedAt} syncError={fillUp.paperlessSyncError} retryEndpoint={`/fill-ups/${fillUp.id}/resync`} fillUpId={fillUp.id} />}
        {fillUp.ynabSyncStatus !== "none" && <SyncBadge label="YNAB" status={fillUp.ynabSyncStatus} retryEndpoint={`/fill-ups/${fillUp.id}/ynab-sync`} fillUpId={fillUp.id} />}
      </div>
    </div>
  );
}

function Row({ label, value }: { label: string; value: string }) {
  return <div><span className="text-xs font-medium text-text-muted">{label}</span><p className="text-sm">{value}</p></div>;
}

function formatRelativeTime(iso: string, now: number): string {
  const diff = now - new Date(iso).getTime();
  const mins = Math.floor(diff / 60000);
  if (mins < 1) return "just now";
  if (mins < 60) return `${mins}m ago`;
  const hrs = Math.floor(mins / 60);
  if (hrs < 24) return `${hrs}h ago`;
  const days = Math.floor(hrs / 24);
  return `${days}d ago`;
}

function useNow(intervalMs = 30000) {
  const [now, setNow] = useState(Date.now());
  useEffect(() => { const id = setInterval(() => setNow(Date.now()), intervalMs); return () => clearInterval(id); }, [intervalMs]);
  return now;
}

function SyncBadge({ label, status, syncedAt, syncError, retryEndpoint, fillUpId }: { label: string; status: string; syncedAt?: string | null; syncError?: string | null; retryEndpoint: string; fillUpId: string }) {
  const qc = useQueryClient();
  const now = useNow();
  const resyncMut = useMutation({
    mutationFn: () => apiFetch<void>(retryEndpoint, { method: "POST" }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["fill-up", fillUpId] }),
  });
  const badgeClass: Record<string, string> = { pending: "badge-warning", synced: "badge-success", failed: "badge-danger" };
  const syncedLabel = syncedAt ? `Synced to ${label} \u2014 ${formatRelativeTime(syncedAt, now)}` : `Synced to ${label}`;
  const labels: Record<string, string> = { pending: "Syncing...", synced: syncedLabel, failed: `${label} sync failed` };
  return (
    <div>
      <div className="flex items-center gap-2">
        <span className={badgeClass[status] ?? ""}>{labels[status] ?? status}</span>
        {status === "failed" && <button onClick={() => resyncMut.mutate()} disabled={resyncMut.isPending} className="link text-xs">Retry</button>}
      </div>
      {status === "failed" && syncError && <p className="mt-1 text-xs text-red-400">{syncError}</p>}
    </div>
  );
}
