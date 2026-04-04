import { useState, useEffect, useCallback } from "react";
import { useNavigate } from "@tanstack/react-router";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useForm, Controller } from "react-hook-form";
import { z } from "zod";
import { standardSchemaResolver } from "@hookform/resolvers/standard-schema";
import { apiFetch } from "@/lib/api";
import type { Vehicle, StationSuggestion, NearbyStation } from "@/lib/types";
import { useToast } from "@/components/Toast";
import CurrencyInput from "@/components/CurrencyInput";

const fillUpSchema = z.object({
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

type FillUpFormData = z.infer<typeof fillUpSchema>;

export default function NewFillUp() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { toast } = useToast();
  const [receiptFile, setReceiptFile] = useState<File | null>(null);
  const [stationQuery, setStationQuery] = useState("");
  const [showSuggestions, setShowSuggestions] = useState(false);
  const [gpsLoading, setGpsLoading] = useState(false);

  const {
    register,
    handleSubmit,
    control,
    setValue,
    watch,
    formState: { errors, isSubmitting },
  } = useForm<FillUpFormData>({
    resolver: standardSchemaResolver(fillUpSchema),
    defaultValues: {
      date: new Date().toISOString().split("T")[0],
      latitude: null,
      longitude: null,
      stationAddress: null,
      notes: null,
      octaneRating: null,
    },
  });

  const { data: vehicles } = useQuery({
    queryKey: ["vehicles"],
    queryFn: () => apiFetch<Vehicle[]>("/api/vehicles"),
  });

  const latitude = watch("latitude");
  const longitude = watch("longitude");

  // Station suggestions
  const { data: stationSuggestions } = useQuery({
    queryKey: ["station-suggestions", stationQuery],
    queryFn: () =>
      apiFetch<StationSuggestion[]>(
        `/api/stations/suggestions?q=${encodeURIComponent(stationQuery)}`
      ),
    enabled: stationQuery.length >= 2,
  });

  // Nearby stations
  const { data: nearbyStations } = useQuery({
    queryKey: ["nearby-stations", latitude, longitude],
    queryFn: () =>
      apiFetch<NearbyStation[]>(
        `/api/stations/nearby?latitude=${latitude}&longitude=${longitude}`
      ),
    enabled: latitude != null && longitude != null,
  });

  const createMutation = useMutation({
    mutationFn: async (data: FillUpFormData) => {
      const body: Record<string, unknown> = {
        ...data,
        gallons: parseFloat(data.gallons),
        pricePerGallon: parseFloat(data.pricePerGallon),
      };

      const fillUp = await apiFetch<{ id: string }>("/api/fill-ups", {
        method: "POST",
        body: JSON.stringify(body),
      });

      // Upload receipt if present
      if (receiptFile) {
        const formData = new FormData();
        formData.append("file", receiptFile);
        await apiFetch(`/api/fill-ups/${fillUp.id}/receipt`, {
          method: "POST",
          body: formData,
        });
      }

      return fillUp;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["fill-ups"] });
      queryClient.invalidateQueries({ queryKey: ["stats"] });
      toast("Fill-up created", "success");
      navigate({ to: "/fill-ups" });
    },
    onError: (err) => {
      toast(err instanceof Error ? err.message : "Failed to create fill-up", "error");
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

  // Auto-fill octane from selected vehicle
  const vehicleId = watch("vehicleId");
  useEffect(() => {
    const vehicle = vehicles?.find((v) => v.id === vehicleId);
    if (vehicle?.octaneRating) {
      setValue("octaneRating", vehicle.octaneRating);
    }
  }, [vehicleId, vehicles, setValue]);

  const inputClass =
    "w-full rounded-lg border border-gray-300 px-3 py-2 text-sm shadow-sm focus:border-blue-500 focus:ring-1 focus:ring-blue-500 focus:outline-none";

  return (
    <div className="mx-auto max-w-2xl space-y-6">
      <h1 className="text-2xl font-bold text-gray-900">New Fill-Up</h1>

      <form
        onSubmit={handleSubmit((data) => createMutation.mutate(data))}
        className="space-y-6 rounded-xl bg-white p-6 shadow-sm"
      >
        {/* Vehicle + Date */}
        <div className="grid gap-4 sm:grid-cols-2">
          <div>
            <label className="mb-1 block text-sm font-medium text-gray-700">Vehicle</label>
            <select {...register("vehicleId")} className={inputClass}>
              <option value="">Select vehicle...</option>
              {vehicles
                ?.filter((v) => v.isActive)
                .map((v) => (
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

          {/* Suggestions dropdown */}
          {showSuggestions && (stationSuggestions?.length || nearbyStations?.length) && (
            <div className="absolute z-10 mt-1 max-h-48 w-full overflow-y-auto rounded-lg border bg-white shadow-lg">
              {nearbyStations && nearbyStations.length > 0 && (
                <>
                  <p className="px-3 pt-2 text-xs font-semibold text-gray-400">Nearby</p>
                  {nearbyStations.map((s) => (
                    <button
                      key={`nearby-${s.stationName}`}
                      type="button"
                      className="w-full px-3 py-2 text-left text-sm hover:bg-gray-100"
                      onMouseDown={() => {
                        setValue("stationName", s.stationName);
                        if (s.stationAddress) setValue("stationAddress", s.stationAddress);
                        setShowSuggestions(false);
                      }}
                    >
                      <span className="font-medium">{s.stationName}</span>
                      <span className="ml-2 text-gray-400">
                        {s.distanceMiles.toFixed(1)} mi
                      </span>
                    </button>
                  ))}
                </>
              )}
              {stationSuggestions && stationSuggestions.length > 0 && (
                <>
                  <p className="px-3 pt-2 text-xs font-semibold text-gray-400">Recent</p>
                  {stationSuggestions.map((s) => (
                    <button
                      key={`suggest-${s.stationName}`}
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
                </>
              )}
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

        {/* Receipt */}
        <div>
          <label className="mb-1 block text-sm font-medium text-gray-700">
            Receipt (optional)
          </label>
          <input
            type="file"
            accept="image/*,.pdf"
            onChange={(e) => setReceiptFile(e.target.files?.[0] ?? null)}
            className="w-full text-sm text-gray-500 file:mr-4 file:rounded-lg file:border-0 file:bg-blue-50 file:px-4 file:py-2 file:text-sm file:font-medium file:text-blue-700 hover:file:bg-blue-100"
          />
          {receiptFile && (
            <p className="mt-1 text-xs text-gray-500">{receiptFile.name}</p>
          )}
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
            onClick={() => navigate({ to: "/fill-ups" })}
            className="rounded-lg border border-gray-300 px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50"
          >
            Cancel
          </button>
          <button
            type="submit"
            disabled={isSubmitting || createMutation.isPending}
            className="rounded-lg bg-blue-600 px-4 py-2 text-sm font-medium text-white shadow-sm hover:bg-blue-700 disabled:opacity-50"
          >
            {createMutation.isPending ? "Saving..." : "Save Fill-Up"}
          </button>
        </div>
      </form>
    </div>
  );
}
