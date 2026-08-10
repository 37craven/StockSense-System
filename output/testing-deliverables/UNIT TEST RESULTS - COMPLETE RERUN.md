# UNIT TEST DOCUMENT

## Test Information

| Field | Value |
|---|---|
| **Module Name** | StockSense System |
| **Test Cycle No.** | 2 - Complete expanded-suite rerun |
| **Component Name** | Domain calculations, business controllers, security, chatbot/email, and SQL-backed workflows |
| **Date Tested** | 2026-08-08 (Asia/Manila) |
| **Type of System** | Web Application |

## Pre-conditions

- StockSense solution built in Release configuration using .NET 8.
- Expanded xUnit suite available in `tests/StockSense.Tests`.
- Dedicated LocalDB instance `StockSenseCodexTest` started for SQL-backed tests.
- Existing EF Core migrations applied to the isolated test database; the database was already up to date.
- `STOCKSENSE_TEST_SQL_CONNECTION` was set only for the test process.
- Development and production databases were not used or modified.
- The real deployed chatbot was tested through safe, read-only requests. No mutation prompts, personal data, or customer identifiers were sent. SMTP was not contacted.

## Action Description

Build the complete StockSense solution, execute all expanded automated tests including the 14 SQL Server tests, and run a safe read-only contract/security matrix against the real deployed chatbot.

## Verification Steps

1. Start the isolated `StockSenseCodexTest` LocalDB instance.
2. Apply the repository's existing EF Core migrations to the isolated database.
3. Build `StockSense.slnx` in Release configuration.
4. Set `STOCKSENSE_TEST_SQL_CONNECTION` to the isolated database for the test process.
5. Execute `tests/StockSense.Tests/StockSense.Tests.csproj`.
6. Confirm the TRX counters show all discovered tests executed.
7. Confirm zero failed, skipped, not-executed, error, aborted, or timed-out tests.
8. Confirm the development database and product source code remain unchanged by the rerun.
9. Call the real chatbot health and `/api/chat` endpoints using read-only Customer, Employee, Admin, validation, and security prompts.
10. Record HTTP status, latency, contract validity, refusals, timeouts, and detected secret-leak signals without reproducing business-sensitive replies.

## Test Results

| Test Scenario | Data / Input Values | Expected Results | Actual Results | Remarks |
|---:|---|---|---|---|
| **1 - Core calculations and workflow rules** | Demand histories across cold-start, blended, and full-data stages; fixed/variable lead times; min/max settings; reorder quantities; package sizes; order and work-order states; invalid and boundary values. | Safety-stock, reorder, receipt-selection, cancellation, and state-transition calculations match deterministic expected values; invalid inputs and transitions are rejected. | **71/71 passed.** All automated assertions for safety-stock mathematics, order-slip calculations, and work-order transition rules matched the expected behavior. | **PASSED** |
| **2 - Business, inventory, compatibility, build, barcode, image, and document units** | Active/inactive products and packages; supplier/mechanic/service records; motorcycle manufacturer/model/version/year combinations; build selections; product images; EAN-13/barcode/QR/PDF-cache inputs; transaction and appointment DTO/action inputs. | Valid business operations return the expected results; invalid, missing, inactive, terminal, malformed, oversized, or incompatible inputs are rejected; authoritative values and mappings are preserved. | **60/60 passed.** Controller-isolated, repository/in-memory, compatibility, build-policy, barcode, image-validation, transaction-mapping, appointment-rule, and document-cache assertions passed. | **PASSED** |
| **3 - Authentication, authorization, API safety, antiforgery, and rate-limit configuration** | Local and external return URLs; Admin/Employee/Customer identities; self-management attempts; invalid roles; raw/technical API errors; antiforgery tokens; authenticated-user and IP rate-limit partitions. | Open redirects and unauthorized/self-destructive actions are rejected; safe messages are returned; role state is preserved on failure; antiforgery and rate-limit configuration behaves as defined. | **39/39 passed.** Account URL, Admin role-management, API-error sanitization, logout-antiforgery, and rate-limiting configuration assertions passed. A security review finding remains: Mechanics, Services, and PreBuilt controllers currently use bare `[Authorize]` rather than explicit Employee/Admin mutation policies. | **PASSED** |
| **4 - Chatbot and email validation plus real chatbot integration** | Application-side chatbot tests plus a warmed live read-only matrix covering Customer, Employee, Admin, invalid-role, malformed, oversized, prompt-injection, secret-request, raw-SQL-denial, and health requests against the deployed chatbot. | Application-side validation and security assertions pass. After service warm-up, the real chatbot returns the expected contract, enforces role boundaries, rejects invalid requests, does not expose detected secrets, and responds within the configured 120-second cap. | Application-side tests: **88/88 passed**. Warmed real-chatbot matrix: **11/11 passed, 0 failed, 0 inconclusive**. Customer help returned HTTP 200 in 2.29 seconds; Employee raw-SQL refusal was confirmed in 99 ms; `/health` returned HTTP 200 in 381 ms; no secret-leak pattern was detected. | **PASSED** |
| **5 - SQL Server workflow and inventory integration evidence** | Isolated migrated LocalDB with unique test products/settings/orders; manual and automatic drafts; duplicate requests; partial/full/over receipts; stale row versions; stock corrections; price/status updates; retry execution strategy. | All 14 database tests execute without skipping; commits are atomic; invalid/over/stale operations leave data unchanged; audits and row versions persist correctly; retry commits once; test data is cleaned up. | **14/14 passed, 0 skipped.** Eight order-slip workflow SQL tests and six product-inventory SQL tests passed against `StockSenseCodexTest`. | **PASSED** |

