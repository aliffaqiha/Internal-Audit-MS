import { expect, type Page } from "@playwright/test"

export const E2E_USERNAME = process.env.E2E_USERNAME ?? "admin"
export const E2E_PASSWORD = process.env.E2E_PASSWORD ?? "Admin1234"

export function unique(prefix: string): string {
  return `${prefix} ${Date.now()} ${Math.floor(Math.random() * 1000)}`
}

export async function login(page: Page, username = E2E_USERNAME, password = E2E_PASSWORD) {
  await page.goto("/login")
  await page.locator("#emailOrUsername").fill(username)
  await page.locator("#password").fill(password)
  await page.getByRole("button", { name: "Masuk" }).click()
  await expect(page).toHaveURL("/", { timeout: 15_000 })
}

export async function expectToast(page: Page, message: string) {
  await expect(page.getByRole("alert").filter({ hasText: message })).toBeVisible()
}

export async function selectOption(page: Page, locator: ReturnType<Page["locator"]>, label: string) {
  await locator.selectOption({ label })
}

/** Login, navigate to a page, and wait for the main content heading to settle. */
export async function openAs(page: Page, path: string, heading: string) {
  await login(page)
  await page.goto(path)
  await expect(page.getByRole("heading", { name: heading })).toBeVisible()
}
