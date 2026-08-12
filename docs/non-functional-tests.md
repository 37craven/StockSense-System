# StockSense — Non-Functional Test Documents

**System Name:** StockSense
**System Type:** Web Application
**User Roles:** Admin, Employee, Customer

This document contains 37 non-functional test documents:

- **Part A:** 18 Web Performance Test Documents (one per module per user role)
- **Part B:** 18 Web Accessibility Test Documents (one per module per user role)
- **Part C:** 1 Mobile Performance Test Document

All result fields are intentionally left blank and marked "To be filled during testing." The QA team executes the tests and records the results using the specified tools against the specified thresholds.

---

# PART A — WEB PERFORMANCE TEST DOCUMENTS

## Common Acceptable Thresholds (Web Performance)

| Metric | Acceptable Range |
|--------|------------------|
| Performance Score | 80–100 |
| Speed Index | 1.5–3.0 seconds |
| FCP (First Contentful Paint) | 1.0–2.0 seconds |
| LCP (Largest Contentful Paint) | 1.8–3.5 seconds |
| TBT (Total Blocking Time) | 100–300 ms |
| CLS (Cumulative Layout Shift) | 0.01–0.10 |

## Common Pre-conditions (Role)

| Role | Pre-condition |
|------|---------------|
| Admin | Browser: Google Chrome (latest version, cleared cache). Network: Chrome DevTools throttling set to "Fast 4G." Device: desktop/laptop. Logged in as an Admin account with seeded system data (sample users, inventory items, transactions, appointments, builds, reports). |
| Employee | Browser: Google Chrome (latest version, cleared cache). Network: Chrome DevTools throttling set to "Fast 4G." Device: desktop/laptop. Logged in as an Employee account with assigned permissions and seeded system data. |
| Customer | Browser: Google Chrome (latest version, cleared cache). Network: Chrome DevTools throttling set to "Fast 4G." Device: desktop/laptop. Logged in as a Customer account with existing profile, prior appointments, and saved builds. |

---

## WEB PERFORMANCE TEST DOCUMENT 1 of 18

| Field | Value |
|-------|-------|
| Test Cycle No. | 1 |
| Application/System Name | StockSense |
| Module Name (UI) | User Access and Role Management (Admin) |
| Pre-condition | Desktop/laptop; Google Chrome (latest, cleared cache); DevTools throttling: Fast 4G; logged in as Admin with seeded users, roles, and permissions. |
| Used Tool(s) | Google Lighthouse, Chrome DevTools |

| Execution Runs | Performance Score | Speed Index (s) | FCP (s) | LCP (s) | TBT (ms) | CLS |
|----------------|-------------------|-----------------|---------|---------|----------|-----|
| 1 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| 2 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| 3 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| 4 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| 5 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| Overall Average | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |

| Prepared By | Administered/Performed By |
|-------------|--------------------------|
| [Blank] | [Blank] |

**Measure:** Load of the user list page, role management page, and user creation/edit form as Admin. Thresholds: Performance Score 80–100; Speed Index 1.5–3.0 s; FCP 1.0–2.0 s; LCP 1.8–3.5 s; TBT 100–300 ms; CLS 0.01–0.10.

---

## WEB PERFORMANCE TEST DOCUMENT 2 of 18

| Field | Value |
|-------|-------|
| Test Cycle No. | 1 |
| Application/System Name | StockSense |
| Module Name (UI) | User Access and Role Management (Employee) |
| Pre-condition | Desktop/laptop; Google Chrome (latest, cleared cache); DevTools throttling: Fast 4G; logged in as Employee with assigned permissions. |
| Used Tool(s) | Google Lighthouse, Chrome DevTools |

| Execution Runs | Performance Score | Speed Index (s) | FCP (s) | LCP (s) | TBT (ms) | CLS |
|----------------|-------------------|-----------------|---------|---------|----------|-----|
| 1 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| 2 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| 3 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| 4 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| 5 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| Overall Average | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |

| Prepared By | Administered/Performed By |
|-------------|--------------------------|
| [Blank] | [Blank] |

**Measure:** Load of the employee profile page, own-role/permission view, and password/security settings page as Employee. Thresholds: Performance Score 80–100; Speed Index 1.5–3.0 s; FCP 1.0–2.0 s; LCP 1.8–3.5 s; TBT 100–300 ms; CLS 0.01–0.10.

---

## WEB PERFORMANCE TEST DOCUMENT 3 of 18

| Field | Value |
|-------|-------|
| Test Cycle No. | 1 |
| Application/System Name | StockSense |
| Module Name (UI) | User Access and Role Management (Customer) |
| Pre-condition | Desktop/laptop; Google Chrome (latest, cleared cache); DevTools throttling: Fast 4G; logged in as Customer with existing profile. |
| Used Tool(s) | Google Lighthouse, Chrome DevTools |

| Execution Runs | Performance Score | Speed Index (s) | FCP (s) | LCP (s) | TBT (ms) | CLS |
|----------------|-------------------|-----------------|---------|---------|----------|-----|
| 1 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| 2 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| 3 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| 4 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| 5 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| Overall Average | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |

| Prepared By | Administered/Performed By |
|-------------|--------------------------|
| [Blank] | [Blank] |

**Measure:** Load of the customer registration/sign-in page and the customer account/profile page. Thresholds: Performance Score 80–100; Speed Index 1.5–3.0 s; FCP 1.0–2.0 s; LCP 1.8–3.5 s; TBT 100–300 ms; CLS 0.01–0.10.

---

## WEB PERFORMANCE TEST DOCUMENT 4 of 18

| Field | Value |
|-------|-------|
| Test Cycle No. | 1 |
| Application/System Name | StockSense |
| Module Name (UI) | Inventory and Procurement Management (Admin) |
| Pre-condition | Desktop/laptop; Google Chrome (latest, cleared cache); DevTools throttling: Fast 4G; logged in as Admin with seeded inventory items, categories, suppliers, and procurement orders. |
| Used Tool(s) | Google Lighthouse, Chrome DevTools |

| Execution Runs | Performance Score | Speed Index (s) | FCP (s) | LCP (s) | TBT (ms) | CLS |
|----------------|-------------------|-----------------|---------|---------|----------|-----|
| 1 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| 2 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| 3 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| 4 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| 5 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| Overall Average | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |

| Prepared By | Administered/Performed By |
|-------------|--------------------------|
| [Blank] | [Blank] |

**Measure:** Load of the inventory item list (full catalog), inventory item detail/edit page, stock levels page, and procurement order list/PO creation page as Admin. Thresholds: Performance Score 80–100; Speed Index 1.5–3.0 s; FCP 1.0–2.0 s; LCP 1.8–3.5 s; TBT 100–300 ms; CLS 0.01–0.10.

