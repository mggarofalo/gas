import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import { ApiError } from "@/lib/api";
import {
  uploadReceiptInBackground,
  pendingReceiptUploads,
} from "@/lib/receiptUpload";

vi.mock("@/lib/api", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@/lib/api")>();
  return {
    ...actual,
    apiFetchRaw: vi.fn(),
  };
});

const { apiFetchRaw } = await import("@/lib/api");
const mockedFetch = vi.mocked(apiFetchRaw);

function okResponse(body: unknown = { id: "f1" }): Response {
  return { json: () => Promise.resolve(body) } as unknown as Response;
}

function makeFile(): File {
  return new File(["fake-bytes"], "receipt.jpg", { type: "image/jpeg" });
}

/** Wait for all in-flight uploads to settle, advancing fake timers as needed. */
async function settle() {
  for (let i = 0; i < 50 && pendingReceiptUploads() > 0; i++) {
    await vi.advanceTimersByTimeAsync(30_000);
  }
}

beforeEach(() => {
  vi.useFakeTimers();
  mockedFetch.mockReset();
});

afterEach(() => {
  vi.useRealTimers();
});

describe("uploadReceiptInBackground", () => {
  it("uploads on the first attempt and reports success", async () => {
    mockedFetch.mockResolvedValueOnce(okResponse({ id: "f1" }));
    const onSuccess = vi.fn();
    const onFailure = vi.fn();

    uploadReceiptInBackground("f1", makeFile(), { onSuccess, onFailure });
    await settle();

    expect(mockedFetch).toHaveBeenCalledTimes(1);
    const [url, options] = mockedFetch.mock.calls[0];
    expect(url).toBe("/api/fill-ups/f1/receipt");
    expect(options?.method).toBe("PUT");
    expect(options?.body).toBeInstanceOf(FormData);
    expect(onSuccess).toHaveBeenCalledWith({ id: "f1" });
    expect(onFailure).not.toHaveBeenCalled();
    expect(pendingReceiptUploads()).toBe(0);
  });

  it("retries transient server errors and eventually succeeds", async () => {
    mockedFetch
      .mockRejectedValueOnce(new ApiError("Server error", 500))
      .mockRejectedValueOnce(new TypeError("Failed to fetch"))
      .mockResolvedValueOnce(okResponse());
    const onSuccess = vi.fn();
    const onFailure = vi.fn();

    uploadReceiptInBackground("f1", makeFile(), { onSuccess, onFailure });
    await settle();

    expect(mockedFetch).toHaveBeenCalledTimes(3);
    expect(onSuccess).toHaveBeenCalledTimes(1);
    expect(onFailure).not.toHaveBeenCalled();
  });

  it("does not retry permanent 4xx failures", async () => {
    mockedFetch.mockRejectedValue(new ApiError("File type not allowed", 400));
    const onSuccess = vi.fn();
    const onFailure = vi.fn();

    uploadReceiptInBackground("f1", makeFile(), { onSuccess, onFailure });
    await settle();

    expect(mockedFetch).toHaveBeenCalledTimes(1);
    expect(onSuccess).not.toHaveBeenCalled();
    expect(onFailure).toHaveBeenCalledTimes(1);
    expect(onFailure.mock.calls[0][0].message).toBe("File type not allowed");
  });

  it("gives up after exhausting retries and reports failure", async () => {
    mockedFetch.mockRejectedValue(new TypeError("Failed to fetch"));
    const onSuccess = vi.fn();
    const onFailure = vi.fn();

    uploadReceiptInBackground("f1", makeFile(), { onSuccess, onFailure });
    await settle();

    // 1 initial attempt + 4 retries
    expect(mockedFetch).toHaveBeenCalledTimes(5);
    expect(onSuccess).not.toHaveBeenCalled();
    expect(onFailure).toHaveBeenCalledTimes(1);
    expect(pendingReceiptUploads()).toBe(0);
  });

  it("registers a beforeunload guard while an upload is pending", async () => {
    let resolveFetch!: (r: Response) => void;
    mockedFetch.mockReturnValueOnce(
      new Promise<Response>((resolve) => {
        resolveFetch = resolve;
      })
    );
    const addSpy = vi.spyOn(window, "addEventListener");
    const removeSpy = vi.spyOn(window, "removeEventListener");

    uploadReceiptInBackground("f1", makeFile());
    expect(pendingReceiptUploads()).toBe(1);
    expect(addSpy).toHaveBeenCalledWith("beforeunload", expect.any(Function));

    resolveFetch(okResponse());
    await settle();

    expect(pendingReceiptUploads()).toBe(0);
    expect(removeSpy).toHaveBeenCalledWith(
      "beforeunload",
      expect.any(Function)
    );

    addSpy.mockRestore();
    removeSpy.mockRestore();
  });
});
