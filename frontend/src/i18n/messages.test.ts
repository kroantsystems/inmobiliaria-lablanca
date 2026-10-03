import { describe, expect, it } from "vitest";
import en from "../../messages/en.json";
import es from "../../messages/es.json";
import gn from "../../messages/gn.json";
import pt from "../../messages/pt.json";

function keys(node: unknown, prefix = ""): string[] {
  if (node === null || typeof node !== "object") return [prefix];
  return Object.entries(node as Record<string, unknown>).flatMap(([key, value]) =>
    keys(value, prefix ? `${prefix}.${key}` : key),
  );
}

describe("message files", () => {
  const reference = keys(es).sort();

  it.each([
    ["pt", pt],
    ["en", en],
    ["gn", gn],
  ])("%s has exactly the same keys as es", (_, messages) => {
    expect(keys(messages).sort()).toEqual(reference);
  });

  it("has no empty texts", () => {
    for (const messages of [es, pt, en, gn]) {
      const empty = keys(messages).filter((key) => {
        const value = key.split(".").reduce<unknown>((node, part) => (node as Record<string, unknown>)[part], messages);
        return typeof value !== "string" || value.trim() === "";
      });
      expect(empty).toEqual([]);
    }
  });
});
