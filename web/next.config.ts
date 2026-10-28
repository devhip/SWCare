import type { NextConfig } from "next";

// Static export like Easy Next: IIS serves out/<route>/index.html, the API is a separate site (plan 10).
const nextConfig: NextConfig = {
  output: "export",
  trailingSlash: true,
  images: { unoptimized: true },
  reactCompiler: true,
  // `*` = one hostname/IP label. 192.168.*.* covers the office LAN.
  allowedDevOrigins: ["192.168.*.*", "10.*.*.*", "127.0.0.1", "localhost"],
};

export default nextConfig;
