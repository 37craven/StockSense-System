-- ============================================================
-- StockSense Seed Data Script
-- Realistic Philippine Motorcycle Parts & Services
-- DELETE existing rows first, then INSERT fresh data
-- ============================================================

-- ============================================================
-- DELETE EXISTING DATA (FK-safe order)
-- ============================================================
PRINT 'Deleting existing data...';

DELETE FROM [PinnedSlips];
DELETE FROM [OrderSlipItems];
DELETE FROM [OrderSlips];
DELETE FROM [TransactionItem];
DELETE FROM [Transactions];
DELETE FROM [PreBuildPackageProduct];
DELETE FROM [ProductStoreService];
DELETE FROM [Products];
DELETE FROM [StoreServices];
DELETE FROM [PreBuildPackages];
DELETE FROM [Appointments];
DELETE FROM [BuildRequests];
DELETE FROM [SalesHistory];
DELETE FROM [Mechanics];
DELETE FROM [Suppliers];

-- Reset identity seeds
DBCC CHECKIDENT ('[Suppliers]', RESEED, 0);
DBCC CHECKIDENT ('[Mechanics]', RESEED, 0);
DBCC CHECKIDENT ('[Products]', RESEED, 0);
DBCC CHECKIDENT ('[StoreServices]', RESEED, 0);
DBCC CHECKIDENT ('[PreBuildPackages]', RESEED, 0);
DBCC CHECKIDENT ('[OrderSlips]', RESEED, 0);
DBCC CHECKIDENT ('[OrderSlipItems]', RESEED, 0);
DBCC CHECKIDENT ('[Transactions]', RESEED, 0);
DBCC CHECKIDENT ('[TransactionItem]', RESEED, 0);
DBCC CHECKIDENT ('[Appointments]', RESEED, 0);
DBCC CHECKIDENT ('[BuildRequests]', RESEED, 0);
DBCC CHECKIDENT ('[SalesHistory]', RESEED, 0);
DBCC CHECKIDENT ('[PinnedSlips]', RESEED, 0);

PRINT 'All existing data deleted.';

-- ============================================================
-- SUPPLIERS (25)
-- ============================================================
PRINT 'Inserting Suppliers...';
SET IDENTITY_INSERT [Suppliers] ON;

INSERT INTO [Suppliers] ([Id], [Name], [Email]) VALUES
(1,  N'JVT Trading',                 N'jvt.trading@email.com'),
(2,  N'Ride Parts Depot',            N'rideparts.depot@email.com'),
(3,  N'Motoworld Philippines',       N'motoworld.ph@email.com'),
(4,  N'Cebu Pacific Motor Parts',    N'cebupacific.mparts@email.com'),
(5,  N'RCP Distributors Inc.',       N'rcp.distributors@email.com'),
(6,  N'Johns Motorcycle Parts',      N'johns.mcparts@email.com'),
(7,  N'3M Auto Supplies',            N'3mauto.supplies@email.com'),
(8,  N'Bikers Hub Trading',          N'bikershub.trading@email.com'),
(9,  N'Maharlika Parts Trading',     N'maharlika.parts@email.com'),
(10, N'Global Moto Parts',           N'global.motoparts@email.com'),
(11, N'Manila Motor Center',         N'manila.motorcenter@email.com'),
(12, N'PITSTOP Philippines',         N'pitstop.ph@email.com'),
(13, N'D Best Motor Parts',          N'dbest.motorparts@email.com'),
(14, N'Riders Choice Trading',       N'riderschoice.trade@email.com'),
(15, N'Scooter Parts PH',            N'scooterparts.ph@email.com'),
(16, N'Motorista Trading',           N'motorista.trading@email.com'),
(17, N'Fast Boys Parts Hub',         N'fastboys.partshub@email.com'),
(18, N'Wheels & Wings Trading',      N'wheels.wings@email.com'),
(19, N'Moto Parts Gallery',          N'motoparts.gallery@email.com'),
(20, N'Banawe Auto Supply',          N'banawe.autosupply@email.com'),
(21, N'Speedlab Parts',              N'speedlab.parts@email.com'),
(22, N'Racing Boy Philippines',      N'racingboy.ph@email.com'),
(23, N'Bajaj PH Distributor',        N'bajaj.ph@email.com'),
(24, N'Kawasaki Parts Trading',      N'kawasaki.partstrade@email.com'),
(25, N'Yamaha Genuine Parts Center', N'yamaha.genuine@email.com');

SET IDENTITY_INSERT [Suppliers] OFF;

-- ============================================================
-- MECHANICS (8)
-- ============================================================
PRINT 'Inserting Mechanics...';
SET IDENTITY_INSERT [Mechanics] ON;

INSERT INTO [Mechanics] ([Id], [Name], [IsActive]) VALUES
(1, N'Juan Dela Cruz',     1),
(2, N'Pedro Santos',       1),
(3, N'Miguel Reyes',       1),
(4, N'Jose Garcia',        1),
(5, N'Carlo Mendoza',      1),
(6, N'Rico Fernandez',     1),
(7, N'Mark Villanueva',    1),
(8, N'Benjie Torres',      0);

SET IDENTITY_INSERT [Mechanics] OFF;

-- ============================================================
-- PRODUCTS (75)
-- ============================================================
PRINT 'Inserting Products...';
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

-- ============================================================
-- STORE SERVICES (8)
-- ============================================================
PRINT 'Inserting StoreServices...';
SET IDENTITY_INSERT [StoreServices] ON;

INSERT INTO [StoreServices] ([Id], [Name], [Price], [Category], [EstimatedMinutes], [Status]) VALUES
(1, N'Change Oil (Shell Advance)',    350.00, N'Change Oil',  30, N'Active'),
(2, N'General Tune-up',               800.00, N'Tune-Up',     60, N'Active'),
(3, N'Brake Overhaul',                600.00, N'Brake',       45, N'Active'),
(4, N'Chain & Sprocket Replacement',  400.00, N'Drive',       60, N'Active'),
(5, N'Tire Change (Tubeless)',        250.00, N'Tire',        30, N'Active'),
(6, N'Full Electrical Check-up',      500.00, N'Electrical',  45, N'Active'),
(7, N'Valves Adjustment',             450.00, N'Engine',      40, N'Active'),
(8, N'PMS (Periodic Maintenance)',   1200.00, N'PMS',         90, N'Active');

SET IDENTITY_INSERT [StoreServices] OFF;

-- ============================================================
-- PRE-BUILD PACKAGES (5)
-- ============================================================
PRINT 'Inserting PreBuildPackages...';
SET IDENTITY_INSERT [PreBuildPackages] ON;

INSERT INTO [PreBuildPackages] ([Id], [Name], [Description], [CompatibleBrand], [CompatibleModel], [TargetCC], [EstimatedAddedCC], [IsActive]) VALUES
(1, N'Daily Ride Setup',   N'Essential upgrades for daily city commute. Reliable and affordable.',  N'Honda',  N'XRM 125',     N'100cc-125cc', 3, 1),
(2, N'Long Ride Kit',      N'Built for endurance on long provincial highways. Extra comfort and safety.', N'Yamaha', N'Mio 125', N'125cc-150cc', 5, 1),
(3, N'Racing Config',      N'Track-ready performance parts for maximum power and handling.',      N'Suzuki', N'Raider 150',  N'150cc-200cc', 12, 1),
(4, N'Budget Build',       N'Cost-effective upgrade package without breaking the bank.',           N'Various',N'Universal',   N'100cc-150cc', 2, 1),
(5, N'Full Overhaul Kit',  N'Complete engine and drivetrain restoration package.',                 N'Kawasaki',N'Barako 175', N'150cc-200cc', 8, 1);

SET IDENTITY_INSERT [PreBuildPackages] OFF;

-- ============================================================
-- PRODUCT-STORE-SERVICE M:M (ProductStoreService)
-- ============================================================
PRINT 'Inserting ProductStoreService...';

-- Service 1: Change Oil → Engine Oils (1-6)
INSERT INTO [ProductStoreService] ([RequiredProductsId], [StoreServicesId]) VALUES
(1,1), (2,1), (3,1), (4,1), (5,1), (6,1);

-- Service 2: Tune-up → Oils, Plugs, Filters
INSERT INTO [ProductStoreService] ([RequiredProductsId], [StoreServicesId]) VALUES
(1,2), (2,2), (3,2), (4,2), (5,2), (6,2),
(7,2), (8,2), (9,2),
(37,2), (38,2), (41,2), (42,2);

-- Service 3: Brake Overhaul → Brake Parts (13-18)
INSERT INTO [ProductStoreService] ([RequiredProductsId], [StoreServicesId]) VALUES
(13,3), (14,3), (15,3), (16,3), (17,3), (18,3);

-- Service 4: Chain & Sprocket → Chains (19-24)
INSERT INTO [ProductStoreService] ([RequiredProductsId], [StoreServicesId]) VALUES
(19,4), (20,4), (21,4), (22,4), (23,4), (24,4);

-- Service 5: Tire Change → Tires (25-30)
INSERT INTO [ProductStoreService] ([RequiredProductsId], [StoreServicesId]) VALUES
(25,5), (26,5), (27,5), (28,5), (29,5), (30,5);

-- Service 6: Electrical → Electrical Parts (67-72)
INSERT INTO [ProductStoreService] ([RequiredProductsId], [StoreServicesId]) VALUES
(43,6), (44,6), (45,6), (46,6), (47,6), (48,6),
(67,6), (68,6), (69,6), (70,6), (71,6), (72,6);

