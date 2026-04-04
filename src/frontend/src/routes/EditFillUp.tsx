import { useNavigate, useParams } from "@tanstack/react-router";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { standardSchemaResolver } from "@hookform/resolvers/standard-schema";
import { z } from "zod";
import { apiFetch } from "../lib/api";
import { useToast } from "../components/Toast";
import type { Vehicle, FillUp } from "../lib/types";
import { useState } from "react";
import { CurrencyInput } from "../components/CurrencyInput";
import { Spinner } from "../components/Spinner";
import { EmptyState } from "../components/EmptyState";

const fillUpSchema = z.object({
  vehicleId: z.string().min(1, "Required"),
  date: z.string().min(1, "Required"),
  stationName: z.string().min(1, "Required").max(200),
  odometerMiles: z.coerce.number().int().positive("Must be > 0"),
  gallons: z.coerce.number().positive("Must be > 0"),
  pricePerGallon: z.coerce.number().positive("Must be > 0"),
  totalCostOverride: z.coerce.number().optional(),
  octaneRating: z.coerce.number().optional(),
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
  octaneRating?: number;
  notes?: string;
}

export function EditFillUpPage() {
  const { id } = useParams({ from: "/authenticated/fill-ups/$id/edit" });
  const navigate = useNavigate();
  const { toast } = useToast();
  const qc = useQueryClient();

  const { data: fillUp, isLoading } = useQuery({ queryKey: ["fill-up", id], queryFn: () => apiFetch<FillUp>(`/fill-ups/${id}`) });
  const { data: vehicles = [] } = useQuery({ queryKey: ["vehicles"], queryFn: () => apiFetch<Vehicle[]>("/vehicles") });

  const [gallonsStr, setGallonsStr] = useState(() => fillUp?.gallons.toFixed(3) ?? "");
  const [priceStr, setPriceStr] = useState(() => fillUp?.pricePerGallon.toFixed(3) ?? "");
  const [totalStr, setTotalStr] = useState(() => fillUp?.totalCost.toFixed(2) ?? "");

  const { register, handleSubmit, setValue, formState: { errors } } = useForm<FillUpForm>({
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    resolver: standardSchemaResolver(fillUpSchema) as any,
    defaultValues: fillUp ? {
      vehicleId: fillUp.vehicleId,
      date: fillUp.date,
      stationName: fillUp.stationName,
      odometerMiles: fillUp.odometerMiles,
      gallons: fillUp.gallons,
      pricePerGallon: fillUp.pricePerGallon,
      octaneRating: fillUp.octaneRating ?? undefined,
      notes: fillUp.notes ?? undefined,
    } : undefined,
  });

  const gallonsNum = parseFloat(gallonsStr) || 0;
  const priceNum = parseFloat(priceStr) || 0;
  const totalNum = parseFloat(totalStr) || 0;
  const computedTotal = gallonsNum && priceNum ? Math.round(gallonsNum * priceNum * 100) / 100 : 0;

  const updateMut = useMutation({
    mutationFn: async (data: FillUpForm) => {
      const fd = new FormData();
      fd.append("vehicleId", data.vehicleId);
      fd.append("date", data.date);
      fd.append("stationName", data.stationName);
      fd.append("odometerMiles", String(data.odometerMiles));
      fd.append("gallons", gallonsStr);
      fd.append("pricePerGallon", priceStr);
      fd.append("totalCost", totalNum > 0 ? totalStr : String(computedTotal));
      if (data.octaneRating) fd.append("octaneRating", String(data.octaneRating));
      if (data.notes) fd.append("notes", data.notes);
      return apiFetch<FillUp>(`/fill-ups/${id}`, { method: "PUT", body: fd });
    },
    onSuccess: () => {
      toast("Fill-up updated");
      qc.invalidateQueries({ queryKey: ["fill-up", id] });
      qc.invalidateQueries({ queryKey: ["fill-ups"] });
      navigate({ to: "/fill-ups/$id", params: { id } });
    },
  });

  if (isLoading) return <Spinner />;
  if (!fillUp) return <EmptyState message="Fill-up not found." />;

  return (
    <div className="mx-auto max-w-lg">
      <h2 className="mb-6 text-2xl font-semibold">Edit Fill-Up</h2>
      <form onSubmit={handleSubmit((d) => updateMut.mutate(d))} className="space-y-6">

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

        <Field label="Gas Station" error={errors.stationName?.message}>
          <input {...register("stationName")} className="input" />
        </Field>

        <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
          <Field label="Gallons" error={gallonsStr && gallonsNum <= 0 ? "Must be > 0" : undefined}>
            <CurrencyInput
              value={gallonsStr}
              onChange={(v) => { setGallonsStr(v); setValue("gallons", parseFloat(v) || 0); }}
              decimals={3}
              maxWhole={3}
              placeholder="0.000"
            />
          </Field>
          <Field label="Price / gal" error={priceStr && priceNum <= 0 ? "Must be > 0" : undefined}>
            <CurrencyInput
              value={priceStr}
              onChange={(v) => { setPriceStr(v); setValue("pricePerGallon", parseFloat(v) || 0); }}
              decimals={3}
              maxWhole={3}
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
          <Field label="Octane">
            <select {...register("octaneRating")} className="input">
              <option value="">-</option>
              <option value="87">87</option>
              <option value="89">89</option>
              <option value="91">91</option>
              <option value="93">93</option>
            </select>
          </Field>
        </div>
        {computedTotal > 0 && !totalNum && (
          <p className="-mt-4 text-xs text-text-muted">Calculated: ${computedTotal.toFixed(2)}</p>
        )}

        <Field label="Odometer" error={errors.odometerMiles?.message}>
          <div className="relative">
            <input type="text" inputMode="numeric" pattern="[0-9]*" autoComplete="off" data-bwignore="" data-1p-ignore="" data-lpignore="true" data-form-type="other" {...register("odometerMiles")} className="input pr-10" />
            <span className="absolute right-3 top-1/2 -translate-y-1/2 text-xs text-text-muted">mi</span>
          </div>
        </Field>

        <Field label="Notes">
          <textarea {...register("notes")} className="input" rows={2} placeholder="Optional" />
        </Field>

        {updateMut.isError && <p className="text-sm text-danger-text">Error: {updateMut.error.message}</p>}
        <div className="flex gap-3">
          <button type="button" onClick={() => navigate({ to: "/fill-ups/$id", params: { id } })} className="btn-outline flex-1 py-3 text-base">
            Cancel
          </button>
          <button type="submit" disabled={updateMut.isPending} className="btn-primary flex-1 py-3 text-base">
            {updateMut.isPending ? "Saving..." : "Save Changes"}
          </button>
        </div>
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
