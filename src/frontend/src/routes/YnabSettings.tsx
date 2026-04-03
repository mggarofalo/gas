import { useState, useEffect, useRef, useCallback } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { apiFetch } from "../lib/api";
import { Spinner } from "../components/Spinner";
import { useToast } from "../components/Toast";
import type { Vehicle } from "../lib/types";

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

      {/* Backfill section — only when connected */}
      {config?.configured && config?.enabled && (
        <BackfillSection />
      )}
    </div>
  );
}

interface BackfillPreview {
  date: string;
  station: string;
  vehicleName: string;
  vehicleId: string;
  gallons: number;
  pricePerGallon: number;
  totalCost: number;
  octane: number;
  mileage: number;
}

interface BackfillResult {
  total: number;
  matched: number;
  imported: number;
  skipped: number;
  failed: number;
  dryRun: boolean;
  errors: string[];
  warnings: string[];
  preview: BackfillPreview[] | null;
}

function BackfillSection() {
  const { toast } = useToast();
  const { data: vehicles = [] } = useQuery({
    queryKey: ["vehicles"],
    queryFn: () => apiFetch<Vehicle[]>("/vehicles"),
  });

  const [sinceDate, setSinceDate] = useState("");
  const [vehicleMappings, setVehicleMappings] = useState<Record<string, string>>({});
  const [scanResult, setScanResult] = useState<BackfillResult | null>(null);
  const [scanning, setScanning] = useState(false);
  const [importing, setImporting] = useState(false);
  const [importResult, setImportResult] = useState<BackfillResult | null>(null);

  // Discover unique unmapped vehicle names from scan
  const unmappedNames = scanResult?.preview
    ?.map((p) => p.vehicleName)
    .filter((name, i, arr) => arr.indexOf(name) === i && !vehicleMappings[name]) ?? [];

  const allMapped = unmappedNames.length === 0 && (scanResult?.preview?.length ?? 0) > 0;

  const handleScan = useCallback(async () => {
    setScanning(true);
    setScanResult(null);
    setImportResult(null);
    try {
      // Scan with empty mappings first to discover vehicle names, then re-scan with mappings
      // Actually, we need mappings to get past validation. Let's collect all known mappings.
      const mappings = { ...vehicleMappings };
      // For vehicle names we already know, include them
      const result = await apiFetch<BackfillResult>("/admin/ynab-backfill", {
        method: "POST",
        body: JSON.stringify({
          sinceDate: sinceDate || null,
          vehicleMappings: Object.keys(mappings).length > 0 ? mappings : { _placeholder: "00000000-0000-0000-0000-000000000000" },
          dryRun: true,
        }),
      });
      setScanResult(result);
    } catch (e) {
      toast(`Scan failed: ${e instanceof Error ? e.message : "Unknown error"}`);
    } finally {
      setScanning(false);
    }
  }, [sinceDate, vehicleMappings, toast]);

  const handleImport = useCallback(async () => {
    if (!allMapped) return;
    setImporting(true);
    try {
      const result = await apiFetch<BackfillResult>("/admin/ynab-backfill", {
        method: "POST",
        body: JSON.stringify({
          sinceDate: sinceDate || null,
          vehicleMappings,
          dryRun: false,
        }),
      });
      setImportResult(result);
      setScanResult(null);
      toast(`Imported ${result.imported} fill-ups`);
    } catch (e) {
      toast(`Import failed: ${e instanceof Error ? e.message : "Unknown error"}`);
    } finally {
      setImporting(false);
    }
  }, [sinceDate, vehicleMappings, allMapped, toast]);

  const handleMapping = useCallback((vehicleName: string, vehicleId: string) => {
    setVehicleMappings((prev) => {
      if (!vehicleId) {
        const { [vehicleName]: _, ...rest } = prev;
        return rest;
      }
      return { ...prev, [vehicleName]: vehicleId };
    });
  }, []);

  return (
    <div className="card mt-6 space-y-4 p-4 sm:p-6">
      <h3 className="text-lg font-semibold">Backfill from YNAB</h3>
      <p className="text-sm text-text-secondary">
        Scan your YNAB transactions for memos matching the gas fill-up pattern
        and import them as historical fill-ups.
      </p>

      {/* Since date filter */}
      <div>
        <label className="label">Since date <span className="text-text-muted">(optional)</span></label>
        <input
          type="date"
          value={sinceDate}
          onChange={(e) => setSinceDate(e.target.value)}
          className="input max-w-xs"
        />
      </div>

      {/* Vehicle mappings — show all known names from scan errors + preview */}
      {scanResult && (scanResult.errors.length > 0 || unmappedNames.length > 0) && (
        <div className="space-y-2">
          <label className="label">Vehicle mappings</label>
          <p className="text-xs text-text-muted">
            Map vehicle names found in YNAB memos to your vehicles.
          </p>
          {[...new Set([
            ...unmappedNames,
            ...scanResult.errors
              .filter((e) => e.startsWith("Unmapped vehicle"))
              .map((e) => e.match(/Unmapped vehicle '([^']+)'/)?.[1])
              .filter(Boolean) as string[],
          ])].map((name) => (
            <div key={name} className="flex items-center gap-2">
              <span className="w-32 truncate text-sm">{name}</span>
              <select
                value={vehicleMappings[name] ?? ""}
                onChange={(e) => handleMapping(name, e.target.value)}
                className="input flex-1"
              >
                <option value="">Select vehicle...</option>
                {vehicles.map((v) => (
                  <option key={v.id} value={v.id}>{v.label}</option>
                ))}
              </select>
            </div>
          ))}
        </div>
      )}

      {/* Scan button */}
      <button
        onClick={handleScan}
        disabled={scanning}
        className="btn-primary"
      >
        {scanning ? "Scanning..." : "Scan YNAB for gas transactions"}
      </button>

      {/* Scan results */}
      {scanResult && (
        <div className="space-y-3 rounded border border-border p-3">
          <div className="flex flex-wrap gap-4 text-sm">
            <span>Total: <strong>{scanResult.total}</strong></span>
            <span>Matched: <strong>{scanResult.matched}</strong></span>
            <span>New: <strong>{scanResult.imported}</strong></span>
            <span className="text-text-muted">Skipped (dup): {scanResult.skipped}</span>
            {scanResult.failed > 0 && <span className="text-danger-text">Failed: {scanResult.failed}</span>}
          </div>

          {scanResult.warnings.length > 0 && (
            <div className="text-xs text-warning-text">
              {scanResult.warnings.map((w, i) => <p key={i}>{w}</p>)}
            </div>
          )}

          {scanResult.errors.length > 0 && (
            <div className="text-xs text-danger-text">
              {scanResult.errors.map((e, i) => <p key={i}>{e}</p>)}
            </div>
          )}

          {/* Preview table */}
          {scanResult.preview && scanResult.preview.length > 0 && (
            <div className="overflow-x-auto">
              <table className="w-full text-xs">
                <thead>
                  <tr className="border-b border-border text-left text-text-muted">
                    <th className="pb-1 pr-3">Date</th>
                    <th className="pb-1 pr-3">Station</th>
                    <th className="pb-1 pr-3">Vehicle</th>
                    <th className="pb-1 pr-3 text-right">Gallons</th>
                    <th className="pb-1 pr-3 text-right">$/gal</th>
                    <th className="pb-1 text-right">Total</th>
                  </tr>
                </thead>
                <tbody>
                  {scanResult.preview.map((p, i) => (
                    <tr key={i} className="border-b border-border/50">
                      <td className="py-1 pr-3">{p.date}</td>
                      <td className="py-1 pr-3">{p.station}</td>
                      <td className="py-1 pr-3">{p.vehicleName}</td>
                      <td className="py-1 pr-3 text-right">{p.gallons.toFixed(3)}</td>
                      <td className="py-1 pr-3 text-right">${p.pricePerGallon.toFixed(3)}</td>
                      <td className="py-1 text-right">${p.totalCost.toFixed(2)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}

          {/* Import button */}
          {allMapped && scanResult.imported > 0 && (
            <button
              onClick={handleImport}
              disabled={importing}
              className="btn-primary"
            >
              {importing ? "Importing..." : `Import ${scanResult.imported} fill-ups`}
            </button>
          )}
        </div>
      )}

      {/* Import result */}
      {importResult && (
        <div className="rounded border border-success-text/30 bg-success-bg p-3 text-sm">
          Imported <strong>{importResult.imported}</strong> fill-ups.
          {importResult.skipped > 0 && <> Skipped {importResult.skipped} duplicates.</>}
          {importResult.failed > 0 && <span className="text-danger-text"> {importResult.failed} failed.</span>}
        </div>
      )}
    </div>
  );
}
