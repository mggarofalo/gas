import {
  createContext,
  useContext,
  useState,
  useEffect,
  useCallback,
  type ReactNode,
} from "react";
import {
  getAccessToken,
  setTokens,
  clearTokens,
  parseJwt,
  type JwtPayload,
} from "../lib/auth";

interface AuthState {
  user: JwtPayload | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  mustResetPassword: boolean;
  login: (email: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
  changePassword: (
    currentPassword: string,
    newPassword: string,
  ) => Promise<void>;
}

const AuthContext = createContext<AuthState | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<JwtPayload | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [mustResetPassword, setMustResetPassword] = useState(false);

  useEffect(() => {
    const token = getAccessToken();
    if (token) {
      const payload = parseJwt(token);
      if (payload && payload.exp * 1000 > Date.now()) {
        setUser(payload);
        setMustResetPassword(payload.mustResetPassword);
      } else {
        clearTokens();
      }
    }
    setIsLoading(false);
  }, []);

  const login = useCallback(async (email: string, password: string) => {
    const res = await fetch("/api/auth/login", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ email, password }),
    });
    if (!res.ok) {
      const body = await res.json().catch(() => ({}));
      throw new Error(body.error || "Login failed");
    }
    const data = await res.json();
    setTokens(data.accessToken, data.refreshToken);
    const payload = parseJwt(data.accessToken)!;
    setUser(payload);
    setMustResetPassword(data.mustResetPassword);
  }, []);

  const logout = useCallback(async () => {
    try {
      const token = getAccessToken();
      if (token) {
        await fetch("/api/auth/logout", {
          method: "POST",
          headers: { Authorization: `Bearer ${token}` },
        });
      }
    } catch {
      // Best-effort
    }
    clearTokens();
    setUser(null);
    setMustResetPassword(false);
  }, []);

  const changePassword = useCallback(
    async (currentPassword: string, newPassword: string) => {
      const token = getAccessToken();
      const res = await fetch("/api/auth/change-password", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${token}`,
        },
        body: JSON.stringify({ currentPassword, newPassword }),
      });
      if (!res.ok) {
        const body = await res.json().catch(() => ({}));
        const errors = body.errors
          ? Object.values(body.errors).flat().join(", ")
          : "Password change failed";
        throw new Error(errors as string);
      }
      const data = await res.json();
      setTokens(data.accessToken, data.refreshToken);
      const payload = parseJwt(data.accessToken)!;
      setUser(payload);
      setMustResetPassword(false);
    },
    [],
  );

  return (
    <AuthContext.Provider
      value={{
        user,
        isAuthenticated: !!user,
        isLoading,
        mustResetPassword,
        login,
        logout,
        changePassword,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used within AuthProvider");
  return ctx;
}
