import { useNavigate } from "@tanstack/react-router";
import { useQuery, useMutation } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { standardSchemaResolver } from "@hookform/resolvers/standard-schema";
import { z } from "zod";
import { apiFetch } from "../lib/api";
import { useToast } from "../components/Toast";
import type { Vehicle, FillUp, NearbyStation } from "../lib/types";
import { useState, useCallback } from "react";

const fillUpSchema = z.object({
  vehicleId: z.string().min(1, "Required"),
  date: z.string().min(1, "Required"),
  stationName: z.string().min(1, "Required").max(200),
  odometerMiles: z.coerce.number().int().positive("Must be > 0"),
  gallons: z.coerce.number().positive("Must be > 0"),
  pricePerGallon: z.coerce.number().positive("Must be > 0"),
  totalCostOverride: z.coerce.number().optional(),
  latitude: z.coerce.number().min(-90).max(90).optional(),
  longitude: z.coerce.number().min(-180).max(180).optional(),
  notes: z.string().optional(),
});
interface FillUpForm {
  vehicleId: string;
  date: string;
  stationName: string;
  odometerMiles: number;
  gallons: number;
  pricePerGallon: number;
  totalCostOverride?: number;
  latitude?: number;
  longitude?: number;
  notes?: string;
}

export function NewFillUpPage() {
  const navigate = useNavigate();
  const { toast } = useToast();
  const [receiptFile, setReceiptFile] = useState<File | null>(null);
  const [receiptPreview, setReceiptPreview] = useState<string | null>(null);
  const [nearbyStations, setNearbyStations] = useState<NearbyStation[]>([]);
  const [loadingNearby, setLoadingNearby] = useState(false);
  const [showGps, setShowGps] = useState(false);

  const { data: vehicles = [] } = useQuery({ queryKey: ["vehicles"], queryFn: () => apiFetch<Vehicle[]>("/vehicles") });

  const { register, handleSubmit, watch, setValue, formState: { errors } } = useForm<FillUpForm>({
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    resolver: standardSchemaResolver(fillUpSchema) as any,
    defaultValues: { date: new Date().toISOString().slice(0, 10), vehicleId: "" },
  });

  const gallons = watch("gallons");
  const pricePerGallon = watch("pricePerGallon");
  const computedTotal = gallons && pricePerGallon ? Math.round(gallons * pricePerGallon * 100) / 100 : 0;

  const createMut = useMutation({
    mutationFn: async (data: FillUpForm) => {
      const fd = new FormData();
      fd.append("vehicleId", data.vehicleId);
      fd.append("date", data.date);
      fd.append("stationName", data.stationName);
      fd.append("odometerMiles", String(data.odometerMiles));
      fd.append("gallons", String(data.gallons));
      fd.append("pricePerGallon", String(data.pricePerGallon));
      fd.append("totalCost", String(data.totalCostOverride || computedTotal));
      if (data.latitude != null) fd.append("latitude", String(data.latitude));
      if (data.longitude != null) fd.append("longitude", String(data.longitude));
      if (data.notes) fd.append("notes", data.notes);
      if (receiptFile) fd.append("receipt", receiptFile);
      return apiFetch<FillUp>("/fill-ups", { method: "POST", body: fd });
    },
    onSuccess: (fillUp) => { toast("Fill-up saved"); navigate({ to: "/fill-ups/$id", params: { id: fillUp.id } }); },
  });

  const handleReceipt = useCallback((e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0] ?? null;
    setReceiptFile(file);
    if (file && file.type.startsWith("image/")) {
      const reader = new FileReader();
      reader.onload = () => setReceiptPreview(reader.result as string);
      reader.readAsDataURL(file);
    } else { setReceiptPreview(null); }
  }, []);

  const handleGeolocation = useCallback(() => {
    setShowGps(true);
    navigator.geolocation.getCurrentPosition(
      async (pos) => {
        const lat = Math.round(pos.coords.latitude * 1e7) / 1e7;
        const lng = Math.round(pos.coords.longitude * 1e7) / 1e7;
        setValue("latitude", lat);
        setValue("longitude", lng);
        setLoadingNearby(true);
        try {
          const stations = await apiFetch<NearbyStation[]>(`/locations/nearby?lat=${lat}&lng=${lng}`);
          setNearbyStations(stations);
          if (stations.length > 0) setValue("stationName", stations[0].stationName);
        } catch { /* non-critical */ }
        finally { setLoadingNearby(false); }
      },
      (err) => alert(`Geolocation error: ${err.message}`),
    );
  }, [setValue]);

  return (
    <div className="mx-auto max-w-lg">
      <h2 className="mb-6 text-2xl font-semibold">New Fill-Up</h2>
      <form onSubmit={handleSubmit((d) => createMut.mutate(d))} className="space-y-5">
        {/* Vehicle + Date row */}
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
          <Field label="Vehicle" error={errors.vehicleId?.message}>
            <select {...register("vehicleId")} className="input">
              <option value="">Select...</option>
              {vehicles.map((v) => <option key={v.id} value={v.id}>{v.label}</option>)}
            </select>
          </Field>
          <Field label="Date" error={errors.date?.message}>
            <input type="date" {...register("date")} className="input" />
          </Field>
        </div>

        {/* Station */}
        <Field label="Gas Station" error={errors.stationName?.message}>
          <div className="flex gap-2">
            <input {...register("stationName")} className="input" placeholder="Shell, BP, etc." />
            <button type="button" onClick={handleGeolocation} className="btn-outline shrink-0 text-xs" title="Auto-fill from GPS">
              {loadingNearby ? "..." : "GPS"}
            </button>
          </div>
          {nearbyStations.length > 1 && (
            <div className="mt-1.5 flex flex-wrap gap-1">
              {nearbyStations.map((s, i) => (
                <button key={`${s.stationName}-${s.stationAddress}`} type="button"
                  onClick={() => setValue("stationName", s.stationName)}
                  className={`rounded border px-2 py-0.5 text-xs transition-colors ${i === 0 ? "border-accent bg-accent-subtle text-accent-text" : "border-border text-text-secondary hover:bg-surface-hover"}`}>
                  {s.stationName} ({s.distanceMiles.toFixed(2)} mi)
                </button>
              ))}
            </div>
          )}
        </Field>

        {/* Odometer */}
        <Field label="Odometer" error={errors.odometerMiles?.message}>
          <div className="relative">
            <input type="number" inputMode="numeric" {...register("odometerMiles")} className="input pr-10" placeholder="45,000" />
            <span className="absolute right-3 top-1/2 -translate-y-1/2 text-xs text-text-muted">mi</span>
          </div>
        </Field>

        {/* Gallons + Price + Total row */}
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
          <Field label="Gallons" error={errors.gallons?.message}>
            <input type="number" inputMode="decimal" step="0.001" {...register("gallons")} className="input" placeholder="0.000" />
          </Field>
          <Field label="Price" error={errors.pricePerGallon?.message}>
            <div className="relative">
              <span className="absolute left-2.5 top-1/2 -translate-y-1/2 text-xs text-text-muted">$</span>
              <input type="number" inputMode="decimal" step="0.001" {...register("pricePerGallon")} className="input pl-6" placeholder="0.000" />
            </div>
          </Field>
          <Field label="Total">
            <div className="relative">
              <span className="absolute left-2.5 top-1/2 -translate-y-1/2 text-xs text-text-muted">$</span>
              <input
                type="number"
                inputMode="decimal"
                step="0.01"
                className="input pl-6"
                placeholder={computedTotal.toFixed(2)}
                {...register("totalCostOverride")}
              />
            </div>
          </Field>
        </div>
        {computedTotal > 0 && !watch("totalCostOverride") && (
          <p className="mt-[-0.75rem] text-xs text-text-muted">
            Calculated: ${computedTotal.toFixed(2)}
          </p>
        )}

        {/* GPS (hidden until requested) */}
        {showGps && (
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
            <Field label="Latitude">
              <input type="number" inputMode="decimal" step="any" {...register("latitude")} className="input" />
            </Field>
            <Field label="Longitude">
              <input type="number" inputMode="decimal" step="any" {...register("longitude")} className="input" />
            </Field>
          </div>
        )}

        {/* Receipt */}
        <Field label="Receipt">
          <label className="flex cursor-pointer items-center justify-center rounded border-2 border-dashed border-border p-4 transition-colors hover:border-accent hover:bg-accent-subtle/30">
            <input type="file" accept="image/jpeg,image/png,image/webp,application/pdf" onChange={handleReceipt} className="hidden" />
            {receiptPreview ? (
              <img src={receiptPreview} alt="Receipt" className="max-h-32 rounded object-contain" />
            ) : receiptFile ? (
              <span className="text-sm text-text-secondary">{receiptFile.name}</span>
            ) : (
              <span className="text-sm text-text-muted">Tap to attach photo</span>
            )}
          </label>
        </Field>

        {/* Notes */}
        <Field label="Notes">
          <textarea {...register("notes")} className="input" rows={2} placeholder="Optional" />
        </Field>

        {createMut.isError && <p className="text-sm text-danger-text">Error: {createMut.error.message}</p>}
        <button type="submit" disabled={createMut.isPending} className="btn-primary w-full py-3 text-base">
          {createMut.isPending ? "Saving..." : "Save Fill-Up"}
        </button>
      </form>
    </div>
  );
}

function Field({ label, error, children }: { label: string; error?: string; children: React.ReactNode }) {
  return (
    <div>
      <label className="label">{label}</label>
      {children}
      {error && <p className="mt-0.5 text-xs text-danger-text">{error}</p>}
    </div>
  );
}
