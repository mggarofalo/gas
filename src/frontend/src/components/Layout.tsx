import { Link, useNavigate } from "@tanstack/react-router";
import { useAuth } from "../contexts/AuthContext";
import type { ReactNode } from "react";

const navItems = [
  { to: "/", label: "Dashboard" },
  { to: "/fill-ups", label: "Fill-Ups" },
  { to: "/fill-ups/new", label: "New Fill-Up" },
  { to: "/vehicles", label: "Vehicles" },
] as const;

export function Layout({ children }: { children: ReactNode }) {
  const { user, logout } = useAuth();
  const navigate = useNavigate();

  const handleLogout = async () => {
    await logout();
    navigate({ to: "/login" });
  };

  return (
    <div className="flex min-h-screen bg-surface text-text-primary">
      <nav className="flex w-56 shrink-0 flex-col border-r border-border bg-surface-raised py-4 pr-4">
        <h1 className="mb-6 pl-4 text-xl font-bold">Gas Tracker</h1>
        <ul className="flex-1 space-y-0.5">
          {navItems.map((item) => (
            <li key={item.to}>
              <Link
                to={item.to}
                className="nav-item [&.active]:nav-item-active"
              >
                {item.label}
              </Link>
            </li>
          ))}
        </ul>
        <div className="border-t border-border pl-4 pt-3">
          {user && <p className="mb-2 truncate px-3 text-xs text-text-muted">{user.email}</p>}
          <button onClick={handleLogout} className="w-full rounded px-3 py-2 text-left text-sm text-text-secondary transition-colors duration-150 hover:bg-surface-hover hover:text-text-primary">
            Sign Out
          </button>
        </div>
      </nav>
      <main className="flex-1 p-6">{children}</main>
    </div>
  );
}
