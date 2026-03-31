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
type VehicleForm = z.infer<typeof vehicleSchema>;

export function VehiclesPage() {
  const qc = useQueryClient();
  const [editingId, setEditingId] = useState<string | null>(null);
  const [showAdd, setShowAdd] = useState(false);

  const { data: vehicles = [], isLoading } = useQuery({
    queryKey: ["vehicles"],
    queryFn: () => apiFetch<Vehicle[]>("/vehicles?active=false"),
  });

  const createMut = useMutation({
    mutationFn: (data: VehicleForm) =>
      apiFetch<Vehicle>("/vehicles", {
        method: "POST",
        body: JSON.stringify(data),
      }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["vehicles"] });
      setShowAdd(false);
    },
  });

  const updateMut = useMutation({
    mutationFn: ({ id, data }: { id: string; data: Partial<VehicleForm> }) =>
      apiFetch<Vehicle>(`/vehicles/${id}`, {
        method: "PUT",
        body: JSON.stringify(data),
      }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["vehicles"] });
      setEditingId(null);
    },
  });

  const toggleMut = useMutation({
    mutationFn: (id: string) =>
      apiFetch<void>(`/vehicles/${id}`, { method: "DELETE" }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["vehicles"] }),
  });

  if (isLoading) return <p className="text-gray-500">Loading...</p>;

  return (
    <div>
      <div className="mb-4 flex items-center justify-between">
        <h2 className="text-2xl font-semibold">Vehicles</h2>
        <button
          onClick={() => setShowAdd(true)}
          className="rounded bg-blue-600 px-4 py-2 text-sm text-white hover:bg-blue-700"
        >
          Add Vehicle
        </button>
      </div>

      {showAdd && (
        <VehicleFormCard
          onSubmit={(data) => createMut.mutate(data)}
          onCancel={() => setShowAdd(false)}
          isPending={createMut.isPending}
        />
      )}

      <div className="space-y-3">
        {vehicles.map((v) =>
          editingId === v.id ? (
            <VehicleFormCard
              key={v.id}
              defaults={v}
              onSubmit={(data) => updateMut.mutate({ id: v.id, data })}
              onCancel={() => setEditingId(null)}
              isPending={updateMut.isPending}
            />
          ) : (
            <div
              key={v.id}
              className={`flex items-center justify-between rounded border p-4 ${v.isActive ? "bg-white" : "bg-gray-100 opacity-60"}`}
            >
              <div>
                <span className="font-medium">{v.label}</span>
                {v.notes && (
                  <span className="ml-2 text-sm text-gray-500">{v.notes}</span>
                )}
                {!v.isActive && (
                  <span className="ml-2 rounded bg-gray-300 px-2 py-0.5 text-xs">
                    Inactive
                  </span>
                )}
              </div>
              <div className="flex gap-2">
                <button
                  onClick={() => setEditingId(v.id)}
                  className="text-sm text-blue-600 hover:underline"
                >
                  Edit
                </button>
                {v.isActive && (
                  <button
                    onClick={() => toggleMut.mutate(v.id)}
                    className="text-sm text-red-600 hover:underline"
                  >
                    Deactivate
                  </button>
                )}
              </div>
            </div>
          ),
        )}
        {vehicles.length === 0 && (
          <p className="text-gray-500">
            No vehicles yet. Add one to get started.
          </p>
        )}
      </div>
    </div>
  );
}

function VehicleFormCard({
  defaults,
  onSubmit,
  onCancel,
  isPending,
}: {
  defaults?: { year: number; make: string; model: string; notes?: string | null };
  onSubmit: (data: VehicleForm) => void;
  onCancel: () => void;
  isPending: boolean;
}) {
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<VehicleForm>({
    resolver: standardSchemaResolver(vehicleSchema),
    defaultValues: defaults
      ? { year: defaults.year, make: defaults.make, model: defaults.model, notes: defaults.notes ?? "" }
      : { year: new Date().getFullYear(), make: "", model: "", notes: "" },
  });

  return (
    <form
      onSubmit={handleSubmit(onSubmit)}
      className="mb-3 rounded border bg-white p-4"
    >
      <div className="grid grid-cols-4 gap-3">
        <div>
          <label className="mb-1 block text-xs font-medium text-gray-600">Year</label>
          <input
            type="number"
            {...register("year")}
            className="w-full rounded border px-2 py-1.5 text-sm"
          />
          {errors.year && <p className="mt-0.5 text-xs text-red-500">{errors.year.message}</p>}
        </div>
        <div>
          <label className="mb-1 block text-xs font-medium text-gray-600">Make</label>
          <input
            {...register("make")}
            className="w-full rounded border px-2 py-1.5 text-sm"
          />
          {errors.make && <p className="mt-0.5 text-xs text-red-500">{errors.make.message}</p>}
        </div>
        <div>
          <label className="mb-1 block text-xs font-medium text-gray-600">Model</label>
          <input
            {...register("model")}
            className="w-full rounded border px-2 py-1.5 text-sm"
          />
          {errors.model && <p className="mt-0.5 text-xs text-red-500">{errors.model.message}</p>}
        </div>
        <div>
          <label className="mb-1 block text-xs font-medium text-gray-600">Notes</label>
          <input
            {...register("notes")}
            className="w-full rounded border px-2 py-1.5 text-sm"
          />
        </div>
      </div>
      <div className="mt-3 flex gap-2">
        <button
          type="submit"
          disabled={isPending}
          className="rounded bg-blue-600 px-3 py-1.5 text-sm text-white hover:bg-blue-700 disabled:opacity-50"
        >
          {isPending ? "Saving..." : "Save"}
        </button>
        <button
          type="button"
          onClick={onCancel}
          className="rounded border px-3 py-1.5 text-sm hover:bg-gray-50"
        >
          Cancel
        </button>
      </div>
    </form>
  );
}
