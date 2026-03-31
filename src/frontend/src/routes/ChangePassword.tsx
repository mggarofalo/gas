import { useState } from "react";
import { useNavigate } from "@tanstack/react-router";
import { useAuth } from "../contexts/AuthContext";

export function ChangePasswordPage() {
  const { changePassword } = useAuth();
  const navigate = useNavigate();
  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setError("");
    if (newPassword !== confirmPassword) { setError("Passwords do not match"); return; }
    if (newPassword.length < 12) { setError("Password must be at least 12 characters"); return; }
    setLoading(true);
    try { await changePassword(currentPassword, newPassword); navigate({ to: "/" }); }
    catch (err) { setError(err instanceof Error ? err.message : "Failed to change password"); }
    finally { setLoading(false); }
  };

  return (
    <div className="flex min-h-screen items-center justify-center bg-surface">
      <div className="card mx-4 w-full max-w-sm p-6 shadow-lg sm:mx-0">
        <h1 className="mb-2 text-center text-xl font-bold">Change Password</h1>
        <p className="mb-6 text-center text-sm text-text-secondary">You must change your password before continuing.</p>
        <form onSubmit={handleSubmit} className="space-y-4">
          <div><label className="label">Current Password</label><input type="password" value={currentPassword} onChange={(e) => setCurrentPassword(e.target.value)} className="input" required autoFocus /></div>
          <div><label className="label">New Password</label><input type="password" value={newPassword} onChange={(e) => setNewPassword(e.target.value)} className="input" required minLength={12} /></div>
          <div><label className="label">Confirm New Password</label><input type="password" value={confirmPassword} onChange={(e) => setConfirmPassword(e.target.value)} className="input" required /></div>
          {error && <p className="text-sm text-danger-text">{error}</p>}
          <button type="submit" disabled={loading} className="btn-primary w-full py-2">{loading ? "Changing..." : "Change Password"}</button>
        </form>
      </div>
    </div>
  );
}
