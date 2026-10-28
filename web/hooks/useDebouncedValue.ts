"use client";

import { useEffect, useState } from "react";

/** คืนค่าหลังหยุดเปลี่ยน `delayMs` — ใช้กับ search ที่ยิง API · ค่าว่างอัปเดตทันที (ปุ่มเคลียร์) */
export function useDebouncedValue<T>(value: T, delayMs = 300): T {
  const skipDebounce = value === "" || value === null || value === undefined;
  const [debounced, setDebounced] = useState(value);

  useEffect(() => {
    if (skipDebounce) return;
    const id = window.setTimeout(() => setDebounced(value), delayMs);
    return () => window.clearTimeout(id);
  }, [value, delayMs, skipDebounce]);

  return skipDebounce ? value : debounced;
}
