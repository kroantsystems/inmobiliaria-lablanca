import { timingSafeEqual } from "node:crypto";
import { revalidateTag } from "next/cache";

const MAX_TAGS = 128;
const MAX_TAG_LENGTH = 256;

function sameSecret(provided: string, expected: string) {
  const a = Buffer.from(provided);
  const b = Buffer.from(expected);
  return a.length === b.length && timingSafeEqual(a, b);
}

/** Chamado pela API depois de cada alteração confirmada; expira as tags na hora (sem servir conteúdo antigo). */
export async function POST(request: Request) {
  const secret = process.env.REVALIDATE_SECRET;
  if (!secret || !sameSecret(request.headers.get("x-revalidate-secret") ?? "", secret)) {
    return Response.json({ error: "unauthorized" }, { status: 401 });
  }

  let tags: unknown;
  try {
    tags = ((await request.json()) as { tags?: unknown }).tags;
  } catch {
    return Response.json({ error: "invalid body" }, { status: 400 });
  }

  const valid =
    Array.isArray(tags) &&
    tags.length > 0 &&
    tags.length <= MAX_TAGS &&
    tags.every((tag) => typeof tag === "string" && tag.length > 0 && tag.length <= MAX_TAG_LENGTH);
  if (!valid) {
    return Response.json({ error: "invalid tags" }, { status: 400 });
  }

  const unique = [...new Set(tags as string[])];
  for (const tag of unique) {
    revalidateTag(tag, { expire: 0 });
  }

  return Response.json({ revalidated: unique });
}