---

## WEB PERFORMANCE TEST DOCUMENT 5 of 18

| Field | Value |
|-------|-------|
| Test Cycle No. | 1 |
| Application/System Name | StockSense |
| Module Name (UI) | Inventory and Procurement Management (Employee) |
| Pre-condition | Desktop/laptop; Google Chrome (latest, cleared cache); DevTools throttling: Fast 4G; logged in as Employee with seeded inventory items and purchase requests. |
| Used Tool(s) | Google Lighthouse, Chrome DevTools |

| Execution Runs | Performance Score | Speed Index (s) | FCP (s) | LCP (s) | TBT (ms) | CLS |
|----------------|-------------------|-----------------|---------|---------|----------|-----|
| 1 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| 2 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| 3 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| 4 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| 5 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| Overall Average | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |

| Prepared By | Administered/Performed By |
|-------------|--------------------------|
| [Blank] | [Blank] |

**Measure:** Load of the inventory lookup/search page, stock adjustment form, and purchase request submission page as Employee. Thresholds: Performance Score 80–100; Speed Index 1.5–3.0 s; FCP 1.0–2.0 s; LCP 1.8–3.5 s; TBT 100–300 ms; CLS 0.01–0.10.

---

## WEB PERFORMANCE TEST DOCUMENT 6 of 18

| Field | Value |
|-------|-------|
| Test Cycle No. | 1 |
| Application/System Name | StockSense |
| Module Name (UI) | Sales and Transaction Management (Admin) |
| Pre-condition | Desktop/laptop; Google Chrome (latest, cleared cache); DevTools throttling: Fast 4G; logged in as Admin with seeded products, completed sales, and transaction records. |
| Used Tool(s) | Google Lighthouse, Chrome DevTools |

| Execution Runs | Performance Score | Speed Index (s) | FCP (s) | LCP (s) | TBT (ms) | CLS |
|----------------|-------------------|-----------------|---------|---------|----------|-----|
| 1 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| 2 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| 3 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| 4 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| 5 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| Overall Average | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |

| Prepared By | Administered/Performed By |
|-------------|--------------------------|
| [Blank] | [Blank] |

**Measure:** Load of the Point-of-Sale (POS) page, sales history/transaction list, transaction detail page, and sales returns/cancellations page as Admin. Thresholds: Performance Score 80–100; Speed Index 1.5–3.0 s; FCP 1.0–2.0 s; LCP 1.8–3.5 s; TBT 100–300 ms; CLS 0.01–0.10.

---

## WEB PERFORMANCE TEST DOCUMENT 7 of 18

| Field | Value |
|-------|-------|
| Test Cycle No. | 1 |
| Application/System Name | StockSense |
| Module Name (UI) | Sales and Transaction Management (Employee) |
| Pre-condition | Desktop/laptop; Google Chrome (latest, cleared cache); DevTools throttling: Fast 4G; logged in as Employee (cashier) with seeded products; POS terminal behaviour. |
| Used Tool(s) | Google Lighthouse, Chrome DevTools |

| Execution Runs | Performance Score | Speed Index (s) | FCP (s) | LCP (s) | TBT (ms) | CLS |
|----------------|-------------------|-----------------|---------|---------|----------|-----|
| 1 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| 2 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| 3 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| 4 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| 5 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| Overall Average | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |

| Prepared By | Administered/Performed By |
|-------------|--------------------------|
| [Blank] | [Blank] |

**Measure:** Load of the POS page with product catalog and cart, order slip list, and receipt/invoice generation view as Employee. Thresholds: Performance Score 80–100; Speed Index 1.5–3.0 s; FCP 1.0–2.0 s; LCP 1.8–3.5 s; TBT 100–300 ms; CLS 0.01–0.10.

---

## WEB PERFORMANCE TEST DOCUMENT 8 of 18

| Field | Value |
|-------|-------|
| Test Cycle No. | 1 |
| Application/System Name | StockSense |
| Module Name (UI) | Appointment and Service Management (Admin) |
| Pre-condition | Desktop/laptop; Google Chrome (latest, cleared cache); DevTools throttling: Fast 4G; logged in as Admin with seeded appointments, service schedules, and service records. |
| Used Tool(s) | Google Lighthouse, Chrome DevTools |

| Execution Runs | Performance Score | Speed Index (s) | FCP (s) | LCP (s) | TBT (ms) | CLS |
|----------------|-------------------|-----------------|---------|---------|----------|-----|
| 1 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| 2 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| 3 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| 4 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| 5 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| Overall Average | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |

| Prepared By | Administered/Performed By |
|-------------|--------------------------|
| [Blank] | [Blank] |

**Measure:** Load of the appointment calendar/schedule view, appointment list and detail page, and service status management page as Admin. Thresholds: Performance Score 80–100; Speed Index 1.5–3.0 s; FCP 1.0–2.0 s; LCP 1.8–3.5 s; TBT 100–300 ms; CLS 0.01–0.10.

---

## WEB PERFORMANCE TEST DOCUMENT 9 of 18

| Field | Value |
|-------|-------|
| Test Cycle No. | 1 |
| Application/System Name | StockSense |
| Module Name (UI) | Appointment and Service Management (Employee) |
| Pre-condition | Desktop/laptop; Google Chrome (latest, cleared cache); DevTools throttling: Fast 4G; logged in as Employee (service technician/mechanic) with seeded appointments. |
| Used Tool(s) | Google Lighthouse, Chrome DevTools |

| Execution Runs | Performance Score | Speed Index (s) | FCP (s) | LCP (s) | TBT (ms) | CLS |
|----------------|-------------------|-----------------|---------|---------|----------|-----|
| 1 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| 2 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| 3 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| 4 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| 5 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| Overall Average | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |

| Prepared By | Administered/Performed By |
|-------------|--------------------------|
| [Blank] | [Blank] |

**Measure:** Load of the service queue/assigned appointments view, service job detail with checklist, and appointment status update page as Employee. Thresholds: Performance Score 80–100; Speed Index 1.5–3.0 s; FCP 1.0–2.0 s; LCP 1.8–3.5 s; TBT 100–300 ms; CLS 0.01–0.10.

---

## WEB PERFORMANCE TEST DOCUMENT 10 of 18

| Field | Value |
|-------|-------|
| Test Cycle No. | 1 |
| Application/System Name | StockSense |
| Module Name (UI) | Appointment and Service Management (Customer) |
| Pre-condition | Desktop/laptop; Google Chrome (latest, cleared cache); DevTools throttling: Fast 4G; logged in as Customer with saved motorcycle and prior appointments. |
| Used Tool(s) | Google Lighthouse, Chrome DevTools |

