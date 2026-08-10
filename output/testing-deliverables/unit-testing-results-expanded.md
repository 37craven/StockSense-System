# StockSense Expanded Unit-Testing Results

**Execution date:** 2026-08-08 (Asia/Manila)  
**Test project:** `tests/StockSense.Tests/StockSense.Tests.csproj`  
**Build configuration:** Release / .NET 8  

## Final result

- Release build: **PASS** (0 errors, 4 repeated `NU1902` warnings for MailKit 4.15.0)
- Unit-test execution: **258 passed, 0 failed**
- SQL integration tests excluded by their repository guard: **14 skipped**
- Total discovered: **272**
- Machine-readable result: `unit-tests-expanded.trx`

The 14 skipped cases are explicitly SQL Server integration tests, not unit tests. They cover transaction, row-version, retry, rollback, inventory-audit, and order-receipt behavior and require `STOCKSENSE_TEST_SQL_CONNECTION`.

## Results for the supplied 25-section checklist

| # | Scope | Unit-test result | What was verified / boundary |
|---:|---|---|---|
| 1 | Safety-stock calculation | **PASS** | 20 cases: history bands, fixed/variable lead time, standard deviation, manual override, minimum/maximum limits, rounding, Z-scores, invalid demand/lead-time/settings. Service database recalculation remains integration scope. |
| 2 | Reordering and order-slip calculations | **PASS** | 35 deterministic cases: reorder point, inventory position, MOQ/package/max rules, status classification/transitions, cancellation, supplier groups, receipt dates and recalculation selection. |
| 3 | Order-slip workflow service | **INTEGRATION SUITE - not counted as unit** | Pure order rules pass. Eight SQL cases for draft creation, duplicates, receipt atomicity, rollback, concurrency and retry were correctly skipped in this unit-only run. Supplier notification requires an adapter mock/integration test. |
| 4 | Product and inventory management | **PARTIAL UNIT PASS** | Defaults, validation and controller-isolated behavior pass. Six SQL cases for persistence, row versions, audit and concurrency were excluded as integration tests. Search/pagination/barcode uniqueness need additional repository/API integration coverage. |
| 5 | Product image upload | **PASS** | 7 cases: valid replacement, oversize, corrupt signature, excessive dimensions, unsupported type and empty file. Filesystem/database cleanup failures remain integration/fault-injection scope. |
| 6 | Motorcycle compatibility | **PASS** | 9 service/controller cases: exact identity, normalization, version/year/open ranges, no match, returned stock risk and staff policy. Database uniqueness remains integration scope. |
| 7 | Motorcycle selection | **PASS** | 4 cases: positive/existing ID, repository selection, authenticated endpoint and readable motorcycle details. Ownership/deactivation need repository integration coverage. |
| 8 | Customer custom-build workflow | **PASS for isolated rules** | 6 cases cover active catalog/package filtering, inactive selections, authoritative product snapshot and total. Persistence, assignment, completion and concurrency are integration/E2E concerns. |
| 9 | Appointment management | **PARTIAL UNIT PASS** | Controller action rules for terminal/build-linked edits and selection persistence pass. Scheduling conflicts, UserManager identity, email, checkout and full status workflow require integration/component tests. |
| 10 | Work-order rules and checkout | **PASS for rules; checkout integration pending** | 16 transition cases cover permitted, same-state, unknown, direct-completion and terminal behavior. Checkout stock/receipt/transaction atomicity requires SQL integration tests. |
| 11 | POS and transactions | **PARTIAL UNIT PASS** | Transaction filtering and DTO/item/void mapping pass. Cart UI state, checkout persistence, inventory restoration and concurrent-sale protection require component and SQL integration tests. |
| 12 | Barcode and documents | **PASS for deterministic units** | 12 cases: EAN-13 validation/generation, deterministic uniqueness, barcode/QR rendering, QR-only PDF, invalid format and one-time/missing PDF-cache retrieval. Full receipt/order-slip content is integration/render-verification scope. |
| 13 | Suppliers, mechanics and services | **PARTIAL UNIT PASS** | Controller-isolated create/read/update/delete/filter/missing-record behavior was exercised. Database uniqueness, foreign-key deletion, concurrency and authorization HTTP behavior require integration tests. |
| 14 | Prebuilt packages | **PARTIAL UNIT PASS** | Empty-package rejection, active product/package filtering, budget/motor matching, toggling and missing delete pass. Full compatibility persistence and concurrent modification remain integration scope. |
| 15 | Authentication and accounts | **PARTIAL UNIT PASS** | Return-URL normalization, open-redirect rejection, logout antiforgery helper behavior and authentication status pass. Registration, confirmation, reset, lockout, 2FA and external login require Identity host/browser integration tests. |
| 16 | Roles and authorization | **PASS for isolated controller logic/metadata** | 8 Admin role mutation cases plus authorization metadata checks pass. **Finding:** `MechanicsController`, `ServicesController`, and `PreBuiltController` currently use only bare `[Authorize]`; authenticated customers may reach mutation endpoints unless another policy blocks them. |
| 17 | Rate limiting and security | **PASS for configuration/helpers** | Partitioning, middleware order, login policy, safe API errors, technical-detail redaction, antiforgery and unknown-field handling pass. Real 429 behavior and spoofed-header behavior require host integration tests. |
| 18 | Chatbot configuration | **PASS** | 11 cases: HTTP/HTTPS, invalid schemes, empty/malformed/base-path/trailing-slash handling and timeout boundaries. Startup logging/secrets require host/log integration checks. |
| 19 | Chatbot HTTP client | **PASS** | 11 cases: endpoint construction, serialization path, response handling, non-success status, malformed/empty responses, cancellation and missing base address. No real chatbot was contacted. |
| 20 | Assistance controller | **PASS** | 26 cases: authentication metadata, input/history limits, role precedence, employee database-query denial, error/timeout mapping, caller cancellation and audit privacy. |
| 21 | Chatbot conversation/history | **PARTIAL UNIT PASS** | 7 lifecycle/reflection cases cover expected navigation interruption handling and retry/reset handler presence. Real message ordering, disabled/send state, reset, retry and cross-user UI state require bUnit component tests. |
| 22 | Role-specific chatbot suggestions | **PASS** | 8 cases cover Customer/Employee/Admin suggestions, unknown-role fallback, assistant names and role-specific copy. |
| 23 | Chatbot message formatter | **PASS** | 23 cases: paragraphs, headings, lists, Unicode, code, tables, accessible empty cells, status tones, malformed input, HTML/scripts/unsafe links as text and mixed ordering. |
| 24 | Chatbot data safety and authorization | **PARTIAL APP-BOUNDARY PASS** | Server-selected role, history-role non-escalation, employee direct-query denial, input limits, safe output formatting and private-log exclusion pass. Prompt-injection response quality, backend tool authorization, PII filtering, cross-user data access and hallucination controls belong to the external chatbot backend, which is not in this repository. |
| 25 | Email and notification services | **PARTIAL VALIDATION PASS** | 2 cases verify invalid SMTP configuration/address failures before any network operation. Full content, HTML encoding, attachment, cancellation, duplicate-send and SMTP-failure unit tests are blocked because the services instantiate sealed MailKit `SmtpClient` internally instead of receiving a transport abstraction. |

