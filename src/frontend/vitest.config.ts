import { defineConfig } from "vitest/config";
import react from "@vitejs/plugin-react";
import path from "node:path";
import { createRequire } from "node:module";
import type { Plugin } from "vite";

const repoRoot = path.resolve(__dirname, "../..");
const req = createRequire(import.meta.url);

/** Resolve bare imports from test files outside src/frontend/ using the frontend's node_modules. */
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
        try {
          return req.resolve(source);
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
  },
});
