import { defineConfig } from "vitest/config";
import react from "@vitejs/plugin-react";
import path from "node:path";
import type { Plugin } from "vite";

const repoRoot = path.resolve(__dirname, "../..");
const frontendNodeModules = path.resolve(__dirname, "node_modules");

/**
 * Resolve bare imports from test files outside src/frontend/ by pointing
 * to the package directory inside the frontend's node_modules. This lets
 * Vite handle entry-point selection (ESM exports map) so source files and
 * test files share the same module instances.
 */
function resolveTestDeps(): Plugin {
  return {
    name: "resolve-test-deps",
    resolveId(source, importer) {
      if (
        importer &&
        importer.includes("/tests/frontend/") &&
        !source.startsWith(".") &&
        !source.startsWith("/") &&
        !source.startsWith("@/") &&
        source !== "vitest"
      ) {
        // Resolve scoped (@org/pkg) and plain packages to frontendNodeModules
        const pkgDir = path.join(frontendNodeModules, ...source.split("/").slice(0, source.startsWith("@") ? 2 : 1));
        try {
          // Return the directory — Vite will resolve the proper entry point
          return this.resolve(source, path.join(pkgDir, "_virtual_importer.js"), { skipSelf: true });
        } catch {
          return null;
        }
      }
      return null;
    },
  };
}

export default defineConfig({
  plugins: [react(), resolveTestDeps()],
  resolve: {
    alias: {
      "@": path.resolve(__dirname, "./src"),
    },
  },
  server: {
    fs: {
      allow: [repoRoot],
    },
  },
  test: {
    environment: "jsdom",
    globals: true,
    setupFiles: ["./test-setup.ts"],
    include: [path.resolve(repoRoot, "tests/frontend/**/*.test.{ts,tsx}")],
    coverage: {
      provider: "v8",
      reportsDirectory: path.resolve(repoRoot, "coverage/frontend"),
      include: ["src/**/*.{ts,tsx}"],
      exclude: ["src/main.tsx", "src/router.tsx"],
    },
  },
});
