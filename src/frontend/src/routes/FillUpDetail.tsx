import { useQuery } from "@tanstack/react-query";
import { Link, useParams } from "@tanstack/react-router";
import { apiFetch } from "@/lib/api";
import type { FillUp } from "@/lib/types";
import Spinner from "@/components/Spinner";

function SyncBadge({ status }: { status: string }) {
  const colors: Record<string, string> = {
    synced: "bg-green-100 text-green-700",
    pending: "bg-yellow-100 text-yellow-700",
    error: "bg-red-100 text-red-700",
    "not-configured": "bg-gray-100 text-gray-500",
    skipped: "bg-gray-100 text-gray-500",
  };
  return (
    <span
      className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${
        colors[status] ?? "bg-gray-100 text-gray-500"
      }`}
    >
      {status}
    </span>
  );
}

export default function FillUpDetail() {
  const { fillUpId } = useParams({ from: "/fill-ups/$fillUpId" as never });

  const { data: fillUp, isLoading } = useQuery({
    queryKey: ["fill-up", fillUpId],
    queryFn: () => apiFetch<FillUp>(`/api/fill-ups/${fillUpId}`),
  });

  if (isLoading) return <Spinner className="mt-20" />;
  if (!fillUp) return <p className="mt-10 text-center text-gray-500">Fill-up not found.</p>;

  return (
    <div className="mx-auto max-w-2xl space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-gray-900">Fill-Up Details</h1>
        <div className="flex gap-2">
          <Link
            to="/fill-ups/$fillUpId/edit"
            params={{ fillUpId }}
            className="rounded-lg border border-gray-300 px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50"
          >
            Edit
          </Link>
          <Link
            to="/fill-ups"
            className="rounded-lg border border-gray-300 px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50"
          >
            Back
          </Link>
        </div>
      </div>

      <div className="rounded-xl bg-white p-6 shadow-sm">
        <dl className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <Field label="Date" value={new Date(fillUp.date).toLocaleDateString()} />
          <Field label="Vehicle" value={fillUp.vehicleLabel} />
          <Field label="Station" value={fillUp.stationName} />
          <Field label="Address" value={fillUp.stationAddress ?? "--"} />
          <Field label="Odometer" value={`${fillUp.odometerMiles.toLocaleString()} mi`} />
          <Field label="Gallons" value={fillUp.gallons.toFixed(3)} />
          <Field label="Price/Gallon" value={`$${fillUp.pricePerGallon.toFixed(3)}`} />
          <Field label="Total Cost" value={`$${fillUp.totalCost.toFixed(2)}`} />

          {fillUp.octaneRating && (
            <Field label="Octane" value={fillUp.octaneRating.toString()} />
          )}
          {fillUp.tripMiles != null && (
            <Field label="Trip Miles" value={fillUp.tripMiles.toFixed(1)} />
          )}
          {fillUp.mpg != null && (
            <Field label="MPG" value={fillUp.mpg.toFixed(1)} />
          )}
          {fillUp.costPerMile != null && (
            <Field label="Cost/Mile" value={`$${fillUp.costPerMile.toFixed(3)}`} />
          )}

          {fillUp.latitude != null && fillUp.longitude != null && (
            <Field
              label="Location"
              value={`${fillUp.latitude.toFixed(5)}, ${fillUp.longitude.toFixed(5)}`}
            />
          )}

          {fillUp.notes && <Field label="Notes" value={fillUp.notes} span2 />}
        </dl>
      </div>

      {/* Sync status */}
      <div className="rounded-xl bg-white p-6 shadow-sm">
        <h2 className="mb-4 text-lg font-semibold text-gray-900">Sync Status</h2>
        <div className="space-y-3">
          <div className="flex items-center justify-between">
            <span className="text-sm text-gray-600">Paperless</span>
            <SyncBadge status={fillUp.paperlessSyncStatus} />
          </div>
          {fillUp.paperlessSyncedAt && (
            <div className="flex items-center justify-between">
              <span className="text-sm text-gray-600">Last Synced</span>
              <span className="text-sm text-gray-900">
                {new Date(fillUp.paperlessSyncedAt).toLocaleString()}
              </span>
            </div>
          )}
          {fillUp.paperlessSyncError && (
            <div className="rounded-lg bg-red-50 p-3 text-sm text-red-700">
              {fillUp.paperlessSyncError}
            </div>
          )}

          <div className="flex items-center justify-between">
            <span className="text-sm text-gray-600">YNAB</span>
            <SyncBadge status={fillUp.ynabSyncStatus} />
          </div>
          {fillUp.ynabAccountName && (
            <div className="flex items-center justify-between">
              <span className="text-sm text-gray-600">YNAB Account</span>
              <span className="text-sm text-gray-900">{fillUp.ynabAccountName}</span>
            </div>
          )}
          {fillUp.ynabCategoryName && (
            <div className="flex items-center justify-between">
              <span className="text-sm text-gray-600">YNAB Category</span>
              <span className="text-sm text-gray-900">{fillUp.ynabCategoryName}</span>
            </div>
          )}
        </div>
      </div>

      {/* Receipt */}
      {fillUp.receiptUrl && (
        <div className="rounded-xl bg-white p-6 shadow-sm">
          <h2 className="mb-4 text-lg font-semibold text-gray-900">Receipt</h2>
          <a
            href={fillUp.receiptUrl}
            target="_blank"
            rel="noopener noreferrer"
            className="inline-flex items-center gap-2 text-sm font-medium text-blue-600 hover:underline"
          >
            View Receipt
            <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M10 6H6a2 2 0 00-2 2v10a2 2 0 002 2h10a2 2 0 002-2v-4M14 4h6m0 0v6m0-6L10 14"
              />
            </svg>
          </a>
        </div>
      )}
    </div>
  );
}

function Field({
  label,
  value,
  span2 = false,
}: {
  label: string;
  value: string;
  span2?: boolean;
}) {
  return (
    <div className={span2 ? "sm:col-span-2" : ""}>
      <dt className="text-sm font-medium text-gray-500">{label}</dt>
      <dd className="mt-1 text-sm text-gray-900">{value}</dd>
    </div>
  );
}
