import { describe, expect, it } from "vitest";
import { adminMediaUrl } from "./media";

describe("adminMediaUrl", () => {
  it("maps the public media URL to the authenticated admin URL", () => {
    expect(adminMediaUrl("/api/public/media/01a0ff86-16f5-7460-a172-810c08bae3d6")).toBe("/api/admin/files/01a0ff86-16f5-7460-a172-810c08bae3d6/content");
  });
});
