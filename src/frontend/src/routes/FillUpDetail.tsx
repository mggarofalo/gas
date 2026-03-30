import { useParams } from "@tanstack/react-router";

export function FillUpDetailPage() {
  const { id } = useParams({ from: "/fill-ups/$id" });

  return (
    <div>
      <h2 className="mb-4 text-2xl font-semibold">Fill-Up Detail</h2>
      <p className="text-gray-500">Detail for fill-up {id} will appear here.</p>
    </div>
  );
}
