import react from "@vitejs/plugin-react";
import { defineConfig } from "vite";

// https://vite.dev/config/
export default defineConfig({
  // Keep generated assets relative so IIS can host the client below /Intranet.
  base: "./",
  plugins: [react()],
});