-- Service 7: Valves Adjustment → Bearings, Seals
INSERT INTO [ProductStoreService] ([RequiredProductsId], [StoreServicesId]) VALUES
(55,7), (56,7), (57,7), (58,7), (59,7), (60,7);

-- Service 8: PMS → Oils, Plugs, Filters, Chain Lube
INSERT INTO [ProductStoreService] ([RequiredProductsId], [StoreServicesId]) VALUES
(1,8), (2,8), (6,8),
(7,8), (8,8),
(37,8), (38,8), (42,8),
(61,8), (62,8);

-- ============================================================
-- PRE-BUILD PACKAGE PRODUCT M:M (PreBuildPackageProduct)
-- ============================================================
PRINT 'Inserting PreBuildPackageProduct...';

-- Package 1: Daily Ride Setup (Oil, plug, brake shoes, tire, battery, cable)
INSERT INTO [PreBuildPackageProduct] ([IncludedProductsId], [PreBuildPackagesId]) VALUES
(1,1), (7,1), (13,1), (25,1), (31,1), (52,1), (66,1);

-- Package 2: Long Ride Kit (Better oil, iridium plug, bendix pads, DID chain, Kenda tire, DRL, CRC, cover)
INSERT INTO [PreBuildPackageProduct] ([IncludedProductsId], [PreBuildPackagesId]) VALUES
(2,2), (11,2), (17,2), (19,2), (26,2), (43,2), (48,2), (61,2), (65,2);

-- Package 3: Racing Config (Repsol, iridium, brake lever, Tsubaki chain, Pirelli tire, throttle cable, fork seal)
INSERT INTO [PreBuildPackageProduct] ([IncludedProductsId], [PreBuildPackagesId]) VALUES
(4,3), (11,3), (15,3), (21,3), (29,3), (49,3), (58,3), (69,3);

-- Package 4: Budget Build (Castrol, Denso plug, TZM shoes, RKP sprocket, Cheng Shin tire, air filter, cables, grips)
INSERT INTO [PreBuildPackageProduct] ([IncludedProductsId], [PreBuildPackagesId]) VALUES
(6,4), (12,4), (18,4), (24,4), (28,4), (41,4), (52,4), (53,4), (66,4);

-- Package 5: Full Overhaul Kit (Motul, Denso iridium, brake lever, RK chain, Sunstar sprocket, Michelin, oil filter,
--               steering bearing, oil seal, reg/rect, cam chain tensioner)
INSERT INTO [PreBuildPackageProduct] ([IncludedProductsId], [PreBuildPackagesId]) VALUES
(3,5), (10,5), (15,5), (20,5), (22,5), (30,5), (37,5), (55,5), (59,5), (67,5), (75,5);

-- ============================================================
-- ORDER SLIPS (20)
-- ============================================================
PRINT 'Inserting OrderSlips...';
SET IDENTITY_INSERT [OrderSlips] ON;

INSERT INTO [OrderSlips] ([Id], [SlipNumber], [DateGenerated], [SupplierId], [IsReceived]) VALUES
(1,  N'PO-2026-0001', N'2026-01-08 09:00:00',  3,  1),
(2,  N'PO-2026-0002', N'2026-01-15 10:30:00',  6,  1),
(3,  N'PO-2026-0003', N'2026-02-01 08:00:00',  8,  1),
(4,  N'PO-2026-0004', N'2026-02-14 14:00:00',  1,  1),
(5,  N'PO-2026-0005', N'2026-03-02 11:00:00',  12, 1),
(6,  N'PO-2026-0006', N'2026-03-18 09:30:00',  24, 1),
(7,  N'PO-2026-0007', N'2026-04-05 08:45:00',  5,  1),
(8,  N'PO-2026-0008', N'2026-04-12 13:00:00',  19, 1),
(9,  N'PO-2026-0009', N'2026-04-20 10:00:00',  2,  1),
(10, N'PO-2026-0010', N'2026-05-04 09:15:00',  10, 1),
(11, N'PO-2026-0011', N'2026-05-11 14:30:00',  18, 0),
(12, N'PO-2026-0012', N'2026-05-18 08:00:00',  22, 0),
(13, N'PO-2026-0013', N'2026-05-25 11:00:00',  4,  0),
(14, N'PO-2026-0014', N'2026-06-01 09:30:00',  9,  0),
(15, N'PO-2026-0015', N'2026-06-05 10:45:00',  14, 0),
(16, N'PO-2026-0016', N'2026-06-08 13:00:00',  7,  0),
(17, N'PO-2026-0017', N'2026-06-10 08:30:00',  25, 0),
(18, N'PO-2026-0018', N'2026-06-11 15:00:00',  11, 0),
(19, N'PO-2026-0019', N'2026-06-12 10:00:00',  16, 0),
(20, N'PO-2026-0020', N'2026-06-14 09:00:00',  20, 0);

SET IDENTITY_INSERT [OrderSlips] OFF;

-- ============================================================
-- ORDER SLIP ITEMS (~55 items across 20 slips)
-- ============================================================
PRINT 'Inserting OrderSlipItems...';
SET IDENTITY_INSERT [OrderSlipItems] ON;

