using SS_MART_API.Core.Domain.Entities;

namespace SS_MART_API.Core.Infrastructure.Data;

public static class Seeder
{
    public static void Seed(AppDbContext db)
    {
        if (db.Products.Any()) return; // already seeded

        // Categories (8)
        var groceries = new Guid("10000000-0000-0000-0000-000000000001");
        var beverages = new Guid("10000000-0000-0000-0000-000000000002");
        var personal = new Guid("10000000-0000-0000-0000-000000000003");
        var snacks = new Guid("10000000-0000-0000-0000-000000000004");
        var dairy = new Guid("10000000-0000-0000-0000-000000000005");
        var cleaning = new Guid("10000000-0000-0000-0000-000000000006");
        var stationery = new Guid("10000000-0000-0000-0000-000000000007");
        var others = new Guid("10000000-0000-0000-0000-000000000008");

        var categories = new List<Category>
        {
            MakeCat("Groceries", "Essential food items", "#4CAF50", "local_grocery_store", 1, groceries),
            MakeCat("Beverages", "Drinks and juices", "#2196F3", "local_bar", 2, beverages),
            MakeCat("Snacks", "Chips, biscuits, and treats", "#FF9800", "cookie", 3, snacks),
            MakeCat("Dairy", "Milk, butter, and cheese", "#FFEB3B", "egg_alt", 4, dairy),
            MakeCat("Personal Care", "Soaps, shampoos, and hygiene", "#E91E63", "spa", 5, personal),
            MakeCat("Cleaning", "Household cleaning products", "#00BCD4", "cleaning_services", 6, cleaning),
            MakeCat("Stationery", "Pens, notebooks, and office", "#9C27B0", "edit", 7, stationery),
            MakeCat("Others", "Miscellaneous items", "#607D8B", "category", 8, others),
        };
        db.Categories.AddRange(categories);

        // Products (25 items)
        var products = new List<Product>
        {
            Make("Basmati Rice 5kg", "BR5", "8901234567001", "1006", 1, 450, 420, 380, 5, groceries),
            Make("Toor Dal 1kg", "TD1", "8901234567002", "0713", 1, 180, 165, 150, 5, groceries),
            Make("Sunflower Oil 1L", "SO1", "8901234567003", "1507", 1, 160, 150, 135, 12, groceries),
            Make("Sugar 1kg", "SG1", "8901234567004", "1701", 1, 55, 50, 45, 5, groceries),
            Make("Wheat Atta 5kg", "WA5", "8901234567005", "1101", 1, 280, 260, 240, 5, groceries),
            Make("Maggi Noodles 70g", "MG7", "8901234567006", "1905", 70, 14, 12, 10, 18, snacks),
            Make("Parle-G 80g", "PG8", "8901234567007", "1905", 80, 10, 9, 7, 18, snacks),
            Make("Lays Classic 52g", "LC5", "8901234567008", "1905", 52, 20, 18, 15, 12, snacks),
            Make("Coca-Cola 750ml", "CC7", "8901234567009", "2202", 750, 40, 38, 32, 12, beverages),
            Make("Bisleri Water 1L", "BW1", "8901234567010", "2201", 1000, 20, 18, 15, 18, beverages),
            Make("Amul Butter 100g", "AB1", "8901234567011", "0405", 100, 55, 50, 45, 12, dairy),
            Make("Amul Milk 500ml", "AM5", "8901234567012", "0401", 500, 30, 28, 24, 12, dairy),
            Make("Paneer 200g", "PN2", "8901234567013", "0406", 200, 80, 75, 68, 12, dairy),
            Make("Colgate 100g", "CG1", "8901234567014", "3306", 100, 95, 85, 75, 18, personal),
            Make("Lifebuoy Soap 100g", "LS1", "8901234567015", "3401", 100, 40, 38, 32, 18, personal),
            Make("Head & Shoulders 180ml", "HS1", "8901234567016", "3305", 180, 220, 200, 180, 18, personal),
            Make("Surf Excel 500g", "SX5", "8901234567017", "3402", 500, 130, 120, 105, 18, personal),
            Make("Tata Salt 1kg", "TS1", "8901234567018", "2501", 1000, 30, 28, 24, 0, groceries),
            Make("Onion 1kg", "ON1", null, "0701", 1000, 40, 35, 30, 0, groceries),
            Make("Tomato 1kg", "TM1", null, "0702", 1000, 50, 45, 38, 0, groceries),
            Make("Potato 1kg", "PT1", null, "0701", 1000, 30, 27, 22, 0, groceries),
            Make("Ginger 100g", "GN1", null, "0712", 100, 30, 28, 24, 0, groceries),
            Make("Chilli Powder 100g", "CP1", "8901234567019", "0904", 100, 65, 60, 52, 5, groceries),
            Make("Turmeric 100g", "TM2", "8901234567020", "0910", 100, 55, 50, 44, 5, groceries),
            Make("Frooti Mango 200ml", "FR2", "8901234567021", "2009", 200, 15, 13, 11, 12, beverages),
        };
        db.Products.AddRange(products);

        // Customers (10)
        var customers = new List<Customer>
        {
            MakeC("Rajesh Kumar", "9876543210", "B2B", 50000, 12000),
            MakeC("Priya Sharma", "9876543211", "B2C", 10000, 500),
            MakeC("Amit Patel", "9876543212", "B2B", 75000, 22000),
            MakeC("Sunita Devi", "9876543213", "B2C", 10000, 150),
            MakeC("Mohammed Ali", "9876543214", "B2C", 15000, 3200),
            MakeC("Walk-in Customer", null, "B2C", 0, 0),
            MakeC("Suresh Traders", "9876543216", "B2B", 100000, 45000),
            MakeC("Anita Verma", "9876543217", "B2C", 10000, 800),
            MakeC("Vikram Singh", "9876543218", "B2B", 60000, 18000),
            MakeC("Deepa Nair", "9876543219", "B2C", 10000, 450),
        };
        db.Customers.AddRange(customers);

        // Employees (3)
        var emp1 = new Employee { Id = Guid.NewGuid(), FullName = "Ravi Kumar", Username = "ravi", PasswordHash = "ravi123", Role = "cashier", Pin = "1234", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var emp2 = new Employee { Id = Guid.NewGuid(), FullName = "Sneha Reddy", Username = "sneha", PasswordHash = "sneha123", Role = "manager", Pin = "5678", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.Employees.AddRange(emp1, emp2);

        // Suppliers (3)
        var supplier1 = new Guid("20000000-0000-0000-0000-000000000001");
        var supplier2 = new Guid("20000000-0000-0000-0000-000000000002");
        var supplier3 = new Guid("20000000-0000-0000-0000-000000000003");

        var suppliers = new List<Supplier>
        {
            MakeSup("Hindustan Unilever Distributor", "9876500001", "hul@example.com", "123 Market Road", "Mumbai", "Maharashtra", "27AABCU9695R1ZV", "AABCU9695R", 500000, 125000, "net30", supplier1),
            MakeSup("ITC Foods Distributor", "9876500002", "itc@example.com", "456 Industrial Area", "Delhi", "Delhi", "07AABCI1234F1Z5", "AABCI1234F", 300000, 75000, "net15", supplier2),
            MakeSup("Local Farm Fresh", "9876500003", "farm@example.com", "789 Farm Road", "Pune", "Maharashtra", "27AABCL5678G1ZQ", "AABCL5678G", 100000, 30000, "cod", supplier3),
        };
        db.Suppliers.AddRange(suppliers);

        // Stock for each product
        var rng = new Random(42);
        foreach (var p in products)
        {
            db.Stocks.Add(new Stock
            {
                Id = Guid.NewGuid(),
                ProductId = p.Id,
                LocationId = "MAIN",
                Quantity = rng.Next(20, 200),
                LastUpdated = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        // A few sample bills
        var billDate1 = DateTime.UtcNow.AddDays(-3);
        var bill1 = new Bill
        {
            Id = Guid.NewGuid(),
            BillNumber = "BILL-0001",
            CustomerId = customers[1].Id,
            BillDate = billDate1,
            Subtotal = 420,
            TaxAmount = 21,
            CgstAmount = 10,
            SgstAmount = 11,
            TaxRuleVersion = "v1",
            DiscountAmount = 0,
            RoundOff = 1,
            TotalAmount = 442,
            PaidAmount = 500,
            DueAmount = 0,
            PaymentMode = "CASH",
            Status = "completed",
            CreatedBy = emp1.Id,
            CreatedAt = billDate1,
            UpdatedAt = billDate1,
            Items = new List<BillItem>
            {
                new BillItem { Id = Guid.NewGuid(), ProductId = products[0].Id, Quantity = 1, UnitPrice = 420, TaxAmount = 21, CgstAmount = 10, SgstAmount = 11, TaxRuleVersion = "v1", TotalAmount = 442, CreatedAt = billDate1, UpdatedAt = billDate1 }
            }
        };
        var bill2Date = DateTime.UtcNow.AddDays(-1);
        var bill2 = new Bill
        {
            Id = Guid.NewGuid(),
            BillNumber = "BILL-0002",
            CustomerId = customers[5].Id,
            BillDate = bill2Date,
            Subtotal = 155,
            TaxAmount = 12,
            CgstAmount = 6,
            SgstAmount = 6,
            TaxRuleVersion = "v1",
            DiscountAmount = 5,
            RoundOff = 0,
            TotalAmount = 162,
            PaidAmount = 162,
            DueAmount = 0,
            PaymentMode = "UPI",
            Status = "completed",
            CreatedBy = emp1.Id,
            CreatedAt = bill2Date,
            UpdatedAt = bill2Date,
            Items = new List<BillItem>
            {
                new BillItem { Id = Guid.NewGuid(), ProductId = products[3].Id, Quantity = 1, UnitPrice = 50, TaxAmount = 0, TaxRuleVersion = "v1", TotalAmount = 50, CreatedAt = bill2Date, UpdatedAt = bill2Date },
                new BillItem { Id = Guid.NewGuid(), ProductId = products[8].Id, Quantity = 2, UnitPrice = 38, TaxAmount = 9, CgstAmount = 5, SgstAmount = 4, TaxRuleVersion = "v1", TotalAmount = 85, CreatedAt = bill2Date, UpdatedAt = bill2Date },
                new BillItem { Id = Guid.NewGuid(), ProductId = products[10].Id, Quantity = 1, UnitPrice = 50, TaxAmount = 6, CgstAmount = 3, SgstAmount = 3, TaxRuleVersion = "v1", TotalAmount = 56, CreatedAt = bill2Date, UpdatedAt = bill2Date },
            }
        };
        db.Bills.AddRange(bill1, bill2);

        // Sample loyalty transactions
        db.LoyaltyTransactions.AddRange(
            new LoyaltyTransaction { Id = Guid.NewGuid(), CustomerId = customers[1].Id, TransactionType = "earn", Points = 44, ReferenceType = "bill", ReferenceId = bill1.Id, CreatedBy = emp1.Id, CreatedAt = billDate1, UpdatedAt = billDate1 },
            new LoyaltyTransaction { Id = Guid.NewGuid(), CustomerId = customers[5].Id, TransactionType = "earn", Points = 16, ReferenceType = "bill", ReferenceId = bill2.Id, CreatedBy = emp1.Id, CreatedAt = bill2Date, UpdatedAt = bill2Date }
        );

        // Expense categories
        var expRent = new Guid("30000000-0000-0000-0000-000000000001");
        var expUtilities = new Guid("30000000-0000-0000-0000-000000000002");
        var expSalary = new Guid("30000000-0000-0000-0000-000000000003");
        var expTransport = new Guid("30000000-0000-0000-0000-000000000004");
        var expMarketing = new Guid("30000000-0000-0000-0000-000000000005");
        var expMisc = new Guid("30000000-0000-0000-0000-000000000006");

        var expenseCategories = new List<ExpenseCategory>
        {
            new ExpenseCategory { Id = expRent, Name = "Rent", Description = "Shop/office rent payments", Color = "#F44336", Icon = "home", SortOrder = 1, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new ExpenseCategory { Id = expUtilities, Name = "Utilities", Description = "Electricity, water, internet", Color = "#FF9800", Icon = "bolt", SortOrder = 2, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new ExpenseCategory { Id = expSalary, Name = "Salary", Description = "Employee salary payments", Color = "#4CAF50", Icon = "people", SortOrder = 3, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new ExpenseCategory { Id = expTransport, Name = "Transport", Description = "Delivery, fuel, logistics", Color = "#2196F3", Icon = "local_shipping", SortOrder = 4, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new ExpenseCategory { Id = expMarketing, Name = "Marketing", Description = "Advertising and promotions", Color = "#9C27B0", Icon = "campaign", SortOrder = 5, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new ExpenseCategory { Id = expMisc, Name = "Miscellaneous", Description = "Other operating expenses", Color = "#607D8B", Icon = "more_horiz", SortOrder = 6, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
        };
        db.ExpenseCategories.AddRange(expenseCategories);

        // Sample expenses
        var exp1 = new Expense { Id = Guid.NewGuid(), ExpenseNumber = "EXP-0001", ExpenseCategoryId = expRent, ExpenseDate = DateTime.UtcNow.AddDays(-25), Amount = 25000, PaymentMode = "BANK", Payee = "Property Owner", Description = "January shop rent", CreatedBy = emp2.Id, CreatedAt = DateTime.UtcNow.AddDays(-25), UpdatedAt = DateTime.UtcNow.AddDays(-25) };
        var exp2 = new Expense { Id = Guid.NewGuid(), ExpenseNumber = "EXP-0002", ExpenseCategoryId = expUtilities, ExpenseDate = DateTime.UtcNow.AddDays(-20), Amount = 3500, PaymentMode = "UPI", Payee = "Electricity Board", Description = "Electricity bill — January", CreatedBy = emp2.Id, CreatedAt = DateTime.UtcNow.AddDays(-20), UpdatedAt = DateTime.UtcNow.AddDays(-20) };
        var exp3 = new Expense { Id = Guid.NewGuid(), ExpenseNumber = "EXP-0003", ExpenseCategoryId = expSalary, ExpenseDate = DateTime.UtcNow.AddDays(-10), Amount = 30000, PaymentMode = "BANK", Payee = "Ravi Kumar", Description = "Monthly salary — Ravi", CreatedBy = emp2.Id, CreatedAt = DateTime.UtcNow.AddDays(-10), UpdatedAt = DateTime.UtcNow.AddDays(-10) };
        var exp4 = new Expense { Id = Guid.NewGuid(), ExpenseNumber = "EXP-0004", ExpenseCategoryId = expTransport, ExpenseDate = DateTime.UtcNow.AddDays(-5), Amount = 1800, PaymentMode = "CASH", Payee = "Fuel Station", Description = "Delivery van fuel refill", CreatedBy = emp1.Id, CreatedAt = DateTime.UtcNow.AddDays(-5), UpdatedAt = DateTime.UtcNow.AddDays(-5) };
        var exp5 = new Expense { Id = Guid.NewGuid(), ExpenseNumber = "EXP-0005", ExpenseCategoryId = expMarketing, ExpenseDate = DateTime.UtcNow.AddDays(-3), Amount = 5000, PaymentMode = "UPI", Payee = "Print Shop", Description = "Pamphlets and banner printing", CreatedBy = emp2.Id, CreatedAt = DateTime.UtcNow.AddDays(-3), UpdatedAt = DateTime.UtcNow.AddDays(-3) };
        var exp6 = new Expense { Id = Guid.NewGuid(), ExpenseNumber = "EXP-0006", ExpenseCategoryId = expUtilities, ExpenseDate = DateTime.UtcNow.AddDays(-2), Amount = 1200, PaymentMode = "CASH", Payee = "Internet Provider", Description = "Broadband monthly plan", CreatedBy = emp2.Id, CreatedAt = DateTime.UtcNow.AddDays(-2), UpdatedAt = DateTime.UtcNow.AddDays(-2) };
        db.Expenses.AddRange(exp1, exp2, exp3, exp4, exp5, exp6);

        // Sample payments
        var pay1 = new Payment { Id = Guid.NewGuid(), PaymentNumber = "PAY-0001", PaymentType = "receive", CustomerId = customers[1].Id, PaymentDate = billDate1, Amount = 500, PaymentMode = "CASH", ReferenceBillId = bill1.Id, Description = "Cash payment for BILL-0001", CreatedBy = emp1.Id, CreatedAt = billDate1, UpdatedAt = billDate1 };
        var pay2 = new Payment { Id = Guid.NewGuid(), PaymentNumber = "PAY-0002", PaymentType = "receive", CustomerId = customers[5].Id, PaymentDate = bill2Date, Amount = 162, PaymentMode = "UPI", ReferenceBillId = bill2.Id, Description = "UPI payment for BILL-0002", CreatedBy = emp1.Id, CreatedAt = bill2Date, UpdatedAt = bill2Date };
        var pay3 = new Payment { Id = Guid.NewGuid(), PaymentNumber = "PAY-0003", PaymentType = "receive", CustomerId = customers[2].Id, PaymentDate = DateTime.UtcNow.AddDays(-7), Amount = 10000, PaymentMode = "BANK", IsAdvance = true, Description = "Advance payment from Amit Patel", CreatedBy = emp1.Id, CreatedAt = DateTime.UtcNow.AddDays(-7), UpdatedAt = DateTime.UtcNow.AddDays(-7) };
        var pay4 = new Payment { Id = Guid.NewGuid(), PaymentNumber = "PAY-0004", PaymentType = "make", SupplierId = supplier1, PaymentDate = DateTime.UtcNow.AddDays(-4), Amount = 50000, PaymentMode = "BANK", ReferencePurchaseOrderId = null, Description = "Partial payment to HUL Distributor", CreatedBy = emp2.Id, CreatedAt = DateTime.UtcNow.AddDays(-4), UpdatedAt = DateTime.UtcNow.AddDays(-4) };
        var pay5 = new Payment { Id = Guid.NewGuid(), PaymentNumber = "PAY-0005", PaymentType = "make", SupplierId = supplier2, PaymentDate = DateTime.UtcNow.AddDays(-2), Amount = 25000, PaymentMode = "CHEQUE", ReferenceNumber = "CHQ-0042", Description = "Cheque payment to ITC Foods", CreatedBy = emp2.Id, CreatedAt = DateTime.UtcNow.AddDays(-2), UpdatedAt = DateTime.UtcNow.AddDays(-2) };
        db.Payments.AddRange(pay1, pay2, pay3, pay4, pay5);

        // Notification templates
        db.NotificationTemplates.AddRange(
            new NotificationTemplate { Id = Guid.NewGuid(), Name = "Bill Receipt (SMS)", Type = "sms", Event = "bill_created", Subject = "Bill Receipt", Body = "Dear {{customer_name}}, your bill {{bill_number}} of ₹{{total_amount}} is generated. Thank you for shopping with SS Mart!", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new NotificationTemplate { Id = Guid.NewGuid(), Name = "Payment Received (SMS)", Type = "sms", Event = "payment_received", Subject = "Payment Confirmation", Body = "Dear {{customer_name}}, we have received ₹{{amount}} against bill {{bill_number}}. Balance: ₹{{balance}}. Thank you!", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new NotificationTemplate { Id = Guid.NewGuid(), Name = "Payment Reminder (SMS)", Type = "sms", Event = "payment_reminder", Subject = "Payment Reminder", Body = "Dear {{customer_name}}, you have an outstanding of ₹{{balance}}. Please clear your dues at the earliest. — SS Mart", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new NotificationTemplate { Id = Guid.NewGuid(), Name = "Low Stock Alert (Email)", Type = "email", Event = "low_stock", Subject = "Low Stock Alert — {{product_name}}", Body = "Product {{product_name}} (SKU: {{sku}}) is running low. Current stock: {{current_stock}}. Reorder level: {{reorder_level}}.", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new NotificationTemplate { Id = Guid.NewGuid(), Name = "Daily Sales Summary (Email)", Type = "email", Event = "daily_summary", Subject = "Daily Sales Summary — {{date}}", Body = "Total sales: ₹{{total_sales}}. Bills: {{bill_count}}. Average bill: ₹{{avg_bill}}. Top product: {{top_product}}.", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new NotificationTemplate { Id = Guid.NewGuid(), Name = "Order Confirmation (WhatsApp)", Type = "whatsapp", Event = "order_confirmed", Subject = "Order Confirmed", Body = "Hi {{customer_name}}! Your order {{order_number}} is confirmed. Total: ₹{{total_amount}}. Expected delivery: {{delivery_date}}.", IsActive = false, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        );

        // Commission rules
        var rule1Id = new Guid("40000000-0000-0000-0000-000000000001");
        var rule2Id = new Guid("40000000-0000-0000-0000-000000000002");
        var rule3Id = new Guid("40000000-0000-0000-0000-000000000003");

        db.CommissionRules.AddRange(
            new CommissionRule { Id = rule1Id, Name = "Standard 2%", Type = "percentage", Value = 2, IsDefault = true, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new CommissionRule { Id = rule2Id, Name = "High-value 5%", Type = "percentage", Value = 5, MinBillAmount = 1000, IsDefault = false, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new CommissionRule { Id = rule3Id, Name = "Fixed ₹10 per bill", Type = "fixed_per_bill", Value = 10, IsDefault = false, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        );

        // Commission entries (seeded from existing bills)
        db.CommissionEntries.AddRange(
            new CommissionEntry { Id = Guid.NewGuid(), EmployeeId = emp1.Id, BillId = bill1.Id, BillNumber = bill1.BillNumber, SaleAmount = bill1.TotalAmount, CommissionRate = 2, CommissionAmount = 8.84m, RuleType = "percentage", SaleDate = bill1.BillDate, Status = "paid", CommissionRuleId = rule1Id, PaidAt = billDate1, CreatedAt = billDate1, UpdatedAt = billDate1 },
            new CommissionEntry { Id = Guid.NewGuid(), EmployeeId = emp1.Id, BillId = bill2.Id, BillNumber = bill2.BillNumber, SaleAmount = bill2.TotalAmount, CommissionRate = 2, CommissionAmount = 3.24m, RuleType = "percentage", SaleDate = bill2.BillDate, Status = "approved", CommissionRuleId = rule1Id, CreatedAt = bill2Date, UpdatedAt = bill2Date },
            new CommissionEntry { Id = Guid.NewGuid(), EmployeeId = emp2.Id, BillId = bill1.Id, BillNumber = bill1.BillNumber, SaleAmount = bill1.TotalAmount, CommissionRate = 5, CommissionAmount = 22.10m, RuleType = "percentage", SaleDate = bill1.BillDate, Status = "paid", CommissionRuleId = rule2Id, PaidAt = billDate1, CreatedAt = billDate1, UpdatedAt = billDate1 }
        );

        db.SaveChanges();
    }

    private static Product Make(string name, string sku, string? barcode, string hsn, double packSize, decimal mrp, decimal sell, decimal purchase, decimal tax, Guid catId)
    {
        return new Product
        {
            Id = Guid.NewGuid(), Name = name, SKU = sku, Barcode = barcode, HSNCode = hsn,
            Unit = "PCS", PackSize = packSize, MRP = mrp, SellingPrice = sell,
            PurchasePrice = purchase, TaxRate = tax, TaxType = "GST",
            CategoryId = catId, ReorderLevel = 10, CurrentStock = 0, IsActive = true,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
    }

    private static Customer MakeC(string name, string? phone, string type, decimal creditLimit, decimal balance)
    {
        return new Customer
        {
            Id = Guid.NewGuid(), Name = name, Phone = phone, Type = type,
            CreditLimit = creditLimit, CurrentBalance = balance,
            IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
    }

    private static Category MakeCat(string name, string? description, string? color, string? icon, int sortOrder, Guid id)
    {
        return new Category
        {
            Id = id, Name = name, Description = description, Color = color,
            Icon = icon, SortOrder = sortOrder,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
    }

    private static Supplier MakeSup(string name, string? phone, string? email, string? address, string? city, string? state, string? gstin, string? pan, decimal creditLimit, decimal balance, string paymentTerms, Guid id)
    {
        return new Supplier
        {
            Id = id, Name = name, Phone = phone, Email = email, Address = address,
            City = city, State = state, GSTIN = gstin, PAN = pan,
            CreditLimit = creditLimit, CurrentBalance = balance,
            PaymentTerms = paymentTerms, IsActive = true,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
    }
}
