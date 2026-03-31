import { Link, useNavigate } from "@tanstack/react-router";
import { useAuth } from "../contexts/AuthContext";
import { useState, useCallback, useEffect, type ReactNode } from "react";

const navItems = [
  { to: "/", label: "Dashboard" },
  { to: "/fill-ups", label: "Fill-Ups" },
  { to: "/fill-ups/new", label: "New Fill-Up" },
  { to: "/vehicles", label: "Vehicles" },
] as const;

export function Layout({ children }: { children: ReactNode }) {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const [sidebarOpen, setSidebarOpen] = useState(false);

  const closeSidebar = useCallback(() => setSidebarOpen(false), []);

  // Close sidebar on Escape key
  useEffect(() => {
    const handleKey = (e: KeyboardEvent) => {
      if (e.key === "Escape") closeSidebar();
    };
    if (sidebarOpen) {
      document.addEventListener("keydown", handleKey);
      return () => document.removeEventListener("keydown", handleKey);
    }
  }, [sidebarOpen, closeSidebar]);

  const handleLogout = async () => {
    closeSidebar();
    await logout();
    navigate({ to: "/login" });
  };

  const sidebarContent = (
    <>
      <h1 className="mb-6 pl-4 text-xl font-bold">Gas Tracker</h1>
      <ul className="flex-1 space-y-0.5">
        {navItems.map((item) => (
          <li key={item.to}>
            <Link
              to={item.to}
              className="nav-item [&.active]:nav-item-active"
              onClick={closeSidebar}
            >
              {item.label}
            </Link>
          </li>
        ))}
      </ul>
      <div className="border-t border-border pl-4 pt-3">
        {user && <p className="mb-2 truncate px-3 text-xs text-text-muted">{user.email}</p>}
        <button onClick={handleLogout} className="nav-item w-full text-left">
          Sign Out
        </button>
        <p className="mt-2 px-3 text-[10px] text-text-muted/50">v{__APP_VERSION__}</p>
      </div>
    </>
  );

  return (
    <div className="flex min-h-screen bg-surface text-text-primary">
      {/* Desktop sidebar (always visible at md+) */}
      <nav className="hidden md:flex w-56 shrink-0 flex-col border-r border-border bg-surface-raised py-4 pr-4">
        {sidebarContent}
      </nav>

      {/* Mobile sidebar overlay */}
      <div
        className="sidebar-backdrop md:hidden"
        data-open={sidebarOpen}
        onClick={closeSidebar}
        aria-hidden="true"
      />
      <nav
        className="sidebar-drawer py-4 pr-4 md:hidden"
        data-open={sidebarOpen}
        aria-label="Main navigation"
      >
        {sidebarContent}
      </nav>

      {/* Main content area */}
      <div className="flex flex-1 flex-col">
        {/* Mobile top bar */}
        <header className="flex items-center border-b border-border bg-surface-raised px-4 py-3 md:hidden">
          <button
            onClick={() => setSidebarOpen(true)}
            className="mr-3 flex h-11 w-11 items-center justify-center rounded text-text-secondary hover:bg-surface-hover hover:text-text-primary touch-manipulation"
            aria-label="Open menu"
          >
            <svg xmlns="http://www.w3.org/2000/svg" className="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
              <path strokeLinecap="round" strokeLinejoin="round" d="M3.75 6.75h16.5M3.75 12h16.5m-16.5 5.25h16.5" />
            </svg>
          </button>
          <span className="text-lg font-bold">Gas Tracker</span>
        </header>

        <main className="flex-1 p-4 md:p-6">{children}</main>
      </div>
    </div>
  );
}