INSERT INTO [OrderSlipItems] ([Id], [OrderSlipId], [ProductName], [Brand], [Category], [CurrentStock], [ReorderTarget], [Quantity], [ReceivedQuantity], [IsPredictedHighDemand], [ConfidenceScore], [Reasoning]) VALUES
-- Slip 1: Supplier 3 (Motoworld) - Repsol oil & lubes
(1,  1, N'Repsol Moto Racing 10W-40 1L',   N'Repsol',  N'Engine Oil',   10,  5,  15, 15, 0, 0.65, N'Standard restock'),
(2,  1, N'Castrol Power1 10W-40 1L',        N'Castrol', N'Engine Oil',   12,  8,  10, 10, 1, 0.85, N'High demand based on previous sales spike'),
(3,  1, N'WD-40 Multi-Use 400ml',           N'WD-40',   N'Tool',         30, 15,  12, 12, 0, 0.45, N'Low volatility item'),
-- Slip 2: Supplier 6 (Johns MC Parts) - NGK plugs
(4,  2, N'NGK CR7HSA Spark Plug',            N'NGK',     N'Spark Plug',  50, 20,  30, 30, 1, 0.92, N'Top-selling plug for XRM, consistently high demand'),
(5,  2, N'NGK CR8E Spark Plug',              N'NGK',     N'Spark Plug',  40, 20,  20, 20, 0, 0.70, N'Moderate demand, seasonal boost expected'),
(6,  2, N'NGK CR9E Spark Plug',              N'NGK',     N'Spark Plug',  30, 15,  15, 15, 0, 0.55, N'Low volume but steady seller'),
-- Slip 3: Supplier 8 (Bikers Hub) - Brake parts
(7,  3, N'KEVS Brake Shoes (Honda XRM)',     N'KEVS',    N'Brake',       30, 15,  25, 25, 1, 0.88, N'Wear-and-tear item, high replacement rate'),
(8,  3, N'KEVS Brake Pads (Yamaha Mio)',     N'KEVS',    N'Brake',       25, 15,  20, 20, 1, 0.82, N'Consistent demand across Mio units'),
(9,  3, N'TZM Brake Shoe Set (125cc)',       N'TZM',     N'Brake',       40, 20,  15, 15, 0, 0.50, N'Standard restock'),
-- Slip 4: Supplier 1 (JVT Trading) - Shell oils
(10, 4, N'Shell Advance AX5 20W-40 1L',      N'Shell',   N'Engine Oil',  25, 10,  30, 30, 1, 0.95, N'Highest volume seller, every customer needs oil'),
(11, 4, N'Shell Advance AX7 10W-40 1L',      N'Shell',   N'Engine Oil',  15, 10,  15, 15, 0, 0.72, N'Premium oil, niche but consistent'),
(12, 4, N'CRC Brake Cleaner 400ml',           N'CRC',     N'Tool',        20, 10,  10, 10, 0, 0.40, N'Low demand item'),
-- Slip 5: Supplier 12 (PITSTOP) - DID chains
(13, 5, N'DID 428VX Chain 100L',             N'DID',     N'Chain',        8,  5,  10, 10, 1, 0.87, N'Premium chain, high-demand for upgrades'),
(14, 5, N'RK 428 Chain 100L',                N'RK',      N'Chain',       12,  5,   8,  8, 0, 0.68, N'Mid-range option, steady orders'),
(15, 5, N'Sunstar Sprocket Set (Honda XRM)', N'Sunstar', N'Sprocket',    18, 10,  12, 12, 1, 0.79, N'Pair with chain replacement common'),
-- Slip 6: Supplier 24 (Motolite) - Batteries
(16, 6, N'Motolite MCP50L Battery 12V 5Ah',  N'Motolite',N'Battery',     10,  5,  15, 15, 1, 0.91, N'High failure rate in summer, prepare for spike'),
(17, 6, N'Motolite MCP70L Battery 12V 9Ah',  N'Motolite',N'Battery',      7,  4,   8,  8, 0, 0.62, N'Less common size but needed'),
-- Slip 7: Supplier 5 (RCP Distributors) - Mobil & Castrol
(18, 7, N'Mobil Super Moto 20W-50 1L',       N'Mobil',   N'Engine Oil',  30, 15,  25, 25, 1, 0.80, N'Price-sensitive customers prefer this brand'),
(19, 7, N'Honda Genuine Oil Filter (XRM)',   N'Honda',   N'Filter',      20, 10,  20, 20, 0, 0.75, N'Oil change companion item'),
(20, 7, N'Yamaha Genuine Oil Filter (Mio)',  N'Yamaha',  N'Filter',      22, 10,  15, 15, 0, 0.70, N'Oil change companion item'),
-- Slip 8: Supplier 19 (Moto Parts Gallery) - NTN bearings
(21, 8, N'NTN Steering Bearing Set',          N'NTN',     N'Bearing',     15,  8,  10, 10, 0, 0.48, N'Low demand, long shelf life'),
(22, 8, N'NTN Fork Seal Set (27mm)',          N'NTN',     N'Seal',        12,  8,   8,  8, 0, 0.52, N'Seasonal demand'),
(23, 8, N'SKF Wheel Bearing 6201',            N'SKF',     N'Bearing',     25, 10,  15, 15, 0, 0.55, N'Standard bearing, consistent needs'),
-- Slip 9: Supplier 2 (Ride Parts Depot) - Motul, Rikor
(24, 9, N'Motul 3000 20W-50 1L',              N'Motul',   N'Engine Oil',  20, 10,  20, 20, 0, 0.68, N'Good mid-range seller'),
(25, 9, N'Rikor MCP50L Battery',              N'Rikor',   N'Battery',     12,  6,  10, 10, 0, 0.55, N'Budget alternative, growing demand'),
(26, 9, N'DOMINO Handle Grip Set',            N'DOMINO',  N'Accessory',   20, 10,  12, 12, 0, 0.38, N'Low priority item'),
-- Slip 10: Supplier 10 (Global Moto Parts) - K&S cables, Philips bulbs
(27, 10, N'K&S Brake Cable (Universal)',       N'K&S',     N'Brake',      45, 20,  20, 20, 0, 0.60, N'Slow but steady mover'),
(28, 10, N'Philips H4 Bulb 12V 60/55W',       N'Philips', N'Lighting',    18, 10,  15, 15, 0, 0.63, N'Bulbs sell year-round'),
(29, 10, N'LED H4 Headlight Bulb (White)',    N'Generic', N'Lighting',    25, 12,  20, 20, 1, 0.77, N'Upgrade trend increasing'),
-- Slip 11: Supplier 18 (Wheels & Wings) - Shinko tires
(30, 11, N'Shinko SR241 2.75-17 Tire',        N'Shinko',  N'Tire',         6,  4,  10,  0, 1, 0.90, N'Popular tire for XRM, stock critically low'),
(31, 11, N'Honda Genuine Throttle Cable',     N'Honda',   N'Cable',       12,  8,   5,  0, 0, 0.45, N'Cable replacement needed occasionally'),
-- Slip 12: Supplier 22 (Racing Boy PH) - Pirelli, YSS
(32, 12, N'Pirelli Angel CT 80/90-17 Tire',   N'Pirelli',  N'Tire',        3,  3,   5,  0, 1, 0.85, N'High-end tire, special order requests up'),
(33, 12, N'Racing Boy Brake Lever Set',       N'Racing Boy', N'Brake',    20, 10,   8,  0, 0, 0.52, N'Customization trend item'),
(34, 12, N'YSS Fork Seal (Yamaha Mio)',       N'YSS',     N'Seal',        14,  8,  10,  0, 0, 0.50, N'Seasonal, rainy season approaching'),
-- Slip 13: Supplier 4 (Cebu Pacific Motor Parts) - Mobil, Honda filter
(35, 13, N'Mobil Super Moto 20W-50 1L',       N'Mobil',   N'Engine Oil',  30, 15,  30,  0, 1, 0.88, N'Always in demand, good margin'),
(36, 13, N'Honda Genuine Oil Filter (XRM)',   N'Honda',   N'Filter',      20, 10,  15,  0, 0, 0.65, N'Companion to oil change'),
(37, 13, N'Hella Dual Tone Horn (12V)',       N'Hella',   N'Electrical',  15,  8,   5,  0, 0, 0.30, N'Low volume item'),
-- Slip 14: Supplier 9 (Maharlika Parts) - Stock electrical, Yamaha filter
(38, 14, N'CDI Unit (Honda XRM 125)',         N'Stock',   N'Electrical',  12,  6,   8,  0, 0, 0.55, N'Replacement part, occasional orders'),
(39, 14, N'Yamaha Genuine Oil Filter (Mio)',  N'Yamaha',  N'Filter',      22, 10,  12,  0, 0, 0.60, N'Routine restock'),
(40, 14, N'Starter Relay (Yamaha Mio 125)',   N'Stock',   N'Electrical',  12,  8,   6,  0, 0, 0.42, N'Low failure rate part'),
-- Slip 15: Supplier 14 (Riders Choice) - Tsubaki chain, Cheng Shin
(41, 15, N'Tsubaki 428 Chain 100L',           N'Tsubaki', N'Chain',       10,  5,   6,  0, 0, 0.58, N'Alternate chain option'),
(42, 15, N'Cheng Shin 2.75-17 Tire',          N'Cheng Shin', N'Tire',    10,  5,  15,  0, 1, 0.82, N'Budget tire, high volume seller'),
(43, 15, N'RKP Sprocket Set (Universal)',     N'RKP',     N'Sprocket',    22, 10,  10,  0, 0, 0.48, N'Entry-level sprocket set'),
-- Slip 16: Supplier 7 (3M Auto Supplies) - Denso parts
(44, 16, N'Denso Iridium IW20 Spark Plug',    N'Denso',   N'Spark Plug',  15, 10,   6,  0, 1, 0.74, N'Iridium plugs growing in popularity'),
(45, 16, N'Denso U20EPR-U Spark Plug',        N'Denso',   N'Spark Plug',  35, 15,  12,  0, 0, 0.55, N'Standard replacement part'),
(46, 16, N'Osram Night Breaker H4 Bulb',      N'Osram',   N'Lighting',    10,  8,   4,  0, 0, 0.48, N'Premium bulb, low volume'),
-- Slip 17: Supplier 25 (Yamaha Genuine) - Xtreme helmet, accessories
(47, 17, N'Xtreme Open Face Helmet (L/XL)',   N'Xtreme',  N'Accessory',   10,  5,   5,  0, 0, 0.60, N'Helmet regulation push may increase demand'),
(48, 17, N'SEC Full Face Helmet (M/L)',       N'SEC',     N'Accessory',    6,  4,   3,  0, 0, 0.55, N'Premium helmet, special orders'),
(49, 17, N'Motorcycle Cover (Universal XL)',  N'Generic', N'Accessory',   15,  8,   5,  0, 0, 0.35, N'Low priority accessory'),
-- Slip 18: Supplier 11 (Manila Motor Center) - Bendix, TZM
(50, 18, N'Bendix Brake Pads (Scooter)',      N'Bendix',  N'Brake',       15, 10,  10,  0, 0, 0.68, N'Quality brake pads for scooters'),
(51, 18, N'TZM Throttle Cable (Universal)',   N'TZM',     N'Cable',       30, 15,  20,  0, 0, 0.45, N'Cheap replacement cable'),
(52, 18, N'LED Tail Light Bulb (Red)',        N'Generic', N'Lighting',    35, 15,  15,  0, 0, 0.38, N'Standard bulb replacement'),
-- Slip 19: Supplier 16 (Motorista Trading) - JT sprockets
(53, 19, N'JT Sprocket Set (Yamaha Mio)',     N'JT',      N'Sprocket',    15, 10,   8,  0, 0, 0.62, N'Standard Mio sprocket set'),
(54, 19, N'LED DRL Strip (Waterproof)',       N'Generic', N'Lighting',    15, 10,  10,  0, 0, 0.45, N'Accessory mod, growing slowly'),
-- Slip 20: Supplier 20 (Banawe Auto Supply) - Dunlop, SKF
(55, 20, N'Dunlop TT900 90/80-17 Tire',       N'Dunlop',  N'Tire',         4,  3,   4,  0, 1, 0.78, N'Performance tire for Raider owners'),
(56, 20, N'SKF Oil Seal Set (Various)',       N'SKF',     N'Seal',        30, 15,  12,  0, 0, 0.35, N'Slow mover but necessary stock');

SET IDENTITY_INSERT [OrderSlipItems] OFF;

-- ============================================================
-- TRANSACTIONS (20)
-- ============================================================
PRINT 'Inserting Transactions...';
SET IDENTITY_INSERT [Transactions] ON;