| Execution Runs | Performance Score | Speed Index (s) | FCP (s) | LCP (s) | TBT (ms) | CLS |
|----------------|-------------------|-----------------|---------|---------|----------|-----|
| 1 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| 2 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| 3 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| 4 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| 5 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| Overall Average | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |

| Prepared By | Administered/Performed By |
|-------------|--------------------------|
| [Blank] | [Blank] |

**Measure:** Load of the appointment booking form (date/service selection), appointment confirmation page, and customer's appointment history list. Thresholds: Performance Score 80–100; Speed Index 1.5–3.0 s; FCP 1.0–2.0 s; LCP 1.8–3.5 s; TBT 100–300 ms; CLS 0.01–0.10.

---

## WEB PERFORMANCE TEST DOCUMENT 11 of 18

| Field | Value |
|-------|-------|
| Test Cycle No. | 1 |
| Application/System Name | StockSense |
| Module Name (UI) | Motorcycle Build Management (Admin) |
| Pre-condition | Desktop/laptop; Google Chrome (latest, cleared cache); DevTools throttling: Fast 4G; logged in as Admin with seeded parts catalog and saved builds. |
| Used Tool(s) | Google Lighthouse, Chrome DevTools |

| Execution Runs | Performance Score | Speed Index (s) | FCP (s) | LCP (s) | TBT (ms) | CLS |
|----------------|-------------------|-----------------|---------|---------|----------|-----|
| 1 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| 2 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| 3 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| 4 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| 5 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| Overall Average | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |

| Prepared By | Administered/Performed By |
|-------------|--------------------------|
| [Blank] | [Blank] |

**Measure:** Load of the build catalog/list of customer builds, build detail with parts configuration tree, and build approval/pricing page as Admin. Thresholds: Performance Score 80–100; Speed Index 1.5–3.0 s; FCP 1.0–2.0 s; LCP 1.8–3.5 s; TBT 100–300 ms; CLS 0.01–0.10.

---

## WEB PERFORMANCE TEST DOCUMENT 12 of 18

| Field | Value |
|-------|-------|
| Test Cycle No. | 1 |
| Application/System Name | StockSense |
| Module Name (UI) | Motorcycle Build Management (Employee) |
| Pre-condition | Desktop/laptop; Google Chrome (latest, cleared cache); DevTools throttling: Fast 4G; logged in as Employee with seeded parts catalog. |
| Used Tool(s) | Google Lighthouse, Chrome DevTools |

| Execution Runs | Performance Score | Speed Index (s) | FCP (s) | LCP (s) | TBT (ms) | CLS |
|----------------|-------------------|-----------------|---------|---------|----------|-----|
| 1 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| 2 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| 3 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| 4 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| 5 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| Overall Average | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |

| Prepared By | Administered/Performed By |
|-------------|--------------------------|
| [Blank] | [Blank] |

**Measure:** Load of the build configurator (part selection/categories), build summary with live total price, and save/quote build actions page as Employee. Thresholds: Performance Score 80–100; Speed Index 1.5–3.0 s; FCP 1.0–2.0 s; LCP 1.8–3.5 s; TBT 100–300 ms; CLS 0.01–0.10.

---

## WEB PERFORMANCE TEST DOCUMENT 13 of 18

| Field | Value |
|-------|-------|
| Test Cycle No. | 1 |
| Application/System Name | StockSense |
| Module Name (UI) | Motorcycle Build Management (Customer) |
| Pre-condition | Desktop/laptop; Google Chrome (latest, cleared cache); DevTools throttling: Fast 4G; logged in as Customer with seeded parts catalog. |
| Used Tool(s) | Google Lighthouse, Chrome DevTools |

| Execution Runs | Performance Score | Speed Index (s) | FCP (s) | LCP (s) | TBT (ms) | CLS |
|----------------|-------------------|-----------------|---------|---------|----------|-----|
| 1 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| 2 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| 3 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| 4 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| 5 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| Overall Average | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |

| Prepared By | Administered/Performed By |
|-------------|--------------------------|
| [Blank] | [Blank] |

**Measure:** Load of the public build configurator, build summary with total price, and saved builds list of the customer. Thresholds: Performance Score 80–100; Speed Index 1.5–3.0 s; FCP 1.0–2.0 s; LCP 1.8–3.5 s; TBT 100–300 ms; CLS 0.01–0.10.

---

## WEB PERFORMANCE TEST DOCUMENT 14 of 18

| Field | Value |
|-------|-------|
| Test Cycle No. | 1 |
| Application/System Name | StockSense |
| Module Name (UI) | Dashboard and Reporting (Admin) |
| Pre-condition | Desktop/laptop; Google Chrome (latest, cleared cache); DevTools throttling: Fast 4G; logged in as Admin with seeded transaction, inventory, and service data across reporting periods. |
| Used Tool(s) | Google Lighthouse, Chrome DevTools |

| Execution Runs | Performance Score | Speed Index (s) | FCP (s) | LCP (s) | TBT (ms) | CLS |
|----------------|-------------------|-----------------|---------|---------|----------|-----|
| 1 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| 2 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| 3 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| 4 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| 5 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| Overall Average | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |

| Prepared By | Administered/Performed By |
|-------------|--------------------------|
| [Blank] | [Blank] |

**Measure:** Load of the full dashboard (KPIs, charts, summary cards), sales report page, inventory report page, and service report page as Admin. Thresholds: Performance Score 80–100; Speed Index 1.5–3.0 s; FCP 1.0–2.0 s; LCP 1.8–3.5 s; TBT 100–300 ms; CLS 0.01–0.10.

---

## WEB PERFORMANCE TEST DOCUMENT 15 of 18

| Field | Value |
|-------|-------|
| Test Cycle No. | 1 |
| Application/System Name | StockSense |
| Module Name (UI) | Dashboard and Reporting (Employee) |
| Pre-condition | Desktop/laptop; Google Chrome (latest, cleared cache); DevTools throttling: Fast 4G; logged in as Employee with seeded data limited to role-visible reports. |
| Used Tool(s) | Google Lighthouse, Chrome DevTools |

| Execution Runs | Performance Score | Speed Index (s) | FCP (s) | LCP (s) | TBT (ms) | CLS |
|----------------|-------------------|-----------------|---------|---------|----------|-----|
| 1 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| 2 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| 3 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| 4 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| 5 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| Overall Average | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |

| Prepared By | Administered/Performed By |
|-------------|--------------------------|
| [Blank] | [Blank] |

