import { describe, expect, it } from "vitest";
import { leadSchema } from "./lead";

const messages = { required: "required", invalidEmail: "email", invalidPhone: "phone", consentRequired: "consent" };
const valid = { name: "Ana", phone: "+595 981 123 456", email: "", message: "", consent: true, website: "" };

describe("leadSchema", () => {
  it("accepts a contact without email", () => {
    expect(leadSchema("Contact", messages).safeParse(valid).success).toBe(true);
  });

  it("trims the name and requires it", () => {
    const result = leadSchema("Contact", messages).safeParse({ ...valid, name: "   " });
    expect(result.success).toBe(false);
    expect(result.error?.issues[0]).toMatchObject({ path: ["name"], message: "required" });
  });

  it("requires at least 8 digits in the phone", () => {
    const result = leadSchema("Contact", messages).safeParse({ ...valid, phone: "12-34" });
    expect(result.error?.issues[0]).toMatchObject({ path: ["phone"], message: "phone" });
  });

  it("validates the email only when filled, and requires it for the newsletter", () => {
    expect(leadSchema("Contact", messages).safeParse({ ...valid, email: "nope" }).error?.issues[0]).toMatchObject({ path: ["email"], message: "email" });
    expect(leadSchema("Newsletter", messages).safeParse(valid).error?.issues[0]).toMatchObject({ path: ["email"], message: "required" });
    expect(leadSchema("Newsletter", messages).safeParse({ ...valid, email: "ana@example.com" }).success).toBe(true);
  });

  it("requires consent", () => {
    expect(leadSchema("Contact", messages).safeParse({ ...valid, consent: false }).error?.issues[0]).toMatchObject({ path: ["consent"], message: "consent" });
  });
});