INSERT INTO [Transactions] ([Id], [InvoiceNumber], [TransactionDate], [TotalAmount]) VALUES
(1,  N'INV-2026-0001', N'2026-01-10 09:30:00',  0),
(2,  N'INV-2026-0002', N'2026-01-15 14:15:00',  0),
(3,  N'INV-2026-0003', N'2026-02-05 10:00:00',  0),
(4,  N'INV-2026-0004', N'2026-02-18 11:30:00',  0),
(5,  N'INV-2026-0005', N'2026-03-03 13:45:00',  0),
(6,  N'INV-2026-0006', N'2026-03-10 08:00:00',  0),
(7,  N'INV-2026-0007', N'2026-03-22 15:30:00',  0),
(8,  N'INV-2026-0008', N'2026-04-01 09:00:00',  0),
(9,  N'INV-2026-0009', N'2026-04-12 10:30:00',  0),
(10, N'INV-2026-0010', N'2026-04-25 14:00:00',  0),
(11, N'INV-2026-0011', N'2026-05-04 11:15:00',  0),
(12, N'INV-2026-0012', N'2026-05-11 08:30:00',  0),
(13, N'INV-2026-0013', N'2026-05-18 13:00:00',  0),
(14, N'INV-2026-0014', N'2026-05-26 09:45:00',  0),
(15, N'INV-2026-0015', N'2026-06-01 10:00:00',  0),
(16, N'INV-2026-0016', N'2026-06-03 14:30:00',  0),
(17, N'INV-2026-0017', N'2026-06-08 08:15:00',  0),
(18, N'INV-2026-0018', N'2026-06-10 11:00:00',  0),
(19, N'INV-2026-0019', N'2026-06-12 15:00:00',  0),
(20, N'INV-2026-0020', N'2026-06-14 09:30:00',  0);

SET IDENTITY_INSERT [Transactions] OFF;

-- ============================================================
-- TRANSACTION ITEMS (50 items across 20 transactions)
-- ============================================================
PRINT 'Inserting TransactionItems...';
SET IDENTITY_INSERT [TransactionItem] ON;

INSERT INTO [TransactionItem] ([Id], [TransactionId], [ProductId], [ProductName], [UnitPrice], [Quantity]) VALUES
-- TX 1: Oil change + filter
(1,  1,  1, N'Shell Advance AX5 20W-40 1L',      180.00, 1),
(2,  1,  37, N'Honda Genuine Oil Filter (XRM)',   250.00, 1),
-- TX 2: Spark plugs + cable
(3,  2,  7,  N'NGK CR7HSA Spark Plug',             95.00, 2),
(4,  2,  50, N'K&S Clutch Cable (Honda XRM)',     150.00, 1),
-- TX 3: Brake shoes + oil
(5,  3,  13, N'KEVS Brake Shoes (Honda XRM)',     180.00, 1),
(6,  3,  5,  N'Mobil Super Moto 20W-50 1L',       250.00, 1),
-- TX 4: Battery + helmet
(7,  4,  31, N'Motolite MCP50L Battery 12V 5Ah', 1800.00, 1),
(8,  4,  63, N'Xtreme Open Face Helmet (L/XL)',  1200.00, 1),
-- TX 5: Chain + sprocket
(9,  5,  20, N'RK 428 Chain 100L',               1200.00, 1),
(10, 5,  23, N'JT Sprocket Set (Yamaha Mio)',     750.00, 1),
(11, 5,  66, N'DOMINO Handle Grip Set (Universal)',350.00, 1),
-- TX 6: Tire
(12, 6,  26, N'Kenda K657 2.75-17 Tire',         1800.00, 2),
-- TX 7: Oil, plugs, filter (PMS)
(13, 7,  2,  N'Shell Advance AX7 10W-40 1L',      350.00, 1),
(14, 7,  9,  N'NGK CR9E Spark Plug',              150.00, 2),
(15, 7,  38, N'Vic Oil Filter (Yamaha Mio)',      180.00, 1),
-- TX 8: Bulbs + DRL
(16, 8,  43, N'Philips H4 Bulb 12V 60/55W',       380.00, 1),
(17, 8,  46, N'Stanley Signal Bulb 12V',           80.00, 2),
(18, 8,  48, N'LED DRL Strip (Waterproof)',       350.00, 1),
-- TX 9: Premium oil + iridium plugs
(19, 9,  4,  N'Repsol Moto Racing 10W-40 1L',     420.00, 2),
(20, 9,  11, N'NGK Iridium CR8EIX Spark Plug',    550.00, 2),
-- TX 10: Brake pads + CRC
(21, 10, 14, N'KEVS Brake Pads (Yamaha Mio)',     250.00, 1),
(22, 10, 61, N'CRC Brake Cleaner 400ml',           250.00, 1),
-- TX 11: DID chain + sprocket
(23, 11, 19, N'DID 428VX Chain 100L',            1800.00, 1),
(24, 11, 22, N'Sunstar Sprocket Set (Honda XRM)', 650.00, 1),
-- TX 12: Full face helmet + WD-40
(25, 12, 64, N'SEC Full Face Helmet (M/L)',      2500.00, 1),
(26, 12, 62, N'WD-40 Multi-Use 400ml',            180.00, 1),
-- TX 13: Tires + tubes
(27, 13, 25, N'Shinko SR241 2.75-17 Tire',       2200.00, 1),
(28, 13, 27, N'Dunlop TT900 90/80-17 Tire',      3500.00, 1),
-- TX 14: Battery + bulbs
(29, 14, 35, N'Rikor MCP50L Battery',            1500.00, 1),
(30, 14, 45, N'LED H4 Headlight Bulb (White)',    450.00, 1),
(31, 14, 47, N'LED Tail Light Bulb (Red)',        120.00, 2),
-- TX 15: Bearings + seals
(32, 15, 55, N'NTN Steering Bearing Set',         450.00, 1),
(33, 15, 58, N'NTN Fork Seal Set (27mm)',         350.00, 1),
-- TX 16: Givi top box + accessories
(34, 16, 73, N'Givi Top Box 45L (Monokey)',      4500.00, 1),
(35, 16, 74, N'TZM Side Mirror (Universal Pair)',  250.00, 1),
(36, 16, 65, N'Motorcycle Cover (Universal XL)',   500.00, 1),
-- TX 17: Electrical parts
(37, 17, 67, N'SHINDENGEN Regulator Rectifier',    650.00, 1),
(38, 17, 69, N'NGK Ignition Coil (Universal)',    550.00, 1),
(39, 17, 71, N'Hella Dual Tone Horn (12V)',       380.00, 1),
-- TX 18: Repair parts
(40, 18, 68, N'CDI Unit (Honda XRM 125)',         450.00, 1),
(41, 18, 56, N'SKF Wheel Bearing 6201',           250.00, 2),
(42, 18, 75, N'Honda Genuine Cam Chain Tensioner', 650.00, 1),
-- TX 19: Various consumables
(43, 19, 3,  N'Motul 3000 20W-50 1L',             280.00, 1),
(44, 19, 8,  N'NGK CR8E Spark Plug',              120.00, 2),
(45, 19, 42, N'Yamaha Genuine Oil Filter (Mio)',  220.00, 1),
(46, 19, 52, N'TZM Throttle Cable (Universal)',   120.00, 1),
-- TX 20: Premium parts
(47, 20, 29, N'Pirelli Angel CT 80/90-17 Tire',  4500.00, 1),
(48, 20, 2,  N'Shell Advance AX7 10W-40 1L',      350.00, 2),
(49, 20, 33, N'GS YUASA YTZ7S Battery',          2800.00, 1),
(50, 20, 10, N'Denso Iridium IW20 Spark Plug',    450.00, 2);

SET IDENTITY_INSERT [TransactionItem] OFF;

-- Update TotalAmount on Transactions based on their items
UPDATE [Transactions] SET [TotalAmount] = (
    SELECT ISNULL(SUM([UnitPrice] * [Quantity]), 0)
    FROM [TransactionItem]
    WHERE [TransactionItem].[TransactionId] = [Transactions].[Id]
);

-- ============================================================
-- APPOINTMENTS (15)
-- ============================================================
PRINT 'Inserting Appointments...';
SET IDENTITY_INSERT [Appointments] ON;

INSERT INTO [Appointments] ([Id], [CustomerName], [AppointmentDate], [CreatedAt], [TimeSlot], [ServicesRequested], [TotalAmount], [Status], [Category], [MechanicName], [DurationMinutes]) VALUES
(1,  N'Maria Santos',        N'2026-06-15', N'2026-06-10 08:30:00', N'09:00', N'Change Oil',                        350.00,  N'Confirmed', N'Change Oil', N'Juan Dela Cruz',    30),
(2,  N'Robert Lim',          N'2026-06-15', N'2026-06-10 09:00:00', N'10:00', N'Brake Overhaul',                    600.00,  N'Confirmed', N'Brake',     N'Pedro Santos',      45),
(3,  N'Angelo Cruz',         N'2026-06-15', N'2026-06-11 10:00:00', N'11:00', N'General Tune-up',                   800.00,  N'Pending',   N'Tune-Up',   N'Miguel Reyes',      60),
(4,  N'Kristine Villanueva', N'2026-06-16', N'2026-06-11 14:30:00', N'09:00', N'Chain & Sprocket Replacement',      400.00,  N'Confirmed', N'Drive',     N'Juan Dela Cruz',    60),
(5,  N'Dante Ramos',         N'2026-06-16', N'2026-06-12 08:00:00', N'13:00', N'Tire Change',                       250.00,  N'Pending',   N'Tire',      N'Jose Garcia',       30),
(6,  N'Liza Mercado',        N'2026-06-16', N'2026-06-12 09:15:00', N'14:00', N'Full Electrical Check-up',          500.00,  N'Confirmed', N'Electrical',N'Carlo Mendoza',     45),
(7,  N'Jake Fernando',       N'2026-06-17', N'2026-06-12 11:00:00', N'08:00', N'Change Oil, Tire Change',           600.00,  N'Pending',   N'General',   N'Rico Fernandez',    45),
(8,  N'Nancy Perez',         N'2026-06-17', N'2026-06-13 08:30:00', N'10:00', N'PMS (Periodic Maintenance)',       1200.00,  N'Confirmed', N'PMS',       N'Juan Dela Cruz',    90),
(9,  N'Rolly Santiago',      N'2026-06-18', N'2026-06-13 09:00:00', N'09:00', N'Valves Adjustment',                 450.00,  N'Pending',   N'Engine',    N'Miguel Reyes',      40),
(10, N'Grace Tan',           N'2026-06-18', N'2026-06-13 10:30:00', N'11:00', N'Brake Overhaul, Oil Change',        950.00,  N'Confirmed', N'Brake',     N'Pedro Santos',      60),
(11, N'Bong Gonzales',       N'2026-06-14', N'2026-06-10 09:00:00', N'09:00', N'General Tune-up',                   800.00,  N'Completed', N'Tune-Up',   N'Mark Villanueva',   60),
(12, N'Tessie Rivera',       N'2026-06-14', N'2026-06-10 10:00:00', N'10:00', N'Change Oil',                        350.00,  N'Completed', N'Change Oil',N'Juan Dela Cruz',    30),
(13, N'Allan Dominguez',     N'2026-06-14', N'2026-06-11 08:00:00', N'13:00', N'Chain & Sprocket, Tire Change',     650.00,  N'Completed', N'Drive',     N'Carlo Mendoza',     75),
(14, N'Corazon Flores',      N'2026-06-19', N'2026-06-14 08:00:00', N'09:00', N'Full Electrical Check-up',          500.00,  N'Pending',   N'Electrical',N'Jose Garcia',       45),
(15, N'Edwin Soriano',       N'2026-06-19', N'2026-06-14 09:00:00', N'14:00', N'PMS (Periodic Maintenance)',       1200.00,  N'Pending',   N'PMS',       N'Rico Fernandez',    90);