**Measure:** Load of the employee dashboard view (role-limited KPIs and charts) and applicable report views. Thresholds: Performance Score 80–100; Speed Index 1.5–3.0 s; FCP 1.0–2.0 s; LCP 1.8–3.5 s; TBT 100–300 ms; CLS 0.01–0.10.

---

## WEB PERFORMANCE TEST DOCUMENT 16 of 18

| Field | Value |
|-------|-------|
| Test Cycle No. | 1 |
| Application/System Name | StockSense |
| Module Name (UI) | AI-Assisted Support (Admin) |
| Pre-condition | Desktop/laptop; Google Chrome (latest, cleared cache); DevTools throttling: Fast 4G; logged in as Admin; AI service available and seeded with inventory/sales context. |
| Used Tool(s) | Google Lighthouse, Chrome DevTools |

| Execution Runs | Performance Score | Speed Index (s) | FCP (s) | LCP (s) | TBT (ms) | CLS |
|----------------|-------------------|-----------------|---------|---------|----------|-----|
| 1 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| 2 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| 3 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| 4 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| 5 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| Overall Average | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |

| Prepared By | Administered/Performed By |
|-------------|--------------------------|
| [Blank] | [Blank] |

**Measure:** Load of the AI support chat interface and the AI chat history/insights page as Admin (chat response latency measured separately in QA functional testing; this document measures page rendering performance). Thresholds: Performance Score 80–100; Speed Index 1.5–3.0 s; FCP 1.0–2.0 s; LCP 1.8–3.5 s; TBT 100–300 ms; CLS 0.01–0.10.

---

## WEB PERFORMANCE TEST DOCUMENT 17 of 18

| Field | Value |
|-------|-------|
| Test Cycle No. | 1 |
| Application/System Name | StockSense |
| Module Name (UI) | AI-Assisted Support (Employee) |
| Pre-condition | Desktop/laptop; Google Chrome (latest, cleared cache); DevTools throttling: Fast 4G; logged in as Employee; AI service available with inventory/sales context. |
| Used Tool(s) | Google Lighthouse, Chrome DevTools |

| Execution Runs | Performance Score | Speed Index (s) | FCP (s) | LCP (s) | TBT (ms) | CLS |
|----------------|-------------------|-----------------|---------|---------|----------|-----|
| 1 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| 2 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| 3 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| 4 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| 5 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| Overall Average | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |

| Prepared By | Administered/Performed By |
|-------------|--------------------------|
| [Blank] | [Blank] |

**Measure:** Load of the AI support chat interface as Employee (e.g., stock lookup, part compatibility, service guidance queries). Thresholds: Performance Score 80–100; Speed Index 1.5–3.0 s; FCP 1.0–2.0 s; LCP 1.8–3.5 s; TBT 100–300 ms; CLS 0.01–0.10.

---

## WEB PERFORMANCE TEST DOCUMENT 18 of 18

| Field | Value |
|-------|-------|
| Test Cycle No. | 1 |
| Application/System Name | StockSense |
| Module Name (UI) | AI-Assisted Support (Customer) |
| Pre-condition | Desktop/laptop; Google Chrome (latest, cleared cache); DevTools throttling: Fast 4G; logged in as Customer; AI service available with public/customer knowledge base. |
| Used Tool(s) | Google Lighthouse, Chrome DevTools |

| Execution Runs | Performance Score | Speed Index (s) | FCP (s) | LCP (s) | TBT (ms) | CLS |
|----------------|-------------------|-----------------|---------|---------|----------|-----|
| 1 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| 2 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| 3 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| 4 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| 5 | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |
| Overall Average | To be filled | To be filled | To be filled | To be filled | To be filled | To be filled |

| Prepared By | Administered/Performed By |
|-------------|--------------------------|
| [Blank] | [Blank] |

**Measure:** Load of the AI support chat interface as Customer (e.g., product inquiries, service availability, order status support). Thresholds: Performance Score 80–100; Speed Index 1.5–3.0 s; FCP 1.0–2.0 s; LCP 1.8–3.5 s; TBT 100–300 ms; CLS 0.01–0.10.

---

# PART B — WEB ACCESSIBILITY TEST DOCUMENTS

## Common Acceptable Thresholds (Web Accessibility)

| Metric | Acceptable Range |
|--------|------------------|
| Compliance Score | 80–100 |
| ERRORS | 0–5 |
| PASSED AUDIT | 35–50 |

## Accessibility Checks Included in Every Scenario

1. Color contrast (text and UI elements against background)
2. Alt text on images
3. ARIA labels on interactive elements
4. Keyboard navigation (full page operable via Tab/Enter/Space/Escape)
5. Focus indicators (visible focus outline on all interactive elements)
6. Form labels (every input/select/textarea has an associated label)
7. Heading structure (correct heading hierarchy, no skipped levels)
8. Screen reader compatibility (content read in logical order, landmarks present)

## WEB ACCESSIBILITY TEST DOCUMENT 1 of 18

| Field | Value |
|-------|-------|
| Test Cycle No. | 1 |
| Application/System Name | StockSense |
| Module Name (UI) | User Access and Role Management (Admin) |
| Pre-condition | Desktop/laptop; Google Chrome (latest); screen reader available (NVDA on Windows or VoiceOver on macOS); logged in as Admin; user list, role management, and user creation pages open. |
| Used Tool(s) | WAVE, axe DevTools |

| Execution Runs | Compliance Score | ERRORS | PASSED AUDIT | NA AUDIT | REMARKS/COMMENTS |
|----------------|------------------|--------|--------------|----------|------------------|
| 1 | To be filled | To be filled | To be filled | To be filled | To be filled |
| 2 | To be filled | To be filled | To be filled | To be filled | To be filled |
| 3 | To be filled | To be filled | To be filled | To be filled | To be filled |
| 4 | To be filled | To be filled | To be filled | To be filled | To be filled |
| 5 | To be filled | To be filled | To be filled | To be filled | To be filled |
| Overall Average | To be filled | To be filled | To be filled | To be filled | To be filled |

| Prepared By | Administered/Performed By |
|-------------|--------------------------|
| [Blank] | [Blank] |

**Measure:** All 8 accessibility checks on user list, role management, and user creation/edit pages. Thresholds: Compliance Score 80–100; ERRORS 0–5; PASSED AUDIT 35–50.

---

## WEB ACCESSIBILITY TEST DOCUMENT 2 of 18

| Field | Value |
|-------|-------|
| Test Cycle No. | 1 |
| Application/System Name | StockSense |
| Module Name (UI) | User Access and Role Management (Employee) |
| Pre-condition | Desktop/laptop; Google Chrome (latest); screen reader available; logged in as Employee; profile and security settings pages open. |
| Used Tool(s) | WAVE, axe DevTools |

