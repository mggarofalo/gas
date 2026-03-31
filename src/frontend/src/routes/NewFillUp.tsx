import { useNavigate } from "@tanstack/react-router";
import { useQuery, useMutation } from "@tanstack/react-query";
import { useForm, Controller } from "react-hook-form";
import { standardSchemaResolver } from "@hookform/resolvers/standard-schema";
import { z } from "zod";
import { apiFetch } from "../lib/api";
import type { Vehicle, FillUp } from "../lib/types";
import { useState, useCallback } from "react";

const numCoerce = z.union([z.string(), z.number()]).pipe(z.coerce.number());

const fillUpSchema = z.object({
  vehicleId: z.string().min(1, "Required"),
  date: z.string().min(1, "Required"),
  stationName: z.string().min(1, "Required").max(200),
  stationAddress: z.string().max(500).optional(),
  odometerMiles: numCoerce.pipe(z.number().int().positive("Must be > 0")),
  gallons: numCoerce.pipe(z.number().positive("Must be > 0")),
  pricePerGallon: numCoerce.pipe(z.number().positive("Must be > 0")),
  totalCostOverride: z.union([z.string(), z.number()]).pipe(z.coerce.number()).optional(),
  latitude: z.union([z.string(), z.number()]).pipe(z.coerce.number().min(-90).max(90)).optional(),
  longitude: z.union([z.string(), z.number()]).pipe(z.coerce.number().min(-180).max(180)).optional(),
  notes: z.string().optional(),
});
type FillUpForm = z.infer<typeof fillUpSchema>;