SET IDENTITY_INSERT [Appointments] OFF;

-- ============================================================
-- BUILD REQUESTS (10)
-- ============================================================
PRINT 'Inserting BuildRequests...';
SET IDENTITY_INSERT [BuildRequests] ON;

INSERT INTO [BuildRequests] ([Id], [CustomerName], [BuildName], [SelectedPartsJson], [TotalPrice], [CreatedAt], [Status]) VALUES
(1, N'Juan Dela Cruz', N'XRM Drag Setup', N'[{"Id": 1, "Name": "Shell Advance AX5 20W-40 1L", "Category": "Engine Oil", "Brand": "Shell", "Price": 180.0, "SupplierName": "JVT Trading", "ImageUrl": "https://placehold.co/300x200"}, {"Id": 7, "Name": "NGK CR7HSA Spark Plug", "Category": "Spark Plug", "Brand": "NGK", "Price": 95.0, "SupplierName": "Johns Motorcycle Parts", "ImageUrl": "https://placehold.co/300x200"}, {"Id": 11, "Name": "NGK Iridium CR8EIX Spark Plug", "Category": "Spark Plug", "Brand": "NGK", "Price": 550.0, "SupplierName": "Johns Motorcycle Parts", "ImageUrl": "https://placehold.co/300x200"}, {"Id": 19, "Name": "DID 428VX Chain 100L", "Category": "Chain", "Brand": "DID", "Price": 1800.0, "SupplierName": "PITSTOP Philippines", "ImageUrl": "https://placehold.co/300x200"}, {"Id": 22, "Name": "Sunstar Sprocket Set (Honda XRM)", "Category": "Sprocket", "Brand": "Sunstar", "Price": 650.0, "SupplierName": "Scooter Parts PH", "ImageUrl": "https://placehold.co/300x200"}, {"Id": 29, "Name": "Pirelli Angel CT 80/90-17 Tire", "Category": "Tire", "Brand": "Pirelli", "Price": 4500.0, "SupplierName": "Racing Boy Philippines", "ImageUrl": "https://placehold.co/300x200"}, {"Id": 69, "Name": "NGK Ignition Coil (Universal)", "Category": "Electrical", "Brand": "NGK", "Price": 550.0, "SupplierName": "Johns Motorcycle Parts", "ImageUrl": "https://placehold.co/300x200"}, {"Id": -999, "Name": "TYPE_CUSTOM", "Category": "SYSTEM_METADATA", "Brand": "Custom Parts Selection", "Price": 0, "SupplierName": "", "ImageUrl": "Juan Dela Cruz"}]', 6650.00, N'2026-05-20 09:00:00', N'Completed'),
(2, N'Paolo Reyes', N'Mio Sporty Build', N'[{"Id": 4, "Name": "Repsol Moto Racing 10W-40 1L", "Category": "Engine Oil", "Brand": "Repsol", "Price": 420.0, "SupplierName": "Motoworld Philippines", "ImageUrl": "https://placehold.co/300x200"}, {"Id": 11, "Name": "NGK Iridium CR8EIX Spark Plug", "Category": "Spark Plug", "Brand": "NGK", "Price": 550.0, "SupplierName": "Johns Motorcycle Parts", "ImageUrl": "https://placehold.co/300x200"}, {"Id": 14, "Name": "KEVS Brake Pads (Yamaha Mio)", "Category": "Brake", "Brand": "KEVS", "Price": 250.0, "SupplierName": "Bikers Hub Trading", "ImageUrl": "https://placehold.co/300x200"}, {"Id": 20, "Name": "RK 428 Chain 100L", "Category": "Chain", "Brand": "RK", "Price": 1200.0, "SupplierName": "D Best Motor Parts", "ImageUrl": "https://placehold.co/300x200"}, {"Id": 23, "Name": "JT Sprocket Set (Yamaha Mio)", "Category": "Sprocket", "Brand": "JT", "Price": 750.0, "SupplierName": "Motorista Trading", "ImageUrl": "https://placehold.co/300x200"}, {"Id": 26, "Name": "Kenda K657 2.75-17 Tire", "Category": "Tire", "Brand": "Kenda", "Price": 1800.0, "SupplierName": "Moto Parts Gallery", "ImageUrl": "https://placehold.co/300x200"}, {"Id": -999, "Name": "TYPE_CUSTOM", "Category": "SYSTEM_METADATA", "Brand": "Custom Parts Selection", "Price": 0, "SupplierName": "", "ImageUrl": "Paolo Reyes"}]', 3950.00, N'2026-05-25 14:30:00', N'Completed'),
(3, N'Jose Garcia', N'Raider Racing', N'[{"Id": 4, "Name": "Repsol Moto Racing 10W-40 1L", "Category": "Engine Oil", "Brand": "Repsol", "Price": 420.0, "SupplierName": "Motoworld Philippines", "ImageUrl": "https://placehold.co/300x200"}, {"Id": 11, "Name": "NGK Iridium CR8EIX Spark Plug", "Category": "Spark Plug", "Brand": "NGK", "Price": 550.0, "SupplierName": "Johns Motorcycle Parts", "ImageUrl": "https://placehold.co/300x200"}, {"Id": 15, "Name": "Racing Boy Brake Lever Set", "Category": "Brake", "Brand": "Racing Boy", "Price": 350.0, "SupplierName": "Maharlika Parts Trading", "ImageUrl": "https://placehold.co/300x200"}, {"Id": 19, "Name": "DID 428VX Chain 100L", "Category": "Chain", "Brand": "DID", "Price": 1800.0, "SupplierName": "PITSTOP Philippines", "ImageUrl": "https://placehold.co/300x200"}, {"Id": 29, "Name": "Pirelli Angel CT 80/90-17 Tire", "Category": "Tire", "Brand": "Pirelli", "Price": 4500.0, "SupplierName": "Racing Boy Philippines", "ImageUrl": "https://placehold.co/300x200"}, {"Id": 67, "Name": "SHINDENGEN Regulator Rectifier", "Category": "Electrical", "Brand": "SHINDENGEN", "Price": 650.0, "SupplierName": "Cebu Pacific Motor Parts", "ImageUrl": "https://placehold.co/300x200"}, {"Id": 69, "Name": "NGK Ignition Coil (Universal)", "Category": "Electrical", "Brand": "NGK", "Price": 550.0, "SupplierName": "Johns Motorcycle Parts", "ImageUrl": "https://placehold.co/300x200"}, {"Id": -999, "Name": "TYPE_CUSTOM", "Category": "SYSTEM_METADATA", "Brand": "Custom Parts Selection", "Price": 0, "SupplierName": "", "ImageUrl": "Jose Garcia"}]', 9170.00, N'2026-06-01 10:00:00', N'In Progress'),
(4, N'Kevin Santos', N'Barako Touring', N'[{"Id": 2, "Name": "Shell Advance AX7 10W-40 1L", "Category": "Engine Oil", "Brand": "Shell", "Price": 350.0, "SupplierName": "JVT Trading", "ImageUrl": "https://placehold.co/300x200"}, {"Id": 8, "Name": "NGK CR8E Spark Plug", "Category": "Spark Plug", "Brand": "NGK", "Price": 120.0, "SupplierName": "Johns Motorcycle Parts", "ImageUrl": "https://placehold.co/300x200"}, {"Id": 17, "Name": "Bendix Brake Pads (Scooter)", "Category": "Brake", "Brand": "Bendix", "Price": 400.0, "SupplierName": "Manila Motor Center", "ImageUrl": "https://placehold.co/300x200"}, {"Id": 21, "Name": "Tsubaki 428 Chain 100L", "Category": "Chain", "Brand": "Tsubaki", "Price": 1500.0, "SupplierName": "Riders Choice Trading", "ImageUrl": "https://placehold.co/300x200"}, {"Id": 26, "Name": "Kenda K657 2.75-17 Tire", "Category": "Tire", "Brand": "Kenda", "Price": 1800.0, "SupplierName": "Moto Parts Gallery", "ImageUrl": "https://placehold.co/300x200"}, {"Id": 33, "Name": "GS YUASA YTZ7S Battery", "Category": "Battery", "Brand": "GS YUASA", "Price": 2800.0, "SupplierName": "JVT Trading", "ImageUrl": "https://placehold.co/300x200"}, {"Id": 65, "Name": "Motorcycle Cover (Universal XL)", "Category": "Accessory", "Brand": "Generic", "Price": 500.0, "SupplierName": "Ride Parts Depot", "ImageUrl": "https://placehold.co/300x200"}, {"Id": -999, "Name": "TYPE_CUSTOM", "Category": "SYSTEM_METADATA", "Brand": "Custom Parts Selection", "Price": 0, "SupplierName": "", "ImageUrl": "Kevin Santos"}]', 9550.00, N'2026-06-03 08:30:00', N'In Progress'),
(5, N'Rico Fernandez', N'Daily Commuter', N'[{"Id": 1, "Name": "Shell Advance AX5 20W-40 1L", "Category": "Engine Oil", "Brand": "Shell", "Price": 180.0, "SupplierName": "JVT Trading", "ImageUrl": "https://placehold.co/300x200"}, {"Id": 7, "Name": "NGK CR7HSA Spark Plug", "Category": "Spark Plug", "Brand": "NGK", "Price": 95.0, "SupplierName": "Johns Motorcycle Parts", "ImageUrl": "https://placehold.co/300x200"}, {"Id": 13, "Name": "KEVS Brake Shoes (Honda XRM)", "Category": "Brake", "Brand": "KEVS", "Price": 180.0, "SupplierName": "Bikers Hub Trading", "ImageUrl": "https://placehold.co/300x200"}, {"Id": 25, "Name": "Shinko SR241 2.75-17 Tire", "Category": "Tire", "Brand": "Shinko", "Price": 2200.0, "SupplierName": "Wheels & Wings Trading", "ImageUrl": "https://placehold.co/300x200"}, {"Id": 31, "Name": "Motolite MCP50L Battery 12V 5Ah", "Category": "Battery", "Brand": "Motolite", "Price": 1800.0, "SupplierName": "Kawasaki Parts Trading", "ImageUrl": "https://placehold.co/300x200"}, {"Id": 52, "Name": "TZM Throttle Cable (Universal)", "Category": "Cable", "Brand": "TZM", "Price": 120.0, "SupplierName": "Fast Boys Parts Hub", "ImageUrl": "https://placehold.co/300x200"}, {"Id": 66, "Name": "DOMINO Handle Grip Set (Universal)", "Category": "Accessory", "Brand": "DOMINO", "Price": 350.0, "SupplierName": "Motoworld Philippines", "ImageUrl": "https://placehold.co/300x200"}, {"Id": -999, "Name": "TYPE_CUSTOM", "Category": "SYSTEM_METADATA", "Brand": "Custom Parts Selection", "Price": 0, "SupplierName": "", "ImageUrl": "Rico Fernandez"}]', 2675.00, N'2026-06-05 11:00:00', N'In Progress'),
(6, N'Aaron Lim', N'Budget Trail', N'[{"Id": 6, "Name": "Castrol Power1 10W-40 1L", "Category": "Engine Oil", "Brand": "Castrol", "Price": 380.0, "SupplierName": "RCP Distributors Inc.", "ImageUrl": "https://placehold.co/300x200"}, {"Id": 12, "Name": "Denso U20EPR-U Spark Plug", "Category": "Spark Plug", "Brand": "Denso", "Price": 180.0, "SupplierName": "3M Auto Supplies", "ImageUrl": "https://placehold.co/300x200"}, {"Id": 18, "Name": "TZM Brake Shoe Set (125cc)", "Category": "Brake", "Brand": "TZM", "Price": 150.0, "SupplierName": "Bikers Hub Trading", "ImageUrl": "https://placehold.co/300x200"}, {"Id": 24, "Name": "RKP Sprocket Set (Universal)", "Category": "Sprocket", "Brand": "RKP", "Price": 550.0, "SupplierName": "Fast Boys Parts Hub", "ImageUrl": "https://placehold.co/300x200"}, {"Id": 28, "Name": "Cheng Shin 2.75-17 Tire", "Category": "Tire", "Brand": "Cheng Shin", "Price": 1500.0, "SupplierName": "Speedlab Parts", "ImageUrl": "https://placehold.co/300x200"}, {"Id": 41, "Name": "Rusi Genuine Air Filter", "Category": "Filter", "Brand": "Rusi", "Price": 150.0, "SupplierName": "Bikers Hub Trading", "ImageUrl": "https://placehold.co/300x200"}, {"Id": 53, "Name": "TZM Clutch Cable (Universal)", "Category": "Cable", "Brand": "TZM", "Price": 110.0, "SupplierName": "Fast Boys Parts Hub", "ImageUrl": "https://placehold.co/300x200"}, {"Id": -999, "Name": "TYPE_CUSTOM", "Category": "SYSTEM_METADATA", "Brand": "Custom Parts Selection", "Price": 0, "SupplierName": "", "ImageUrl": "Aaron Lim"}]', 2885.00, N'2026-06-07 09:15:00', N'Pending'),
(7, N'Michelle Tan', N'Tricycle Utility', N'[{"Id": 5, "Name": "Mobil Super Moto 20W-50 1L", "Category": "Engine Oil", "Brand": "Mobil", "Price": 250.0, "SupplierName": "Cebu Pacific Motor Parts", "ImageUrl": "https://placehold.co/300x200"}, {"Id": 8, "Name": "NGK CR8E Spark Plug", "Category": "Spark Plug", "Brand": "NGK", "Price": 120.0, "SupplierName": "Johns Motorcycle Parts", "ImageUrl": "https://placehold.co/300x200"}, {"Id": 13, "Name": "KEVS Brake Shoes (Honda XRM)", "Category": "Brake", "Brand": "KEVS", "Price": 180.0, "SupplierName": "Bikers Hub Trading", "ImageUrl": "https://placehold.co/300x200"}, {"Id": 28, "Name": "Cheng Shin 2.75-17 Tire", "Category": "Tire", "Brand": "Cheng Shin", "Price": 1500.0, "SupplierName": "Speedlab Parts", "ImageUrl": "https://placehold.co/300x200"}, {"Id": 34, "Name": "Motolite MCP70L Battery 12V 9Ah", "Category": "Battery", "Brand": "Motolite", "Price": 2100.0, "SupplierName": "Kawasaki Parts Trading", "ImageUrl": "https://placehold.co/300x200"}, {"Id": 50, "Name": "K&S Clutch Cable (Honda XRM)", "Category": "Cable", "Brand": "K&S", "Price": 150.0, "SupplierName": "Motorista Trading", "ImageUrl": "https://placehold.co/300x200"}, {"Id": 62, "Name": "WD-40 Multi-Use 400ml", "Category": "Tool", "Brand": "WD-40", "Price": 180.0, "SupplierName": "Kawasaki Parts Trading", "ImageUrl": "https://placehold.co/300x200"}, {"Id": -999, "Name": "TYPE_CUSTOM", "Category": "SYSTEM_METADATA", "Brand": "Custom Parts Selection", "Price": 0, "SupplierName": "", "ImageUrl": "Michelle Tan"}]', 3050.00, N'2026-06-08 14:00:00', N'Pending'),
(8, N'Ramon Bautista', N'Full Rebuild', N'[{"Id": 3, "Name": "Motul 3000 20W-50 1L", "Category": "Engine Oil", "Brand": "Motul", "Price": 280.0, "SupplierName": "Ride Parts Depot", "ImageUrl": "https://placehold.co/300x200"}, {"Id": 10, "Name": "Denso Iridium IW20 Spark Plug", "Category": "Spark Plug", "Brand": "Denso", "Price": 450.0, "SupplierName": "3M Auto Supplies", "ImageUrl": "https://placehold.co/300x200"}, {"Id": 15, "Name": "Racing Boy Brake Lever Set", "Category": "Brake", "Brand": "Racing Boy", "Price": 350.0, "SupplierName": "Maharlika Parts Trading", "ImageUrl": "https://placehold.co/300x200"}, {"Id": 20, "Name": "RK 428 Chain 100L", "Category": "Chain", "Brand": "RK", "Price": 1200.0, "SupplierName": "D Best Motor Parts", "ImageUrl": "https://placehold.co/300x200"}, {"Id": 22, "Name": "Sunstar Sprocket Set (Honda XRM)", "Category": "Sprocket", "Brand": "Sunstar", "Price": 650.0, "SupplierName": "Scooter Parts PH", "ImageUrl": "https://placehold.co/300x200"}, {"Id": 30, "Name": "Michelin Pilot Street 90/80-17", "Category": "Tire", "Brand": "Michelin", "Price": 5200.0, "SupplierName": "Bajaj PH Distributor", "ImageUrl": "https://placehold.co/300x200"}, {"Id": 37, "Name": "Honda Genuine Oil Filter (XRM)", "Category": "Filter", "Brand": "Honda", "Price": 250.0, "SupplierName": "Cebu Pacific Motor Parts", "ImageUrl": "https://placehold.co/300x200"}, {"Id": 55, "Name": "NTN Steering Bearing Set", "Category": "Bearing", "Brand": "NTN", "Price": 450.0, "SupplierName": "Moto Parts Gallery", "ImageUrl": "https://placehold.co/300x200"}, {"Id": 59, "Name": "SKF Oil Seal Set (Various)", "Category": "Seal", "Brand": "SKF", "Price": 180.0, "SupplierName": "Banawe Auto Supply", "ImageUrl": "https://placehold.co/300x200"}, {"Id": 67, "Name": "SHINDENGEN Regulator Rectifier", "Category": "Electrical", "Brand": "SHINDENGEN", "Price": 650.0, "SupplierName": "Cebu Pacific Motor Parts", "ImageUrl": "https://placehold.co/300x200"}, {"Id": 75, "Name": "Honda Genuine Cam Chain Tensioner", "Category": "Engine", "Brand": "Honda", "Price": 650.0, "SupplierName": "PITSTOP Philippines", "ImageUrl": "https://placehold.co/300x200"}, {"Id": -999, "Name": "TYPE_CUSTOM", "Category": "SYSTEM_METADATA", "Brand": "Custom Parts Selection", "Price": 0, "SupplierName": "", "ImageUrl": "Ramon Bautista"}]', 13130.00, N'2026-06-10 10:30:00', N'Pending'),
(9, N'Carla Mercado', N'Mio Elegance', N'[{"Id": 4, "Name": "Repsol Moto Racing 10W-40 1L", "Category": "Engine Oil", "Brand": "Repsol", "Price": 420.0, "SupplierName": "Motoworld Philippines", "ImageUrl": "https://placehold.co/300x200"}, {"Id": 14, "Name": "KEVS Brake Pads (Yamaha Mio)", "Category": "Brake", "Brand": "KEVS", "Price": 250.0, "SupplierName": "Bikers Hub Trading", "ImageUrl": "https://placehold.co/300x200"}, {"Id": 23, "Name": "JT Sprocket Set (Yamaha Mio)", "Category": "Sprocket", "Brand": "JT", "Price": 750.0, "SupplierName": "Motorista Trading", "ImageUrl": "https://placehold.co/300x200"}, {"Id": 26, "Name": "Kenda K657 2.75-17 Tire", "Category": "Tire", "Brand": "Kenda", "Price": 1800.0, "SupplierName": "Moto Parts Gallery", "ImageUrl": "https://placehold.co/300x200"}, {"Id": 45, "Name": "LED H4 Headlight Bulb (White)", "Category": "Lighting", "Brand": "Generic", "Price": 450.0, "SupplierName": "PITSTOP Philippines", "ImageUrl": "https://placehold.co/300x200"}, {"Id": 48, "Name": "LED DRL Strip (Waterproof)", "Category": "Lighting", "Brand": "Generic", "Price": 350.0, "SupplierName": "Scooter Parts PH", "ImageUrl": "https://placehold.co/300x200"}, {"Id": 64, "Name": "SEC Full Face Helmet (M/L)", "Category": "Accessory", "Brand": "SEC", "Price": 2500.0, "SupplierName": "JVT Trading", "ImageUrl": "https://placehold.co/300x200"}, {"Id": -999, "Name": "TYPE_CUSTOM", "Category": "SYSTEM_METADATA", "Brand": "Custom Parts Selection", "Price": 0, "SupplierName": "", "ImageUrl": "Carla Mercado"}]', 6080.00, N'2026-06-12 08:00:00', N'Pending'),
(10, N'Dennis Cruz', N'XRM Long Ride', N'[{"Id": 2, "Name": "Shell Advance AX7 10W-40 1L", "Category": "Engine Oil", "Brand": "Shell", "Price": 350.0, "SupplierName": "JVT Trading", "ImageUrl": "https://placehold.co/300x200"}, {"Id": 11, "Name": "NGK Iridium CR8EIX Spark Plug", "Category": "Spark Plug", "Brand": "NGK", "Price": 550.0, "SupplierName": "Johns Motorcycle Parts", "ImageUrl": "https://placehold.co/300x200"}, {"Id": 17, "Name": "Bendix Brake Pads (Scooter)", "Category": "Brake", "Brand": "Bendix", "Price": 400.0, "SupplierName": "Manila Motor Center", "ImageUrl": "https://placehold.co/300x200"}, {"Id": 19, "Name": "DID 428VX Chain 100L", "Category": "Chain", "Brand": "DID", "Price": 1800.0, "SupplierName": "PITSTOP Philippines", "ImageUrl": "https://placehold.co/300x200"}, {"Id": 26, "Name": "Kenda K657 2.75-17 Tire", "Category": "Tire", "Brand": "Kenda", "Price": 1800.0, "SupplierName": "Moto Parts Gallery", "ImageUrl": "https://placehold.co/300x200"}, {"Id": 43, "Name": "Philips H4 Bulb 12V 60/55W", "Category": "Lighting", "Brand": "Philips", "Price": 380.0, "SupplierName": "Global Moto Parts", "ImageUrl": "https://placehold.co/300x200"}, {"Id": 48, "Name": "LED DRL Strip (Waterproof)", "Category": "Lighting", "Brand": "Generic", "Price": 350.0, "SupplierName": "Scooter Parts PH", "ImageUrl": "https://placehold.co/300x200"}, {"Id": 61, "Name": "CRC Brake Cleaner 400ml", "Category": "Tool", "Brand": "CRC", "Price": 250.0, "SupplierName": "Bajaj PH Distributor", "ImageUrl": "https://placehold.co/300x200"}, {"Id": 65, "Name": "Motorcycle Cover (Universal XL)", "Category": "Accessory", "Brand": "Generic", "Price": 500.0, "SupplierName": "Ride Parts Depot", "ImageUrl": "https://placehold.co/300x200"}, {"Id": -999, "Name": "TYPE_CUSTOM", "Category": "SYSTEM_METADATA", "Brand": "Custom Parts Selection", "Price": 0, "SupplierName": "", "ImageUrl": "Dennis Cruz"}]', 6800.00, N'2026-06-14 09:00:00', N'Pending');

