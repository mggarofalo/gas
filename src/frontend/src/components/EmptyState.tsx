import type { ReactNode } from "react";

export function EmptyState({
  icon,
  message,
  children,
}: {
  icon?: ReactNode;
  message: string;
  children?: ReactNode;
}) {
  return (
    <div className="empty-state">
      {icon && <div className="mb-3 text-text-muted">{icon}</div>}
      <p className="text-sm text-text-muted">{message}</p>
      {children && <div className="mt-3">{children}</div>}
    </div>
  );
}
