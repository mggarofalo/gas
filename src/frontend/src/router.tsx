import { lazy, Suspense } from "react";
import {
  createRouter,
  createRootRoute,
  createRoute,
  Outlet,
  redirect,
} from "@tanstack/react-router";
import { Layout } from "./components/Layout";
import { Spinner } from "./components/Spinner";
import { getAccessToken, parseJwt } from "./lib/auth";

function SuspenseRoute({ children }: { children: React.ReactNode }) {
  return (
    <Suspense fallback={<Spinner />}>
      <div className="page-enter">{children}</div>
    </Suspense>
  );
}

const LoginPage = lazy(() => import("./routes/Login").then((m) => ({ default: m.LoginPage })));
const ChangePasswordPage = lazy(() => import("./routes/ChangePassword").then((m) => ({ default: m.ChangePasswordPage })));
const DashboardPage = lazy(() => import("./routes/Dashboard").then((m) => ({ default: m.DashboardPage })));
const FillUpsPage = lazy(() => import("./routes/FillUps").then((m) => ({ default: m.FillUpsPage })));
const NewFillUpPage = lazy(() => import("./routes/NewFillUp").then((m) => ({ default: m.NewFillUpPage })));
const FillUpDetailPage = lazy(() => import("./routes/FillUpDetail").then((m) => ({ default: m.FillUpDetailPage })));
const VehiclesPage = lazy(() => import("./routes/Vehicles").then((m) => ({ default: m.VehiclesPage })));
const YnabSettingsPage = lazy(() => import("./routes/YnabSettings").then((m) => ({ default: m.YnabSettingsPage })));

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

const loginRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/login",
  component: () => <SuspenseRoute><LoginPage /></SuspenseRoute>,
});

const changePasswordRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/change-password",
  component: () => <SuspenseRoute><ChangePasswordPage /></SuspenseRoute>,
});

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
  component: () => <SuspenseRoute><DashboardPage /></SuspenseRoute>,
});

const fillUpsRoute = createRoute({
  getParentRoute: () => authenticatedRoute,
  path: "/fill-ups",
  component: () => <SuspenseRoute><FillUpsPage /></SuspenseRoute>,
});

const newFillUpRoute = createRoute({
  getParentRoute: () => authenticatedRoute,
  path: "/fill-ups/new",
  component: () => <SuspenseRoute><NewFillUpPage /></SuspenseRoute>,
});

const fillUpDetailRoute = createRoute({
  getParentRoute: () => authenticatedRoute,
  path: "/fill-ups/$id",
  component: () => <SuspenseRoute><FillUpDetailPage /></SuspenseRoute>,
});

const vehiclesRoute = createRoute({
  getParentRoute: () => authenticatedRoute,
  path: "/vehicles",
  component: () => <SuspenseRoute><VehiclesPage /></SuspenseRoute>,
});

const ynabSettingsRoute = createRoute({
  getParentRoute: () => authenticatedRoute,
  path: "/settings/ynab",
  component: () => <SuspenseRoute><YnabSettingsPage /></SuspenseRoute>,
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
    ynabSettingsRoute,
  ]),
]);

export const router = createRouter({ routeTree });

declare module "@tanstack/react-router" {
  interface Register {
    router: typeof router;
  }
}