SET IDENTITY_INSERT [BuildRequests] OFF;

-- ============================================================
-- SALES HISTORY (50 records for ML training)
-- ============================================================
PRINT 'Inserting SalesHistory...';
SET IDENTITY_INSERT [SalesHistory] ON;

INSERT INTO [SalesHistory] ([Id], [Date], [ProductID], [ProductName], [Brand], [Category], [QtySold], [UnitPrice], [TotalSales], [MonthNum], [Year]) VALUES
-- January 2026 (8 records)
(1,  N'2026-01-10', N'1', N'Shell Advance AX5 20W-40 1L',      N'Shell',    N'Engine Oil',  15, 180,   2700.0,  1, 2026.0),
(2,  N'2026-01-12', N'7', N'NGK CR7HSA Spark Plug',            N'NGK',      N'Spark Plug',  8,   95,   760.0,   1, 2026.0),
(3,  N'2026-01-15', N'31',N'Motolite MCP50L Battery 12V 5Ah',  N'Motolite', N'Battery',      3, 1800,  5400.0,   1, 2026.0),
(4,  N'2026-01-18', N'5', N'Mobil Super Moto 20W-50 1L',       N'Mobil',    N'Engine Oil',  12, 250,  3000.0,   1, 2026.0),
(5,  N'2026-01-20', N'13',N'KEVS Brake Shoes (Honda XRM)',     N'KEVS',     N'Brake',        6, 180,  1080.0,   1, 2026.0),
(6,  N'2026-01-22', N'25',N'Shinko SR241 2.75-17 Tire',        N'Shinko',   N'Tire',         2, 2200,  4400.0,   1, 2026.0),
(7,  N'2026-01-25', N'38',N'Vic Oil Filter (Yamaha Mio)',      N'Vic',      N'Filter',       8, 180,  1440.0,   1, 2026.0),
(8,  N'2026-01-28', N'62',N'WD-40 Multi-Use 400ml',            N'WD-40',    N'Tool',        10, 180,  1800.0,   1, 2026.0),

