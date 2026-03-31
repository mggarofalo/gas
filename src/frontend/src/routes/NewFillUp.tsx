import { useNavigate } from "@tanstack/react-router";
import { useQuery, useMutation } from "@tanstack/react-query";
import { useForm, Controller } from "react-hook-form";
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
  stationAddress: z.string().max(500).optional(),
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
  stationAddress?: string;
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

  const { data: vehicles = [] } = useQuery({ queryKey: ["vehicles"], queryFn: () => apiFetch<Vehicle[]>("/vehicles") });

  const { register, handleSubmit, watch, setValue, formState: { errors }, control } = useForm<FillUpForm>({
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
      if (data.stationAddress) fd.append("stationAddress", data.stationAddress);
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
          if (stations.length > 0) {
            setValue("stationName", stations[0].stationName);
            if (stations[0].stationAddress) setValue("stationAddress", stations[0].stationAddress);
          }
        } catch { /* non-critical */ }
        finally { setLoadingNearby(false); }
      },
      (err) => alert(`Geolocation error: ${err.message}`),
    );
  }, [setValue]);

  return (
    <div className="mx-auto max-w-xl">
      <h2 className="mb-4 text-2xl font-semibold">New Fill-Up</h2>
      <form onSubmit={handleSubmit((d) => createMut.mutate(d))} className="space-y-4">
        <Field label="Vehicle" error={errors.vehicleId?.message}>
          <select {...register("vehicleId")} className="input">
            <option value="">Select vehicle...</option>
            {vehicles.map((v) => <option key={v.id} value={v.id}>{v.label}</option>)}
          </select>
        </Field>

        <Field label="Date" error={errors.date?.message}>
          <input type="date" {...register("date")} className="input" />
        </Field>

        <Field label="Gas Station" error={errors.stationName?.message}>
          <input {...register("stationName")} className="input" placeholder="Shell, BP, etc." />
          {loadingNearby && <p className="mt-1 text-xs text-text-muted">Looking up nearby stations...</p>}
          {nearbyStations.length > 1 && (
            <div className="mt-1 flex flex-wrap gap-1">
              {nearbyStations.map((s, i) => (
                <button key={`${s.stationName}-${s.stationAddress}`} type="button"
                  onClick={() => { setValue("stationName", s.stationName); if (s.stationAddress) setValue("stationAddress", s.stationAddress); }}
                  className={`rounded border px-2 py-0.5 text-xs ${i === 0 ? "border-accent bg-accent-subtle text-accent-text" : "border-border text-text-secondary hover:bg-surface-hover"}`}>
                  {s.stationName} ({s.distanceMiles.toFixed(2)} mi, {s.visitCount}x)
                </button>
              ))}
            </div>
          )}
        </Field>

        <Field label="Station Address (optional)"><input {...register("stationAddress")} className="input" placeholder="123 Main St" /></Field>
        <Field label="Current Mileage" error={errors.odometerMiles?.message}><input type="number" {...register("odometerMiles")} className="input" /></Field>

        <div className="grid grid-cols-2 gap-3">
          <Field label="Gallons" error={errors.gallons?.message}><input type="number" step="0.001" {...register("gallons")} className="input" /></Field>
          <Field label="Price / Gallon" error={errors.pricePerGallon?.message}><input type="number" step="0.001" {...register("pricePerGallon")} className="input" /></Field>
        </div>

        <Field label="Total Cost">
          <div className="flex items-center gap-2">
            <span className="text-sm text-text-secondary">${computedTotal.toFixed(2)}</span>
            <Controller name="totalCostOverride" control={control} render={({ field }) => (
              <input type="number" step="0.01" placeholder="Override" className="input flex-1" value={field.value ?? ""} onChange={field.onChange} />
            )} />
          </div>
        </Field>

        <div>
          <div className="mb-1 flex items-center gap-2">
            <span className="label mb-0">GPS Coordinates</span>
            <button type="button" onClick={handleGeolocation} className="link text-xs">Use My Location</button>
          </div>
          <div className="grid grid-cols-2 gap-3">
            <input type="number" step="any" {...register("latitude")} placeholder="Latitude" className="input" />
            <input type="number" step="any" {...register("longitude")} placeholder="Longitude" className="input" />
          </div>
        </div>

        <Field label="Receipt (optional)">
          <input type="file" accept="image/jpeg,image/png,image/webp,application/pdf" onChange={handleReceipt} className="input" />
          {receiptPreview && <img src={receiptPreview} alt="Receipt preview" className="mt-2 h-32 rounded border border-border object-contain" />}
          {receiptFile && !receiptPreview && <p className="mt-1 text-sm text-text-secondary">{receiptFile.name}</p>}
        </Field>

        <Field label="Notes (optional)"><textarea {...register("notes")} className="input" rows={2} /></Field>

        {createMut.isError && <p className="text-sm text-danger-text">Error: {createMut.error.message}</p>}
        <button type="submit" disabled={createMut.isPending} className="btn-primary w-full py-2.5">
          {createMut.isPending ? "Saving..." : "Save Fill-Up"}
        </button>
      </form>
    </div>
  );
}

function Field({ label, error, children }: { label: string; error?: string; children: React.ReactNode }) {
  return <div><label className="label">{label}</label>{children}{error && <p className="mt-0.5 text-xs text-danger-text">{error}</p>}</div>;
}
