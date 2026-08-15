import { expect, test } from "@playwright/test"

import { E2E_PASSWORD, E2E_USERNAME, login } from "./helpers"

test.describe("Authentication", () => {
  test("logs in with valid credentials and lands on the dashboard", async ({ page }) => {
    await login(page)
    await expect(page.getByRole("link", { name: "Dashboard" })).toBeVisible()
    await expect(page.getByRole("button", { name: "Keluar" })).toBeVisible()
  })

  test("shows an error and stays on the login page for wrong credentials", async ({ page }) => {
    await page.goto("/login")
    await page.locator("#emailOrUsername").fill(E2E_USERNAME)
    await page.locator("#password").fill("wrong-password")
    await page.getByRole("button", { name: "Masuk" }).click()

    await expect(page.getByText("Username/email atau password salah.")).toBeVisible()
    await expect(page).toHaveURL(/\/login$/)
  })

  test("redirects unauthenticated visitors to the login page", async ({ page }) => {
    await page.goto("/audits")
    await expect(page).toHaveURL(/\/login$/)
  })

  test("logs out and returns to the login page", async ({ page }) => {
    await login(page)
    await page.getByRole("button", { name: "Keluar" }).click()
    await expect(page).toHaveURL(/\/login$/)
    await expect(page.getByText("Masuk ke IAMS")).toBeVisible()
  })
})
