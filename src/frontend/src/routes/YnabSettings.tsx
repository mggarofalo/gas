import { useState, useEffect } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { standardSchemaResolver } from "@hookform/resolvers/standard-schema";
import { apiFetch } from "@/lib/api";
import type { YnabConfig, YnabAccount, YnabCategory } from "@/lib/types";
import { useToast } from "@/components/Toast";
import Spinner from "@/components/Spinner";

const tokenSchema = z.object({
  token: z.string().min(1, "YNAB personal access token is required"),
});

const configSchema = z.object({
  syncPlan: z.string().min(1, "Sync plan is required"),
  accountId: z.string().nullable().optional(),
  categoryId: z.string().nullable().optional(),
});

type TokenFormData = z.infer<typeof tokenSchema>;
type ConfigFormData = z.infer<typeof configSchema>;

export default function YnabSettings() {
  const queryClient = useQueryClient();
  const { toast } = useToast();
  const [showTokenForm, setShowTokenForm] = useState(false);

  const { data: config, isLoading } = useQuery({
    queryKey: ["ynab-config"],
    queryFn: () => apiFetch<YnabConfig>("/api/ynab/config"),
  });

  const { data: accounts } = useQuery({
    queryKey: ["ynab-accounts"],
    queryFn: () => apiFetch<YnabAccount[]>("/api/ynab/accounts"),
    enabled: config?.hasToken === true,
  });

  const { data: categories } = useQuery({
    queryKey: ["ynab-categories"],
    queryFn: () => apiFetch<YnabCategory[]>("/api/ynab/categories"),
    enabled: config?.hasToken === true,
  });

  // Token form
  const tokenForm = useForm<TokenFormData>({
    resolver: standardSchemaResolver(tokenSchema),
  });

  const saveTokenMutation = useMutation({
    mutationFn: (data: TokenFormData) =>
      apiFetch("/api/ynab/token", {
        method: "PUT",
        body: JSON.stringify(data),
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["ynab-config"] });
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

  // Config form
  const configForm = useForm<ConfigFormData>({
    resolver: standardSchemaResolver(configSchema),
  });

  useEffect(() => {
    if (config) {
      configForm.reset({
        syncPlan: config.syncPlan,
        accountId: config.accountId,
        categoryId: config.categoryId,
      });
    }
  }, [config, configForm]);

  const saveConfigMutation = useMutation({
    mutationFn: (data: ConfigFormData) =>
      apiFetch("/api/ynab/config", {
        method: "PUT",
        body: JSON.stringify(data),
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["ynab-config"] });
      toast("YNAB settings saved", "success");
    },
    onError: (err) => {
      toast(err instanceof Error ? err.message : "Failed to save settings", "error");
    },
  });

  const syncMutation = useMutation({
    mutationFn: () => apiFetch("/api/ynab/sync", { method: "POST" }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["ynab-config"] });
      toast("YNAB sync started", "success");
    },
    onError: (err) => {
      toast(err instanceof Error ? err.message : "Sync failed", "error");
    },
  });

  if (isLoading) return <Spinner className="mt-20" />;

  const inputClass =
    "w-full rounded-lg border border-gray-300 px-3 py-2 text-sm shadow-sm focus:border-blue-500 focus:ring-1 focus:ring-blue-500 focus:outline-none";

  return (
    <div className="mx-auto max-w-2xl space-y-6">
      <h1 className="text-2xl font-bold text-gray-900">YNAB Settings</h1>

      {/* Token section */}
      <div className="rounded-xl bg-white p-6 shadow-sm">
        <div className="flex items-center justify-between">
          <div>
            <h2 className="text-lg font-semibold text-gray-900">API Token</h2>
            <p className="text-sm text-gray-500">
              {config?.hasToken
                ? "Token is configured"
                : "No token configured"}
            </p>
          </div>
          <div className="flex items-center gap-2">
            <span
              className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${
                config?.hasToken
                  ? "bg-green-100 text-green-700"
                  : "bg-yellow-100 text-yellow-700"
              }`}
            >
              {config?.hasToken ? "Connected" : "Not Connected"}
            </span>
            <button
              onClick={() => setShowTokenForm(!showTokenForm)}
              className="rounded-lg border border-gray-300 px-3 py-1.5 text-sm font-medium text-gray-700 hover:bg-gray-50"
            >
              {config?.hasToken ? "Update Token" : "Add Token"}
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
              {...tokenForm.register("token")}
              className={inputClass}
            />
            {tokenForm.formState.errors.token && (
              <p className="text-xs text-red-600">
                {tokenForm.formState.errors.token.message}
              </p>
            )}
            <div className="flex gap-2">
              <button
                type="submit"
                disabled={saveTokenMutation.isPending}
                className="rounded-lg bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700 disabled:opacity-50"
              >
                {saveTokenMutation.isPending ? "Saving..." : "Save Token"}
              </button>
              <button
                type="button"
                onClick={() => setShowTokenForm(false)}
                className="rounded-lg border border-gray-300 px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50"
              >
                Cancel
              </button>
            </div>
          </form>
        )}
      </div>

      {/* Config section */}
      {config?.hasToken && (
        <div className="rounded-xl bg-white p-6 shadow-sm">
          <h2 className="mb-4 text-lg font-semibold text-gray-900">Sync Configuration</h2>

          <form
            onSubmit={configForm.handleSubmit((data) =>
              saveConfigMutation.mutate(data)
            )}
            className="space-y-4"
          >
            <div>
              <label className="mb-1 block text-sm font-medium text-gray-700">
                Sync Plan
              </label>
              <select {...configForm.register("syncPlan")} className={inputClass}>
                <option value="disabled">Disabled</option>
                <option value="import-only">Import Only</option>
                <option value="full-sync">Full Sync</option>
              </select>
            </div>

            <div>
              <label className="mb-1 block text-sm font-medium text-gray-700">
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
              <label className="mb-1 block text-sm font-medium text-gray-700">
                Default Category
              </label>
              <select {...configForm.register("categoryId")} className={inputClass}>
                <option value="">None</option>
                {categories?.map((c) => (
                  <option key={c.id} value={c.id}>
                    {c.groupName}: {c.name}
                  </option>
                ))}
              </select>
            </div>

            <div className="flex gap-3">
              <button
                type="submit"
                disabled={saveConfigMutation.isPending}
                className="rounded-lg bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700 disabled:opacity-50"
              >
                {saveConfigMutation.isPending ? "Saving..." : "Save Settings"}
              </button>
              <button
                type="button"
                onClick={() => syncMutation.mutate()}
                disabled={syncMutation.isPending}
                className="rounded-lg border border-gray-300 px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50 disabled:opacity-50"
              >
                {syncMutation.isPending ? "Syncing..." : "Sync Now"}
              </button>
            </div>
          </form>
        </div>
      )}
    </div>
  );
}
