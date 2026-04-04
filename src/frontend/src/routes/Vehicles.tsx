import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { standardSchemaResolver } from "@hookform/resolvers/standard-schema";
import { apiFetch } from "@/lib/api";
import type { Vehicle } from "@/lib/types";
import { useToast } from "@/components/Toast";
import Spinner from "@/components/Spinner";
import EmptyState from "@/components/EmptyState";

const vehicleSchema = z.object({
  year: z.number().int().min(1900).max(2100),
  make: z.string().min(1, "Make is required"),
  model: z.string().min(1, "Model is required"),
  notes: z.string().nullable().optional(),
  octaneRating: z.number().nullable().optional(),
});

type VehicleFormData = z.infer<typeof vehicleSchema>;

export default function Vehicles() {
  const queryClient = useQueryClient();
  const { toast } = useToast();
  const [editingId, setEditingId] = useState<string | null>(null);
  const [showAddForm, setShowAddForm] = useState(false);

  const { data: vehicles, isLoading } = useQuery({
    queryKey: ["vehicles"],
    queryFn: () => apiFetch<Vehicle[]>("/api/vehicles"),
  });

  const createMutation = useMutation({
    mutationFn: (data: VehicleFormData) =>
      apiFetch<Vehicle>("/api/vehicles", {
        method: "POST",
        body: JSON.stringify(data),
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["vehicles"] });
      toast("Vehicle added", "success");
      setShowAddForm(false);
    },
    onError: (err) => {
      toast(err instanceof Error ? err.message : "Failed to add vehicle", "error");
    },
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: VehicleFormData }) =>
      apiFetch<Vehicle>(`/api/vehicles/${id}`, {
        method: "PUT",
        body: JSON.stringify(data),
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["vehicles"] });
      toast("Vehicle updated", "success");
      setEditingId(null);
    },
    onError: (err) => {
      toast(err instanceof Error ? err.message : "Failed to update vehicle", "error");
    },
  });

  const deactivateMutation = useMutation({
    mutationFn: (id: string) =>
      apiFetch(`/api/vehicles/${id}`, { method: "DELETE" }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["vehicles"] });
      toast("Vehicle deactivated", "success");
    },
    onError: (err) => {
      toast(err instanceof Error ? err.message : "Failed to deactivate vehicle", "error");
    },
  });

  if (isLoading) return <Spinner className="mt-20" />;

  return (
    <div className="mx-auto max-w-3xl space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-gray-900">Vehicles</h1>
        {!showAddForm && (
          <button
            onClick={() => setShowAddForm(true)}
            className="rounded-lg bg-blue-600 px-4 py-2 text-sm font-medium text-white shadow-sm hover:bg-blue-700"
          >
            Add Vehicle
          </button>
        )}
      </div>

      {/* Add form */}
      {showAddForm && (
        <VehicleForm
          onSubmit={(data) => createMutation.mutate(data)}
          onCancel={() => setShowAddForm(false)}
          isPending={createMutation.isPending}
        />
      )}

      {/* Vehicle list */}
      {!vehicles || vehicles.length === 0 ? (
        <EmptyState
          title="No vehicles"
          message="Add your first vehicle to get started."
          action={
            !showAddForm ? (
              <button
                onClick={() => setShowAddForm(true)}
                className="rounded-lg bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700"
              >
                Add Vehicle
              </button>
            ) : undefined
          }
        />
      ) : (
        <div className="space-y-4">
          {vehicles.map((vehicle) =>
            editingId === vehicle.id ? (
              <VehicleForm
                key={vehicle.id}
                initialValues={vehicle}
                onSubmit={(data) =>
                  updateMutation.mutate({ id: vehicle.id, data })
                }
                onCancel={() => setEditingId(null)}
                isPending={updateMutation.isPending}
              />
            ) : (
              <div
                key={vehicle.id}
                className={`rounded-xl bg-white p-6 shadow-sm ${
                  !vehicle.isActive ? "opacity-60" : ""
                }`}
              >
                <div className="flex items-start justify-between">
                  <div>
                    <h3 className="text-lg font-semibold text-gray-900">
                      {vehicle.label}
                    </h3>
                    <div className="mt-1 flex flex-wrap gap-3 text-sm text-gray-500">
                      {vehicle.octaneRating && (
                        <span>{vehicle.octaneRating} octane</span>
                      )}
                      <span
                        className={`inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium ${
                          vehicle.isActive
                            ? "bg-green-100 text-green-700"
                            : "bg-gray-100 text-gray-500"
                        }`}
                      >
                        {vehicle.isActive ? "Active" : "Inactive"}
                      </span>
                    </div>
                    {vehicle.notes && (
                      <p className="mt-2 text-sm text-gray-500">{vehicle.notes}</p>
                    )}
                  </div>
                  <div className="flex gap-2">
                    <button
                      onClick={() => setEditingId(vehicle.id)}
                      className="rounded-lg border border-gray-300 px-3 py-1.5 text-sm font-medium text-gray-700 hover:bg-gray-50"
                    >
                      Edit
                    </button>
                    {vehicle.isActive && (
                      <button
                        onClick={() => {
                          if (confirm("Deactivate this vehicle?")) {
                            deactivateMutation.mutate(vehicle.id);
                          }
                        }}
                        className="rounded-lg border border-red-300 px-3 py-1.5 text-sm font-medium text-red-600 hover:bg-red-50"
                      >
                        Deactivate
                      </button>
                    )}
                  </div>
                </div>
              </div>
            )
          )}
        </div>
      )}
    </div>
  );
}