| Execution Runs | Compliance Score | ERRORS | PASSED AUDIT | NA AUDIT | REMARKS/COMMENTS |
|----------------|------------------|--------|--------------|----------|------------------|
| 1 | To be filled | To be filled | To be filled | To be filled | To be filled |
| 2 | To be filled | To be filled | To be filled | To be filled | To be filled |
| 3 | To be filled | To be filled | To be filled | To be filled | To be filled |
| 4 | To be filled | To be filled | To be filled | To be filled | To be filled |
| 5 | To be filled | To be filled | To be filled | To be filled | To be filled |
| Overall Average | To be filled | To be filled | To be filled | To be filled | To be filled |

| Prepared By | Administered/Performed By |
|-------------|--------------------------|
| [Blank] | [Blank] |

**Measure:** All 8 accessibility checks on employee profile and security/settings pages. Thresholds: Compliance Score 80–100; ERRORS 0–5; PASSED AUDIT 35–50.

---

## WEB ACCESSIBILITY TEST DOCUMENT 3 of 18

| Field | Value |
|-------|-------|
| Test Cycle No. | 1 |
| Application/System Name | StockSense |
| Module Name (UI) | User Access and Role Management (Customer) |
| Pre-condition | Desktop/laptop; Google Chrome (latest); screen reader available; registration page and logged-in customer account page open. |
| Used Tool(s) | WAVE, axe DevTools |

| Execution Runs | Compliance Score | ERRORS | PASSED AUDIT | NA AUDIT | REMARKS/COMMENTS |
|----------------|------------------|--------|--------------|----------|------------------|
| 1 | To be filled | To be filled | To be filled | To be filled | To be filled |
| 2 | To be filled | To be filled | To be filled | To be filled | To be filled |
| 3 | To be filled | To be filled | To be filled | To be filled | To be filled |
| 4 | To be filled | To be filled | To be filled | To be filled | To be filled |
| 5 | To be filled | To be filled | To be filled | To be filled | To be filled |
| Overall Average | To be filled | To be filled | To be filled | To be filled | To be filled |

| Prepared By | Administered/Performed By |
|-------------|--------------------------|
| [Blank] | [Blank] |

**Measure:** All 8 accessibility checks on registration/sign-in and customer account pages. Thresholds: Compliance Score 80–100; ERRORS 0–5; PASSED AUDIT 35–50.

---

## WEB ACCESSIBILITY TEST DOCUMENT 4 of 18

| Field | Value |
|-------|-------|
| Test Cycle No. | 1 |
| Application/System Name | StockSense |
| Module Name (UI) | Inventory and Procurement Management (Admin) |
| Pre-condition | Desktop/laptop; Google Chrome (latest); screen reader available; logged in as Admin; inventory list, item detail, stock levels, and procurement order pages open. |
| Used Tool(s) | WAVE, axe DevTools |

| Execution Runs | Compliance Score | ERRORS | PASSED AUDIT | NA AUDIT | REMARKS/COMMENTS |
|----------------|------------------|--------|--------------|----------|------------------|
| 1 | To be filled | To be filled | To be filled | To be filled | To be filled |
| 2 | To be filled | To be filled | To be filled | To be filled | To be filled |
| 3 | To be filled | To be filled | To be filled | To be filled | To be filled |
| 4 | To be filled | To be filled | To be filled | To be filled | To be filled |
| 5 | To be filled | To be filled | To be filled | To be filled | To be filled |
| Overall Average | To be filled | To be filled | To be filled | To be filled | To be filled |

| Prepared By | Administered/Performed By |
|-------------|--------------------------|
| [Blank] | [Blank] |

**Measure:** All 8 accessibility checks on inventory management and procurement pages. Thresholds: Compliance Score 80–100; ERRORS 0–5; PASSED AUDIT 35–50.

---

## WEB ACCESSIBILITY TEST DOCUMENT 5 of 18

| Field | Value |
|-------|-------|
| Test Cycle No. | 1 |
| Application/System Name | StockSense |
| Module Name (UI) | Inventory and Procurement Management (Employee) |
| Pre-condition | Desktop/laptop; Google Chrome (latest); screen reader available; logged in as Employee; inventory lookup, stock adjustment, and purchase request pages open. |
| Used Tool(s) | WAVE, axe DevTools |

| Execution Runs | Compliance Score | ERRORS | PASSED AUDIT | NA AUDIT | REMARKS/COMMENTS |
|----------------|------------------|--------|--------------|----------|------------------|
| 1 | To be filled | To be filled | To be filled | To be filled | To be filled |
| 2 | To be filled | To be filled | To be filled | To be filled | To be filled |
| 3 | To be filled | To be filled | To be filled | To be filled | To be filled |
| 4 | To be filled | To be filled | To be filled | To be filled | To be filled |
| 5 | To be filled | To be filled | To be filled | To be filled | To be filled |
| Overall Average | To be filled | To be filled | To be filled | To be filled | To be filled |

| Prepared By | Administered/Performed By |
|-------------|--------------------------|
| [Blank] | [Blank] |

**Measure:** All 8 accessibility checks on inventory lookup and stock adjustment pages. Thresholds: Compliance Score 80–100; ERRORS 0–5; PASSED AUDIT 35–50.

---

## WEB ACCESSIBILITY TEST DOCUMENT 6 of 18

| Field | Value |
|-------|-------|
| Test Cycle No. | 1 |
| Application/System Name | StockSense |
| Module Name (UI) | Sales and Transaction Management (Admin) |
| Pre-condition | Desktop/laptop; Google Chrome (latest); screen reader available; logged in as Admin; POS, sales history, transaction detail, and returns pages open. |
| Used Tool(s) | WAVE, axe DevTools |

| Execution Runs | Compliance Score | ERRORS | PASSED AUDIT | NA AUDIT | REMARKS/COMMENTS |
|----------------|------------------|--------|--------------|----------|------------------|
| 1 | To be filled | To be filled | To be filled | To be filled | To be filled |
| 2 | To be filled | To be filled | To be filled | To be filled | To be filled |
| 3 | To be filled | To be filled | To be filled | To be filled | To be filled |
| 4 | To be filled | To be filled | To be filled | To be filled | To be filled |
| 5 | To be filled | To be filled | To be filled | To be filled | To be filled |
| Overall Average | To be filled | To be filled | To be filled | To be filled | To be filled |

| Prepared By | Administered/Performed By |
|-------------|--------------------------|
| [Blank] | [Blank] |

**Measure:** All 8 accessibility checks on POS and transaction management pages. Thresholds: Compliance Score 80–100; ERRORS 0–5; PASSED AUDIT 35–50.

---

## WEB ACCESSIBILITY TEST DOCUMENT 7 of 18

