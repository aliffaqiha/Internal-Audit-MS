import { expect, test } from "@playwright/test"

import { expectToast, login, unique } from "./helpers"

test.describe("Audit plan lifecycle", () => {
  test("creates, runs and completes an audit plan, then generates and downloads the report", async ({
    page,
  }) => {
    const title = unique("E2E Rencana Audit")
    await login(page)

    // --- Create draft ---
    await page.goto("/audits")
    await page.getByRole("button", { name: "Buat Rencana Audit" }).click()

    const dialog = page.getByRole("dialog")
    await dialog.locator("#title").fill(title)
    await dialog.locator("#standard").selectOption({ label: "IT" })
    await dialog.locator("#departmentId").selectOption({ label: "IT" })
    await dialog.locator("#objective").fill("Verifikasi kontrol akses dan backup.")
    await dialog.locator("#scope").fill("Departemen IT, periode tahun berjalan.")
    await dialog.locator("#startDate").fill("2026-08-01")
    await dialog.locator("#endDate").fill("2026-08-31")
    await dialog.getByRole("button", { name: "Buat Draf" }).click()

    await expectToast(page, "Rencana audit berhasil dibuat")

    // --- Open detail ---
    await page.getByRole("link", { name: title }).first().click()
    await expect(page.getByRole("button", { name: "Submit untuk Persetujuan" })).toBeVisible()

    // --- Submit ---
    await page.getByRole("button", { name: "Submit untuk Persetujuan" }).click()
    await expectToast(page, "Rencana audit diajukan untuk persetujuan")
    await expect(page.locator('span[data-slot="badge"]').filter({ hasText: "Dikirim" })).toBeVisible()

    // --- Approve ---
    await page.getByRole("button", { name: "Setujui" }).click()
    await expectToast(page, "Rencana audit telah disetujui")
    await expect(page.locator('span[data-slot="badge"]').filter({ hasText: "Disetujui" })).toBeVisible()

    // --- Start ---
    await page.getByRole("button", { name: "Mulai Audit" }).click()
    await expectToast(page, "Audit resmi dimulai")
    await expect(page.locator('span[data-slot="badge"]').filter({ hasText: "Berjalan" })).toBeVisible()

    // --- Complete checklist (all items must leave "Pending" before completing) ---
    const checklist = page.locator("table").last()
    const questions = await checklist.locator("tbody tr td:nth-child(2)").allTextContents()
    for (const q of questions) {
      await checklist.locator("tbody tr", { hasText: q }).locator("select").selectOption("Pass")
    }
    await expect
      .poll(() =>
        checklist
          .locator("tbody tr select")
          .evaluateAll((els) => els.every((e) => (e as HTMLSelectElement).value === "Pass"))
      )
      .toBe(true)

    // --- Complete ---
    await page.getByRole("button", { name: "Selesaikan Audit" }).click()
    await expectToast(page, "Audit selesai dilaksanakan")
    await expect(page.locator('span[data-slot="badge"]').filter({ hasText: "Selesai" })).toBeVisible()

    // --- Generate report ---
    await page.getByRole("button", { name: "Buat Laporan" }).click()
    await expectToast(page, "Laporan audit berhasil dibuat!")
    await expect(page.getByText(/Laporan-Audit-.*\.pdf/)).toBeVisible()

    // --- Download report (exercises the blob download path) ---
    const downloadPromise = page.waitForEvent("download")
    await page.getByRole("button", { name: "Unduh PDF" }).click()
    const download = await downloadPromise
    expect(download.suggestedFilename()).toMatch(/\.pdf$/)
  })
})
