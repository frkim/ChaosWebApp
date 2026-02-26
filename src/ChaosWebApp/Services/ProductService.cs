using ChaosWebApp.Models;

namespace ChaosWebApp.Services;

public interface IProductService
{
    IReadOnlyList<Product> GetAllProducts();
    Product? GetProductById(int id);
    PagedResponse<Product> GetProducts(int page, int pageSize, string sortBy, bool ascending, string filter);
    void AddRandomProducts(int count);
    int TotalCount { get; }
}

public class ProductService : IProductService
{
    private readonly List<Product> _products;
    private readonly ReaderWriterLockSlim _lock = new();
    private int _nextId;

    public int TotalCount
    {
        get
        {
            _lock.EnterReadLock();
            try { return _products.Count; }
            finally { _lock.ExitReadLock(); }
        }
    }

    public ProductService()
    {
        _products = CreateSeedProducts();
        _nextId = _products.Max(p => p.Id) + 1;
    }

    public IReadOnlyList<Product> GetAllProducts()
    {
        _lock.EnterReadLock();
        try { return _products.ToList().AsReadOnly(); }
        finally { _lock.ExitReadLock(); }
    }

    public Product? GetProductById(int id)
    {
        _lock.EnterReadLock();
        try { return _products.FirstOrDefault(p => p.Id == id); }
        finally { _lock.ExitReadLock(); }
    }

    public PagedResponse<Product> GetProducts(int page, int pageSize, string sortBy, bool ascending, string filter)
    {
        _lock.EnterReadLock();
        try
        {
            IEnumerable<Product> query = _products;

            // Filter
            if (!string.IsNullOrWhiteSpace(filter))
            {
                var f = filter.Trim().ToLowerInvariant();
                query = query.Where(p =>
                    p.Name.Contains(f, StringComparison.OrdinalIgnoreCase) ||
                    p.Brand.Contains(f, StringComparison.OrdinalIgnoreCase) ||
                    p.Category.Contains(f, StringComparison.OrdinalIgnoreCase) ||
                    p.SubCategory.Contains(f, StringComparison.OrdinalIgnoreCase) ||
                    p.Description.Contains(f, StringComparison.OrdinalIgnoreCase));
            }

            // Sort
            query = sortBy?.ToLowerInvariant() switch
            {
                "name"        => ascending ? query.OrderBy(p => p.Name)        : query.OrderByDescending(p => p.Name),
                "brand"       => ascending ? query.OrderBy(p => p.Brand)       : query.OrderByDescending(p => p.Brand),
                "category"    => ascending ? query.OrderBy(p => p.Category)    : query.OrderByDescending(p => p.Category),
                "price"       => ascending ? query.OrderBy(p => p.Price)       : query.OrderByDescending(p => p.Price),
                "rating"      => ascending ? query.OrderBy(p => p.Rating)      : query.OrderByDescending(p => p.Rating),
                "stock"       => ascending ? query.OrderBy(p => p.Stock)       : query.OrderByDescending(p => p.Stock),
                "addeddate"   => ascending ? query.OrderBy(p => p.AddedDate)   : query.OrderByDescending(p => p.AddedDate),
                _             => ascending ? query.OrderBy(p => p.Id)          : query.OrderByDescending(p => p.Id),
            };

            var list = query.ToList();
            var total = list.Count;
            var items = list.Skip((page - 1) * pageSize).Take(pageSize);

            return new PagedResponse<Product>
            {
                Items     = items,
                TotalCount = total,
                Page      = page,
                PageSize  = pageSize
            };
        }
        finally { _lock.ExitReadLock(); }
    }

    public void AddRandomProducts(int count)
    {
        var rng = new Random();
        var newProducts = Enumerable.Range(0, count).Select(_ => GenerateRandomProduct(rng)).ToList();

        _lock.EnterWriteLock();
        try
        {
            foreach (var p in newProducts)
            {
                p.Id = _nextId++;
                _products.Add(p);
            }
        }
        finally { _lock.ExitWriteLock(); }
    }

