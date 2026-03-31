import {
  createRouter,
  createRootRoute,
  createRoute,
  Outlet,
  redirect,
} from "@tanstack/react-router";
import { Layout } from "./components/Layout";
import { DashboardPage } from "./routes/Dashboard";
import { FillUpsPage } from "./routes/FillUps";
import { NewFillUpPage } from "./routes/NewFillUp";
import { FillUpDetailPage } from "./routes/FillUpDetail";
import { VehiclesPage } from "./routes/Vehicles";
import { LoginPage } from "./routes/Login";
import { ChangePasswordPage } from "./routes/ChangePassword";
import { getAccessToken, parseJwt } from "./lib/auth";

function requireAuth() {
  const token = getAccessToken();
  if (!token) throw redirect({ to: "/login" });
  const payload = parseJwt(token);
  if (!payload || payload.exp * 1000 < Date.now()) throw redirect({ to: "/login" });
  if (payload.mustResetPassword) throw redirect({ to: "/change-password" });
}

const rootRoute = createRootRoute({
  component: () => <Outlet />,
});

// Public routes (no layout)
const loginRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/login",
  component: LoginPage,
});

const changePasswordRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/change-password",
  component: ChangePasswordPage,
});

// Authenticated layout
const authenticatedRoute = createRoute({
  getParentRoute: () => rootRoute,
  id: "authenticated",
  beforeLoad: () => requireAuth(),
  component: () => (
    <Layout>
      <Outlet />
    </Layout>
  ),
});

const indexRoute = createRoute({
  getParentRoute: () => authenticatedRoute,
  path: "/",
  component: DashboardPage,
});

const fillUpsRoute = createRoute({
  getParentRoute: () => authenticatedRoute,
  path: "/fill-ups",
  component: FillUpsPage,
});

const newFillUpRoute = createRoute({
  getParentRoute: () => authenticatedRoute,
  path: "/fill-ups/new",
  component: NewFillUpPage,
});

const fillUpDetailRoute = createRoute({
  getParentRoute: () => authenticatedRoute,
  path: "/fill-ups/$id",
  component: FillUpDetailPage,
});

const vehiclesRoute = createRoute({
  getParentRoute: () => authenticatedRoute,
  path: "/vehicles",
  component: VehiclesPage,
});

const routeTree = rootRoute.addChildren([
  loginRoute,
  changePasswordRoute,
  authenticatedRoute.addChildren([
    indexRoute,
    fillUpsRoute,
    newFillUpRoute,
    fillUpDetailRoute,
    vehiclesRoute,
  ]),
]);

export const router = createRouter({ routeTree });

declare module "@tanstack/react-router" {
  interface Register {
    router: typeof router;
  }
}
