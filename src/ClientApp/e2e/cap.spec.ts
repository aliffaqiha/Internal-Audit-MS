import { expect, test } from "@playwright/test"

import { login, unique } from "./helpers"

test.describe("Corrective action plan (CAP)", () => {
  test("creates a CAP, runs it through to verification and closes it", async ({ page }) => {
    const findingTitle = unique("E2E Temuan CAP")
    await login(page)

    // --- Seed a finding to attach the CAP to ---
    await page.goto("/findings")
    await page.getByRole("button", { name: "Buat Temuan" }).click()
    const dialog = page.getByRole("dialog")
    await dialog.locator("#title").fill(findingTitle)
    await dialog.getByRole("button", { name: "Buat Temuan" }).click()
    await page.getByRole("link", { name: findingTitle }).first().click()
    await expect(page.getByRole("button", { name: "Buat CAP" })).toBeVisible()

    // --- Create CAP ---
    await page.getByRole("button", { name: "Buat CAP" }).click()
    const capDialog = page.getByRole("dialog")
    await capDialog.locator("#action").fill("Menerapkan review akses bulanan dan hardening server.")
    await capDialog.locator("#picName").fill("Budi IT")
    await capDialog.locator("#targetDate").fill("2026-09-15")
    await capDialog.locator("#progress").fill("0")
    await capDialog.getByRole("button", { name: "Buat CAP" }).click()

    // --- CAP is created with status Open ---
    await expect(page.getByText("Terbuka")).toBeVisible()
    await expect(page.getByText("Menerapkan review akses bulanan dan hardening server.")).toBeVisible()

    // --- Start ---
    await page.getByRole("button", { name: "Mulai" }).click()
    await expect(page.getByText("Berjalan")).toBeVisible()

    // --- Update progress to 100% ---
    await page.getByRole("button", { name: "Ubah" }).click()
    await page.getByRole("dialog").locator("#progress").fill("100")
    await page.getByRole("dialog").getByRole("button", { name: "Simpan Perubahan" }).click()

    // --- Submit for verification ---
    await page.getByRole("button", { name: "Ajukan Verifikasi" }).click()
    await expect(page.getByText("Menunggu Verifikasi")).toBeVisible()

    // --- Approve & close (native confirm dialog) ---
    page.on("dialog", (d) => d.accept())
    await page.getByRole("button", { name: "Setujui & Tutup" }).click()
    await expect(page.getByText("Ditutup")).toBeVisible()
  })
})
