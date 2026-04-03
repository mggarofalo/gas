import { useState, useCallback } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { Link } from "@tanstack/react-router";
import { apiFetch } from "../lib/api";
import { Spinner } from "../components/Spinner";
import { EmptyState } from "../components/EmptyState";
import { useToast } from "../components/Toast";
import type { Vehicle, YnabImportPage, YnabImport } from "../lib/types";

interface PullResult {
  newImports: number;
  skipped: number;
  errors: number;
  errorMessages: string[];
}

export function YnabImportsPage() {
  const qc = useQueryClient();
  const { toast } = useToast();
  const [page, setPage] = useState(1);

  const { data: vehicles = [] } = useQuery({
    queryKey: ["vehicles"],
    queryFn: () => apiFetch<Vehicle[]>("/vehicles"),
  });

  const { data, isLoading } = useQuery({
    queryKey: ["ynab-imports", page],
    queryFn: () => apiFetch<YnabImportPage>(`/ynab/imports?page=${page}&pageSize=50`),
  });

  const pullMut = useMutation({
    mutationFn: () => apiFetch<PullResult>("/ynab/imports/pull", { method: "POST", body: JSON.stringify({}) }),
    onSuccess: (r) => {
      if (r) {
        toast(`Pulled ${r.newImports} new imports (${r.skipped} skipped)`);
        if (r.errorMessages.length > 0)
          toast(`Errors: ${r.errorMessages.slice(0, 3).join("; ")}`);
      }
      qc.invalidateQueries({ queryKey: ["ynab-imports"] });
    },
    onError: (e) => toast(`Pull failed: ${e.message}`),
  });

  const approveAllMut = useMutation({
    mutationFn: () => apiFetch<{ approved: number }>("/ynab/imports/approve-all", { method: "POST" }),
    onSuccess: (r) => {
      toast(`Approved ${r?.approved ?? 0} imports`);
      qc.invalidateQueries({ queryKey: ["ynab-imports"] });
      qc.invalidateQueries({ queryKey: ["fill-ups"] });
    },
  });

  const totalPages = data ? Math.ceil(data.totalCount / 50) : 0;
  const completeCount = data?.items.filter(isComplete).length ?? 0;

  return (
    <div className="mx-auto max-w-4xl">
      <div className="mb-4 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h2 className="text-2xl font-semibold">YNAB Import Queue</h2>
          <Link to="/settings/ynab" className="link text-sm">Back to YNAB Settings</Link>
        </div>
        <div className="flex gap-2">
          <button onClick={() => pullMut.mutate()} disabled={pullMut.isPending} className="btn-primary">
            {pullMut.isPending ? "Pulling..." : "Pull from YNAB"}
          </button>
          {completeCount > 0 && (
            <button onClick={() => approveAllMut.mutate()} disabled={approveAllMut.isPending} className="btn-outline">
              {approveAllMut.isPending ? "Approving..." : `Approve All (${completeCount})`}
            </button>
          )}
        </div>
      </div>

      {data && (
        <div className="mb-3 flex gap-4 text-sm text-text-secondary">
          <span>{data.totalCount} pending</span>
          <span className="text-success-text">{completeCount} complete</span>
          <span className="text-warning-text">{data.totalCount - completeCount} incomplete</span>
        </div>
      )}

      {isLoading ? <Spinner /> : !data || data.items.length === 0 ? (
        <EmptyState message="No pending imports. Click 'Pull from YNAB' to scan for transactions." />
      ) : (
        <>
          <div className="space-y-3">
            {data.items.map((imp) => (
              <ImportRow key={imp.id} imp={imp} vehicles={vehicles} />
            ))}
          </div>

          {totalPages > 1 && (
            <div className="mt-3 flex items-center justify-center gap-2 text-sm">
              <button onClick={() => setPage((p) => Math.max(1, p - 1))} disabled={page <= 1} className="btn-outline disabled:opacity-40">Prev</button>
              <span>{page} / {totalPages}</span>
              <button onClick={() => setPage((p) => Math.min(totalPages, p + 1))} disabled={page >= totalPages} className="btn-outline disabled:opacity-40">Next</button>
            </div>
          )}
        </>
      )}
    </div>
  );
}

function isComplete(imp: YnabImport): boolean {
  return !!(imp.vehicleId && imp.gallons && imp.gallons > 0 && imp.pricePerGallon && imp.pricePerGallon > 0 && imp.odometerMiles && imp.odometerMiles > 0);
}