export function NewFillUpPage() {
  const navigate = useNavigate();
  const [receiptFile, setReceiptFile] = useState<File | null>(null);
  const [receiptPreview, setReceiptPreview] = useState<string | null>(null);

  const { data: vehicles = [] } = useQuery({
    queryKey: ["vehicles"],
    queryFn: () => apiFetch<Vehicle[]>("/vehicles"),
  });

  const {
    register,
    handleSubmit,
    watch,
    setValue,
    formState: { errors },
    control,
  } = useForm<FillUpForm>({
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    resolver: standardSchemaResolver(fillUpSchema) as any,
    defaultValues: {
      date: new Date().toISOString().slice(0, 10),
      vehicleId: "",
    },
  });

  const gallons = watch("gallons");
  const pricePerGallon = watch("pricePerGallon");
  const computedTotal =
    gallons && pricePerGallon
      ? Math.round(gallons * pricePerGallon * 100) / 100
      : 0;

  const createMut = useMutation({
    mutationFn: async (data: FillUpForm) => {
      const fd = new FormData();
      fd.append("vehicleId", data.vehicleId);
      fd.append("date", data.date);
      fd.append("stationName", data.stationName);
      if (data.stationAddress) fd.append("stationAddress", data.stationAddress);
      fd.append("odometerMiles", String(data.odometerMiles));
      fd.append("gallons", String(data.gallons));
      fd.append("pricePerGallon", String(data.pricePerGallon));
      fd.append(
        "totalCost",
        String(data.totalCostOverride || computedTotal),
      );
      if (data.latitude != null) fd.append("latitude", String(data.latitude));
      if (data.longitude != null)
        fd.append("longitude", String(data.longitude));
      if (data.notes) fd.append("notes", data.notes);
      if (receiptFile) fd.append("receipt", receiptFile);

      return apiFetch<FillUp>("/fill-ups", { method: "POST", body: fd });
    },
    onSuccess: (fillUp) => navigate({ to: "/fill-ups/$id", params: { id: fillUp.id } }),
  });

  const handleReceipt = useCallback((e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0] ?? null;
    setReceiptFile(file);
    if (file && file.type.startsWith("image/")) {
      const reader = new FileReader();
      reader.onload = () => setReceiptPreview(reader.result as string);
      reader.readAsDataURL(file);
    } else {
      setReceiptPreview(null);
    }
  }, []);

  const handleGeolocation = useCallback(() => {
    navigator.geolocation.getCurrentPosition(
      (pos) => {
        setValue("latitude", Math.round(pos.coords.latitude * 1e7) / 1e7);
        setValue("longitude", Math.round(pos.coords.longitude * 1e7) / 1e7);
      },
      (err) => alert(`Geolocation error: ${err.message}`),
    );
  }, [setValue]);

  return (
    <div className="mx-auto max-w-xl">
      <h2 className="mb-4 text-2xl font-semibold">New Fill-Up</h2>

      <form onSubmit={handleSubmit((d) => createMut.mutate(d))} className="space-y-4">
        {/* Vehicle */}
        <Field label="Vehicle" error={errors.vehicleId?.message}>
          <select {...register("vehicleId")} className="input">
            <option value="">Select vehicle...</option>
            {vehicles.map((v) => (
              <option key={v.id} value={v.id}>{v.label}</option>
            ))}
          </select>
        </Field>

        {/* Date */}
        <Field label="Date" error={errors.date?.message}>
          <input type="date" {...register("date")} className="input" />
        </Field>

        {/* Station */}
        <Field label="Gas Station" error={errors.stationName?.message}>
          <input {...register("stationName")} className="input" placeholder="Shell, BP, etc." />
        </Field>

        <Field label="Station Address (optional)">
          <input {...register("stationAddress")} className="input" placeholder="123 Main St" />
        </Field>

        {/* Odometer */}
        <Field label="Current Mileage" error={errors.odometerMiles?.message}>
          <input type="number" {...register("odometerMiles")} className="input" />
        </Field>

        {/* Gallons + Price */}
        <div className="grid grid-cols-2 gap-3">
          <Field label="Gallons" error={errors.gallons?.message}>
            <input type="number" step="0.001" {...register("gallons")} className="input" />
          </Field>
          <Field label="Price / Gallon" error={errors.pricePerGallon?.message}>
            <input type="number" step="0.001" {...register("pricePerGallon")} className="input" />
          </Field>
        </div>

        {/* Total */}
        <Field label="Total Cost">
          <div className="flex items-center gap-2">
            <span className="text-sm text-gray-500">${computedTotal.toFixed(2)}</span>
            <Controller
              name="totalCostOverride"
              control={control}
              render={({ field }) => (
                <input
                  type="number"
                  step="0.01"
                  placeholder="Override"
                  className="input flex-1"
                  value={field.value ?? ""}
                  onChange={field.onChange}
                />
              )}
            />
          </div>
        </Field>

        {/* GPS */}
        <div>
          <div className="mb-1 flex items-center gap-2">
            <span className="text-xs font-medium text-gray-600">GPS Coordinates</span>
            <button
              type="button"
              onClick={handleGeolocation}
              className="text-xs text-blue-600 hover:underline"
            >
              Use My Location
            </button>
          </div>
          <div className="grid grid-cols-2 gap-3">
            <input type="number" step="any" {...register("latitude")} placeholder="Latitude" className="input" />
            <input type="number" step="any" {...register("longitude")} placeholder="Longitude" className="input" />
          </div>
        </div>

        {/* Receipt */}
        <Field label="Receipt (optional)">
          <input type="file" accept="image/jpeg,image/png,image/webp,application/pdf" onChange={handleReceipt} className="input" />
          {receiptPreview && (
            <img src={receiptPreview} alt="Receipt preview" className="mt-2 h-32 rounded border object-contain" />
          )}
          {receiptFile && !receiptPreview && (
            <p className="mt-1 text-sm text-gray-500">{receiptFile.name}</p>
          )}
        </Field>

        {/* Notes */}
        <Field label="Notes (optional)">
          <textarea {...register("notes")} className="input" rows={2} />
        </Field>

        {createMut.isError && (
          <p className="text-sm text-red-600">Error: {createMut.error.message}</p>
        )}

        <button
          type="submit"
          disabled={createMut.isPending}
          className="w-full rounded bg-blue-600 py-2.5 text-white hover:bg-blue-700 disabled:opacity-50"
        >
          {createMut.isPending ? "Saving..." : "Save Fill-Up"}
        </button>
      </form>
    </div>
  );
}

function Field({
  label,
  error,
  children,
}: {
  label: string;
  error?: string;
  children: React.ReactNode;
}) {
  return (
    <div>
      <label className="mb-1 block text-xs font-medium text-gray-600">{label}</label>
      {children}
      {error && <p className="mt-0.5 text-xs text-red-500">{error}</p>}
    </div>
  );
}
