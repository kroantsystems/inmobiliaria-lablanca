import { describe, expect, it } from "vitest";
import { ACCEPT_ATTRIBUTE, validateUpload } from "./validation";

const MB = 1024 * 1024;
const file = (name: string, size: number) => ({ name, size });

describe("validateUpload", () => {
  it.each([
    ["foto.png", "image"],
    ["foto.JPG", "image"],
    ["foto.jpeg", "image"],
    ["foto.webp", "image"],
    ["tour.mp4", "video"],
    ["tour.mov", "video"],
    ["tour.webm", "video"],
    ["contrato.pdf", "document"],
    ["modelo.doc", "document"],
    ["modelo.docx", "document"],
  ])("accepts %s as %s", (name, kind) => {
    expect(validateUpload(file(name, MB))).toEqual({ ok: true, kind });
  });

  it.each(["setup.exe", "fotos.zip", "logo.svg", "pagina.html", "sem-extensao"])("rejects %s", (name) => {
    expect(validateUpload(file(name, MB))).toEqual({ ok: false, error: "typeNotAllowed" });
  });

  it("enforces per-kind size limits", () => {
    expect(validateUpload(file("foto.png", 60 * MB))).toEqual({ ok: false, error: "tooLarge", limitMb: 50 });
    expect(validateUpload(file("tour.mp4", 51 * MB))).toEqual({ ok: false, error: "tooLarge", limitMb: 50 });
    expect(validateUpload(file("contrato.pdf", 20 * MB))).toEqual({ ok: false, error: "tooLarge", limitMb: 15 });
    expect(validateUpload(file("foto.png", 50 * MB)).ok).toBe(true);
  });

  it("exposes the accept attribute for file inputs", () => {
    expect(ACCEPT_ATTRIBUTE).toContain(".png");
    expect(ACCEPT_ATTRIBUTE).toContain(".docx");
    expect(ACCEPT_ATTRIBUTE).not.toContain(".svg");
  });
});
