import { useNavigate } from "@tanstack/react-router";
import { useQuery, useMutation } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { standardSchemaResolver } from "@hookform/resolvers/standard-schema";
import { z } from "zod";
import { apiFetch } from "../lib/api";
import { useToast } from "../components/Toast";
import type { Vehicle, FillUp, NearbyStation, StationSuggestion } from "../lib/types";
import { useState, useCallback, useRef, useEffect } from "react";
import { CurrencyInput } from "../components/CurrencyInput";

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
  const [gpsAcquired, setGpsAcquired] = useState(false);
  const [gpsFailed, setGpsFailed] = useState(false);

  // Controlled numeric inputs (string state for formatting)
  const [gallonsStr, setGallonsStr] = useState("");
  const [priceStr, setPriceStr] = useState("");
  const [totalStr, setTotalStr] = useState("");

  // Station autocomplete state
  const [suggestions, setSuggestions] = useState<StationSuggestion[]>([]);
  const [showSuggestions, setShowSuggestions] = useState(false);
  const suggestionsRef = useRef<HTMLDivElement>(null);
  const debounceRef = useRef<ReturnType<typeof setTimeout>>(null);

  const { data: vehicles = [] } = useQuery({ queryKey: ["vehicles"], queryFn: () => apiFetch<Vehicle[]>("/vehicles") });

  const { register, handleSubmit, setValue, formState: { errors } } = useForm<FillUpForm>({
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    resolver: standardSchemaResolver(fillUpSchema) as any,
    defaultValues: { date: new Date().toISOString().slice(0, 10), vehicleId: "" },
  });

  const gallonsNum = parseFloat(gallonsStr) || 0;
  const priceNum = parseFloat(priceStr) || 0;
  const totalNum = parseFloat(totalStr) || 0;
  const computedTotal = gallonsNum && priceNum ? Math.round(gallonsNum * priceNum * 100) / 100 : 0;

  const createMut = useMutation({
    mutationFn: async (data: FillUpForm) => {
      const fd = new FormData();
      fd.append("vehicleId", data.vehicleId);
      fd.append("date", data.date);
      fd.append("stationName", data.stationName);
      fd.append("odometerMiles", String(data.odometerMiles));
      fd.append("gallons", gallonsStr);
      fd.append("pricePerGallon", priceStr);
      fd.append("totalCost", totalNum > 0 ? totalStr : String(computedTotal));
      if (data.latitude != null) fd.append("latitude", String(data.latitude));
      if (data.longitude != null) fd.append("longitude", String(data.longitude));
      if (data.notes) fd.append("notes", data.notes);
      if (receiptFile) fd.append("receipt", receiptFile);
      return apiFetch<FillUp>("/fill-ups", { method: "POST", body: fd });
    },
    onSuccess: (fillUp) => { toast("Fill-up saved"); navigate({ to: "/fill-ups/$id", params: { id: fillUp.id } }); },
  });

  // Close suggestions on outside click
  useEffect(() => {
    const handler = (e: MouseEvent) => {
      if (suggestionsRef.current && !suggestionsRef.current.contains(e.target as Node)) {
        setShowSuggestions(false);
      }
    };
    document.addEventListener("mousedown", handler);
    return () => document.removeEventListener("mousedown", handler);
  }, []);

  // Station name autocomplete
  const handleStationInput = useCallback((e: React.ChangeEvent<HTMLInputElement>) => {
    const val = e.target.value;
    setValue("stationName", val);

    if (debounceRef.current) clearTimeout(debounceRef.current);
    if (val.length < 2) { setSuggestions([]); setShowSuggestions(false); return; }

    debounceRef.current = setTimeout(async () => {
      try {
        const results = await apiFetch<StationSuggestion[]>(`/stations/search?q=${encodeURIComponent(val)}`);
        setSuggestions(results);
        setShowSuggestions(results.length > 0);
      } catch { /* non-critical */ }
    }, 200);
  }, [setValue]);

  const selectSuggestion = useCallback((name: string) => {
    setValue("stationName", name);
    setShowSuggestions(false);
    setSuggestions([]);
  }, [setValue]);

  const allowedTypes = ["image/jpeg", "image/png", "image/webp", "image/heic", "image/heif", "application/pdf"];
  const handleReceipt = useCallback((e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0] ?? null;
    if (file && !allowedTypes.some((t) => file.type === t || file.type === "")) {
      // type="" happens with HEIC on some browsers — allow it through
      alert("Please select an image or PDF file.");
      return;
    }
    setReceiptFile(file);
    if (file && file.type.startsWith("image/")) {
      const reader = new FileReader();
      reader.onload = () => setReceiptPreview(reader.result as string);
      reader.readAsDataURL(file);
    } else { setReceiptPreview(null); }
  }, []);

  const handleGeolocation = useCallback(() => {
    if (!navigator.geolocation) { setGpsFailed(true); return; }
    setLoadingNearby(true);
    setGpsFailed(false);
    setGpsAcquired(false);
    navigator.geolocation.getCurrentPosition(
      async (pos) => {
        const lat = Math.round(pos.coords.latitude * 1e7) / 1e7;
        const lng = Math.round(pos.coords.longitude * 1e7) / 1e7;
        setValue("latitude", lat);
        setValue("longitude", lng);
        setGpsAcquired(true);
        try {
          const stations = await apiFetch<NearbyStation[]>(`/locations/nearby?lat=${lat}&lng=${lng}`);
          setNearbyStations(stations);
          if (stations.length > 0) setValue("stationName", stations[0].stationName);
        } catch { /* non-critical */ }
        finally { setLoadingNearby(false); }
      },
      (err) => {
        setLoadingNearby(false);
        setGpsFailed(true);
        console.warn("Geolocation error:", err.code, err.message);
      },
      // Explicit options required for iOS Safari — omitting timeout
      // or using Infinity causes immediate TIMEOUT error on iOS
      { enableHighAccuracy: false, timeout: 15000, maximumAge: 300000 },
    );
  }, [setValue]);

  // No auto-fire — iOS Safari requires a user gesture for geolocation

  const stationNameReg = register("stationName");

  return (
    <div className="mx-auto max-w-lg">
      <h2 className="mb-6 text-2xl font-semibold">New Fill-Up</h2>
      <form onSubmit={handleSubmit((d) => createMut.mutate(d))} className="space-y-6">

        {/* --- Section: Vehicle + Date --- */}
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

        {/* --- Section: Station / Location --- */}
        <div>
          <Field label="Gas Station" error={errors.stationName?.message}>
            <div className="relative" ref={suggestionsRef}>
              <div className="flex gap-2">
                <input
                  {...stationNameReg}
                  onChange={(e) => { stationNameReg.onChange(e); handleStationInput(e); }}
                  onFocus={() => { if (suggestions.length > 0) setShowSuggestions(true); }}
                  className="input"
                  placeholder={loadingNearby ? "Locating..." : "Start typing..."}
                  autoComplete="off"
                />
                <button type="button" onClick={() => { setGpsFailed(false); handleGeolocation(); }}
                  className={`shrink-0 rounded border px-3 py-1.5 text-xs transition-colors ${
                    loadingNearby ? "border-warning-bg text-warning-text animate-pulse" :
                    gpsAcquired ? "border-success-text/30 bg-success-bg text-success-text" :
                    gpsFailed ? "border-danger-text/30 text-danger-text" :
                    "border-border text-text-secondary hover:bg-surface-hover"
                  }`}>
                  {loadingNearby ? "..." : gpsAcquired ? "\u2713 GPS" : gpsFailed ? "\u2717 GPS" : "GPS"}
                </button>
              </div>
              {showSuggestions && (
                <div className="absolute z-10 mt-1 w-full rounded border border-border bg-surface-raised shadow-lg">
                  {suggestions.map((s) => (
                    <button
                      key={s.stationName}
                      type="button"
                      onClick={() => selectSuggestion(s.stationName)}
                      className="flex w-full items-center justify-between px-3 py-2 text-sm text-text-primary hover:bg-surface-hover"
                    >
                      <span>{s.stationName}</span>
                      <span className="text-xs text-text-muted">{s.visitCount}x</span>
                    </button>
                  ))}
                </div>
              )}
            </div>
          </Field>
          {nearbyStations.length > 1 && (
            <div className="mt-2 flex flex-wrap gap-1">
              {nearbyStations.map((s, i) => (
                <button key={`${s.stationName}-${i}`} type="button"
                  onClick={() => { setValue("stationName", s.stationName); setShowSuggestions(false); }}
                  className={`rounded border px-2 py-0.5 text-xs transition-colors ${i === 0 ? "border-accent bg-accent-subtle text-accent-text" : "border-border text-text-secondary hover:bg-surface-hover"}`}>
                  {s.stationName} ({s.distanceMiles.toFixed(2)} mi)
                </button>
              ))}
            </div>
          )}
        </div>

        {/* --- Section: Gallons + Price --- */}
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
          <Field label="Gallons" error={gallonsStr && gallonsNum <= 0 ? "Must be > 0" : undefined}>
            <CurrencyInput
              value={gallonsStr}
              onChange={(v) => { setGallonsStr(v); setValue("gallons", parseFloat(v) || 0); }}
              decimals={3}
              placeholder="0.000"
            />
          </Field>
          <Field label="Price / gal" error={priceStr && priceNum <= 0 ? "Must be > 0" : undefined}>
            <CurrencyInput
              value={priceStr}
              onChange={(v) => { setPriceStr(v); setValue("pricePerGallon", parseFloat(v) || 0); }}
              decimals={3}
              prefix="$"
              placeholder="0.000"
            />
          </Field>
          <Field label="Total">
            <CurrencyInput
              value={totalStr}
              onChange={(v) => { setTotalStr(v); setValue("totalCostOverride", parseFloat(v) || undefined); }}
              decimals={2}
              prefix="$"
              placeholder={computedTotal > 0 ? computedTotal.toFixed(2) : "0.00"}
            />
          </Field>
        </div>
        {computedTotal > 0 && !totalNum && (
          <p className="-mt-4 text-xs text-text-muted">Calculated: ${computedTotal.toFixed(2)}</p>
        )}

        {/* --- Section: Mileage --- */}
        <Field label="Odometer" error={errors.odometerMiles?.message}>
          <div className="relative">
            <input type="text" inputMode="numeric" pattern="[0-9]*" autoComplete="off" data-bwignore="" data-1p-ignore="" data-lpignore="true" data-form-type="other" {...register("odometerMiles")} className="input pr-10" placeholder="45000" />
            <span className="absolute right-3 top-1/2 -translate-y-1/2 text-xs text-text-muted">mi</span>
          </div>
        </Field>

        {/* --- Section: Receipt --- */}
        <Field label="Receipt">
          {receiptPreview ? (
            <div className="relative inline-block">
              <img src={receiptPreview} alt="Receipt" className="max-h-48 rounded border border-border object-contain" />
              <button type="button" onClick={() => { setReceiptFile(null); setReceiptPreview(null); }}
                className="absolute -right-2 -top-2 flex h-6 w-6 items-center justify-center rounded-full bg-surface-hover text-text-secondary hover:bg-danger hover:text-white transition-colors">
                &times;
              </button>
            </div>
          ) : receiptFile ? (
            <div className="flex items-center gap-2 text-sm text-text-secondary">
              <span>{receiptFile.name}</span>
              <button type="button" onClick={() => { setReceiptFile(null); setReceiptPreview(null); }} className="link text-xs">Remove</button>
            </div>
          ) : (
            <label className="flex cursor-pointer items-center justify-center gap-2 rounded border-2 border-dashed border-border p-6 transition-colors hover:border-accent hover:bg-accent-subtle/30">
              {/* No accept attr — iOS Safari only shows "Scan Documents" when unrestricted */}
              <input type="file" onChange={handleReceipt} className="hidden" />
              <svg className="h-5 w-5 text-text-muted" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={1.5}>
                <path strokeLinecap="round" strokeLinejoin="round" d="M6.827 6.175A2.31 2.31 0 015.186 7.23c-.38.054-.757.112-1.134.175C2.999 7.58 2.25 8.507 2.25 9.574V18a2.25 2.25 0 002.25 2.25h15A2.25 2.25 0 0021.75 18V9.574c0-1.067-.75-1.994-1.802-2.169a47.865 47.865 0 00-1.134-.175 2.31 2.31 0 01-1.64-1.055l-.822-1.316a2.192 2.192 0 00-1.736-1.039 48.774 48.774 0 00-5.232 0 2.192 2.192 0 00-1.736 1.039l-.821 1.316z" />
                <path strokeLinecap="round" strokeLinejoin="round" d="M16.5 12.75a4.5 4.5 0 11-9 0 4.5 4.5 0 019 0zM18.75 10.5h.008v.008h-.008V10.5z" />
              </svg>
              <span className="text-sm text-text-muted">Scan or attach receipt</span>
            </label>
          )}
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
