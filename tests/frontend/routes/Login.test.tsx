import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";

const mockNavigate = vi.fn();

vi.mock("@tanstack/react-router", () => ({
  useNavigate: () => mockNavigate,
  useRouter: () => ({ navigate: mockNavigate }),
  useRouterState: () => ({}),
  useMatch: () => ({}),
}));

const mockLogin = vi.fn();
const mockSetTokens = vi.fn();
const mockMustResetPassword = vi.fn().mockReturnValue(false);

vi.mock("@/lib/api", () => ({
  login: (...args: unknown[]) => mockLogin(...args),
  setTokens: (...args: unknown[]) => mockSetTokens(...args),
  mustResetPassword: () => mockMustResetPassword(),
}));

const { default: Login } = await import("@/routes/Login");

describe("Login", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockMustResetPassword.mockReturnValue(false);
  });

  it("renders email and password fields with a sign-in button", () => {
    render(<Login />);

    expect(screen.getByLabelText("Email")).toBeInTheDocument();
    expect(screen.getByLabelText("Password")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Sign in" })).toBeInTheDocument();
  });

  it("renders the app title", () => {
    render(<Login />);
    expect(screen.getByText("Gas Tracker")).toBeInTheDocument();
  });

  it("calls login API with entered credentials on submit", async () => {
    const user = userEvent.setup();
    mockLogin.mockResolvedValueOnce({ accessToken: "a", refreshToken: "r" });
    render(<Login />);

    await user.type(screen.getByLabelText("Email"), "test@example.com");
    await user.type(screen.getByLabelText("Password"), "password123");
    await user.click(screen.getByRole("button", { name: "Sign in" }));

    await waitFor(() => {
      expect(mockLogin).toHaveBeenCalledWith("test@example.com", "password123");
    });
  });

  it("stores tokens and navigates to '/' on successful login", async () => {
    const user = userEvent.setup();
    const tokens = { accessToken: "a", refreshToken: "r" };
    mockLogin.mockResolvedValueOnce(tokens);
    render(<Login />);

    await user.type(screen.getByLabelText("Email"), "test@example.com");
    await user.type(screen.getByLabelText("Password"), "password123");
    await user.click(screen.getByRole("button", { name: "Sign in" }));

    await waitFor(() => {
      expect(mockSetTokens).toHaveBeenCalledWith(tokens);
      expect(mockNavigate).toHaveBeenCalledWith({ to: "/" });
    });
  });

  it("navigates to '/change-password' when mustResetPassword is true", async () => {
    const user = userEvent.setup();
    mockLogin.mockResolvedValueOnce({ accessToken: "a", refreshToken: "r" });
    mockMustResetPassword.mockReturnValue(true);
    render(<Login />);

    await user.type(screen.getByLabelText("Email"), "test@example.com");
    await user.type(screen.getByLabelText("Password"), "password123");
    await user.click(screen.getByRole("button", { name: "Sign in" }));

    await waitFor(() => {
      expect(mockNavigate).toHaveBeenCalledWith({ to: "/change-password" });
    });
  });

  it("displays error message on failed login", async () => {
    const user = userEvent.setup();
    mockLogin.mockRejectedValueOnce(new Error("Invalid credentials"));
    render(<Login />);

    await user.type(screen.getByLabelText("Email"), "test@example.com");
    await user.type(screen.getByLabelText("Password"), "wrong");
    await user.click(screen.getByRole("button", { name: "Sign in" }));

    await waitFor(() => {
      expect(screen.getByText("Invalid credentials")).toBeInTheDocument();
    });
  });

  it("shows 'Signing in...' while submitting", async () => {
    const user = userEvent.setup();
    let resolveLogin!: (v: unknown) => void;
    mockLogin.mockReturnValueOnce(
      new Promise((resolve) => {
        resolveLogin = resolve;
      }),
    );
    render(<Login />);

    await user.type(screen.getByLabelText("Email"), "test@example.com");
    await user.type(screen.getByLabelText("Password"), "password123");
    await user.click(screen.getByRole("button", { name: "Sign in" }));

    await waitFor(() => {
      expect(screen.getByRole("button", { name: "Signing in..." })).toBeDisabled();
    });

    resolveLogin({ accessToken: "a", refreshToken: "r" });
    await waitFor(() => {
      expect(screen.getByRole("button", { name: "Sign in" })).toBeInTheDocument();
    });
  });
});