    // ── Seed data ──────────────────────────────────────────────────────────────
    private static List<Product> CreateSeedProducts() =>
    [
        // ── Smartphones ───────────────────────────────────────────────────────
        new()
        {
            Id = 1, Name = "Apple iPhone 16 Pro 256GB Natural Titanium", Brand = "Apple",
            Category = "Phones", SubCategory = "Smartphones",
            Price = 1229m, OriginalPrice = 1329m, Stock = 42,
            Rating = 4.8, ReviewCount = 3812,
            Description = "The iPhone 16 Pro features the A18 Pro chip, a 6.3-inch Super Retina XDR display with ProMotion 120Hz, a triple 48MP camera system with 5x optical zoom, and a record 27-hour battery life.",
            Ean = "0195949082535", IsNew = true, IsPromotion = true, AddedDate = DateTime.UtcNow.AddDays(-5)
        },
        new()
        {
            Id = 2, Name = "Samsung Galaxy S25 Ultra 512GB Titanium Black", Brand = "Samsung",
            Category = "Phones", SubCategory = "Smartphones",
            Price = 1419m, Stock = 28,
            Rating = 4.7, ReviewCount = 2145,
            Description = "The Galaxy S25 Ultra features the Snapdragon 8 Elite processor, a 6.9\" Dynamic AMOLED 120Hz display, built-in S Pen, 200MP main camera, and 5000mAh battery.",
            Ean = "8806095311807", IsNew = true, IsPromotion = false, AddedDate = DateTime.UtcNow.AddDays(-3)
        },
        new()
        {
            Id = 3, Name = "Google Pixel 9 Pro 256GB Obsidian", Brand = "Google",
            Category = "Phones", SubCategory = "Smartphones",
            Price = 1099m, OriginalPrice = 1199m, Stock = 15,
            Rating = 4.6, ReviewCount = 987,
            Description = "The Pixel 9 Pro offers built-in Google AI with the Tensor G4 chip, 7 years of guaranteed updates, a 6.3\" OLED display, and the best Android photography experience.",
            Ean = "0840244707218", IsNew = true, IsPromotion = true, AddedDate = DateTime.UtcNow.AddDays(-10)
        },
        new()
        {
            Id = 4, Name = "Apple iPhone 15 128GB Black", Brand = "Apple",
            Category = "Phones", SubCategory = "Smartphones",
            Price = 829m, OriginalPrice = 969m, Stock = 67,
            Rating = 4.7, ReviewCount = 5234,
            Description = "The iPhone 15 with A16 Bionic chip, Dynamic Island, USB-C, and a 48MP main camera.",
            Ean = "0195949030345", IsNew = false, IsPromotion = true, AddedDate = DateTime.UtcNow.AddDays(-120)
        },
        // ── Laptops ───────────────────────────────────────────────────────────
        new()
        {
            Id = 5, Name = "Apple MacBook Pro 14\" M3 Pro 18GB / 512GB Silver", Brand = "Apple",
            Category = "Computers", SubCategory = "Laptops",
            Price = 2199m, Stock = 12,
            Rating = 4.9, ReviewCount = 1456,
            Description = "MacBook Pro 14-inch with M3 Pro chip (11-core CPU, 14-core GPU), 18GB unified memory, 512GB SSD, Liquid Retina XDR display, up to 18 hours of battery life.",
            Ean = "0195949100572", IsNew = false, IsPromotion = false, AddedDate = DateTime.UtcNow.AddDays(-90)
        },
        new()
        {
            Id = 6, Name = "Dell XPS 15 9530 Intel Core i7 16GB / 512GB", Brand = "Dell",
            Category = "Computers", SubCategory = "Laptops",
            Price = 1599m, OriginalPrice = 1799m, Stock = 8,
            Rating = 4.5, ReviewCount = 678,
            Description = "Dell XPS 15 with Intel Core i7-13700H processor, 16GB DDR5, 512GB NVMe SSD, 15.6\" OLED 3.5K touch display, and NVIDIA GeForce RTX 4060.",
            Ean = "0884116433476", IsNew = false, IsPromotion = true, AddedDate = DateTime.UtcNow.AddDays(-45)
        },
        new()
        {
            Id = 7, Name = "Lenovo ThinkPad X1 Carbon Gen 12 Intel Core Ultra 7 32GB / 1TB", Brand = "Lenovo",
            Category = "Computers", SubCategory = "Laptops",
            Price = 2099m, Stock = 5,
            Rating = 4.7, ReviewCount = 342,
            Description = "Professional ultramobile with Intel Core Ultra 7 155U, 32GB LPDDR5, 1TB SSD, 14\" WUXGA IPS display, and MIL-STD-810H certification.",
            Ean = "0196804764139", IsNew = true, IsPromotion = false, AddedDate = DateTime.UtcNow.AddDays(-20)
        },
        new()
        {
            Id = 8, Name = "ASUS ROG Zephyrus G16 AMD Ryzen AI 9 32GB / 2TB RTX 4080", Brand = "ASUS",
            Category = "Computers", SubCategory = "Gaming Laptops",
            Price = 2499m, OriginalPrice = 2799m, Stock = 6,
            Rating = 4.8, ReviewCount = 523,
            Description = "Premium gaming laptop with AMD Ryzen AI 9 HX 370, 32GB DDR5, 2TB SSD, NVIDIA RTX 4080 12GB, and 16\" QHD+ 240Hz ROG Nebula display.",
            Ean = "4711387479254", IsNew = true, IsPromotion = true, AddedDate = DateTime.UtcNow.AddDays(-15)
        },
        // ── Tablets ───────────────────────────────────────────────────────────
        new()
        {
            Id = 9, Name = "Apple iPad Pro 13\" M4 256GB Wi-Fi Silver", Brand = "Apple",
            Category = "Tablets", SubCategory = "Apple Tablets",
            Price = 1299m, Stock = 22,
            Rating = 4.9, ReviewCount = 987,
            Description = "iPad Pro with M4 chip, 13-inch Ultra Retina XDR OLED display, ultra-thin 5.1mm design, and Apple Pencil Pro compatibility.",
            Ean = "0195949128667", IsNew = true, IsPromotion = false, AddedDate = DateTime.UtcNow.AddDays(-8)
        },
        new()
        {
            Id = 10, Name = "Samsung Galaxy Tab S9 FE 256GB Wi-Fi Graphite", Brand = "Samsung",
            Category = "Tablets", SubCategory = "Android Tablets",
            Price = 449m, OriginalPrice = 549m, Stock = 34,
            Rating = 4.4, ReviewCount = 1234,
            Description = "Samsung tablet with 10.9\" FHD+ TFT LCD display, Exynos 1380 processor, 8GB RAM, 10,090mAh battery, and IP68 resistance.",
            Ean = "8806095102931", IsNew = false, IsPromotion = true, AddedDate = DateTime.UtcNow.AddDays(-60)
        },
        // ── Headphones ────────────────────────────────────────────────────────
        new()
        {
            Id = 11, Name = "Sony WH-1000XM5 Bluetooth ANC Headphones Black", Brand = "Sony",
            Category = "Audio", SubCategory = "Headphones",
            Price = 299m, OriginalPrice = 379m, Stock = 88,
            Rating = 4.8, ReviewCount = 8765,
            Description = "The WH-1000XM5 is the industry-leading noise-canceling headphone. With 30 hours of battery life, 8 microphones, and DSEE Extreme technology, it delivers exceptional sound quality.",
            Ean = "4548736132580", IsNew = false, IsPromotion = true, AddedDate = DateTime.UtcNow.AddDays(-180)
        },
        new()
        {
            Id = 12, Name = "Apple AirPods Pro (2nd Generation) with MagSafe Case", Brand = "Apple",
            Category = "Audio", SubCategory = "Earbuds",
            Price = 249m, Stock = 156,
            Rating = 4.7, ReviewCount = 12456,
            Description = "AirPods Pro 2 with active noise cancellation, personalized spatial audio, H2 chip, and 6-hour battery life (30 hours with case).",
            Ean = "0194253553915", IsNew = false, IsPromotion = false, AddedDate = DateTime.UtcNow.AddDays(-200)
        },
        new()
        {
            Id = 13, Name = "Bose QuietComfort 45 Bluetooth ANC Headphones White", Brand = "Bose",
            Category = "Audio", SubCategory = "Headphones",
            Price = 279m, OriginalPrice = 329m, Stock = 45,
            Rating = 4.6, ReviewCount = 3421,
            Description = "The Bose QC45 combines legendary noise cancellation, exceptional comfort, and 24 hours of battery life for an immersive listening experience.",
            Ean = "017817833011", IsNew = false, IsPromotion = true, AddedDate = DateTime.UtcNow.AddDays(-150)
        },
        new()
        {
            Id = 14, Name = "JBL Charge 5 Waterproof Bluetooth Speaker Red", Brand = "JBL",
            Category = "Audio", SubCategory = "Speakers",
            Price = 159m, OriginalPrice = 199m, Stock = 72,
            Rating = 4.6, ReviewCount = 5678,
            Description = "Portable speaker with powerful sound, IP67 waterproofing, built-in PowerBank, and 20 hours of battery life.",
            Ean = "6925281990571", IsNew = false, IsPromotion = true, AddedDate = DateTime.UtcNow.AddDays(-100)
        },
        // ── Smart TVs ─────────────────────────────────────────────────────────
        new()
        {
            Id = 15, Name = "Samsung Neo QLED 65\" QN95D 4K Smart TV 2024", Brand = "Samsung",
            Category = "TV & Video", SubCategory = "Televisions",
            Price = 1799m, OriginalPrice = 2299m, Stock = 7,
            Rating = 4.7, ReviewCount = 876,
            Description = "65\" Neo QLED TV with Neural Quantum 4K Gen2 processor, HDR 2000 nits, Dolby Atmos, and Gaming Hub for the ultimate experience.",
            Ean = "8806094975901", IsNew = true, IsPromotion = true, AddedDate = DateTime.UtcNow.AddDays(-25)
        },
        new()
        {
            Id = 16, Name = "LG OLED evo G4 55\" 4K Smart TV", Brand = "LG",
            Category = "TV & Video", SubCategory = "Televisions",
            Price = 1599m, Stock = 9,
            Rating = 4.8, ReviewCount = 654,
            Description = "55\" OLED evo G4 TV with α11 AI 4K processor, MLA (Micro Lens Array), HDR10/Dolby Vision IQ, and 4 HDMI 2.1 ports.",
            Ean = "8806087636528", IsNew = true, IsPromotion = false, AddedDate = DateTime.UtcNow.AddDays(-30)
        },
        // ── Cameras ───────────────────────────────────────────────────────────
        new()
        {
            Id = 17, Name = "Sony Alpha A7 IV Body Only Mirrorless 33MP", Brand = "Sony",
            Category = "Photo & Video", SubCategory = "Mirrorless Cameras",
            Price = 2699m, Stock = 11,
            Rating = 4.9, ReviewCount = 1234,
            Description = "Full-frame 33MP mirrorless camera with intelligent AF system, 4K 60p video, and dual SD card slots.",
            Ean = "4548736127852", IsNew = false, IsPromotion = false, AddedDate = DateTime.UtcNow.AddDays(-200)
        },
        new()
        {
            Id = 18, Name = "Canon EOS R6 Mark II Body Only", Brand = "Canon",
            Category = "Photo & Video", SubCategory = "Mirrorless Cameras",
            Price = 2499m, OriginalPrice = 2799m, Stock = 8,
            Rating = 4.8, ReviewCount = 876,
            Description = "Canon 24.2MP mirrorless with Dual Pixel CMOS AF II, 40fps burst, and oversampled 4K HDR video.",
            Ean = "4549292205367", IsNew = false, IsPromotion = true, AddedDate = DateTime.UtcNow.AddDays(-80)
        },
        // ── Gaming ────────────────────────────────────────────────────────────
        new()
        {
            Id = 19, Name = "Sony PlayStation 5 Slim Disc Console", Brand = "Sony",
            Category = "Gaming", SubCategory = "Consoles",
            Price = 449m, Stock = 23,
            Rating = 4.8, ReviewCount = 15678,
            Description = "PS5 Slim console with Ultra HD Blu-ray drive, 1TB NVMe SSD, DualSense controller, 8K resolution, and Ray Tracing.",
            Ean = "711719577980", IsNew = false, IsPromotion = false, AddedDate = DateTime.UtcNow.AddDays(-90)
        },
        new()
        {
            Id = 20, Name = "Microsoft Xbox Series X 1TB", Brand = "Microsoft",
            Category = "Gaming", SubCategory = "Consoles",
            Price = 499m, Stock = 18,
            Rating = 4.7, ReviewCount = 8934,
            Description = "Xbox Series X console with 1TB NVMe SSD, 8K resolution, up to 120fps refresh rate, and full backward compatibility.",
            Ean = "0889842640816", IsNew = false, IsPromotion = false, AddedDate = DateTime.UtcNow.AddDays(-120)
        },
        new()
        {
            Id = 21, Name = "Nintendo Switch OLED White", Brand = "Nintendo",
            Category = "Gaming", SubCategory = "Consoles",
            Price = 319m, Stock = 45,
            Rating = 4.8, ReviewCount = 22345,
            Description = "Hybrid console with 7-inch OLED screen, enhanced TV dock, 64GB internal storage, and 4.5-9 hours of portable battery life.",
            Ean = "0045496453442", IsNew = false, IsPromotion = false, AddedDate = DateTime.UtcNow.AddDays(-300)
        },
        new()
        {
            Id = 22, Name = "DualSense PS5 Controller White", Brand = "Sony",
            Category = "Gaming", SubCategory = "Accessories",
            Price = 69m, OriginalPrice = 79m, Stock = 123,
            Rating = 4.7, ReviewCount = 34567,
            Description = "DualSense controller with haptic feedback, adaptive triggers, built-in microphone, and rechargeable battery.",
            Ean = "711719827528", IsNew = false, IsPromotion = true, AddedDate = DateTime.UtcNow.AddDays(-200)
        },
        new()
        {
            Id = 23, Name = "The Legend of Zelda: Tears of the Kingdom Nintendo Switch", Brand = "Nintendo",
            Category = "Gaming", SubCategory = "Video Games",
            Price = 49m, OriginalPrice = 69m, Stock = 234,
            Rating = 4.9, ReviewCount = 45678,
            Description = "Epic sequel to Breath of the Wild. Explore the kingdom of Hyrule and sky islands in this extraordinary open-world adventure.",
            Ean = "0045496478384", IsNew = false, IsPromotion = true, AddedDate = DateTime.UtcNow.AddDays(-400)
        },
        // ── Books ─────────────────────────────────────────────────────────────
        new()
        {
            Id = 24, Name = "The Three-Body Problem - Liu Cixin", Brand = "Tor Books",
            Category = "Books", SubCategory = "Science Fiction",
            Price = 9.90m, OriginalPrice = 11.50m, Stock = 456,
            Rating = 4.7, ReviewCount = 23456,
            Description = "First volume of the Three-Body trilogy. A monumental work of Chinese science fiction, Hugo Award for Best Novel 2015.",
            Ean = "9782330131067", IsNew = false, IsPromotion = true, AddedDate = DateTime.UtcNow.AddDays(-500)
        },
        new()
        {
            Id = 25, Name = "Clean Code: A Handbook of Agile Software Craftsmanship - Robert C. Martin", Brand = "Pearson",
            Category = "Books", SubCategory = "Computer Science",
            Price = 42m, Stock = 87,
            Rating = 4.6, ReviewCount = 12345,
            Description = "The definitive reference for writing clean and maintainable code. Essential for every professional developer.",
            Ean = "9780132350884", IsNew = false, IsPromotion = false, AddedDate = DateTime.UtcNow.AddDays(-700)
        },
        new()
        {
            Id = 26, Name = "Atomic Habits - James Clear", Brand = "Avery",
            Category = "Books", SubCategory = "Self-Help",
            Price = 19.90m, Stock = 234,
            Rating = 4.8, ReviewCount = 34567,
            Description = "Transform your habits and achieve extraordinary results. The revolutionary method to improve 1% every day.",
            Ean = "9782080274960", IsNew = false, IsPromotion = false, AddedDate = DateTime.UtcNow.AddDays(-365)
        },
        // ── Music (CDs) ───────────────────────────────────────────────────────
        new()
        {
            Id = 27, Name = "Daft Punk - Random Access Memories (Drumless Edition) CD", Brand = "Columbia",
            Category = "Music", SubCategory = "CD",
            Price = 14.99m, Stock = 123,
            Rating = 4.9, ReviewCount = 8765,
            Description = "The Drumless edition of Daft Punk's legendary album, reimagined to reveal all its harmonies.",
            Ean = "0194398821429", IsNew = false, IsPromotion = false, AddedDate = DateTime.UtcNow.AddDays(-200)
        },
        new()
        {
            Id = 28, Name = "Stromae - Multitude CD", Brand = "Mosaert",
            Category = "Music", SubCategory = "CD",
            Price = 16.99m, OriginalPrice = 19.99m, Stock = 89,
            Rating = 4.7, ReviewCount = 5678,
            Description = "Stromae's third studio album, an intimate and universal work about vulnerability and the human condition.",
            Ean = "5054197239205", IsNew = false, IsPromotion = true, AddedDate = DateTime.UtcNow.AddDays(-300)
        },
        // ── Movies ────────────────────────────────────────────────────────────
        new()
        {
            Id = 29, Name = "Oppenheimer 4K Blu-Ray + Blu-Ray", Brand = "Universal",
            Category = "Movies & Series", SubCategory = "4K Blu-Ray",
            Price = 24.99m, OriginalPrice = 34.99m, Stock = 167,
            Rating = 4.8, ReviewCount = 12345,
            Description = "Christopher Nolan's masterpiece in 4K Ultra HD with Dolby Atmos sound for a cinematic experience at home.",
            Ean = "5053083268190", IsNew = false, IsPromotion = true, AddedDate = DateTime.UtcNow.AddDays(-180)
        },
        new()
        {
            Id = 30, Name = "Dune - Deuxième Partie Blu-Ray", Brand = "Warner Bros",
            Category = "Movies & Series", SubCategory = "Blu-Ray",
            Price = 19.99m, Stock = 234,
            Rating = 4.7, ReviewCount = 8765,
            Description = "The epic conclusion of Denis Villeneuve's adaptation. Paul Atreides leads the Fremen in their holy war against the Empire.",
            Ean = "5051895485507", IsNew = false, IsPromotion = false, AddedDate = DateTime.UtcNow.AddDays(-100)
        },
        // ── Smart Home ────────────────────────────────────────────────────────
        new()
        {
            Id = 31, Name = "Amazon Echo Dot (5th Generation) Charcoal", Brand = "Amazon",
            Category = "Smart Home", SubCategory = "Voice Assistant",
            Price = 54.99m, OriginalPrice = 64.99m, Stock = 345,
            Rating = 4.4, ReviewCount = 45678,
            Description = "Smart speaker with Alexa, improved sound, built-in temperature sensor, and multicolor LED ring.",
            Ean = "0840080595464", IsNew = false, IsPromotion = true, AddedDate = DateTime.UtcNow.AddDays(-250)
        },
        new()
        {
            Id = 32, Name = "Philips Hue Starter Kit E27 White & Color Ambiance", Brand = "Philips",
            Category = "Smart Home", SubCategory = "Lighting",
            Price = 149m, OriginalPrice = 179m, Stock = 78,
            Rating = 4.5, ReviewCount = 23456,
            Description = "Starter kit with Hue bridge and 3 color E27 bulbs to create the perfect ambiance in your living room.",
            Ean = "8718696728987", IsNew = false, IsPromotion = true, AddedDate = DateTime.UtcNow.AddDays(-300)
        },
        // ── Wearables ─────────────────────────────────────────────────────────
        new()
        {
            Id = 33, Name = "Apple Watch Series 10 46mm GPS Aluminum Midnight Black", Brand = "Apple",
            Category = "Smartwatches", SubCategory = "Apple Watch",
            Price = 449m, Stock = 56,
            Rating = 4.7, ReviewCount = 7890,
            Description = "The thinnest and lightest Apple Watch ever. Sleep apnea detection, ECG, SpO2, and up to 18 hours of battery life.",
            Ean = "0195949441028", IsNew = true, IsPromotion = false, AddedDate = DateTime.UtcNow.AddDays(-12)
        },
        new()
        {
            Id = 34, Name = "Samsung Galaxy Watch 7 44mm LTE Silver", Brand = "Samsung",
            Category = "Smartwatches", SubCategory = "Smartwatches",
            Price = 329m, OriginalPrice = 399m, Stock = 43,
            Rating = 4.5, ReviewCount = 3456,
            Description = "Galaxy Watch 7 with Exynos W1000 chip, advanced sleep analysis, BioActive Sensor, and 40-hour battery life.",
            Ean = "8806095449555", IsNew = true, IsPromotion = true, AddedDate = DateTime.UtcNow.AddDays(-30)
        },
        // ── Accessories & Peripherals ─────────────────────────────────────────
        new()
        {
            Id = 35, Name = "Logitech MX Master 3S Wireless Mouse Graphite", Brand = "Logitech",
            Category = "Computers", SubCategory = "Mice",
            Price = 99m, OriginalPrice = 119m, Stock = 234,
            Rating = 4.8, ReviewCount = 34567,
            Description = "Premium mouse with 8000 DPI sensor, electromagnetic MagSpeed scroll wheel, advanced ergonomics, and 70-day battery life.",
            Ean = "097855176189", IsNew = false, IsPromotion = true, AddedDate = DateTime.UtcNow.AddDays(-180)
        },
        new()
        {
            Id = 36, Name = "Samsung T7 Shield External SSD 1TB Beige", Brand = "Samsung",
            Category = "Computers", SubCategory = "Storage",
            Price = 89m, OriginalPrice = 109m, Stock = 189,
            Rating = 4.7, ReviewCount = 12345,
            Description = "Ultra-fast external SSD (1050 MB/s) resistant to shocks, dust, and water (IP65) with AES 256-bit encryption.",
            Ean = "8806094374490", IsNew = false, IsPromotion = true, AddedDate = DateTime.UtcNow.AddDays(-90)
        },
        new()
        {
            Id = 37, Name = "Apple MagSafe Fast Charger 15W White", Brand = "Apple",
            Category = "Accessories", SubCategory = "Chargers",
            Price = 45m, Stock = 567,
            Rating = 4.5, ReviewCount = 23456,
            Description = "MagSafe charger for iPhone with optimal 15W charging power thanks to perfect magnetic alignment.",
            Ean = "0194252049785", IsNew = false, IsPromotion = false, AddedDate = DateTime.UtcNow.AddDays(-400)
        },
        new()
        {
            Id = 38, Name = "Anker 737 Power Bank 140W 24000mAh", Brand = "Anker",
            Category = "Accessories", SubCategory = "Batteries",
            Price = 129m, Stock = 78,
            Rating = 4.7, ReviewCount = 5678,
            Description = "High-capacity 24000mAh portable charger with 140W fast charging, 3 simultaneous charging ports, and smart LED display.",
            Ean = "0194644126781", IsNew = false, IsPromotion = false, AddedDate = DateTime.UtcNow.AddDays(-150)
        },
        // ── More Tech ─────────────────────────────────────────────────────────
        new()
        {
            Id = 39, Name = "DJI Mini 4 Pro Drone Only", Brand = "DJI",
            Category = "Photo & Video", SubCategory = "Drones",
            Price = 759m, Stock = 23,
            Rating = 4.8, ReviewCount = 3456,
            Description = "Compact drone under 249g, 4K/60fps HDR video, omnidirectional obstacle sensing, and 34-minute flight time.",
            Ean = "6941565967153", IsNew = true, IsPromotion = false, AddedDate = DateTime.UtcNow.AddDays(-60)
        },
        new()
        {
            Id = 40, Name = "GoPro HERO12 Black", Brand = "GoPro",
            Category = "Photo & Video", SubCategory = "Action Cameras",
            Price = 299m, OriginalPrice = 399m, Stock = 56,
            Rating = 4.6, ReviewCount = 8765,
            Description = "Action camera with HyperSmooth 6.0 stabilization, 5.3K 60fps video, waterproof to 10m, and improved 2-hour battery.",
            Ean = "0818279028830", IsNew = false, IsPromotion = true, AddedDate = DateTime.UtcNow.AddDays(-120)
        },
        // ── Office & Peripherals ──────────────────────────────────────────────
        new()
        {
            Id = 41, Name = "Brother MFC-L3760CDW Color Laser Printer", Brand = "Brother",
            Category = "Computers", SubCategory = "Printers",
            Price = 349m, OriginalPrice = 429m, Stock = 12,
            Rating = 4.3, ReviewCount = 1234,
            Description = "Color laser multifunction WiFi with auto duplex printing, 50-sheet ADF scanner, and Ethernet.",
            Ean = "4977766819084", IsNew = false, IsPromotion = true, AddedDate = DateTime.UtcNow.AddDays(-90)
        },
        new()
        {
            Id = 42, Name = "LG UltraWide 34\" Curved Monitor QHD 100Hz", Brand = "LG",
            Category = "Computers", SubCategory = "Monitors",
            Price = 449m, OriginalPrice = 599m, Stock = 17,
            Rating = 4.6, ReviewCount = 4567,
            Description = "34\" curved IPS QHD+ (3440x1440) monitor with HDR10, AMD FreeSync, 60W USB-C, and 5ms response time.",
            Ean = "8806091987990", IsNew = false, IsPromotion = true, AddedDate = DateTime.UtcNow.AddDays(-60)
        },
        // ── More Gaming ───────────────────────────────────────────────────────
        new()
        {
            Id = 43, Name = "Hogwarts Legacy PS5", Brand = "Warner Bros Interactive",
            Category = "Gaming", SubCategory = "Video Games",
            Price = 39.99m, OriginalPrice = 79.99m, Stock = 345,
            Rating = 4.7, ReviewCount = 56789,
            Description = "Explore the magical world of Hogwarts in the 1800s. An open-world action-adventure RPG set in the wizarding world.",
            Ean = "5051892226684", IsNew = false, IsPromotion = true, AddedDate = DateTime.UtcNow.AddDays(-400)
        },
        new()
        {
            Id = 44, Name = "Razer BlackShark V2 Pro Wireless Gaming Headset", Brand = "Razer",
            Category = "Gaming", SubCategory = "Accessories",
            Price = 149m, OriginalPrice = 199m, Stock = 67,
            Rating = 4.5, ReviewCount = 7890,
            Description = "Wireless gaming headset with 50mm TriForce Titanium drivers, SmartSwitch, and 70 hours of battery life.",
            Ean = "8886419378327", IsNew = false, IsPromotion = true, AddedDate = DateTime.UtcNow.AddDays(-150)
        },
        new()
        {
            Id = 45, Name = "Corsair K100 RGB Mechanical Keyboard Cherry MX Speed", Brand = "Corsair",
            Category = "Gaming", SubCategory = "Accessories",
            Price = 219m, OriginalPrice = 259m, Stock = 34,
            Rating = 4.7, ReviewCount = 5678,
            Description = "Premium mechanical keyboard with Cherry MX Speed switches, OPX Axle, iCUE control dial, and per-key RGB backlighting.",
            Ean = "0840006616719", IsNew = false, IsPromotion = true, AddedDate = DateTime.UtcNow.AddDays(-200)
        },
        // ── More Books ────────────────────────────────────────────────────────
        new()
        {
            Id = 46, Name = "The Pragmatic Programmer - Andrew Hunt & David Thomas", Brand = "Addison-Wesley",
            Category = "Books", SubCategory = "Computer Science",
            Price = 45m, Stock = 123,
            Rating = 4.8, ReviewCount = 23456,
            Description = "The essential guide for the pragmatic developer. From theory to practice for building quality software.",
            Ean = "9780135957059", IsNew = false, IsPromotion = false, AddedDate = DateTime.UtcNow.AddDays(-600)
        },
        new()
        {
            Id = 47, Name = "Foundation - Isaac Asimov (Complete)", Brand = "Del Rey",
            Category = "Books", SubCategory = "Science Fiction",
            Price = 34.90m, Stock = 78,
            Rating = 4.8, ReviewCount = 12345,
            Description = "The complete edition of Asimov's masterpiece. The saga of the Galactic Empire and the Foundation that works to preserve civilization.",
            Ean = "9782290388341", IsNew = false, IsPromotion = false, AddedDate = DateTime.UtcNow.AddDays(-800)
        },
        new()
        {
            Id = 48, Name = "Microsoft Surface Pro 11 Snapdragon X Elite 16GB / 512GB", Brand = "Microsoft",
            Category = "Tablets", SubCategory = "Windows Tablets",
            Price = 1499m, Stock = 9,
            Rating = 4.5, ReviewCount = 567,
            Description = "Tablet-PC with Snapdragon X Elite, 16GB RAM, 512GB SSD, 13\" PixelSense Flow 120Hz display, and 14-hour battery life.",
            Ean = "0889842986716", IsNew = true, IsPromotion = false, AddedDate = DateTime.UtcNow.AddDays(-20)
        },
        new()
        {
            Id = 49, Name = "Dyson V15 Detect Absolute Cordless Vacuum", Brand = "Dyson",
            Category = "Appliances", SubCategory = "Vacuums",
            Price = 699m, OriginalPrice = 799m, Stock = 23,
            Rating = 4.6, ReviewCount = 8765,
            Description = "Dyson laser vacuum with Fluffy laser detection, HEPA filtration, 60-minute runtime, and LCD performance display.",
            Ean = "5025155069622", IsNew = false, IsPromotion = true, AddedDate = DateTime.UtcNow.AddDays(-180)
        },
        new()
        {
            Id = 50, Name = "Nespresso Vertuo Next Premium Titanium + Aeroccino", Brand = "Nespresso",
            Category = "Appliances", SubCategory = "Coffee Machines",
            Price = 169m, OriginalPrice = 229m, Stock = 56,
            Rating = 4.5, ReviewCount = 12345,
            Description = "Nespresso Vertuo machine with Centrifusion technology, barcode capsule recognition, and Aeroccino 3 milk frother.",
            Ean = "7630047641157", IsNew = false, IsPromotion = true, AddedDate = DateTime.UtcNow.AddDays(-250)
        }
    ];