| Field | Value |
|-------|-------|
| Test Cycle No. | 1 |
| Application/System Name | StockSense |
| Module Name (UI) | Sales and Transaction Management (Employee) |
| Pre-condition | Desktop/laptop; Google Chrome (latest); screen reader available; logged in as Employee; POS page with catalog and cart, order slips, and receipt view open. |
| Used Tool(s) | WAVE, axe DevTools |

| Execution Runs | Compliance Score | ERRORS | PASSED AUDIT | NA AUDIT | REMARKS/COMMENTS |
|----------------|------------------|--------|--------------|----------|------------------|
| 1 | To be filled | To be filled | To be filled | To be filled | To be filled |
| 2 | To be filled | To be filled | To be filled | To be filled | To be filled |
| 3 | To be filled | To be filled | To be filled | To be filled | To be filled |
| 4 | To be filled | To be filled | To be filled | To be filled | To be filled |
| 5 | To be filled | To be filled | To be filled | To be filled | To be filled |
| Overall Average | To be filled | To be filled | To be filled | To be filled | To be filled |

| Prepared By | Administered/Performed By |
|-------------|--------------------------|
| [Blank] | [Blank] |

**Measure:** All 8 accessibility checks on POS page (including cart drawer), order slips, and receipts. Thresholds: Compliance Score 80–100; ERRORS 0–5; PASSED AUDIT 35–50.

---

## WEB ACCESSIBILITY TEST DOCUMENT 8 of 18

| Field | Value |
|-------|-------|
| Test Cycle No. | 1 |
| Application/System Name | StockSense |
| Module Name (UI) | Appointment and Service Management (Admin) |
| Pre-condition | Desktop/laptop; Google Chrome (latest); screen reader available; logged in as Admin; calendar, appointment list/detail, and service status pages open. |
| Used Tool(s) | WAVE, axe DevTools |

| Execution Runs | Compliance Score | ERRORS | PASSED AUDIT | NA AUDIT | REMARKS/COMMENTS |
|----------------|------------------|--------|--------------|----------|------------------|
| 1 | To be filled | To be filled | To be filled | To be filled | To be filled |
| 2 | To be filled | To be filled | To be filled | To be filled | To be filled |
| 3 | To be filled | To be filled | To be filled | To be filled | To be filled |
| 4 | To be filled | To be filled | To be filled | To be filled | To be filled |
| 5 | To be filled | To be filled | To be filled | To be filled | To be filled |
| Overall Average | To be filled | To be filled | To be filled | To be filled | To be filled |

| Prepared By | Administered/Performed By |
|-------------|--------------------------|
| [Blank] | [Blank] |

**Measure:** All 8 accessibility checks on appointment schedule, list, and service management pages. Thresholds: Compliance Score 80–100; ERRORS 0–5; PASSED AUDIT 35–50.

---

## WEB ACCESSIBILITY TEST DOCUMENT 9 of 18

| Field | Value |
|-------|-------|
| Test Cycle No. | 1 |
| Application/System Name | StockSense |
| Module Name (UI) | Appointment and Service Management (Employee) |
| Pre-condition | Desktop/laptop; Google Chrome (latest); screen reader available; logged in as Employee; service queue, job detail, and status update pages open. |
| Used Tool(s) | WAVE, axe DevTools |

| Execution Runs | Compliance Score | ERRORS | PASSED AUDIT | NA AUDIT | REMARKS/COMMENTS |
|----------------|------------------|--------|--------------|----------|------------------|
| 1 | To be filled | To be filled | To be filled | To be filled | To be filled |
| 2 | To be filled | To be filled | To be filled | To be filled | To be filled |
| 3 | To be filled | To be filled | To be filled | To be filled | To be filled |
| 4 | To be filled | To be filled | To be filled | To be filled | To be filled |
| 5 | To be filled | To be filled | To be filled | To be filled | To be filled |
| Overall Average | To be filled | To be filled | To be filled | To be filled | To be filled |

| Prepared By | Administered/Performed By |
|-------------|--------------------------|
| [Blank] | [Blank] |

**Measure:** All 8 accessibility checks on service queue, job detail, and status update pages. Thresholds: Compliance Score 80–100; ERRORS 0–5; PASSED AUDIT 35–50.

---

## WEB ACCESSIBILITY TEST DOCUMENT 10 of 18

| Field | Value |
|-------|-------|
| Test Cycle No. | 1 |
| Application/System Name | StockSense |
| Module Name (UI) | Appointment and Service Management (Customer) |
| Pre-condition | Desktop/laptop; Google Chrome (latest); screen reader available; logged in as Customer; booking form, confirmation, and history pages open. |
| Used Tool(s) | WAVE, axe DevTools |

| Execution Runs | Compliance Score | ERRORS | PASSED AUDIT | NA AUDIT | REMARKS/COMMENTS |
|----------------|------------------|--------|--------------|----------|------------------|
| 1 | To be filled | To be filled | To be filled | To be filled | To be filled |
| 2 | To be filled | To be filled | To be filled | To be filled | To be filled |
| 3 | To be filled | To be filled | To be filled | To be filled | To be filled |
| 4 | To be filled | To be filled | To be filled | To be filled | To be filled |
| 5 | To be filled | To be filled | To be filled | To be filled | To be filled |
| Overall Average | To be filled | To be filled | To be filled | To be filled | To be filled |

| Prepared By | Administered/Performed By |
|-------------|--------------------------|
| [Blank] | [Blank] |

**Measure:** All 8 accessibility checks on appointment booking form, confirmation, and appointment history pages. Thresholds: Compliance Score 80–100; ERRORS 0–5; PASSED AUDIT 35–50.

---

## WEB ACCESSIBILITY TEST DOCUMENT 11 of 18

| Field | Value |
|-------|-------|
| Test Cycle No. | 1 |
| Application/System Name | StockSense |
| Module Name (UI) | Motorcycle Build Management (Admin) |
| Pre-condition | Desktop/laptop; Google Chrome (latest); screen reader available; logged in as Admin; build list, build detail, and approval pages open. |
| Used Tool(s) | WAVE, axe DevTools |

| Execution Runs | Compliance Score | ERRORS | PASSED AUDIT | NA AUDIT | REMARKS/COMMENTS |
|----------------|------------------|--------|--------------|----------|------------------|
| 1 | To be filled | To be filled | To be filled | To be filled | To be filled |
| 2 | To be filled | To be filled | To be filled | To be filled | To be filled |
| 3 | To be filled | To be filled | To be filled | To be filled | To be filled |
| 4 | To be filled | To be filled | To be filled | To be filled | To be filled |
| 5 | To be filled | To be filled | To be filled | To be filled | To be filled |
| Overall Average | To be filled | To be filled | To be filled | To be filled | To be filled |

