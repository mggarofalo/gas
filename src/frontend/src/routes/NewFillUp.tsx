import { useState, useEffect, useCallback, useMemo } from "react";
import { useNavigate } from "@tanstack/react-router";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useForm, Controller } from "react-hook-form";
import { z } from "zod";
import { standardSchemaResolver } from "@hookform/resolvers/standard-schema";
import { apiFetch } from "@/lib/api";
import type {
  Vehicle,
  StationSuggestion,
  NearbyStation,
  YnabConfig,
  YnabAccount,
  YnabCategory,
} from "@/lib/types";
import { useToast } from "@/components/Toast";
import CurrencyInput from "@/components/CurrencyInput";

const fillUpSchema = z.object({
  vehicleId: z.string().min(1, "Vehicle is required"),
  date: z.string().min(1, "Date is required"),
  odometerMiles: z.number().positive("Must be positive"),
  pricePerGallon: z.string().min(1, "Price per gallon is required"),
  totalPrice: z.string().min(1, "Total price is required"),
  octaneRating: z.number().nullable().optional(),
  stationName: z.string().min(1, "Station name is required"),
  stationAddress: z.string().nullable().optional(),
  latitude: z.number().nullable().optional(),
  longitude: z.number().nullable().optional(),
  notes: z.string().nullable().optional(),
  ynabAccountId: z.string().nullable().optional(),
  ynabCategoryId: z.string().nullable().optional(),
});

type FillUpFormData = z.infer<typeof fillUpSchema>;

// Cached endpoints return accountId/categoryId instead of id
interface CachedAccount {
  accountId: string;
  name: string;
  type?: string;
  balance?: number;
}