## Overall Result

| Metric | Result |
|---|---:|
| Total tests discovered | 272 |
| Total tests executed | 272 |
| Passed | **272** |
| Failed | **0** |
| Skipped / Not executed | **0** |
| Errors / Timeouts / Aborted | **0** |
| Console test duration | 2 seconds |
| TRX run elapsed time | Approximately 3.22 seconds |
| Release build | **PASSED - 0 errors** |

**Overall assessment: PASSED AFTER CHATBOT WARM-UP.** All 272 repository automated tests passed, all 14 SQL Server tests executed without skipping, and all 11 warmed real-chatbot checks passed. The earlier cold-run timeout remains an operational cold-start finding, not a warm-run functional failure.

## Build Warning

The build produced four repeated `NU1902` warnings because MailKit 4.15.0 has a known moderate-severity vulnerability (`GHSA-9j88-vvj5-vhgr`) in the Client and Infrastructure dependency graphs. This warning did not fail the build or tests, but the package should be reviewed and upgraded.

## Evidence

- Machine-readable test results: `unit-tests-complete-rerun.trx`
- Real chatbot results: `REAL CHATBOT TEST RESULTS.md` and `REAL CHATBOT TEST RESULTS.json`
- Warmed real-chatbot rerun: `REAL CHATBOT WARM-RUN RESULTS.md` and `REAL CHATBOT WARM-RUN RESULTS.json`
- Test project: `tests/StockSense.Tests/StockSense.Tests.csproj`
- Isolated database: `(localdb)\StockSenseCodexTest` / `StockSenseCodexTest`
- Test run started: 2026-08-08 18:44:29.5306576 +08:00
- Test run finished: 2026-08-08 18:44:32.7527800 +08:00

## Sign-off

| Role | Printed Name / Signature |
|---|---|
| **Prepared By** | Codex automated test execution - authorized signature pending |
| **Administered/Performed By** | Project test administrator - printed name and signature pending |

## Scope Notes

- The report mirrors the fields and five PASSED/FAILED scenario rows required by `UNIT TEST.docx`.
- Scenario 5 is database integration evidence. It is included because it accounts for the 14 previously skipped tests, but it should not be misrepresented as a pure unit test.
- Full Blazor browser behavior and real SMTP delivery still require component/integration testing. The live chatbot contract/security matrix was executed separately and is reported above.