-- February 2026 (9 records)
(9,  N'2026-02-03', N'1', N'Shell Advance AX5 20W-40 1L',      N'Shell',    N'Engine Oil',  18, 180,  3240.0,   2, 2026.0),
(10, N'2026-02-05', N'8', N'NGK CR8E Spark Plug',              N'NGK',      N'Spark Plug', 10, 120,  1200.0,   2, 2026.0),
(11, N'2026-02-08', N'2', N'Shell Advance AX7 10W-40 1L',      N'Shell',    N'Engine Oil',   5, 350,  1750.0,   2, 2026.0),
(12, N'2026-02-10', N'34',N'Motolite MCP70L Battery 12V 9Ah',  N'Motolite', N'Battery',      2, 2100,  4200.0,  2, 2026.0),
(13, N'2026-02-14', N'19',N'DID 428VX Chain 100L',             N'DID',      N'Chain',        3, 1800,  5400.0,  2, 2026.0),
(14, N'2026-02-18', N'14',N'KEVS Brake Pads (Yamaha Mio)',     N'KEVS',     N'Brake',        5, 250,  1250.0,   2, 2026.0),
(15, N'2026-02-20', N'43',N'Philips H4 Bulb 12V 60/55W',       N'Philips',  N'Lighting',     4, 380,  1520.0,   2, 2026.0),
(16, N'2026-02-22', N'50',N'K&S Clutch Cable (Honda XRM)',     N'K&S',      N'Cable',        5, 150,   750.0,   2, 2026.0),
(17, N'2026-02-25', N'63',N'Xtreme Open Face Helmet (L/XL)',   N'Xtreme',   N'Accessory',    2, 1200, 2400.0,   2, 2026.0),

