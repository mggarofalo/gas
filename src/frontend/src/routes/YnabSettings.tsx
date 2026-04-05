import { useState, useEffect } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { standardSchemaResolver } from "@hookform/resolvers/standard-schema";
import { apiFetch } from "@/lib/api";
import type { YnabConfig, YnabPlan, YnabAccount, YnabCategory } from "@/lib/types";
import { useToast } from "@/components/Toast";
import Spinner from "@/components/Spinner";

const tokenSchema = z.object({
  apiToken: z.string().min(1, "YNAB personal access token is required"),
});

type TokenFormData = z.infer<typeof tokenSchema>;

interface ConfigFormValues {
  planId: string;
  accountId: string;
  categoryId: string;
  enabled: boolean;
}

export default function YnabSettings() {
  const queryClient = useQueryClient();
  const { toast } = useToast();
  const [showTokenForm, setShowTokenForm] = useState(false);

  const { data: config, isLoading } = useQuery({
    queryKey: ["ynab-config"],
    queryFn: () => apiFetch<YnabConfig>("/api/settings/ynab"),
  });

  const { data: plans } = useQuery({
    queryKey: ["ynab-plans"],
    queryFn: () => apiFetch<YnabPlan[]>("/api/ynab/plans"),
    enabled: config?.configured === true,
  });

  // Config form — declared early so we can watch planId for account/category fetching
  const configForm = useForm<ConfigFormValues>({
    defaultValues: {
      planId: "",
      accountId: "",
      categoryId: "",
      enabled: false,
    },
  });

  // Watch the form's planId so accounts/categories update reactively
  const watchedPlanId = configForm.watch("planId");
  const activePlanId = watchedPlanId || config?.planId;

  const { data: accounts } = useQuery({
    queryKey: ["ynab-accounts", activePlanId],
    queryFn: () =>
      apiFetch<YnabAccount[]>(`/api/ynab/plans/${activePlanId}/accounts`),
    enabled: config?.configured === true && !!activePlanId,
  });

  const { data: categories } = useQuery({
    queryKey: ["ynab-categories", activePlanId],
    queryFn: () =>
      apiFetch<YnabCategory[]>(`/api/ynab/plans/${activePlanId}/categories`),
    enabled: config?.configured === true && !!activePlanId,
  });

  // Token form
  const tokenForm = useForm<TokenFormData>({
    resolver: standardSchemaResolver(tokenSchema),
  });

  const saveTokenMutation = useMutation({
    mutationFn: (data: TokenFormData) =>
      apiFetch("/api/settings/ynab/token", {
        method: "PUT",
        body: JSON.stringify(data),
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["ynab-config"] });
      queryClient.invalidateQueries({ queryKey: ["ynab-plans"] });
      queryClient.invalidateQueries({ queryKey: ["ynab-accounts"] });
      queryClient.invalidateQueries({ queryKey: ["ynab-categories"] });
      toast("YNAB token saved", "success");
      setShowTokenForm(false);
      tokenForm.reset();
    },
    onError: (err) => {
      toast(err instanceof Error ? err.message : "Failed to save token", "error");
    },
  });

  useEffect(() => {
    if (config) {
      configForm.reset({
        planId: config.planId ?? "",
        accountId: config.accountId ?? "",
        categoryId: config.categoryId ?? "",
        enabled: config.enabled,
      });
    }
  }, [config, configForm]);

  const saveConfigMutation = useMutation({
    mutationFn: (data: ConfigFormValues) => {
      const selectedPlan = plans?.find((p) => p.id === data.planId);
      const selectedAccount = accounts?.find((a) => a.id === data.accountId);
      const selectedCategory = categories?.find((c) => c.id === data.categoryId);

      return apiFetch("/api/settings/ynab", {
        method: "PUT",
        body: JSON.stringify({
          planId: data.planId || null,
          planName: selectedPlan?.name ?? null,
          accountId: data.accountId || null,
          accountName: selectedAccount?.name ?? null,
          categoryId: data.categoryId || null,
          categoryName: selectedCategory?.name ?? null,
          enabled: data.enabled,
        }),
      });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["ynab-config"] });
      toast("YNAB settings saved", "success");
    },
    onError: (err) => {
      toast(err instanceof Error ? err.message : "Failed to save settings", "error");
    },
  });

  const pullMutation = useMutation({
    mutationFn: () => apiFetch("/api/ynab/imports/pull", { method: "POST" }),
    onSuccess: () => {
      toast("YNAB import pull started", "success");
    },
    onError: (err) => {
      toast(err instanceof Error ? err.message : "Pull failed", "error");
    },
  });

  if (isLoading) return <Spinner className="mt-20" />;

  const inputClass =
    "w-full rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-3 py-2 text-sm text-gray-900 dark:text-gray-100 shadow-sm focus:border-blue-500 focus:ring-1 focus:ring-blue-500 focus:outline-none";

  return (
    <div className="mx-auto max-w-2xl space-y-6">
      <h1 className="text-2xl font-bold text-gray-900 dark:text-gray-100">YNAB Settings</h1>

      {/* Token section */}
      <div className="rounded-xl bg-white dark:bg-gray-800 p-6 shadow-sm dark:shadow-gray-900/30">
        <div className="flex items-center justify-between">
          <div>
            <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100">API Token</h2>
            <p className="text-sm text-gray-500 dark:text-gray-400">
              {config?.configured
                ? `Token configured (${config.maskedToken})`
                : "No token configured"}
            </p>
          </div>
          <div className="flex items-center gap-2">
            <span
              className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${
                config?.configured
                  ? "bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400"
                  : "bg-yellow-100 text-yellow-700 dark:bg-yellow-900/30 dark:text-yellow-400"
              }`}
            >
              {config?.configured ? "Connected" : "Not Connected"}
            </span>
            <button
              onClick={() => setShowTokenForm(!showTokenForm)}
              className="rounded-lg border border-gray-300 dark:border-gray-600 px-3 py-1.5 text-sm font-medium text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700"
            >
              {config?.configured ? "Update Token" : "Add Token"}
            </button>
          </div>
        </div>

        {showTokenForm && (
          <form
            onSubmit={tokenForm.handleSubmit((data) =>
              saveTokenMutation.mutate(data)
            )}
            className="mt-4 space-y-3"
          >
            <input
              type="password"
              placeholder="YNAB Personal Access Token"
              {...tokenForm.register("apiToken")}
              className={inputClass}
            />
            {tokenForm.formState.errors.apiToken && (
              <p className="text-xs text-red-600">
                {tokenForm.formState.errors.apiToken.message}
              </p>
            )}
            <div className="flex gap-2">
              <button
                type="submit"
                disabled={saveTokenMutation.isPending}
                className="rounded-lg bg-blue-600 dark:bg-blue-500 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700 dark:hover:bg-blue-600 disabled:opacity-50"
              >
                {saveTokenMutation.isPending ? "Saving..." : "Save Token"}
              </button>
              <button
                type="button"
                onClick={() => setShowTokenForm(false)}
                className="rounded-lg border border-gray-300 dark:border-gray-600 px-4 py-2 text-sm font-medium text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700"
              >
                Cancel
              </button>
            </div>
          </form>
        )}
      </div>

      {/* Config section */}
      {config?.configured && (
        <div className="rounded-xl bg-white dark:bg-gray-800 p-6 shadow-sm dark:shadow-gray-900/30">
          <h2 className="mb-4 text-lg font-semibold text-gray-900 dark:text-gray-100">Sync Configuration</h2>

          <form
            onSubmit={configForm.handleSubmit((data) =>
              saveConfigMutation.mutate(data)
            )}
            className="space-y-4"
          >
            <div className="flex items-center gap-3">
              <label className="relative inline-flex cursor-pointer items-center">
                <input
                  type="checkbox"
                  {...configForm.register("enabled")}
                  className="peer sr-only"
                />
                <div className="peer h-6 w-11 rounded-full bg-gray-200 after:absolute after:left-[2px] after:top-[2px] after:h-5 after:w-5 after:rounded-full after:border after:border-gray-300 after:bg-white after:transition-all peer-checked:bg-blue-600 peer-checked:after:translate-x-full peer-checked:after:border-white peer-focus:ring-2 peer-focus:ring-blue-300" />
              </label>
              <span className="text-sm font-medium text-gray-700 dark:text-gray-300">
                Enable YNAB sync
              </span>
            </div>

            <div>
              <label className="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300">
                Budget
              </label>
              <select {...configForm.register("planId")} className={inputClass}>
                <option value="">Select budget...</option>
                {plans?.map((p) => (
                  <option key={p.id} value={p.id}>
                    {p.name}
                  </option>
                ))}
              </select>
            </div>

            <div>
              <label className="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300">
                Default Account
              </label>
              <select {...configForm.register("accountId")} className={inputClass}>
                <option value="">None</option>
                {accounts?.map((a) => (
                  <option key={a.id} value={a.id}>
                    {a.name}
                  </option>
                ))}
              </select>
            </div>

            <div>
              <label className="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300">
                Default Category
              </label>
              <select {...configForm.register("categoryId")} className={inputClass}>
                <option value="">None</option>
                {categories?.map((c) => (
                  <option key={c.id} value={c.id}>
                    {c.categoryGroupName}: {c.name}
                  </option>
                ))}
              </select>
            </div>

            <div className="flex gap-3">
              <button
                type="submit"
                disabled={saveConfigMutation.isPending}
                className="rounded-lg bg-blue-600 dark:bg-blue-500 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700 dark:hover:bg-blue-600 disabled:opacity-50"
              >
                {saveConfigMutation.isPending ? "Saving..." : "Save Settings"}
              </button>
              <button
                type="button"
                onClick={() => pullMutation.mutate()}
                disabled={pullMutation.isPending}
                className="rounded-lg border border-gray-300 px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50 disabled:opacity-50"
              >
                {pullMutation.isPending ? "Pulling..." : "Pull from YNAB"}
              </button>
            </div>
          </form>
        </div>
      )}
    </div>
  );
}