## New or expanded automated coverage

- Expanded `SafetyStockMathTests`, `WorkOrderRulesTests`, `BarcodePdfTests`, and `ProductImageUploadValidationTests`.
- Expanded `ChatbotOptionsTests`, `AssistanceClientTests`, `AssistanceControllerTests`, and `ChatMessageFormatterTests`.
- Added `BusinessControllerUnitTests` with 20 isolated business/controller tests.
- Added `EmailServiceValidationTests` with network-free email configuration/address validation.

## Remaining work by correct test type

### Integration tests

- SQL transactions, row versions, audit records, rollback and concurrency
- Appointment scheduling conflicts and checkout
- POS checkout and inventory restoration
- Repository uniqueness, foreign-key and pagination behavior
- ASP.NET Identity registration, confirmation, password reset, lockout and 2FA
- Real rate-limit and authorization middleware responses

### Component tests

- Blazor appointment, POS and chatbot state using bUnit
- Chat send/retry/reset/loading/ARIA behavior
- Responsive table/card components and form validation

### External chatbot-backend tests

- Prompt-injection resistance of the model/tool backend
- Per-customer data isolation and tool authorization
- PII/secret filtering and hallucination policy

### Testability refactor needed

- Inject an email/SMTP transport abstraction into `EmailSender` and `OrderEmailSender` to permit complete deterministic email unit tests without network access.
