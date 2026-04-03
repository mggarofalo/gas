import { useState, useEffect, useRef } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { apiFetch } from "../lib/api";
import { Spinner } from "../components/Spinner";
import { useToast } from "../components/Toast";

interface YnabConfig {
  configured: boolean;
  maskedToken?: string;
  planId?: string;
  planName?: string;
  accountId?: string;
  accountName?: string;
  categoryId?: string;
  categoryName?: string;
  enabled?: boolean;
}

interface YnabPlan {
  id: string;
  name: string;
  lastModifiedOn: string | null;
}

interface YnabAccount {
  id: string;
  name: string;
  type: string;
  balance: number;
}

interface YnabCategory {
  id: string;
  name: string;
  categoryGroupName: string;
}

export function YnabSettingsPage() {
  const qc = useQueryClient();
  const { toast } = useToast();

  const { data: config, isLoading } = useQuery({
    queryKey: ["ynab-settings"],
    queryFn: () => apiFetch<YnabConfig>("/settings/ynab"),
  });

  const [token, setToken] = useState("");
  const [planId, setPlanId] = useState("");
  const [planName, setPlanName] = useState("");
  const [accountId, setAccountId] = useState("");
  const [accountName, setAccountName] = useState("");
  const [categoryId, setCategoryId] = useState("");
  const [categoryName, setCategoryName] = useState("");
  const [enabled, setEnabled] = useState(false);
  const [showToken, setShowToken] = useState(false);
  const initialized = useRef(false);

  // Sync local state from server config on initial load only
  useEffect(() => {
    if (config?.configured && !initialized.current) {
      initialized.current = true;
      setPlanId(config.planId ?? "");
      setPlanName(config.planName ?? "");
      setAccountId(config.accountId ?? "");
      setAccountName(config.accountName ?? "");
      setCategoryId(config.categoryId ?? "");
      setCategoryName(config.categoryName ?? "");
      setEnabled(config.enabled ?? false);
    }
  }, [config]);

  // Fetch plans when configured
  const { data: plans = [], isFetching: loadingPlans } = useQuery({
    queryKey: ["ynab-plans"],
    queryFn: () => apiFetch<YnabPlan[]>("/ynab/plans"),
    enabled: !!config?.configured,
  });

  // Fetch accounts when plan is selected
  const { data: accounts = [], isFetching: loadingAccounts } = useQuery({
    queryKey: ["ynab-accounts", planId],
    queryFn: () => apiFetch<YnabAccount[]>(`/ynab/plans/${planId}/accounts`),
    enabled: !!planId && !!config?.configured,
  });

  // Fetch categories when plan is selected
  const { data: categories = [], isFetching: loadingCategories } = useQuery({
    queryKey: ["ynab-categories", planId],
    queryFn: () => apiFetch<YnabCategory[]>(`/ynab/plans/${planId}/categories`),
    enabled: !!planId && !!config?.configured,
  });

  const saveMut = useMutation({
    mutationFn: (body: Record<string, unknown>) =>
      apiFetch("/settings/ynab", { method: "PUT", body: JSON.stringify(body) }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["ynab-settings"] });
      toast("YNAB settings saved");
      setToken("");
      setShowToken(false);
    },
  });

  const deleteMut = useMutation({
    mutationFn: () => apiFetch<void>("/settings/ynab", { method: "DELETE" }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["ynab-settings"] });
      qc.removeQueries({ queryKey: ["ynab-plans"] });
      setPlanId("");
      setPlanName("");
      setAccountId("");
      setAccountName("");
      setCategoryId("");
      setCategoryName("");
      setToken("");
      setEnabled(false);
      initialized.current = false;
      toast("YNAB disconnected");
    },
  });

  const handleSave = () => {
    if (!config?.configured && !token.trim()) return;
    saveMut.mutate({
      apiToken: token.trim() || "unchanged",
      planId: planId || null,
      planName: planName || null,
      accountId: accountId || null,
      accountName: accountName || null,
      categoryId: categoryId || null,
      categoryName: categoryName || null,
      enabled,
    });
  };

  const handlePlanChange = (id: string) => {
    setPlanId(id);
    const plan = plans.find((p) => p.id === id);
    setPlanName(plan?.name ?? "");
    setAccountId("");
    setAccountName("");
    setCategoryId("");
    setCategoryName("");
  };

  const handleAccountChange = (id: string) => {
    setAccountId(id);
    const account = accounts.find((a) => a.id === id);
    setAccountName(account?.name ?? "");
  };

  const handleCategoryChange = (id: string) => {
    setCategoryId(id);
    const cat = categories.find((c) => c.id === id);
    setCategoryName(cat?.name ?? "");
  };

  if (isLoading) return <Spinner />;

  return (
    <div className="mx-auto max-w-lg">
      <h2 className="mb-6 text-2xl font-semibold">YNAB Settings</h2>

      <div className="card space-y-5 p-4 sm:p-6">
        {/* Connection status */}
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-2">
            <span className={`h-2.5 w-2.5 rounded-full ${config?.configured ? "bg-success-text" : "bg-text-muted"}`} />
            <span className="text-sm font-medium">{config?.configured ? "Connected" : "Not connected"}</span>
          </div>
          {config?.configured && (
            <button onClick={() => deleteMut.mutate()} disabled={deleteMut.isPending} className="text-sm text-danger-text hover:underline">
              Disconnect
            </button>
          )}
        </div>

        {/* API Token */}
        <div>
          <label className="label">API Token</label>
          {config?.configured && !showToken ? (
            <div className="flex items-center gap-2">
              <span className="text-sm text-text-secondary">{config.maskedToken}</span>
              <button onClick={() => setShowToken(true)} className="link text-xs">Change</button>
            </div>
          ) : (
            <input
              type="password"
              value={token}
              onChange={(e) => setToken(e.target.value)}
              className="input"
              placeholder="Paste your YNAB personal access token"
            />
          )}
        </div>

        {/* Initial connect button */}
        {!config?.configured && (
          <button onClick={handleSave} disabled={!token.trim() || saveMut.isPending} className="btn-primary w-full">
            {saveMut.isPending ? "Connecting..." : "Connect to YNAB"}
          </button>
        )}

        {/* Configured: show plan/account/category dropdowns */}
        {config?.configured && (
          <>
            {/* Plan (Budget) */}
            <div>
              <label className="label">Budget</label>
              <select value={planId} onChange={(e) => handlePlanChange(e.target.value)} className="input" disabled={loadingPlans}>
                <option value="">{loadingPlans ? "Loading..." : "Select a budget"}</option>
                {plans.map((p) => <option key={p.id} value={p.id}>{p.name}</option>)}
              </select>
            </div>

            {/* Account */}
            {planId && (
              <div>
                <label className="label">Account</label>
                <select value={accountId} onChange={(e) => handleAccountChange(e.target.value)} className="input" disabled={loadingAccounts}>
                  <option value="">{loadingAccounts ? "Loading..." : "Select an account"}</option>
                  {accounts.map((a) => <option key={a.id} value={a.id}>{a.name} ({a.type})</option>)}
                </select>
              </div>
            )}

            {/* Category (optional) */}
            {planId && (
              <div>
                <label className="label">Category <span className="text-text-muted">(optional)</span></label>
                <select value={categoryId} onChange={(e) => handleCategoryChange(e.target.value)} className="input" disabled={loadingCategories}>
                  <option value="">{loadingCategories ? "Loading..." : "Auto-categorize"}</option>
                  {categories.map((c) => <option key={c.id} value={c.id}>{c.categoryGroupName}: {c.name}</option>)}
                </select>
              </div>
            )}

            {/* Enable toggle */}
            <label className="flex items-center gap-3 cursor-pointer">
              <input type="checkbox" checked={enabled} onChange={(e) => setEnabled(e.target.checked)} className="h-4 w-4 accent-accent" />
              <span className="text-sm">Auto-sync fill-ups to YNAB</span>
            </label>

            {/* Save button */}
            <button onClick={handleSave} disabled={saveMut.isPending || (showToken && !token.trim())} className="btn-primary w-full">
              {saveMut.isPending ? "Saving..." : "Save Settings"}
            </button>
          </>
        )}

        {saveMut.isError && <p className="text-sm text-danger-text">Error: {saveMut.error.message}</p>}
        {deleteMut.isError && <p className="text-sm text-danger-text">Error: {deleteMut.error.message}</p>}
      </div>

      {/* Import queue link — only when connected */}
      {config?.configured && config?.enabled && (
        <div className="card mt-6 space-y-3 p-4 sm:p-6">
          <h3 className="text-lg font-semibold">Pull from YNAB</h3>
          <p className="text-sm text-text-secondary">
            Import gas transactions from YNAB into Gas Tracker. Transactions are queued for review before creating fill-ups.
          </p>
          <a href="/settings/ynab/imports" className="btn-primary inline-block">Open Import Queue</a>
        </div>
      )}
    </div>
  );
}

