import { Link } from "@tanstack/react-router";

export function FillUpsPage() {
  return (
    <div>
      <div className="mb-4 flex items-center justify-between">
        <h2 className="text-2xl font-semibold">Fill-Up History</h2>
        <Link
          to="/fill-ups/new"
          className="rounded bg-blue-600 px-4 py-2 text-sm text-white hover:bg-blue-700"
        >
          Add Fill-Up
        </Link>
      </div>
      <p className="text-gray-500">Fill-up history table will appear here.</p>
    </div>
  );
}