function ImportRow({ imp, vehicles }: { imp: YnabImport; vehicles: Vehicle[] }) {
  const qc = useQueryClient();
  const { toast } = useToast();
  const totalCost = Math.abs(imp.amountMilliunits) / 1000;
  const complete = isComplete(imp);

  const [gallons, setGallons] = useState(imp.gallons?.toString() ?? "");
  const [price, setPrice] = useState(imp.pricePerGallon?.toString() ?? "");
  const [octane, setOctane] = useState(imp.octaneRating?.toString() ?? "");
  const [odometer, setOdometer] = useState(imp.odometerMiles?.toString() ?? "");
  const [vehicleId, setVehicleId] = useState(imp.vehicleId ?? "");

  const saveMut = useMutation({
    mutationFn: (body: Record<string, unknown>) =>
      apiFetch(`/ynab/imports/${imp.id}`, { method: "PUT", body: JSON.stringify(body) }),
  });

  const approveMut = useMutation({
    mutationFn: () => apiFetch(`/ynab/imports/${imp.id}/approve`, { method: "POST" }),
    onSuccess: () => {
      toast("Import approved");
      qc.invalidateQueries({ queryKey: ["ynab-imports"] });
      qc.invalidateQueries({ queryKey: ["fill-ups"] });
    },
    onError: (e) => toast(`Approve failed: ${e.message}`),
  });

  const dismissMut = useMutation({
    mutationFn: () => apiFetch<void>(`/ynab/imports/${imp.id}`, { method: "DELETE" }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["ynab-imports"] });
    },
  });

  const handleSave = useCallback(() => {
    saveMut.mutate({
      gallons: gallons ? parseFloat(gallons) : null,
      pricePerGallon: price ? parseFloat(price) : null,
      octaneRating: octane ? parseInt(octane) : null,
      odometerMiles: odometer ? parseInt(odometer) : null,
      vehicleId: vehicleId || null,
    });
  }, [gallons, price, octane, odometer, vehicleId, saveMut]);

  const handleApprove = useCallback(() => {
    // Save first, then approve
    saveMut.mutate(
      {
        gallons: gallons ? parseFloat(gallons) : null,
        pricePerGallon: price ? parseFloat(price) : null,
        octaneRating: octane ? parseInt(octane) : null,
        odometerMiles: odometer ? parseInt(odometer) : null,
        vehicleId: vehicleId || null,
      },
      { onSuccess: () => approveMut.mutate() },
    );
  }, [gallons, price, octane, odometer, vehicleId, saveMut, approveMut]);

  return (
    <div className={`card p-3 ${complete ? "border-l-2 border-l-success-text" : "border-l-2 border-l-warning-text"}`}>
      {/* Header: date, payee, amount */}
      <div className="mb-2 flex items-center justify-between text-sm">
        <div>
          <span className="font-medium">{imp.date}</span>
          <span className="mx-2 text-text-muted">{imp.payeeName}</span>
          {imp.memo && <span className="text-xs text-text-muted italic">({imp.memo})</span>}
        </div>
        <span className="font-semibold">${totalCost.toFixed(2)}</span>
      </div>

      {/* Editable fields */}
      <div className="grid grid-cols-2 gap-2 text-sm sm:grid-cols-5">
        <div>
          <label className="text-xs text-text-muted">Vehicle</label>
          <select value={vehicleId} onChange={(e) => setVehicleId(e.target.value)} className="input text-sm">
            <option value="">Select...</option>
            {vehicles.map((v) => <option key={v.id} value={v.id}>{v.label}</option>)}
          </select>
          {imp.vehicleName && !vehicleId && <span className="text-xs text-warning-text">Parsed: {imp.vehicleName}</span>}
        </div>
        <div>
          <label className="text-xs text-text-muted">Gallons</label>
          <input type="number" step="0.001" value={gallons} onChange={(e) => setGallons(e.target.value)} className="input text-sm" placeholder="0.000" />
        </div>
        <div>
          <label className="text-xs text-text-muted">$/Gallon</label>
          <input type="number" step="0.001" value={price} onChange={(e) => setPrice(e.target.value)} className="input text-sm" placeholder="0.000" />
        </div>
        <div>
          <label className="text-xs text-text-muted">Octane</label>
          <input type="number" value={octane} onChange={(e) => setOctane(e.target.value)} className="input text-sm" placeholder="87" />
        </div>
        <div>
          <label className="text-xs text-text-muted">Odometer</label>
          <input type="number" value={odometer} onChange={(e) => setOdometer(e.target.value)} className="input text-sm" placeholder="0" />
        </div>
      </div>

      {/* Actions */}
      <div className="mt-2 flex gap-2">
        <button onClick={handleApprove} disabled={approveMut.isPending || saveMut.isPending} className="btn-primary text-xs">
          {approveMut.isPending ? "..." : "Approve"}
        </button>
        <button onClick={handleSave} disabled={saveMut.isPending} className="btn-outline text-xs">
          {saveMut.isPending ? "..." : "Save"}
        </button>
        <button onClick={() => dismissMut.mutate()} disabled={dismissMut.isPending} className="text-xs text-danger-text hover:underline">
          Dismiss
        </button>
      </div>
    </div>
  );
}