| Prepared By | Administered/Performed By |
|-------------|--------------------------|
| [Blank] | [Blank] |

**Measure:** All 8 accessibility checks on build catalog, build detail, and approval/pricing pages. Thresholds: Compliance Score 80–100; ERRORS 0–5; PASSED AUDIT 35–50.

---

## WEB ACCESSIBILITY TEST DOCUMENT 12 of 18

| Field | Value |
|-------|-------|
| Test Cycle No. | 1 |
| Application/System Name | StockSense |
| Module Name (UI) | Motorcycle Build Management (Employee) |
| Pre-condition | Desktop/laptop; Google Chrome (latest); screen reader available; logged in as Employee; build configurator and build summary pages open. |
| Used Tool(s) | WAVE, axe DevTools |

| Execution Runs | Compliance Score | ERRORS | PASSED AUDIT | NA AUDIT | REMARKS/COMMENTS |
|----------------|------------------|--------|--------------|----------|------------------|
| 1 | To be filled | To be filled | To be filled | To be filled | To be filled |
| 2 | To be filled | To be filled | To be filled | To be filled | To be filled |
| 3 | To be filled | To be filled | To be filled | To be filled | To be filled |
| 4 | To be filled | To be filled | To be filled | To be filled | To be filled |
| 5 | To be filled | To be filled | To be filled | To be filled | To be filled |
| Overall Average | To be filled | To be filled | To be filled | To be filled | To be filled |

| Prepared By | Administered/Performed By |
|-------------|--------------------------|
| [Blank] | [Blank] |

**Measure:** All 8 accessibility checks on the build configurator and live-price summary. Thresholds: Compliance Score 80–100; ERRORS 0–5; PASSED AUDIT 35–50.

---

## WEB ACCESSIBILITY TEST DOCUMENT 13 of 18

| Field | Value |
|-------|-------|
| Test Cycle No. | 1 |
| Application/System Name | StockSense |
| Module Name (UI) | Motorcycle Build Management (Customer) |
| Pre-condition | Desktop/laptop; Google Chrome (latest); screen reader available; logged in as Customer; public configurator, summary, and saved builds pages open. |
| Used Tool(s) | WAVE, axe DevTools |

| Execution Runs | Compliance Score | ERRORS | PASSED AUDIT | NA AUDIT | REMARKS/COMMENTS |
|----------------|------------------|--------|--------------|----------|------------------|
| 1 | To be filled | To be filled | To be filled | To be filled | To be filled |
| 2 | To be filled | To be filled | To be filled | To be filled | To be filled |
| 3 | To be filled | To be filled | To be filled | To be filled | To be filled |
| 4 | To be filled | To be filled | To be filled | To be filled | To be filled |
| 5 | To be filled | To be filled | To be filled | To be filled | To be filled |
| Overall Average | To be filled | To be filled | To be filled | To be filled | To be filled |

| Prepared By | Administered/Performed By |
|-------------|--------------------------|
| [Blank] | [Blank] |

**Measure:** All 8 accessibility checks on the customer-facing configurator and saved builds list. Thresholds: Compliance Score 80–100; ERRORS 0–5; PASSED AUDIT 35–50.

---

## WEB ACCESSIBILITY TEST DOCUMENT 14 of 18

| Field | Value |
|-------|-------|
| Test Cycle No. | 1 |
| Application/System Name | StockSense |
| Module Name (UI) | Dashboard and Reporting (Admin) |
| Pre-condition | Desktop/laptop; Google Chrome (latest); screen reader available; logged in as Admin; dashboard and report pages open with seeded data. |
| Used Tool(s) | WAVE, axe DevTools |

| Execution Runs | Compliance Score | ERRORS | PASSED AUDIT | NA AUDIT | REMARKS/COMMENTS |
|----------------|------------------|--------|--------------|----------|------------------|
| 1 | To be filled | To be filled | To be filled | To be filled | To be filled |
| 2 | To be filled | To be filled | To be filled | To be filled | To be filled |
| 3 | To be filled | To be filled | To be filled | To be filled | To be filled |
| 4 | To be filled | To be filled | To be filled | To be filled | To be filled |
| 5 | To be filled | To be filled | To be filled | To be filled | To be filled |
| Overall Average | To be filled | To be filled | To be filled | To be filled | To be filled |

| Prepared By | Administered/Performed By |
|-------------|--------------------------|
| [Blank] | [Blank] |

**Measure:** All 8 accessibility checks on dashboard (KPI cards, charts with accessible data tables/alternative text) and report pages. Thresholds: Compliance Score 80–100; ERRORS 0–5; PASSED AUDIT 35–50.

---

## WEB ACCESSIBILITY TEST DOCUMENT 15 of 18

| Field | Value |
|-------|-------|
| Test Cycle No. | 1 |
| Application/System Name | StockSense |
| Module Name (UI) | Dashboard and Reporting (Employee) |
| Pre-condition | Desktop/laptop; Google Chrome (latest); screen reader available; logged in as Employee; role-limited dashboard open. |
| Used Tool(s) | WAVE, axe DevTools |

| Execution Runs | Compliance Score | ERRORS | PASSED AUDIT | NA AUDIT | REMARKS/COMMENTS |
|----------------|------------------|--------|--------------|----------|------------------|
| 1 | To be filled | To be filled | To be filled | To be filled | To be filled |
| 2 | To be filled | To be filled | To be filled | To be filled | To be filled |
| 3 | To be filled | To be filled | To be filled | To be filled | To be filled |
| 4 | To be filled | To be filled | To be filled | To be filled | To be filled |
| 5 | To be filled | To be filled | To be filled | To be filled | To be filled |
| Overall Average | To be filled | To be filled | To be filled | To be filled | To be filled |

| Prepared By | Administered/Performed By |
|-------------|--------------------------|
| [Blank] | [Blank] |

**Measure:** All 8 accessibility checks on the employee dashboard view. Thresholds: Compliance Score 80–100; ERRORS 0–5; PASSED AUDIT 35–50.

---

## WEB ACCESSIBILITY TEST DOCUMENT 16 of 18

| Field | Value |
|-------|-------|
| Test Cycle No. | 1 |
| Application/System Name | StockSense |
| Module Name (UI) | AI-Assisted Support (Admin) |
| Pre-condition | Desktop/laptop; Google Chrome (latest); screen reader available; logged in as Admin; AI chat interface open and functional. |
| Used Tool(s) | WAVE, axe DevTools |

