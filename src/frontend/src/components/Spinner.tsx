export function Spinner({ size = "md" }: { size?: "sm" | "md" | "lg" }) {
  const dim = { sm: "h-4 w-4", md: "h-6 w-6", lg: "h-8 w-8" }[size];
  return (
    <div className="flex items-center justify-center py-8">
      <div className={`spinner ${dim}`} />
    </div>
  );
}
