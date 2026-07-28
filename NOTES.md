# StockSense — Mental Notes

## Topics for later
- Rename stock → inventory across the app
- Fix add product modals (removed during merge — old page had New Product modal, new safety stock dashboard doesn't)
- Fix PDF label generation (EAN-13 + QR code on A6 landscape)
- Wire up OrderSlip email sending (`OrderSlipHelper.SendEmailAsync` exists but never called)
- Create `<EmptyState>` component for table empty states ("No BLANK found")

## Done this session
- Merged `feature/historical-chart/improvements` into `feature/barcode-scanner`
- Resolved conflicts in `ManageSafetyStock.razor` (accepted new dashboard, re-added Print Label + `DownloadBarcodePdf`)
- Resolved conflicts in `POS.razor` (kept scanner card CSS)
- Applied 4 EF migrations to DB
- Wiped all data except auth + migrations history, seeded 100 products
- Cleaned tracked artifacts: `.DS_Store`, `.idea/`, `StockSense/`, `tmp/`, `.agents/`, `bootstrap/`
- Renamed `app.min.css` → `tailwind.css`

## Config
- Dev DB: Docker SQL Server on `localhost,1433` (SA / `YourPassword123!`)
- SMTP sender: `stocksenceaccg@gmail.com`
- SMTP in `appsettings.json` committed with plaintext credentials — move to user-secrets or env vars before production
- `AddDatabaseDeveloperPageExceptionFilter()` in `Program.cs:69` is NOT guarded by `IsDevelopment()` — fix before prod

## Git practices to fix
- Commit messages: use conventional commits (`feat:`, `fix:`, `chore:`) instead of `deploy(5)`, `Publish`, etc.
- `feature/motor-selection` branch never merged into `main` — integrate or close
- `feature/barcode-scanner` branch not yet merged into `main`
- Secrets committed in git history — need `git filter-repo` or BFG to scrub


## Products INSERT

```sql
SET IDENTITY_INSERT [Products] ON;

INSERT INTO [Products] ([Id], [Name], [Category], [Brand], [Price], [ImageUrl], [CurrentStock], [ReorderTarget], [SupplierId]) VALUES

-- Engine Oil (1-6)
(1,  N'Shell Advance AX5 20W-40 1L',      N'Engine Oil',   N'Shell',     180.00,  N'https://placehold.co/300x200', 25, 10, 1),
(2,  N'Shell Advance AX7 10W-40 1L',      N'Engine Oil',   N'Shell',     350.00,  N'https://placehold.co/300x200', 15, 10, 1),
(3,  N'Motul 3000 20W-50 1L',             N'Engine Oil',   N'Motul',     280.00,  N'https://placehold.co/300x200', 20, 10, 2),
(4,  N'Repsol Moto Racing 10W-40 1L',     N'Engine Oil',   N'Repsol',    420.00,  N'https://placehold.co/300x200', 10,  5, 3),
(5,  N'Mobil Super Moto 20W-50 1L',       N'Engine Oil',   N'Mobil',     250.00,  N'https://placehold.co/300x200', 30, 15, 4),
(6,  N'Castrol Power1 10W-40 1L',         N'Engine Oil',   N'Castrol',   380.00,  N'https://placehold.co/300x200', 12,  8, 5),

-- Spark Plugs (7-12)
(7,  N'NGK CR7HSA Spark Plug',            N'Spark Plug',   N'NGK',        95.00,  N'https://placehold.co/300x200', 50, 20, 6),
(8,  N'NGK CR8E Spark Plug',              N'Spark Plug',   N'NGK',       120.00,  N'https://placehold.co/300x200', 40, 20, 6),
(9,  N'NGK CR9E Spark Plug',              N'Spark Plug',   N'NGK',       150.00,  N'https://placehold.co/300x200', 30, 15, 6),
(10, N'Denso Iridium IW20 Spark Plug',    N'Spark Plug',   N'Denso',     450.00,  N'https://placehold.co/300x200', 15, 10, 7),
(11, N'NGK Iridium CR8EIX Spark Plug',    N'Spark Plug',   N'NGK',       550.00,  N'https://placehold.co/300x200', 10,  5, 6),
(12, N'Denso U20EPR-U Spark Plug',        N'Spark Plug',   N'Denso',     180.00,  N'https://placehold.co/300x200', 35, 15, 7),

-- Brake System (13-18)
(13, N'KEVS Brake Shoes (Honda XRM)',     N'Brake',        N'KEVS',      180.00,  N'https://placehold.co/300x200', 30, 15, 8),
(14, N'KEVS Brake Pads (Yamaha Mio)',     N'Brake',        N'KEVS',      250.00,  N'https://placehold.co/300x200', 25, 15, 8),
(15, N'Racing Boy Brake Lever Set',       N'Brake',        N'Racing Boy', 350.00, N'https://placehold.co/300x200', 20, 10, 9),
(16, N'K&S Brake Cable (Universal)',      N'Brake',        N'K&S',       120.00,  N'https://placehold.co/300x200', 45, 20, 10),
(17, N'Bendix Brake Pads (Scooter)',      N'Brake',        N'Bendix',    400.00,  N'https://placehold.co/300x200', 15, 10, 11),
(18, N'TZM Brake Shoe Set (125cc)',       N'Brake',        N'TZM',       150.00,  N'https://placehold.co/300x200', 40, 20, 8),

-- Chain & Sprocket (19-24)
(19, N'DID 428VX Chain 100L',             N'Chain',        N'DID',     1800.00,  N'https://placehold.co/300x200',  8,  5, 12),
(20, N'RK 428 Chain 100L',                N'Chain',        N'RK',      1200.00,  N'https://placehold.co/300x200', 12,  5, 13),
(21, N'Tsubaki 428 Chain 100L',           N'Chain',        N'Tsubaki', 1500.00,  N'https://placehold.co/300x200', 10,  5, 14),
(22, N'Sunstar Sprocket Set (Honda XRM)', N'Sprocket',     N'Sunstar',   650.00,  N'https://placehold.co/300x200', 18, 10, 15),
(23, N'JT Sprocket Set (Yamaha Mio)',     N'Sprocket',     N'JT',        750.00,  N'https://placehold.co/300x200', 15, 10, 16),
(24, N'RKP Sprocket Set (Universal)',     N'Sprocket',     N'RKP',       550.00,  N'https://placehold.co/300x200', 22, 10, 17),

-- Tires (25-30)
(25, N'Shinko SR241 2.75-17 Tire',        N'Tire',         N'Shinko',  2200.00,  N'https://placehold.co/300x200',  6,  4, 18),
(26, N'Kenda K657 2.75-17 Tire',          N'Tire',         N'Kenda',   1800.00,  N'https://placehold.co/300x200',  8,  4, 19),
(27, N'Dunlop TT900 90/80-17 Tire',       N'Tire',         N'Dunlop',  3500.00,  N'https://placehold.co/300x200',  4,  3, 20),
(28, N'Cheng Shin 2.75-17 Tire',          N'Tire',         N'Cheng Shin', 1500.00, N'https://placehold.co/300x200', 10, 5, 21),
(29, N'Pirelli Angel CT 80/90-17 Tire',   N'Tire',         N'Pirelli', 4500.00,  N'https://placehold.co/300x200',  3,  3, 22),
(30, N'Michelin Pilot Street 90/80-17',   N'Tire',         N'Michelin', 5200.00, N'https://placehold.co/300x200',  2,  2, 23),

-- Batteries (31-36)
(31, N'Motolite MCP50L Battery 12V 5Ah',  N'Battery',      N'Motolite', 1800.00, N'https://placehold.co/300x200', 10, 5, 24),
(32, N'Amaron MCP50L Battery',            N'Battery',      N'Amaron',   2200.00,  N'https://placehold.co/300x200',  8, 5, 25),
(33, N'GS YUASA YTZ7S Battery',           N'Battery',      N'GS YUASA', 2800.00, N'https://placehold.co/300x200',  5, 3,  1),
(34, N'Motolite MCP70L Battery 12V 9Ah',  N'Battery',      N'Motolite', 2100.00, N'https://placehold.co/300x200',  7, 4, 24),
(35, N'Rikor MCP50L Battery',             N'Battery',      N'Rikor',    1500.00,  N'https://placehold.co/300x200', 12, 6,  2),
(36, N'Panasonic LC 12V 7Ah Battery',     N'Battery',      N'Panasonic',1600.00, N'https://placehold.co/300x200',  8, 5,  3),

-- Filters (37-42)
(37, N'Honda Genuine Oil Filter (XRM)',   N'Filter',       N'Honda',     250.00,  N'https://placehold.co/300x200', 20, 10, 4),
(38, N'Vic Oil Filter (Yamaha Mio)',      N'Filter',       N'Vic',       180.00,  N'https://placehold.co/300x200', 25, 10, 5),
(39, N'K&N Universal Air Filter',         N'Filter',       N'K&N',     1500.00,  N'https://placehold.co/300x200',  5, 3,  6),
(40, N'BMC Sport Air Filter',             N'Filter',       N'BMC',       850.00,  N'https://placehold.co/300x200',  8, 5,  7),
(41, N'Rusi Genuine Air Filter',          N'Filter',       N'Rusi',      150.00,  N'https://placehold.co/300x200', 30, 15, 8),
(42, N'Yamaha Genuine Oil Filter (Mio)',  N'Filter',       N'Yamaha',    220.00,  N'https://placehold.co/300x200', 22, 10, 9),

-- Lighting (43-48)
(43, N'Philips H4 Bulb 12V 60/55W',       N'Lighting',     N'Philips',   380.00,  N'https://placehold.co/300x200', 18, 10, 10),
(44, N'Osram Night Breaker H4 Bulb',      N'Lighting',     N'Osram',     650.00,  N'https://placehold.co/300x200', 10,  8, 11),
(45, N'LED H4 Headlight Bulb (White)',    N'Lighting',     N'Generic',   450.00,  N'https://placehold.co/300x200', 25, 12, 12),
(46, N'Stanley Signal Bulb 12V',          N'Lighting',     N'Stanley',    80.00,  N'https://placehold.co/300x200', 60, 30, 13),
(47, N'LED Tail Light Bulb (Red)',        N'Lighting',     N'Generic',   120.00,  N'https://placehold.co/300x200', 35, 15, 14),
(48, N'LED DRL Strip (Waterproof)',       N'Lighting',     N'Generic',   350.00,  N'https://placehold.co/300x200', 15, 10, 15),

-- Cables (49-54)
(49, N'K&S Throttle Cable (Universal)',   N'Cable',        N'K&S',       180.00,  N'https://placehold.co/300x200', 20, 10, 16),
(50, N'K&S Clutch Cable (Honda XRM)',     N'Cable',        N'K&S',       150.00,  N'https://placehold.co/300x200', 25, 10, 16),
(51, N'K&S Speedo Cable (Yamaha Mio)',    N'Cable',        N'K&S',       160.00,  N'https://placehold.co/300x200', 18, 10, 16),
(52, N'TZM Throttle Cable (Universal)',   N'Cable',        N'TZM',       120.00,  N'https://placehold.co/300x200', 30, 15, 17),
(53, N'TZM Clutch Cable (Universal)',     N'Cable',        N'TZM',       110.00,  N'https://placehold.co/300x200', 35, 15, 17),
(54, N'Honda Genuine Throttle Cable',     N'Cable',        N'Honda',     350.00,  N'https://placehold.co/300x200', 12,  8, 18),

-- Bearings & Seals (55-60)
(55, N'NTN Steering Bearing Set',         N'Bearing',      N'NTN',       450.00,  N'https://placehold.co/300x200', 15,  8, 19),
(56, N'SKF Wheel Bearing 6201',           N'Bearing',      N'SKF',       250.00,  N'https://placehold.co/300x200', 25, 10, 20),
(57, N'KOYO Wheel Bearing 6301',          N'Bearing',      N'KOYO',      280.00,  N'https://placehold.co/300x200', 22, 10, 21),
(58, N'NTN Fork Seal Set (27mm)',         N'Seal',         N'NTN',       350.00,  N'https://placehold.co/300x200', 12,  8, 19),
(59, N'SKF Oil Seal Set (Various)',       N'Seal',         N'SKF',       180.00,  N'https://placehold.co/300x200', 30, 15, 20),
(60, N'YSS Fork Seal (Yamaha Mio)',       N'Seal',         N'YSS',       300.00,  N'https://placehold.co/300x200', 14,  8, 22),

-- Tools & Accessories (61-66)
(61, N'CRC Brake Cleaner 400ml',          N'Tool',         N'CRC',       250.00,  N'https://placehold.co/300x200', 20, 10, 23),
(62, N'WD-40 Multi-Use 400ml',            N'Tool',         N'WD-40',     180.00,  N'https://placehold.co/300x200', 30, 15, 24),
(63, N'Xtreme Open Face Helmet (L/XL)',   N'Accessory',    N'Xtreme',   1200.00,  N'https://placehold.co/300x200', 10,  5, 25),
(64, N'SEC Full Face Helmet (M/L)',       N'Accessory',    N'SEC',      2500.00,  N'https://placehold.co/300x200',  6,  4,  1),
(65, N'Motorcycle Cover (Universal XL)',  N'Accessory',    N'Generic',   500.00,  N'https://placehold.co/300x200', 15,  8,  2),
(66, N'DOMINO Handle Grip Set (Universal)', N'Accessory',  N'DOMINO',    350.00,  N'https://placehold.co/300x200', 20, 10,  3),

-- Electrical (67-72)
(67, N'SHINDENGEN Regulator Rectifier',   N'Electrical',   N'SHINDENGEN', 650.00, N'https://placehold.co/300x200', 10,  5,  4),
(68, N'CDI Unit (Honda XRM 125)',         N'Electrical',   N'Stock',     450.00,  N'https://placehold.co/300x200', 12,  6,  5),
(69, N'NGK Ignition Coil (Universal)',    N'Electrical',   N'NGK',       550.00,  N'https://placehold.co/300x200',  8,  5,  6),
(70, N'Starter Relay (Yamaha Mio 125)',   N'Electrical',   N'Stock',     350.00,  N'https://placehold.co/300x200', 12,  8,  7),
(71, N'Hella Dual Tone Horn (12V)',       N'Electrical',   N'Hella',     380.00,  N'https://placehold.co/300x200', 15,  8,  8),
(72, N'Voltage Regulator (Kawasaki)',     N'Electrical',   N'Stock',     500.00,  N'https://placehold.co/300x200',  8,  5,  9),

-- Others (73-75)
(73, N'Givi Top Box 45L (Monokey)',       N'Accessory',    N'Givi',     4500.00,  N'https://placehold.co/300x200',  3,  2, 10),
(74, N'TZM Side Mirror (Universal Pair)', N'Accessory',    N'TZM',       250.00,  N'https://placehold.co/300x200', 25, 10, 11),
(75, N'Honda Genuine Cam Chain Tensioner',N'Engine',       N'Honda',     650.00,  N'https://placehold.co/300x200',  8,  5, 12);

SET IDENTITY_INSERT [Products] OFF;
```

## NOTES BY CRAVEN
- env file
- fix pdf label generation
- emptystate and loadingstate components across the board
- remove dead commits
- responsiveness and ui overhaul
-