interface CachedCategory {
  categoryId: string;
  name: string;
  categoryGroupName: string;
}

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
      ynabAccountId: null,
      ynabCategoryId: null,
    },
  });

  const { data: vehicles } = useQuery({
    queryKey: ["vehicles"],
    queryFn: () => apiFetch<Vehicle[]>("/api/vehicles"),
  });

  // YNAB config + cached accounts/categories for the picker
  const { data: ynabConfig } = useQuery({
    queryKey: ["ynab-config"],
    queryFn: () => apiFetch<YnabConfig>("/api/settings/ynab"),
  });

  const { data: ynabAccounts } = useQuery({
    queryKey: ["ynab-accounts-cached"],
    queryFn: async () => {
      const cached = await apiFetch<CachedAccount[]>("/api/ynab/accounts/cached");
      return cached.map((a): YnabAccount => ({
        id: a.accountId,
        name: a.name,
        type: a.type,
        balance: a.balance,
      }));
    },
    enabled: ynabConfig?.configured === true,
  });

  const { data: ynabCategories } = useQuery({
    queryKey: ["ynab-categories-cached"],
    queryFn: async () => {
      const cached = await apiFetch<CachedCategory[]>("/api/ynab/categories/cached");
      return cached.map((c): YnabCategory => ({
        id: c.categoryId,
        name: c.name,
        categoryGroupName: c.categoryGroupName,
      }));
    },
    enabled: ynabConfig?.configured === true,
  });

  // Set YNAB defaults from config once loaded (wait for dropdown data)
  const [ynabDefaultsApplied, setYnabDefaultsApplied] = useState(false);
  useEffect(() => {
    if (ynabDefaultsApplied) return;
    if (!ynabConfig) return;
    if (!ynabConfig.configured) {
      setYnabDefaultsApplied(true);
      return;
    }
    if (!ynabAccounts || !ynabCategories) return;
    if (ynabConfig.accountId) setValue("ynabAccountId", ynabConfig.accountId);
    if (ynabConfig.categoryId) setValue("ynabCategoryId", ynabConfig.categoryId);
    setYnabDefaultsApplied(true);
  }, [ynabConfig, ynabAccounts, ynabCategories, ynabDefaultsApplied, setValue]);

  const latitude = watch("latitude");
  const longitude = watch("longitude");

  // Station suggestions
  const { data: stationSuggestions } = useQuery({
    queryKey: ["station-suggestions", stationQuery],
    queryFn: () =>
      apiFetch<StationSuggestion[]>(
        `/api/stations/search?q=${encodeURIComponent(stationQuery)}`
      ),
    enabled: stationQuery.length >= 2,
  });

  // Nearby stations
  const { data: nearbyStations } = useQuery({
    queryKey: ["nearby-stations", latitude, longitude],
    queryFn: () =>
      apiFetch<NearbyStation[]>(
        `/api/locations/nearby?lat=${latitude}&lng=${longitude}`
      ),
    enabled: latitude != null && longitude != null,
  });

  // Calculate gallons from totalPrice / pricePerGallon
  const watchedTotalPrice = watch("totalPrice");
  const watchedPricePerGallon = watch("pricePerGallon");
  const calculatedGallons = useMemo(() => {
    const total = parseFloat(watchedTotalPrice);
    const ppg = parseFloat(watchedPricePerGallon);
    if (!total || !ppg || ppg === 0) return null;
    return Math.round((total / ppg) * 1000) / 1000;
  }, [watchedTotalPrice, watchedPricePerGallon]);

  const createMutation = useMutation({
    mutationFn: async (data: FillUpFormData) => {
      const ppg = parseFloat(data.pricePerGallon);
      const totalCost = parseFloat(data.totalPrice);
      const gallons = ppg > 0 ? Math.round((totalCost / ppg) * 1000) / 1000 : 0;

      // Build FormData to match backend's ReadFormAsync()
      const formData = new FormData();
      formData.append("vehicleId", data.vehicleId);
      formData.append("date", data.date);
      formData.append("odometerMiles", data.odometerMiles.toString());
      formData.append("gallons", gallons.toString());
      formData.append("pricePerGallon", ppg.toString());
      formData.append("totalCost", totalCost.toString());
      formData.append("stationName", data.stationName);
      if (data.octaneRating != null)
        formData.append("octaneRating", data.octaneRating.toString());
      if (data.stationAddress)
        formData.append("stationAddress", data.stationAddress);
      if (data.latitude != null)
        formData.append("latitude", data.latitude.toString());
      if (data.longitude != null)
        formData.append("longitude", data.longitude.toString());
      if (data.notes) formData.append("notes", data.notes);

      // YNAB fields
      if (data.ynabAccountId) {
        formData.append("ynabAccountId", data.ynabAccountId);
        const account = ynabAccounts?.find((a) => a.id === data.ynabAccountId);
        if (account) formData.append("ynabAccountName", account.name);
      }
      if (data.ynabCategoryId) {
        formData.append("ynabCategoryId", data.ynabCategoryId);
        const category = ynabCategories?.find((c) => c.id === data.ynabCategoryId);
        if (category) formData.append("ynabCategoryName", category.name);
      }

      // Receipt in same request
      if (receiptFile) formData.append("receipt", receiptFile);

      return apiFetch<{ id: string }>("/api/fill-ups", {
        method: "POST",
        body: formData,
      });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["fill-ups"] });
      queryClient.invalidateQueries({ queryKey: ["stats"] });
      toast("Fill-up created", "success");
      navigate({ to: "/fill-ups" });
    },
    onError: (err) => {
      toast(
        err instanceof Error ? err.message : "Failed to create fill-up",
        "error"
      );
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
    "w-full rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-3 py-2 text-sm text-gray-900 dark:text-gray-100 shadow-sm focus:border-blue-500 focus:ring-1 focus:ring-blue-500 focus:outline-none";

  return (
    <div className="mx-auto max-w-2xl space-y-6">
      <h1 className="text-2xl font-bold text-gray-900 dark:text-gray-100">New Fill-Up</h1>

      <form
        onSubmit={handleSubmit((data) => createMutation.mutate(data))}
        className="space-y-6 rounded-xl bg-white dark:bg-gray-800 p-6 shadow-sm dark:shadow-gray-900/30"
      >
        {/* Vehicle + Date */}
        <div className="grid gap-4 sm:grid-cols-2">
          <div>
            <label className="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300">
              Vehicle
            </label>
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
              <p className="mt-1 text-xs text-red-600">
                {errors.vehicleId.message}
              </p>
            )}
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300">
              Date
            </label>
            <input type="date" {...register("date")} className={inputClass} />
            {errors.date && (
              <p className="mt-1 text-xs text-red-600">
                {errors.date.message}
              </p>
            )}
          </div>
        </div>

        {/* Odometer + Octane */}
        <div className="grid gap-4 sm:grid-cols-2">
          <div>
            <label className="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300">
              Odometer (miles)
            </label>
            <input
              type="number"
              step="1"
              {...register("odometerMiles", { valueAsNumber: true })}
              className={inputClass}
            />
            {errors.odometerMiles && (
              <p className="mt-1 text-xs text-red-600">
                {errors.odometerMiles.message}
              </p>
            )}
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300">
              Octane Rating
            </label>
            <input
              type="number"
              step="1"
              {...register("octaneRating", { valueAsNumber: true })}
              className={inputClass}
            />
          </div>
        </div>

        {/* Price per Gallon + Total Price + Calculated Gallons */}
        <div className="grid gap-4 sm:grid-cols-3">
          <div>
            <label className="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300">
              Price/Gallon
            </label>
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
              <p className="mt-1 text-xs text-red-600">
                {errors.pricePerGallon.message}
              </p>
            )}
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300">
              Total Price
            </label>
            <Controller
              name="totalPrice"
              control={control}
              render={({ field }) => (
                <CurrencyInput
                  value={field.value ?? ""}
                  onChange={field.onChange}
                  decimals={2}
                  placeholder="0.00"
                  className={inputClass}
                />
              )}
            />
            {errors.totalPrice && (
              <p className="mt-1 text-xs text-red-600">
                {errors.totalPrice.message}
              </p>
            )}
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300">
              Gallons
            </label>
            <div className="flex h-[38px] items-center rounded-lg border border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-900 px-3 text-sm text-gray-600 dark:text-gray-400">
              {calculatedGallons != null
                ? calculatedGallons.toFixed(3)
                : "--"}
            </div>
            <p className="mt-1 text-xs text-gray-400 dark:text-gray-500">Calculated</p>
          </div>
        </div>

        {/* Station */}
        <div className="relative">
          <label className="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300">
            Station Name
          </label>
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
            <p className="mt-1 text-xs text-red-600">
              {errors.stationName.message}
            </p>
          )}

          {/* Suggestions dropdown */}
          {showSuggestions &&
            (stationSuggestions?.length || nearbyStations?.length) && (
              <div className="absolute z-10 mt-1 max-h-48 w-full overflow-y-auto rounded-lg border dark:border-gray-700 bg-white dark:bg-gray-800 shadow-lg">
                {nearbyStations && nearbyStations.length > 0 && (
                  <>
                    <p className="px-3 pt-2 text-xs font-semibold text-gray-400 dark:text-gray-500">
                      Nearby
                    </p>
                    {nearbyStations.map((s) => (
                      <button
                        key={`nearby-${s.stationName}`}
                        type="button"
                        className="w-full px-3 py-2 text-left text-sm text-gray-900 dark:text-gray-100 hover:bg-gray-100 dark:hover:bg-gray-700"
                        onMouseDown={() => {
                          setValue("stationName", s.stationName);
                          if (s.stationAddress)
                            setValue("stationAddress", s.stationAddress);
                          setShowSuggestions(false);
                        }}
                      >
                        <span className="font-medium">{s.stationName}</span>
                        <span className="ml-2 text-gray-400 dark:text-gray-500">
                          {s.distanceMiles.toFixed(1)} mi
                        </span>
                      </button>
                    ))}
                  </>
                )}
                {stationSuggestions && stationSuggestions.length > 0 && (
                  <>
                    <p className="px-3 pt-2 text-xs font-semibold text-gray-400 dark:text-gray-500">
                      Recent
                    </p>
                    {stationSuggestions.map((s) => (
                      <button
                        key={`suggest-${s.stationName}`}
                        type="button"
                        className="w-full px-3 py-2 text-left text-sm text-gray-900 dark:text-gray-100 hover:bg-gray-100 dark:hover:bg-gray-700"
                        onMouseDown={() => {
                          setValue("stationName", s.stationName);
                          setShowSuggestions(false);
                        }}
                      >
                        <span className="font-medium">{s.stationName}</span>
                        <span className="ml-2 text-gray-400 dark:text-gray-500">
                          {s.visitCount} visits
                        </span>
                      </button>
                    ))}
                  </>
                )}
              </div>
            )}
        </div>

        {/* Station address */}
        <div>
          <label className="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300">
            Station Address (optional)
          </label>
          <input {...register("stationAddress")} className={inputClass} />
        </div>

        {/* GPS */}
        <div>
          <label className="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300">
            GPS Location
          </label>
          <div className="flex items-center gap-3">
            <button
              type="button"
              onClick={getGps}
              disabled={gpsLoading}
              className="rounded-lg border border-gray-300 dark:border-gray-600 px-3 py-2 text-sm font-medium text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700 disabled:opacity-50"
            >
              {gpsLoading ? "Getting location..." : "Use Current Location"}
            </button>
            {latitude != null && longitude != null && (
              <span className="text-sm text-gray-500 dark:text-gray-400">
                {latitude.toFixed(5)}, {longitude.toFixed(5)}
              </span>
            )}
          </div>
        </div>

        {/* YNAB Account & Category */}
        {ynabConfig?.configured && (
          <div className="grid gap-4 sm:grid-cols-2">
            <div>
              <label className="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300">
                YNAB Account
              </label>
              <select
                {...register("ynabAccountId")}
                className={inputClass}
              >
                <option value="">None</option>
                {ynabAccounts?.map((a) => (
                  <option key={a.id} value={a.id}>
                    {a.name}
                  </option>
                ))}
              </select>
            </div>
            <div>
              <label className="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300">
                YNAB Category
              </label>
              <select
                {...register("ynabCategoryId")}
                className={inputClass}
              >
                <option value="">None</option>
                {ynabCategories?.map((c) => (
                  <option key={c.id} value={c.id}>
                    {c.categoryGroupName}: {c.name}
                  </option>
                ))}
              </select>
            </div>
          </div>
        )}

        {/* Receipt */}
        <div>
          <label className="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300">
            Receipt (optional)
          </label>
          <input
            type="file"
            accept="image/*,.pdf"
            onChange={(e) => setReceiptFile(e.target.files?.[0] ?? null)}
            className="w-full text-sm text-gray-500 dark:text-gray-400 file:mr-4 file:rounded-lg file:border-0 file:bg-blue-50 dark:file:bg-blue-900/30 file:px-4 file:py-2 file:text-sm file:font-medium file:text-blue-700 dark:file:text-blue-400 hover:file:bg-blue-100 dark:hover:file:bg-blue-900/50"
          />
          {receiptFile && (
            <p className="mt-1 text-xs text-gray-500 dark:text-gray-400">{receiptFile.name}</p>
          )}
        </div>

        {/* Notes */}
        <div>
          <label className="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300">
            Notes (optional)
          </label>
          <textarea {...register("notes")} rows={3} className={inputClass} />
        </div>

        {/* Submit */}
        <div className="flex justify-end gap-3">
          <button
            type="button"
            onClick={() => navigate({ to: "/fill-ups" })}
            className="rounded-lg border border-gray-300 dark:border-gray-600 px-4 py-2 text-sm font-medium text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700"
          >
            Cancel
          </button>
          <button
            type="submit"
            disabled={isSubmitting || createMutation.isPending}
            className="rounded-lg bg-blue-600 dark:bg-blue-500 px-4 py-2 text-sm font-medium text-white shadow-sm hover:bg-blue-700 dark:hover:bg-blue-600 disabled:opacity-50"
          >
            {createMutation.isPending ? "Saving..." : "Save Fill-Up"}
          </button>
        </div>
      </form>
    </div>
  );
}
