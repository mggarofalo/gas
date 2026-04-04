import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useForm, Controller } from "react-hook-form";
import { z } from "zod";
import { standardSchemaResolver } from "@hookform/resolvers/standard-schema";
import { apiFetch } from "@/lib/api";
import type { YnabImportPage, YnabImport, Vehicle } from "@/lib/types";
import { useToast } from "@/components/Toast";
import Spinner from "@/components/Spinner";
import EmptyState from "@/components/EmptyState";
import CurrencyInput from "@/components/CurrencyInput";

const editImportSchema = z.object({
  gallons: z.string().nullable().optional(),
  pricePerGallon: z.string().nullable().optional(),
  octaneRating: z.number().nullable().optional(),
  odometerMiles: z.number().nullable().optional(),
  vehicleId: z.string().nullable().optional(),
});

type EditImportFormData = z.infer<typeof editImportSchema>;

export default function YnabImports() {
  const queryClient = useQueryClient();
  const { toast } = useToast();
  const [page, setPage] = useState(1);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [statusFilter, setStatusFilter] = useState("pending");
  const pageSize = 20;

  const params = new URLSearchParams({
    page: page.toString(),
    pageSize: pageSize.toString(),
    status: statusFilter,
  });

  const { data: vehicles } = useQuery({
    queryKey: ["vehicles"],
    queryFn: () => apiFetch<Vehicle[]>("/api/vehicles"),
  });

  const { data, isLoading } = useQuery({
    queryKey: ["ynab-imports", page, statusFilter],
    queryFn: () => apiFetch<YnabImportPage>(`/api/ynab/imports?${params}`),
  });

  const approveMutation = useMutation({
    mutationFn: (id: string) =>
      apiFetch(`/api/ynab/imports/${id}/approve`, { method: "POST" }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["ynab-imports"] });
      queryClient.invalidateQueries({ queryKey: ["fill-ups"] });
      queryClient.invalidateQueries({ queryKey: ["stats"] });
      toast("Import approved and fill-up created", "success");
    },
    onError: (err) => {
      toast(err instanceof Error ? err.message : "Failed to approve", "error");
    },
  });

  const dismissMutation = useMutation({
    mutationFn: (id: string) =>
      apiFetch(`/api/ynab/imports/${id}/dismiss`, { method: "POST" }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["ynab-imports"] });
      toast("Import dismissed", "success");
    },
    onError: (err) => {
      toast(err instanceof Error ? err.message : "Failed to dismiss", "error");
    },
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: EditImportFormData }) => {
      const body: Record<string, unknown> = {
        ...data,
        gallons: data.gallons ? parseFloat(data.gallons) : null,
        pricePerGallon: data.pricePerGallon ? parseFloat(data.pricePerGallon) : null,
      };
      return apiFetch(`/api/ynab/imports/${id}`, {
        method: "PUT",
        body: JSON.stringify(body),
      });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["ynab-imports"] });
      toast("Import updated", "success");
      setEditingId(null);
    },
    onError: (err) => {
      toast(err instanceof Error ? err.message : "Failed to update", "error");
    },
  });

  const syncMutation = useMutation({
    mutationFn: () => apiFetch("/api/ynab/sync", { method: "POST" }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["ynab-imports"] });
      toast("Sync started", "success");
    },
    onError: (err) => {
      toast(err instanceof Error ? err.message : "Sync failed", "error");
    },
  });

  const totalPages = data ? Math.ceil(data.totalCount / pageSize) : 0;

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-4">
        <h1 className="text-2xl font-bold text-gray-900">YNAB Imports</h1>
        <button
          onClick={() => syncMutation.mutate()}
          disabled={syncMutation.isPending}
          className="rounded-lg bg-blue-600 px-4 py-2 text-sm font-medium text-white shadow-sm hover:bg-blue-700 disabled:opacity-50"
        >
          {syncMutation.isPending ? "Syncing..." : "Sync Now"}
        </button>
      </div>

      {/* Status filter */}
      <div className="flex gap-2">
        {["pending", "approved", "dismissed", "all"].map((status) => (
          <button
            key={status}
            onClick={() => {
              setStatusFilter(status);
              setPage(1);
            }}
            className={`rounded-lg px-3 py-1.5 text-sm font-medium capitalize ${
              statusFilter === status
                ? "bg-blue-100 text-blue-700"
                : "text-gray-600 hover:bg-gray-100"
            }`}
          >
            {status}
          </button>
        ))}
      </div>

      {isLoading ? (
        <Spinner className="mt-10" />
      ) : !data || data.items.length === 0 ? (
        <EmptyState
          title="No imports found"
          message={
            statusFilter === "pending"
              ? "No pending imports to review."
              : "No imports match this filter."
          }
        />
      ) : (
        <>
          <div className="space-y-4">
            {data.items.map((imp) =>
              editingId === imp.id ? (
                <ImportEditForm
                  key={imp.id}
                  import_={imp}
                  vehicles={vehicles ?? []}
                  onSubmit={(formData) =>
                    updateMutation.mutate({ id: imp.id, data: formData })
                  }
                  onCancel={() => setEditingId(null)}
                  isPending={updateMutation.isPending}
                />
              ) : (
                <ImportCard
                  key={imp.id}
                  import_={imp}
                  onEdit={() => setEditingId(imp.id)}
                  onApprove={() => approveMutation.mutate(imp.id)}
                  onDismiss={() => dismissMutation.mutate(imp.id)}
                  isPending={
                    approveMutation.isPending || dismissMutation.isPending
                  }
                />
              )
            )}
          </div>

          {/* Pagination */}
          {totalPages > 1 && (
            <div className="flex items-center justify-between">
              <p className="text-sm text-gray-500">
                {data.totalCount} import{data.totalCount !== 1 ? "s" : ""}
              </p>
              <div className="flex gap-2">
                <button
                  disabled={page <= 1}
                  onClick={() => setPage((p) => p - 1)}
                  className="rounded-lg border px-3 py-1.5 text-sm disabled:opacity-50"
                >
                  Previous
                </button>
                <span className="flex items-center px-3 text-sm text-gray-500">
                  {page} / {totalPages}
                </span>
                <button
                  disabled={page >= totalPages}
                  onClick={() => setPage((p) => p + 1)}
                  className="rounded-lg border px-3 py-1.5 text-sm disabled:opacity-50"
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

function ImportCard({
  import_,
  onEdit,
  onApprove,
  onDismiss,
  isPending,
}: {
  import_: YnabImport;
  onEdit: () => void;
  onApprove: () => void;
  onDismiss: () => void;
  isPending: boolean;
}) {
  const amount = Math.abs(import_.amountMilliunits / 1000);

  const statusColors: Record<string, string> = {
    pending: "bg-yellow-100 text-yellow-700",
    approved: "bg-green-100 text-green-700",
    dismissed: "bg-gray-100 text-gray-500",
  };

  return (
    <div className="rounded-xl bg-white p-6 shadow-sm">
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div className="space-y-1">
          <div className="flex items-center gap-3">
            <h3 className="font-semibold text-gray-900">{import_.payeeName}</h3>
            <span
              className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${
                statusColors[import_.status] ?? "bg-gray-100 text-gray-500"
              }`}
            >
              {import_.status}
            </span>
          </div>
          <p className="text-sm text-gray-500">
            {new Date(import_.date).toLocaleDateString()} &middot; ${amount.toFixed(2)}
          </p>
          {import_.memo && (
            <p className="text-sm text-gray-500">Memo: {import_.memo}</p>
          )}

          {/* Parsed details */}
          <div className="mt-2 flex flex-wrap gap-4 text-sm text-gray-600">
            {import_.gallons != null && (
              <span>Gallons: {import_.gallons.toFixed(3)}</span>
            )}
            {import_.pricePerGallon != null && (
              <span>$/Gal: ${import_.pricePerGallon.toFixed(3)}</span>
            )}
            {import_.octaneRating != null && (
              <span>Octane: {import_.octaneRating}</span>
            )}
            {import_.odometerMiles != null && (
              <span>Odometer: {import_.odometerMiles.toLocaleString()}</span>
            )}
            {import_.vehicleName && (
              <span>Vehicle: {import_.vehicleName}</span>
            )}
          </div>
        </div>

        {import_.status === "pending" && (
          <div className="flex gap-2">
            <button
              onClick={onEdit}
              className="rounded-lg border border-gray-300 px-3 py-1.5 text-sm font-medium text-gray-700 hover:bg-gray-50"
            >
              Edit
            </button>
            <button
              onClick={onApprove}
              disabled={isPending}
              className="rounded-lg bg-green-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-green-700 disabled:opacity-50"
            >
              Approve
            </button>
            <button
              onClick={onDismiss}
              disabled={isPending}
              className="rounded-lg border border-red-300 px-3 py-1.5 text-sm font-medium text-red-600 hover:bg-red-50 disabled:opacity-50"
            >
              Dismiss
            </button>
          </div>
        )}
      </div>
    </div>
  );
}

function ImportEditForm({
  import_,
  vehicles,
  onSubmit,
  onCancel,
  isPending,
}: {
  import_: YnabImport;
  vehicles: Vehicle[];
  onSubmit: (data: EditImportFormData) => void;
  onCancel: () => void;
  isPending: boolean;
}) {
  const {
    register,
    handleSubmit,
    control,
  } = useForm<EditImportFormData>({
    resolver: standardSchemaResolver(editImportSchema),
    defaultValues: {
      gallons: import_.gallons?.toString() ?? "",
      pricePerGallon: import_.pricePerGallon?.toString() ?? "",
      octaneRating: import_.octaneRating,
      odometerMiles: import_.odometerMiles,
      vehicleId: import_.vehicleId,
    },
  });

  const amount = Math.abs(import_.amountMilliunits / 1000);

  const inputClass =
    "w-full rounded-lg border border-gray-300 px-3 py-2 text-sm shadow-sm focus:border-blue-500 focus:ring-1 focus:ring-blue-500 focus:outline-none";

  return (
    <form
      onSubmit={handleSubmit(onSubmit)}
      className="rounded-xl bg-white p-6 shadow-sm"
    >
      <div className="mb-4">
        <h3 className="font-semibold text-gray-900">{import_.payeeName}</h3>
        <p className="text-sm text-gray-500">
          {new Date(import_.date).toLocaleDateString()} &middot; ${amount.toFixed(2)}
        </p>
        {import_.memo && (
          <p className="text-sm text-gray-500">Memo: {import_.memo}</p>
        )}
      </div>

      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        <div>
          <label className="mb-1 block text-sm font-medium text-gray-700">Gallons</label>
          <Controller
            name="gallons"
            control={control}
            render={({ field }) => (
              <CurrencyInput
                value={field.value ?? ""}
                onChange={field.onChange}
                decimals={3}
                placeholder="0.000"
                className={inputClass}
              />
            )}
          />
        </div>

        <div>
          <label className="mb-1 block text-sm font-medium text-gray-700">Price/Gallon</label>
          <Controller
            name="pricePerGallon"
            control={control}
            render={({ field }) => (
              <CurrencyInput
                value={field.value ?? ""}
                onChange={field.onChange}
                decimals={3}
                placeholder="0.000"
                className={inputClass}
              />
            )}
          />
        </div>

        <div>
          <label className="mb-1 block text-sm font-medium text-gray-700">Octane</label>
          <input
            type="number"
            {...register("octaneRating", { valueAsNumber: true })}
            className={inputClass}
          />
        </div>

        <div>
          <label className="mb-1 block text-sm font-medium text-gray-700">Odometer</label>
          <input
            type="number"
            {...register("odometerMiles", { valueAsNumber: true })}
            className={inputClass}
          />
        </div>

        <div>
          <label className="mb-1 block text-sm font-medium text-gray-700">Vehicle</label>
          <select {...register("vehicleId")} className={inputClass}>
            <option value="">Select...</option>
            {vehicles
              .filter((v) => v.isActive)
              .map((v) => (
                <option key={v.id} value={v.id}>
                  {v.label}
                </option>
              ))}
          </select>
        </div>
      </div>

      <div className="mt-4 flex gap-2">
        <button
          type="submit"
          disabled={isPending}
          className="rounded-lg bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700 disabled:opacity-50"
        >
          {isPending ? "Saving..." : "Save"}
        </button>
        <button
          type="button"
          onClick={onCancel}
          className="rounded-lg border border-gray-300 px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50"
        >
          Cancel
        </button>
      </div>
    </form>
  );
}
