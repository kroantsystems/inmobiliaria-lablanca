import { describe, expect, it } from "vitest";
import { videoEmbedUrl } from "./video";

describe("videoEmbedUrl", () => {
  it.each([
    ["https://www.youtube.com/watch?v=dQw4w9WgXcQ", "https://www.youtube-nocookie.com/embed/dQw4w9WgXcQ"],
    ["https://youtu.be/dQw4w9WgXcQ?si=abc", "https://www.youtube-nocookie.com/embed/dQw4w9WgXcQ"],
    ["https://youtube.com/shorts/dQw4w9WgXcQ", "https://www.youtube-nocookie.com/embed/dQw4w9WgXcQ"],
    ["https://m.youtube.com/embed/dQw4w9WgXcQ", "https://www.youtube-nocookie.com/embed/dQw4w9WgXcQ"],
    ["https://vimeo.com/123456789", "https://player.vimeo.com/video/123456789"],
    ["https://player.vimeo.com/video/123456789", "https://player.vimeo.com/video/123456789"],
  ])("converts %s", (url, expected) => {
    expect(videoEmbedUrl(url)).toBe(expected);
  });

  it.each(["https://example.com/video.mp4", "javascript:alert(1)", "https://www.youtube.com/watch?v=<script>", "not a url"])("rejects %s", (url) => {
    expect(videoEmbedUrl(url)).toBeNull();
  });
});
