import {
  createRouter,
  createRootRoute,
  createRoute,
  Outlet,
} from "@tanstack/react-router";
import { Layout } from "./components/Layout";
import { DashboardPage } from "./routes/Dashboard";
import { FillUpsPage } from "./routes/FillUps";
import { NewFillUpPage } from "./routes/NewFillUp";
import { FillUpDetailPage } from "./routes/FillUpDetail";
import { VehiclesPage } from "./routes/Vehicles";

const rootRoute = createRootRoute({
  component: () => (
    <Layout>
      <Outlet />
    </Layout>
  ),
});

const indexRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/",
  component: DashboardPage,
});

const fillUpsRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/fill-ups",
  component: FillUpsPage,
});

const newFillUpRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/fill-ups/new",
  component: NewFillUpPage,
});

const fillUpDetailRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/fill-ups/$id",
  component: FillUpDetailPage,
});

const vehiclesRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/vehicles",
  component: VehiclesPage,
});

const routeTree = rootRoute.addChildren([
  indexRoute,
  fillUpsRoute,
  newFillUpRoute,
  fillUpDetailRoute,
  vehiclesRoute,
]);

export const router = createRouter({ routeTree });

declare module "@tanstack/react-router" {
  interface Register {
    router: typeof router;
  }
}
