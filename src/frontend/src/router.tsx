import {
  createRouter,
  createRootRoute,
  createRoute,
  redirect,
  Outlet,
} from "@tanstack/react-router";
import { isAuthenticated, mustResetPassword } from "@/lib/api";
import Layout from "@/components/Layout";
import Login from "@/routes/Login";
import ChangePassword from "@/routes/ChangePassword";
import Dashboard from "@/routes/Dashboard";
import FillUps from "@/routes/FillUps";
import NewFillUp from "@/routes/NewFillUp";
import FillUpDetail from "@/routes/FillUpDetail";
import EditFillUp from "@/routes/EditFillUp";
import Vehicles from "@/routes/Vehicles";
import YnabSettings from "@/routes/YnabSettings";
import YnabImports from "@/routes/YnabImports";

function requireAuth() {
  if (!isAuthenticated()) {
    throw redirect({ to: "/login" });
  }
  if (mustResetPassword()) {
    throw redirect({ to: "/change-password" });
  }
}

// Root route
const rootRoute = createRootRoute({
  component: Outlet,
});

// Public routes
const loginRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/login",
  component: Login,
});

const changePasswordRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/change-password",
  beforeLoad: () => {
    if (!isAuthenticated()) {
      throw redirect({ to: "/login" });
    }
  },
  component: ChangePassword,
});

// Authenticated layout wrapper
const authenticatedRoute = createRoute({
  getParentRoute: () => rootRoute,
  id: "authenticated",
  beforeLoad: requireAuth,
  component: () => (
    <Layout>
      <Outlet />
    </Layout>
  ),
});

// Dashboard
const dashboardRoute = createRoute({
  getParentRoute: () => authenticatedRoute,
  path: "/",
  component: Dashboard,
});

// Fill-ups
const fillUpsRoute = createRoute({
  getParentRoute: () => authenticatedRoute,
  path: "/fill-ups",
  component: FillUps,
});

const newFillUpRoute = createRoute({
  getParentRoute: () => authenticatedRoute,
  path: "/fill-ups/new",
  component: NewFillUp,
});

const fillUpDetailRoute = createRoute({
  getParentRoute: () => authenticatedRoute,
  path: "/fill-ups/$fillUpId",
  component: FillUpDetail,
});

const editFillUpRoute = createRoute({
  getParentRoute: () => authenticatedRoute,
  path: "/fill-ups/$fillUpId/edit",
  component: EditFillUp,
});

// Vehicles
const vehiclesRoute = createRoute({
  getParentRoute: () => authenticatedRoute,
  path: "/vehicles",
  component: Vehicles,
});

// YNAB
const ynabSettingsRoute = createRoute({
  getParentRoute: () => authenticatedRoute,
  path: "/ynab/settings",
  component: YnabSettings,
});

const ynabImportsRoute = createRoute({
  getParentRoute: () => authenticatedRoute,
  path: "/ynab/imports",
  component: YnabImports,
});

// Route tree
const routeTree = rootRoute.addChildren([
  loginRoute,
  changePasswordRoute,
  authenticatedRoute.addChildren([
    dashboardRoute,
    fillUpsRoute,
    newFillUpRoute,
    fillUpDetailRoute,
    editFillUpRoute,
    vehiclesRoute,
    ynabSettingsRoute,
    ynabImportsRoute,
  ]),
]);

export const router = createRouter({
  routeTree,
  defaultPreload: "intent",
});

// Type registration
declare module "@tanstack/react-router" {
  interface Register {
    router: typeof router;
  }
}
