import { useState } from "react";
import { useNavigate } from "@tanstack/react-router";
import { useAuth } from "../contexts/AuthContext";

export function LoginPage() {
  const { login, isAuthenticated, mustResetPassword } = useAuth();
  const navigate = useNavigate();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);

  if (isAuthenticated && !mustResetPassword) { navigate({ to: "/" }); return null; }
  if (mustResetPassword) { navigate({ to: "/change-password" }); return null; }

  const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setError("");
    setLoading(true);
    try { await login(email, password); }
    catch (err) { setError(err instanceof Error ? err.message : "Login failed"); }
    finally { setLoading(false); }
  };

  return (
    <div className="flex min-h-screen items-center justify-center bg-surface">
      <div className="card mx-4 w-full max-w-sm p-6 shadow-lg sm:mx-0">
        <h1 className="mb-6 text-center text-xl font-bold">Gas Tracker</h1>
        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="label">Email</label>
            <input type="email" inputMode="email" value={email} onChange={(e) => setEmail(e.target.value)} className="input" required autoFocus autoComplete="email" />
          </div>
          <div>
            <label className="label">Password</label>
            <input type="password" value={password} onChange={(e) => setPassword(e.target.value)} className="input" required autoComplete="current-password" />
          </div>
          {error && <p className="text-sm text-danger-text">{error}</p>}
          <button type="submit" disabled={loading} className="btn-primary w-full py-2">{loading ? "Signing in..." : "Sign In"}</button>
        </form>
      </div>
    </div>
  );
}
