import { useState, useEffect, useCallback } from "react";
import { useNavigate, useParams } from "@tanstack/react-router";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useForm, Controller } from "react-hook-form";
import { z } from "zod";
import { standardSchemaResolver } from "@hookform/resolvers/standard-schema";
import { apiFetch } from "@/lib/api";
import type { FillUp, Vehicle, StationSuggestion } from "@/lib/types";
import { useToast } from "@/components/Toast";
import CurrencyInput from "@/components/CurrencyInput";
import Spinner from "@/components/Spinner";

const editFillUpSchema = z.object({
  vehicleId: z.string().min(1, "Vehicle is required"),
  date: z.string().min(1, "Date is required"),
  odometerMiles: z.number().positive("Must be positive"),
  gallons: z.string().min(1, "Gallons is required"),
  pricePerGallon: z.string().min(1, "Price is required"),
  octaneRating: z.number().nullable().optional(),
  stationName: z.string().min(1, "Station name is required"),
  stationAddress: z.string().nullable().optional(),
  latitude: z.number().nullable().optional(),
  longitude: z.number().nullable().optional(),
  notes: z.string().nullable().optional(),
});

type EditFillUpFormData = z.infer<typeof editFillUpSchema>;

export default function EditFillUp() {
  const { fillUpId } = useParams({ from: "/fill-ups/$fillUpId/edit" as never });
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { toast } = useToast();
  const [stationQuery, setStationQuery] = useState("");
  const [showSuggestions, setShowSuggestions] = useState(false);
  const [gpsLoading, setGpsLoading] = useState(false);

  const { data: fillUp, isLoading } = useQuery({
    queryKey: ["fill-up", fillUpId],
    queryFn: () => apiFetch<FillUp>(`/api/fill-ups/${fillUpId}`),
  });

  const { data: vehicles } = useQuery({
    queryKey: ["vehicles"],
    queryFn: () => apiFetch<Vehicle[]>("/api/vehicles"),
  });

  const { data: stationSuggestions } = useQuery({
    queryKey: ["station-suggestions", stationQuery],
    queryFn: () =>
      apiFetch<StationSuggestion[]>(
        `/api/stations/suggestions?q=${encodeURIComponent(stationQuery)}`
      ),
    enabled: stationQuery.length >= 2,
  });

  const {
    register,
    handleSubmit,
    control,
    setValue,
    watch,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<EditFillUpFormData>({
    resolver: standardSchemaResolver(editFillUpSchema),
  });

  // Populate form when fillUp loads
  useEffect(() => {
    if (fillUp) {
      reset({
        vehicleId: fillUp.vehicleId,
        date: fillUp.date.split("T")[0],
        odometerMiles: fillUp.odometerMiles,
        gallons: fillUp.gallons.toString(),
        pricePerGallon: fillUp.pricePerGallon.toString(),
        octaneRating: fillUp.octaneRating,
        stationName: fillUp.stationName,
        stationAddress: fillUp.stationAddress,
        latitude: fillUp.latitude,
        longitude: fillUp.longitude,
        notes: fillUp.notes,
      });
    }
  }, [fillUp, reset]);

  const latitude = watch("latitude");
  const longitude = watch("longitude");

  const updateMutation = useMutation({
    mutationFn: async (data: EditFillUpFormData) => {
      const body: Record<string, unknown> = {
        ...data,
        gallons: parseFloat(data.gallons),
        pricePerGallon: parseFloat(data.pricePerGallon),
      };

      return apiFetch<FillUp>(`/api/fill-ups/${fillUpId}`, {
        method: "PUT",
        body: JSON.stringify(body),
      });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["fill-ups"] });
      queryClient.invalidateQueries({ queryKey: ["fill-up", fillUpId] });
      queryClient.invalidateQueries({ queryKey: ["stats"] });
      toast("Fill-up updated", "success");
      navigate({ to: "/fill-ups/$fillUpId", params: { fillUpId } });
    },
    onError: (err) => {
      toast(err instanceof Error ? err.message : "Failed to update fill-up", "error");
    },
  });

  const getGps = useCallback(() => {
    if (!navigator.geolocation) {
      toast("Geolocation not supported", "error");
      return;
    }
    setGpsLoading(true);
    navigator.geolocation.getCurrentPosition(
      (pos) => {
        setValue("latitude", pos.coords.latitude);
        setValue("longitude", pos.coords.longitude);
        setGpsLoading(false);
      },
      (err) => {
        toast(`GPS error: ${err.message}`, "error");
        setGpsLoading(false);
      },
      { enableHighAccuracy: true, timeout: 10000 }
    );
  }, [setValue, toast]);

  if (isLoading) return <Spinner className="mt-20" />;
  if (!fillUp) return <p className="mt-10 text-center text-gray-500">Fill-up not found.</p>;

  const inputClass =
    "w-full rounded-lg border border-gray-300 px-3 py-2 text-sm shadow-sm focus:border-blue-500 focus:ring-1 focus:ring-blue-500 focus:outline-none";

  return (
    <div className="mx-auto max-w-2xl space-y-6">
      <h1 className="text-2xl font-bold text-gray-900">Edit Fill-Up</h1>

      <form
        onSubmit={handleSubmit((data) => updateMutation.mutate(data))}
        className="space-y-6 rounded-xl bg-white p-6 shadow-sm"
      >
        {/* Vehicle + Date */}
        <div className="grid gap-4 sm:grid-cols-2">
          <div>
            <label className="mb-1 block text-sm font-medium text-gray-700">Vehicle</label>
            <select {...register("vehicleId")} className={inputClass}>
              <option value="">Select vehicle...</option>
              {vehicles?.map((v) => (
                <option key={v.id} value={v.id}>
                  {v.label}
                </option>
              ))}
            </select>
            {errors.vehicleId && (
              <p className="mt-1 text-xs text-red-600">{errors.vehicleId.message}</p>
            )}
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium text-gray-700">Date</label>
            <input type="date" {...register("date")} className={inputClass} />
            {errors.date && (
              <p className="mt-1 text-xs text-red-600">{errors.date.message}</p>
            )}
          </div>
        </div>

        {/* Odometer + Octane */}
        <div className="grid gap-4 sm:grid-cols-2">
          <div>
            <label className="mb-1 block text-sm font-medium text-gray-700">Odometer (miles)</label>
            <input
              type="number"
              step="1"
              {...register("odometerMiles", { valueAsNumber: true })}
              className={inputClass}
            />
            {errors.odometerMiles && (
              <p className="mt-1 text-xs text-red-600">{errors.odometerMiles.message}</p>
            )}
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium text-gray-700">Octane Rating</label>
            <input
              type="number"
              step="1"
              {...register("octaneRating", { valueAsNumber: true })}
              className={inputClass}
            />
          </div>
        </div>

        {/* Gallons + Price */}
        <div className="grid gap-4 sm:grid-cols-2">
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
            {errors.gallons && (
              <p className="mt-1 text-xs text-red-600">{errors.gallons.message}</p>
            )}
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium text-gray-700">Price per Gallon</label>
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
            {errors.pricePerGallon && (
              <p className="mt-1 text-xs text-red-600">{errors.pricePerGallon.message}</p>
            )}
          </div>
        </div>

        {/* Station */}
        <div className="relative">
          <label className="mb-1 block text-sm font-medium text-gray-700">Station Name</label>
          <input
            {...register("stationName")}
            autoComplete="off"
            onChange={(e) => {
              register("stationName").onChange(e);
              setStationQuery(e.target.value);
              setShowSuggestions(true);
            }}
            onFocus={() => setShowSuggestions(true)}
            onBlur={() => setTimeout(() => setShowSuggestions(false), 200)}
            className={inputClass}
          />
          {errors.stationName && (
            <p className="mt-1 text-xs text-red-600">{errors.stationName.message}</p>
          )}

          {showSuggestions && stationSuggestions && stationSuggestions.length > 0 && (
            <div className="absolute z-10 mt-1 max-h-48 w-full overflow-y-auto rounded-lg border bg-white shadow-lg">
              {stationSuggestions.map((s) => (
                <button
                  key={s.stationName}
                  type="button"
                  className="w-full px-3 py-2 text-left text-sm hover:bg-gray-100"
                  onMouseDown={() => {
                    setValue("stationName", s.stationName);
                    setShowSuggestions(false);
                  }}
                >
                  <span className="font-medium">{s.stationName}</span>
                  <span className="ml-2 text-gray-400">{s.visitCount} visits</span>
                </button>
              ))}
            </div>
          )}
        </div>

        {/* Station address */}
        <div>
          <label className="mb-1 block text-sm font-medium text-gray-700">
            Station Address (optional)
          </label>
          <input {...register("stationAddress")} className={inputClass} />
        </div>

        {/* GPS */}
        <div>
          <label className="mb-1 block text-sm font-medium text-gray-700">GPS Location</label>
          <div className="flex items-center gap-3">
            <button
              type="button"
              onClick={getGps}
              disabled={gpsLoading}
              className="rounded-lg border border-gray-300 px-3 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50 disabled:opacity-50"
            >
              {gpsLoading ? "Getting location..." : "Use Current Location"}
            </button>
            {latitude != null && longitude != null && (
              <span className="text-sm text-gray-500">
                {latitude.toFixed(5)}, {longitude.toFixed(5)}
              </span>
            )}
          </div>
        </div>

        {/* Notes */}
        <div>
          <label className="mb-1 block text-sm font-medium text-gray-700">Notes (optional)</label>
          <textarea {...register("notes")} rows={3} className={inputClass} />
        </div>

        {/* Submit */}
        <div className="flex justify-end gap-3">
          <button
            type="button"
            onClick={() =>
              navigate({ to: "/fill-ups/$fillUpId", params: { fillUpId } })
            }
            className="rounded-lg border border-gray-300 px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50"
          >
            Cancel
          </button>
          <button
            type="submit"
            disabled={isSubmitting || updateMutation.isPending}
            className="rounded-lg bg-blue-600 px-4 py-2 text-sm font-medium text-white shadow-sm hover:bg-blue-700 disabled:opacity-50"
          >
            {updateMutation.isPending ? "Saving..." : "Save Changes"}
          </button>
        </div>
      </form>
    </div>
  );
}
