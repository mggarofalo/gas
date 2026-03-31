import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { standardSchemaResolver } from "@hookform/resolvers/standard-schema";
import { z } from "zod";
import { apiFetch } from "../lib/api";
import type { Vehicle } from "../lib/types";

const vehicleSchema = z.object({
  year: z.coerce.number().int().min(1900).max(2100),
  make: z.string().min(1, "Required").max(100),
  model: z.string().min(1, "Required").max(100),
  notes: z.string().max(500).optional(),
});
interface VehicleForm { year: number; make: string; model: string; notes?: string; }

export function VehiclesPage() {
  const qc = useQueryClient();
  const [editingId, setEditingId] = useState<string | null>(null);
  const [showAdd, setShowAdd] = useState(false);

  const { data: vehicles = [], isLoading } = useQuery({ queryKey: ["vehicles"], queryFn: () => apiFetch<Vehicle[]>("/vehicles?active=false") });

  const createMut = useMutation({
    mutationFn: (data: VehicleForm) => apiFetch<Vehicle>("/vehicles", { method: "POST", body: JSON.stringify(data) }),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ["vehicles"] }); setShowAdd(false); },
  });
  const updateMut = useMutation({
    mutationFn: ({ id, data }: { id: string; data: Partial<VehicleForm> }) => apiFetch<Vehicle>(`/vehicles/${id}`, { method: "PUT", body: JSON.stringify(data) }),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ["vehicles"] }); setEditingId(null); },
  });
  const toggleMut = useMutation({
    mutationFn: (id: string) => apiFetch<void>(`/vehicles/${id}`, { method: "DELETE" }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["vehicles"] }),
  });

  if (isLoading) return <p className="text-text-secondary">Loading...</p>;

  return (
    <div>
      <div className="mb-4 flex items-center justify-between">
        <h2 className="text-2xl font-semibold">Vehicles</h2>
        <button onClick={() => setShowAdd(true)} className="btn-primary">Add Vehicle</button>
      </div>

      {showAdd && <VehicleFormCard onSubmit={(data) => createMut.mutate(data)} onCancel={() => setShowAdd(false)} isPending={createMut.isPending} />}

      <div className="space-y-3">
        {vehicles.map((v) =>
          editingId === v.id ? (
            <VehicleFormCard key={v.id} defaults={v} onSubmit={(data) => updateMut.mutate({ id: v.id, data })} onCancel={() => setEditingId(null)} isPending={updateMut.isPending} />
          ) : (
            <div key={v.id} className={`flex items-center justify-between rounded border border-border p-4 ${v.isActive ? "bg-surface-raised" : "bg-surface-hover/50 opacity-60"}`}>
              <div>
                <span className="font-medium">{v.label}</span>
                {v.notes && <span className="ml-2 text-sm text-text-secondary">{v.notes}</span>}
                {!v.isActive && <span className="badge-muted ml-2">Inactive</span>}
              </div>
              <div className="flex gap-2">
                <button onClick={() => setEditingId(v.id)} className="link text-sm">Edit</button>
                {v.isActive && <button onClick={() => toggleMut.mutate(v.id)} className="text-sm text-danger-text hover:underline">Deactivate</button>}
              </div>
            </div>
          ),
        )}
        {vehicles.length === 0 && <p className="text-text-secondary">No vehicles yet. Add one to get started.</p>}
      </div>
    </div>
  );
}

function VehicleFormCard({ defaults, onSubmit, onCancel, isPending }: {
  defaults?: { year: number; make: string; model: string; notes?: string | null };
  onSubmit: (data: VehicleForm) => void;
  onCancel: () => void;
  isPending: boolean;
}) {
  const { register, handleSubmit, formState: { errors } } = useForm<VehicleForm>({
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    resolver: standardSchemaResolver(vehicleSchema) as any,
    defaultValues: defaults ? { year: defaults.year, make: defaults.make, model: defaults.model, notes: defaults.notes ?? "" } : { year: new Date().getFullYear(), make: "", model: "", notes: "" },
  });

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="card mb-3 p-4">
      <div className="grid grid-cols-4 gap-3">
        <div><label className="label">Year</label><input type="number" {...register("year")} className="input" />{errors.year && <p className="mt-0.5 text-xs text-danger-text">{errors.year.message}</p>}</div>
        <div><label className="label">Make</label><input {...register("make")} className="input" />{errors.make && <p className="mt-0.5 text-xs text-danger-text">{errors.make.message}</p>}</div>
        <div><label className="label">Model</label><input {...register("model")} className="input" />{errors.model && <p className="mt-0.5 text-xs text-danger-text">{errors.model.message}</p>}</div>
        <div><label className="label">Notes</label><input {...register("notes")} className="input" /></div>
      </div>
      <div className="mt-3 flex gap-2">
        <button type="submit" disabled={isPending} className="btn-primary">{isPending ? "Saving..." : "Save"}</button>
        <button type="button" onClick={onCancel} className="btn-outline">Cancel</button>
      </div>
    </form>
  );
}