-- March 2026 (9 records)
(18, N'2026-03-02', N'1', N'Shell Advance AX5 20W-40 1L',      N'Shell',    N'Engine Oil',  20, 180,  3600.0,   3, 2026.0),
(19, N'2026-03-05', N'7', N'NGK CR7HSA Spark Plug',            N'NGK',      N'Spark Plug', 12,  95,  1140.0,   3, 2026.0),
(20, N'2026-03-08', N'5', N'Mobil Super Moto 20W-50 1L',       N'Mobil',    N'Engine Oil',  15, 250,  3750.0,   3, 2026.0),
(21, N'2026-03-10', N'26',N'Kenda K657 2.75-17 Tire',          N'Kenda',    N'Tire',         3, 1800,  5400.0,  3, 2026.0),
(22, N'2026-03-14', N'20',N'RK 428 Chain 100L',                N'RK',       N'Chain',        4, 1200,  4800.0,  3, 2026.0),
(23, N'2026-03-18', N'13',N'KEVS Brake Shoes (Honda XRM)',     N'KEVS',     N'Brake',        8, 180,  1440.0,   3, 2026.0),
(24, N'2026-03-20', N'22',N'Sunstar Sprocket Set (Honda XRM)', N'Sunstar',  N'Sprocket',     5, 650,  3250.0,   3, 2026.0),
(25, N'2026-03-22', N'66',N'DOMINO Handle Grip Set (Universal)',N'DOMINO',  N'Accessory',    4, 350,  1400.0,   3, 2026.0),
(26, N'2026-03-28', N'37',N'Honda Genuine Oil Filter (XRM)',   N'Honda',    N'Filter',      10, 250,  2500.0,   3, 2026.0),

-- April 2026 (9 records)
(27, N'2026-04-02', N'1', N'Shell Advance AX5 20W-40 1L',      N'Shell',    N'Engine Oil',  22, 180,  3960.0,   4, 2026.0),
(28, N'2026-04-05', N'3', N'Motul 3000 20W-50 1L',            N'Motul',    N'Engine Oil',   8, 280,  2240.0,   4, 2026.0),
(29, N'2026-04-08', N'8', N'NGK CR8E Spark Plug',              N'NGK',      N'Spark Plug', 15, 120,  1800.0,   4, 2026.0),
(30, N'2026-04-12', N'33',N'GS YUASA YTZ7S Battery',           N'GS YUASA', N'Battery',      2, 2800,  5600.0,  4, 2026.0),
(31, N'2026-04-15', N'25',N'Shinko SR241 2.75-17 Tire',        N'Shinko',   N'Tire',         4, 2200,  8800.0,  4, 2026.0),
(32, N'2026-04-18', N'18',N'TZM Brake Shoe Set (125cc)',       N'TZM',      N'Brake',       10, 150,  1500.0,   4, 2026.0),
(33, N'2026-04-20', N'42',N'Yamaha Genuine Oil Filter (Mio)',  N'Yamaha',   N'Filter',       8, 220,  1760.0,   4, 2026.0),
(34, N'2026-04-25', N'64',N'SEC Full Face Helmet (M/L)',       N'SEC',      N'Accessory',    1, 2500, 2500.0,   4, 2026.0),
(35, N'2026-04-28', N'74',N'TZM Side Mirror (Universal Pair)', N'TZM',      N'Accessory',    6, 250,  1500.0,   4, 2026.0),

-- May 2026 (9 records)
(36, N'2026-05-03', N'1', N'Shell Advance AX5 20W-40 1L',      N'Shell',    N'Engine Oil',  25, 180,  4500.0,   5, 2026.0),
(37, N'2026-05-06', N'6', N'Castrol Power1 10W-40 1L',        N'Castrol',  N'Engine Oil',   7, 380,  2660.0,   5, 2026.0),
(38, N'2026-05-10', N'7', N'NGK CR7HSA Spark Plug',            N'NGK',      N'Spark Plug', 18,  95,  1710.0,   5, 2026.0),
(39, N'2026-05-12', N'26',N'Kenda K657 2.75-17 Tire',          N'Kenda',    N'Tire',         5, 1800,  9000.0,  5, 2026.0),
(40, N'2026-05-15', N'10',N'Denso Iridium IW20 Spark Plug',    N'Denso',    N'Spark Plug',  3, 450,  1350.0,   5, 2026.0),
(41, N'2026-05-18', N'21',N'Tsubaki 428 Chain 100L',           N'Tsubaki',  N'Chain',        2, 1500,  3000.0,  5, 2026.0),
(42, N'2026-05-22', N'55',N'NTN Steering Bearing Set',         N'NTN',      N'Bearing',      3, 450,  1350.0,   5, 2026.0),
(43, N'2026-05-25', N'45',N'LED H4 Headlight Bulb (White)',    N'Generic',  N'Lighting',     8, 450,  3600.0,   5, 2026.0),
(44, N'2026-05-28', N'61',N'CRC Brake Cleaner 400ml',           N'CRC',      N'Tool',         6, 250,  1500.0,   5, 2026.0),

-- June 2026 (6 records)
(45, N'2026-06-02', N'1', N'Shell Advance AX5 20W-40 1L',      N'Shell',    N'Engine Oil',  10, 180,  1800.0,   6, 2026.0),
(46, N'2026-06-05', N'4', N'Repsol Moto Racing 10W-40 1L',     N'Repsol',   N'Engine Oil',   3, 420,  1260.0,   6, 2026.0),
(47, N'2026-06-07', N'7', N'NGK CR7HSA Spark Plug',            N'NGK',      N'Spark Plug',  8,  95,   760.0,   6, 2026.0),
(48, N'2026-06-09', N'13',N'KEVS Brake Shoes (Honda XRM)',     N'KEVS',     N'Brake',        5, 180,   900.0,   6, 2026.0),
(49, N'2026-06-11', N'28',N'Cheng Shin 2.75-17 Tire',          N'Cheng Shin', N'Tire',       4, 1500, 6000.0,   6, 2026.0),
(50, N'2026-06-13', N'57',N'KOYO Wheel Bearing 6301',          N'KOYO',     N'Bearing',      3, 280,   840.0,   6, 2026.0);

SET IDENTITY_INSERT [SalesHistory] OFF;

-- ============================================================
-- PINNED SLIPS (5)
-- ============================================================
PRINT 'Inserting PinnedSlips...';
SET IDENTITY_INSERT [PinnedSlips] ON;

INSERT INTO [PinnedSlips] ([Id], [UserId], [SlipData], [UpdatedAt]) VALUES
(1, N'admindemo@example.com', N'{"slipNumber":"PO-2026-0004","supplier":"JVT Trading","dateGenerated":"2026-01-15","items":[{"product":"Shell Advance AX5 20W-40 1L","quantity":30,"received":30},{"product":"Shell Advance AX7 10W-40 1L","quantity":15,"received":15},{"product":"CRC Brake Cleaner 400ml","quantity":10,"received":10}]}', N'2026-06-14 08:00:00'),
(2, N'admindemo@example.com', N'{"slipNumber":"PO-2026-0010","supplier":"Global Moto Parts","dateGenerated":"2026-05-04","items":[{"product":"K&S Brake Cable (Universal)","quantity":20,"received":20},{"product":"Philips H4 Bulb 12V 60/55W","quantity":15,"received":15},{"product":"LED H4 Headlight Bulb (White)","quantity":20,"received":20}]}', N'2026-06-14 08:00:00'),
(3, N'admindemo@example.com', N'{"slipNumber":"PO-2026-0011","supplier":"Wheels & Wings Trading","dateGenerated":"2026-05-11","items":[{"product":"Shinko SR241 2.75-17 Tire","quantity":10,"received":0},{"product":"Honda Genuine Throttle Cable","quantity":5,"received":0}]}', N'2026-06-14 08:00:00'),
(4, N'admindemo@example.com', N'{"slipNumber":"PO-2026-0015","supplier":"Riders Choice Trading","dateGenerated":"2026-06-05","items":[{"product":"Tsubaki 428 Chain 100L","quantity":6,"received":0},{"product":"Cheng Shin 2.75-17 Tire","quantity":15,"received":0},{"product":"RKP Sprocket Set (Universal)","quantity":10,"received":0}]}', N'2026-06-14 08:00:00'),
(5, N'admindemo@example.com', N'{"slipNumber":"PO-2026-0018","supplier":"Manila Motor Center","dateGenerated":"2026-06-11","items":[{"product":"Bendix Brake Pads (Scooter)","quantity":10,"received":0},{"product":"TZM Throttle Cable (Universal)","quantity":20,"received":0},{"product":"LED Tail Light Bulb (Red)","quantity":15,"received":0}]}', N'2026-06-14 08:00:00');

SET IDENTITY_INSERT [PinnedSlips] OFF;

PRINT 'DONE';
GO