| Execution Runs | Compliance Score | ERRORS | PASSED AUDIT | NA AUDIT | REMARKS/COMMENTS |
|----------------|------------------|--------|--------------|----------|------------------|
| 1 | To be filled | To be filled | To be filled | To be filled | To be filled |
| 2 | To be filled | To be filled | To be filled | To be filled | To be filled |
| 3 | To be filled | To be filled | To be filled | To be filled | To be filled |
| 4 | To be filled | To be filled | To be filled | To be filled | To be filled |
| 5 | To be filled | To be filled | To be filled | To be filled | To be filled |
| Overall Average | To be filled | To be filled | To be filled | To be filled | To be filled |

| Prepared By | Administered/Performed By |
|-------------|--------------------------|
| [Blank] | [Blank] |

**Measure:** All 8 accessibility checks on the AI chat interface (input field and label, send button, message region with role status, dynamic updates announced to screen reader). Thresholds: Compliance Score 80–100; ERRORS 0–5; PASSED AUDIT 35–50.

---

## WEB ACCESSIBILITY TEST DOCUMENT 17 of 18

| Field | Value |
|-------|-------|
| Test Cycle No. | 1 |
| Application/System Name | StockSense |
| Module Name (UI) | AI-Assisted Support (Employee) |
| Pre-condition | Desktop/laptop; Google Chrome (latest); screen reader available; logged in as Employee; AI chat interface open and functional. |
| Used Tool(s) | WAVE, axe DevTools |

| Execution Runs | Compliance Score | ERRORS | PASSED AUDIT | NA AUDIT | REMARKS/COMMENTS |
|----------------|------------------|--------|--------------|----------|------------------|
| 1 | To be filled | To be filled | To be filled | To be filled | To be filled |
| 2 | To be filled | To be filled | To be filled | To be filled | To be filled |
| 3 | To be filled | To be filled | To be filled | To be filled | To be filled |
| 4 | To be filled | To be filled | To be filled | To be filled | To be filled |
| 5 | To be filled | To be filled | To be filled | To be filled | To be filled |
| Overall Average | To be filled | To be filled | To be filled | To be filled | To be filled |

| Prepared By | Administered/Performed By |
|-------------|--------------------------|
| [Blank] | [Blank] |

**Measure:** All 8 accessibility checks on the AI chat interface as used by employees. Thresholds: Compliance Score 80–100; ERRORS 0–5; PASSED AUDIT 35–50.

---

## WEB ACCESSIBILITY TEST DOCUMENT 18 of 18

| Field | Value |
|-------|-------|
| Test Cycle No. | 1 |
| Application/System Name | StockSense |
| Module Name (UI) | AI-Assisted Support (Customer) |
| Pre-condition | Desktop/laptop; Google Chrome (latest); screen reader available; logged in as Customer; AI chat interface open and functional. |
| Used Tool(s) | WAVE, axe DevTools |

| Execution Runs | Compliance Score | ERRORS | PASSED AUDIT | NA AUDIT | REMARKS/COMMENTS |
|----------------|------------------|--------|--------------|----------|------------------|
| 1 | To be filled | To be filled | To be filled | To be filled | To be filled |
| 2 | To be filled | To be filled | To be filled | To be filled | To be filled |
| 3 | To be filled | To be filled | To be filled | To be filled | To be filled |
| 4 | To be filled | To be filled | To be filled | To be filled | To be filled |
| 5 | To be filled | To be filled | To be filled | To be filled | To be filled |
| Overall Average | To be filled | To be filled | To be filled | To be filled | To be filled |

| Prepared By | Administered/Performed By |
|-------------|--------------------------|
| [Blank] | [Blank] |

**Measure:** All 8 accessibility checks on the customer-facing AI chat interface. Thresholds: Compliance Score 80–100; ERRORS 0–5; PASSED AUDIT 35–50.

---

# PART C — MOBILE PERFORMANCE TEST DOCUMENT

## Common Acceptable Thresholds (Mobile Performance)

| Network | Load Time | Response Time | Rendering Time |
|---------|-----------|---------------|----------------|
| Wi-Fi | < 2.0 seconds | 1–3 seconds | 1–3 seconds |
| 4G | < 3.0 seconds | 1–3 seconds | 1–3 seconds |
| 3G | < 4.0 seconds | 1–3 seconds | 1–3 seconds |

## MOBILE PERFORMANCE TEST DOCUMENT 1 of 1

| Field | Value |
|-------|-------|
| Date Tested | To be filled during testing |
| Application/System Name | StockSense |
| Pre-condition | Smartphone (e.g., iPhone/Android, min 375×667 viewport) connected to the specified network, or Chrome DevTools device emulation with network throttling (Wi-Fi / 4G / 3G). Browser cache cleared before each run. Test each network condition (Wi-Fi, 4G, 3G) separately and log the network used per run. |
| Used Tool(s) | Google Lighthouse Mobile, Chrome DevTools |

| Execution Runs | Application Load Time (s) | Application Response Time (s) | Screen Rendering Time (s) |
|----------------|--------------------------|------------------------------|---------------------------|
| 1 | To be filled | To be filled | To be filled |
| 2 | To be filled | To be filled | To be filled |
| 3 | To be filled | To be filled | To be filled |
| 4 | To be filled | To be filled | To be filled |
| 5 | To be filled | To be filled | To be filled |
| Overall Average | To be filled | To be filled | To be filled |

| Prepared By | Administered/Performed By |
|-------------|--------------------------|
| [Blank] | [Blank] |

**Definitions:**
- **Application Load Time:** Time from request until the application is usable (blank screen gone, login/dashboard rendered).
- **Application Response Time:** Time from user tap/input until the application reacts (e.g., button press, page navigation, item added to cart).
- **Screen Rendering Time:** Time for the new screen's content to fully render after navigation.

## Mobile Test Scenarios

Run the document above for each scenario per network:

| Test Scenario | Description |
|---------------|-------------|
| Initial Page Load | Open StockSense on mobile browser (load time of landing/login page). |
| POS Transaction | Complete a sale on mobile: browse catalog, add to cart, complete checkout. Measure load, response, and rendering times at each step. |
| Barcode Scanning | Use the camera to scan a product barcode; measure response time from scan trigger to product identified/added. |
| Appointment Booking | Book an appointment on mobile: select service, pick date/time, submit booking; measure load, response, and rendering times. |
| Build Configuration | Build a motorcycle on mobile: navigate parts catalog, add parts, view live total; measure load, response, and rendering times. |
| AI Support Query | Ask the AI support a question on mobile; measure response time until the answer renders. |
| Dashboard View | Load the dashboard on mobile; measure load and rendering times. |

---

*End of document. 37 non-functional test documents total: 18 Web Performance, 18 Web Accessibility, 1 Mobile Performance.*