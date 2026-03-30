import { Link } from "@tanstack/react-router";
import type { ReactNode } from "react";

const navItems = [
  { to: "/", label: "Dashboard" },
  { to: "/fill-ups", label: "Fill-Ups" },
  { to: "/fill-ups/new", label: "New Fill-Up" },
  { to: "/vehicles", label: "Vehicles" },
] as const;

export function Layout({ children }: { children: ReactNode }) {
  return (
    <div className="flex min-h-screen bg-gray-50">
      <nav className="w-56 shrink-0 border-r border-gray-200 bg-white p-4">
        <h1 className="mb-6 text-xl font-bold">Gas Tracker</h1>
        <ul className="space-y-1">
          {navItems.map((item) => (
            <li key={item.to}>
              <Link
                to={item.to}
                className="block rounded px-3 py-2 text-sm text-gray-700 hover:bg-gray-100 [&.active]:bg-blue-50 [&.active]:font-medium [&.active]:text-blue-700"
              >
                {item.label}
              </Link>
            </li>
          ))}
        </ul>
      </nav>
      <main className="flex-1 p-6">{children}</main>
    </div>
  );
}
