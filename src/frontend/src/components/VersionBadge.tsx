import { useQuery } from "@tanstack/react-query";
import { apiFetch } from "@/lib/api";

interface HealthInfo {
  status: string;
  version?: string;
  commit?: string | null;
}

/** Running app version, sourced from /health (build-arg injected; "dev" locally). */
export default function VersionBadge() {
  const { data } = useQuery({
    queryKey: ["health-version"],
    queryFn: () => apiFetch<HealthInfo>("/health"),
    staleTime: Infinity,
    retry: false,
  });

  if (!data?.version) return null;

  const label = data.version === "dev" ? "dev" : `v${data.version}`;
  return (
    <p
      className="px-3 pt-3 text-xs text-gray-500 dark:text-gray-400"
      title={data.commit ? `Commit ${data.commit}` : undefined}
    >
      Gas Tracker {label}
    </p>
  );
}