function VehicleForm({
  initialValues,
  onSubmit,
  onCancel,
  isPending,
}: {
  initialValues?: Vehicle;
  onSubmit: (data: VehicleFormData) => void;
  onCancel: () => void;
  isPending: boolean;
}) {
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<VehicleFormData>({
    resolver: standardSchemaResolver(vehicleSchema),
    defaultValues: initialValues
      ? {
          year: initialValues.year,
          make: initialValues.make,
          model: initialValues.model,
          notes: initialValues.notes,
          octaneRating: initialValues.octaneRating,
        }
      : {
          year: new Date().getFullYear(),
          notes: null,
          octaneRating: null,
        },
  });

  const inputClass =
    "w-full rounded-lg border border-gray-300 px-3 py-2 text-sm shadow-sm focus:border-blue-500 focus:ring-1 focus:ring-blue-500 focus:outline-none";

  return (
    <form
      onSubmit={handleSubmit(onSubmit)}
      className="space-y-4 rounded-xl bg-white p-6 shadow-sm"
    >
      <div className="grid gap-4 sm:grid-cols-3">
        <div>
          <label className="mb-1 block text-sm font-medium text-gray-700">Year</label>
          <input
            type="number"
            {...register("year", { valueAsNumber: true })}
            className={inputClass}
          />
          {errors.year && (
            <p className="mt-1 text-xs text-red-600">{errors.year.message}</p>
          )}
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium text-gray-700">Make</label>
          <input {...register("make")} className={inputClass} />
          {errors.make && (
            <p className="mt-1 text-xs text-red-600">{errors.make.message}</p>
          )}
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium text-gray-700">Model</label>
          <input {...register("model")} className={inputClass} />
          {errors.model && (
            <p className="mt-1 text-xs text-red-600">{errors.model.message}</p>
          )}
        </div>
      </div>

      <div className="grid gap-4 sm:grid-cols-2">
        <div>
          <label className="mb-1 block text-sm font-medium text-gray-700">
            Octane Rating (optional)
          </label>
          <input
            type="number"
            {...register("octaneRating", { valueAsNumber: true })}
            className={inputClass}
          />
        </div>
        <div>
          <label className="mb-1 block text-sm font-medium text-gray-700">
            Notes (optional)
          </label>
          <input {...register("notes")} className={inputClass} />
        </div>
      </div>

      <div className="flex justify-end gap-3">
        <button
          type="button"
          onClick={onCancel}
          className="rounded-lg border border-gray-300 px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50"
        >
          Cancel
        </button>
        <button
          type="submit"
          disabled={isPending}
          className="rounded-lg bg-blue-600 px-4 py-2 text-sm font-medium text-white shadow-sm hover:bg-blue-700 disabled:opacity-50"
        >
          {isPending ? "Saving..." : initialValues ? "Update" : "Add Vehicle"}
        </button>
      </div>
    </form>
  );
}