    // ── Random product generator ───────────────────────────────────────────────
    private static readonly string[] _randomBrands = ["Sony", "Samsung", "Apple", "LG", "Logitech", "Razer", "ASUS", "Dell", "HP", "Lenovo", "Canon", "Nikon", "JBL", "Bose", "Philips", "Dyson", "Nintendo", "Microsoft", "Amazon", "Google"];
    private static readonly string[] _randomCategories = ["Phones", "Computers", "Audio", "Gaming", "TV & Video", "Photo & Video", "Smart Home", "Accessories"];
    private static readonly string[] _randomAdjectives = ["Pro", "Elite", "Ultra", "Max", "Plus", "Premium", "Advanced", "Smart", "Slim", "Nano"];
    private static readonly string[] _randomColors = ["Black", "White", "Silver", "Blue", "Red", "Gray", "Gold", "Green"];

    private static Product GenerateRandomProduct(Random rng)
    {
        var brand = _randomBrands[rng.Next(_randomBrands.Length)];
        var category = _randomCategories[rng.Next(_randomCategories.Length)];
        var adj = _randomAdjectives[rng.Next(_randomAdjectives.Length)];
        var color = _randomColors[rng.Next(_randomColors.Length)];
        var model = $"{brand} {adj} {rng.Next(100, 9999)} {color}";
        var price = Math.Round((decimal)(rng.NextDouble() * 1500 + 9.99), 2);
        var hasPromo = rng.NextDouble() > 0.6;
        var originalPrice = hasPromo ? price * (decimal)(1 + rng.NextDouble() * 0.4) : (decimal?)null;
        if (originalPrice.HasValue) originalPrice = Math.Round(originalPrice.Value, 2);

        return new Product
        {
            Name = model,
            Brand = brand,
            Category = category,
            SubCategory = category,
            Price = price,
            OriginalPrice = originalPrice,
            Stock = rng.Next(0, 500),
            Rating = Math.Round(rng.NextDouble() * 1.5 + 3.5, 1),
            ReviewCount = rng.Next(10, 50000),
            Description = $"High-quality {brand} product in the {category} category. Modern design and exceptional performance.",
            Ean = string.Concat(Enumerable.Range(0, 13).Select(_ => rng.Next(0, 10).ToString())), // Mock EAN — not a valid EAN-13 (no check digit)
            IsNew = rng.NextDouble() > 0.8,
            IsPromotion = hasPromo,
            AddedDate = DateTime.UtcNow.AddDays(-rng.Next(0, 730))
        };
    }
}
