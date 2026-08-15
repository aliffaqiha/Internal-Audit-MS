import path from "node:path"
import { fileURLToPath } from "node:url"

import { expect, test } from "@playwright/test"

import { expectToast, login, unique } from "./helpers"

const fixture = path.join(path.dirname(fileURLToPath(import.meta.url)), "fixtures", "evidence.pdf")

test.describe("Finding", () => {
  test("creates a finding, uploads evidence and downloads it", async ({ page }) => {
    const title = unique("E2E Temuan")
    await login(page)

    // --- Create finding ---
    await page.goto("/findings")
    await page.getByRole("button", { name: "Buat Temuan" }).click()

    const dialog = page.getByRole("dialog")
    await dialog.locator("#title").fill(title)
    await dialog.locator("#description").fill("Deskripsi temuan hasil audit IT.")
    await dialog.locator("#category").fill("Access Control")
    await dialog.locator("#dueDate").fill("2026-09-30")
    await dialog.locator("select").nth(0).selectOption({ label: "Tinggi" })
    await dialog.locator("select").nth(1).selectOption({ label: "IT" })
    await dialog.locator("#recommendation").fill("Terapkan least-privilege dan review berkala.")
    await dialog.getByRole("button", { name: "Buat Temuan" }).click()

    await expectToast(page, "Temuan audit berhasil dibuat")

    // --- Open detail ---
    await page.getByRole("link", { name: title }).first().click()
    await expect(page.getByRole("button", { name: "Unggah Bukti" })).toBeVisible()

    // --- Upload evidence (valid PDF passes magic-byte sniff) ---
    await page.getByRole("button", { name: "Unggah Bukti" }).click()
    await page.locator('input[type="file"]').first().setInputFiles(fixture)
    await expectToast(page, "Bukti temuan berhasil diunggah!")

    await expect(page.getByText("evidence.pdf")).toBeVisible()
    await expect(page.getByText("v1")).toBeVisible()

    // --- Download evidence ---
    const downloadPromise = page.waitForEvent("download")
    await page.getByRole("button", { name: "Unduh" }).click()
    const download = await downloadPromise
    expect(download.suggestedFilename()).toMatch(/evidence\.pdf$/)
  })
})
