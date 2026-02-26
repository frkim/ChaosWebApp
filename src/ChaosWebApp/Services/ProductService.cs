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
            Id = 1, Name = "Apple iPhone 16 Pro 256 Go Titane naturel", Brand = "Apple",
            Category = "Téléphonie", SubCategory = "Smartphones",
            Price = 1229m, OriginalPrice = 1329m, Stock = 42,
            Rating = 4.8, ReviewCount = 3812,
            Description = "L'iPhone 16 Pro embarque la puce A18 Pro, un écran Super Retina XDR de 6,3 pouces avec ProMotion 120 Hz, un système de triple caméra 48 Mpx avec zoom optique 5x et une autonomie record de 27 heures.",
            Ean = "0195949082535", IsNew = true, IsPromotion = true, AddedDate = DateTime.UtcNow.AddDays(-5)
        },
        new()
        {
            Id = 2, Name = "Samsung Galaxy S25 Ultra 512 Go Titanium Black", Brand = "Samsung",
            Category = "Téléphonie", SubCategory = "Smartphones",
            Price = 1419m, Stock = 28,
            Rating = 4.7, ReviewCount = 2145,
            Description = "Le Galaxy S25 Ultra intègre le processeur Snapdragon 8 Elite, un écran Dynamic AMOLED 6,9\" 120Hz, un S Pen intégré, une caméra principale 200 Mpx et une batterie de 5000 mAh.",
            Ean = "8806095311807", IsNew = true, IsPromotion = false, AddedDate = DateTime.UtcNow.AddDays(-3)
        },
        new()
        {
            Id = 3, Name = "Google Pixel 9 Pro 256 Go Obsidian", Brand = "Google",
            Category = "Téléphonie", SubCategory = "Smartphones",
            Price = 1099m, OriginalPrice = 1199m, Stock = 15,
            Rating = 4.6, ReviewCount = 987,
            Description = "Le Pixel 9 Pro offre l'IA Google intégrée avec la puce Tensor G4, des mises à jour garanties 7 ans, un écran OLED 6,3\" et la meilleure expérience photographique Android.",
            Ean = "0840244707218", IsNew = true, IsPromotion = true, AddedDate = DateTime.UtcNow.AddDays(-10)
        },
        new()
        {
            Id = 4, Name = "Apple iPhone 15 128 Go Noir", Brand = "Apple",
            Category = "Téléphonie", SubCategory = "Smartphones",
            Price = 829m, OriginalPrice = 969m, Stock = 67,
            Rating = 4.7, ReviewCount = 5234,
            Description = "L'iPhone 15 avec puce A16 Bionic, Dynamic Island, USB-C et une caméra principale 48 Mpx.",
            Ean = "0195949030345", IsNew = false, IsPromotion = true, AddedDate = DateTime.UtcNow.AddDays(-120)
        },
        // ── Laptops ───────────────────────────────────────────────────────────
        new()
        {
            Id = 5, Name = "Apple MacBook Pro 14\" M3 Pro 18 Go / 512 Go Argent", Brand = "Apple",
            Category = "Informatique", SubCategory = "PC Portables",
            Price = 2199m, Stock = 12,
            Rating = 4.9, ReviewCount = 1456,
            Description = "MacBook Pro 14 pouces avec puce M3 Pro (11 cœurs CPU, 14 cœurs GPU), 18 Go de mémoire unifiée, SSD 512 Go, écran Liquid Retina XDR, autonomie jusqu'à 18 heures.",
            Ean = "0195949100572", IsNew = false, IsPromotion = false, AddedDate = DateTime.UtcNow.AddDays(-90)
        },
        new()
        {
            Id = 6, Name = "Dell XPS 15 9530 Intel Core i7 16 Go / 512 Go", Brand = "Dell",
            Category = "Informatique", SubCategory = "PC Portables",
            Price = 1599m, OriginalPrice = 1799m, Stock = 8,
            Rating = 4.5, ReviewCount = 678,
            Description = "Dell XPS 15 avec processeur Intel Core i7-13700H, 16 Go DDR5, SSD NVMe 512 Go, écran OLED 3.5K tactile 15,6\" et NVIDIA GeForce RTX 4060.",
            Ean = "0884116433476", IsNew = false, IsPromotion = true, AddedDate = DateTime.UtcNow.AddDays(-45)
        },
        new()
        {
            Id = 7, Name = "Lenovo ThinkPad X1 Carbon Gen 12 Intel Core Ultra 7 32 Go / 1 To", Brand = "Lenovo",
            Category = "Informatique", SubCategory = "PC Portables",
            Price = 2099m, Stock = 5,
            Rating = 4.7, ReviewCount = 342,
            Description = "Ultramobile professionnel avec Intel Core Ultra 7 155U, 32 Go LPDDR5, SSD 1 To, écran IPS 14\" WUXGA et certification MIL-STD-810H.",
            Ean = "0196804764139", IsNew = true, IsPromotion = false, AddedDate = DateTime.UtcNow.AddDays(-20)
        },
        new()
        {
            Id = 8, Name = "ASUS ROG Zephyrus G16 AMD Ryzen AI 9 32 Go / 2 To RTX 4080", Brand = "ASUS",
            Category = "Informatique", SubCategory = "PC Portables Gaming",
            Price = 2499m, OriginalPrice = 2799m, Stock = 6,
            Rating = 4.8, ReviewCount = 523,
            Description = "PC portable gaming premium avec AMD Ryzen AI 9 HX 370, 32 Go DDR5, SSD 2 To, NVIDIA RTX 4080 12 Go et écran ROG Nebula 16\" QHD+ 240 Hz.",
            Ean = "4711387479254", IsNew = true, IsPromotion = true, AddedDate = DateTime.UtcNow.AddDays(-15)
        },
        // ── Tablets ───────────────────────────────────────────────────────────
        new()
        {
            Id = 9, Name = "Apple iPad Pro 13\" M4 256 Go Wi-Fi Argent", Brand = "Apple",
            Category = "Tablettes", SubCategory = "Tablettes Apple",
            Price = 1299m, Stock = 22,
            Rating = 4.9, ReviewCount = 987,
            Description = "iPad Pro avec puce M4, écran Ultra Retina XDR OLED 13 pouces, design ultra-fin de 5,1 mm et Apple Pencil Pro compatible.",
            Ean = "0195949128667", IsNew = true, IsPromotion = false, AddedDate = DateTime.UtcNow.AddDays(-8)
        },
        new()
        {
            Id = 10, Name = "Samsung Galaxy Tab S9 FE 256 Go Wi-Fi Graphite", Brand = "Samsung",
            Category = "Tablettes", SubCategory = "Tablettes Android",
            Price = 449m, OriginalPrice = 549m, Stock = 34,
            Rating = 4.4, ReviewCount = 1234,
            Description = "Tablette Samsung avec écran TFT LCD 10,9\" FHD+, processeur Exynos 1380, 8 Go RAM, batterie 10 090 mAh et résistance IP68.",
            Ean = "8806095102931", IsNew = false, IsPromotion = true, AddedDate = DateTime.UtcNow.AddDays(-60)
        },
        // ── Headphones ────────────────────────────────────────────────────────
        new()
        {
            Id = 11, Name = "Sony WH-1000XM5 Casque Bluetooth ANC Noir", Brand = "Sony",
            Category = "Audio", SubCategory = "Casques",
            Price = 299m, OriginalPrice = 379m, Stock = 88,
            Rating = 4.8, ReviewCount = 8765,
            Description = "Le WH-1000XM5 est le casque à réduction de bruit leader du secteur. Avec 30 heures d'autonomie, 8 micros et la technologie DSEE Extreme, il offre une qualité sonore exceptionnelle.",
            Ean = "4548736132580", IsNew = false, IsPromotion = true, AddedDate = DateTime.UtcNow.AddDays(-180)
        },
        new()
        {
            Id = 12, Name = "Apple AirPods Pro (2e génération) avec boîtier MagSafe", Brand = "Apple",
            Category = "Audio", SubCategory = "Écouteurs",
            Price = 249m, Stock = 156,
            Rating = 4.7, ReviewCount = 12456,
            Description = "AirPods Pro 2 avec réduction de bruit active, son spatial personnalisé, puce H2 et autonomie de 6 heures (30 heures avec le boîtier).",
            Ean = "0194253553915", IsNew = false, IsPromotion = false, AddedDate = DateTime.UtcNow.AddDays(-200)
        },
        new()
        {
            Id = 13, Name = "Bose QuietComfort 45 Casque Bluetooth ANC Blanc", Brand = "Bose",
            Category = "Audio", SubCategory = "Casques",
            Price = 279m, OriginalPrice = 329m, Stock = 45,
            Rating = 4.6, ReviewCount = 3421,
            Description = "Le Bose QC45 combine réduction de bruit légendaire, confort exceptionnel et 24 heures d'autonomie pour une expérience d'écoute immersive.",
            Ean = "017817833011", IsNew = false, IsPromotion = true, AddedDate = DateTime.UtcNow.AddDays(-150)
        },
        new()
        {
            Id = 14, Name = "JBL Charge 5 Enceinte Bluetooth Étanche Rouge", Brand = "JBL",
            Category = "Audio", SubCategory = "Enceintes",
            Price = 159m, OriginalPrice = 199m, Stock = 72,
            Rating = 4.6, ReviewCount = 5678,
            Description = "Enceinte portative avec son puissant, étanchéité IP67, PowerBank intégré et autonomie de 20 heures.",
            Ean = "6925281990571", IsNew = false, IsPromotion = true, AddedDate = DateTime.UtcNow.AddDays(-100)
        },
        // ── Smart TVs ─────────────────────────────────────────────────────────
        new()
        {
            Id = 15, Name = "Samsung Neo QLED 65\" QN95D 4K Smart TV 2024", Brand = "Samsung",
            Category = "TV & Vidéo", SubCategory = "Téléviseurs",
            Price = 1799m, OriginalPrice = 2299m, Stock = 7,
            Rating = 4.7, ReviewCount = 876,
            Description = "TV Neo QLED 65\" avec processeur Neural Quantum 4K Gen2, HDR 2000 nits, Dolby Atmos et Gaming Hub pour une expérience ultime.",
            Ean = "8806094975901", IsNew = true, IsPromotion = true, AddedDate = DateTime.UtcNow.AddDays(-25)
        },
        new()
        {
            Id = 16, Name = "LG OLED evo G4 55\" 4K Smart TV", Brand = "LG",
            Category = "TV & Vidéo", SubCategory = "Téléviseurs",
            Price = 1599m, Stock = 9,
            Rating = 4.8, ReviewCount = 654,
            Description = "TV OLED evo G4 55\" avec processeur α11 AI 4K, MLA (Micro Lens Array), HDR10/Dolby Vision IQ et 4 ports HDMI 2.1.",
            Ean = "8806087636528", IsNew = true, IsPromotion = false, AddedDate = DateTime.UtcNow.AddDays(-30)
        },
        // ── Cameras ───────────────────────────────────────────────────────────
        new()
        {
            Id = 17, Name = "Sony Alpha A7 IV Boîtier Nu Hybride 33 Mpx", Brand = "Sony",
            Category = "Photo & Vidéo", SubCategory = "Appareils Photo Hybrides",
            Price = 2699m, Stock = 11,
            Rating = 4.9, ReviewCount = 1234,
            Description = "Appareil photo hybride plein format 33 Mpx avec système AF intelligent, vidéo 4K 60p et double slot SD.",
            Ean = "4548736127852", IsNew = false, IsPromotion = false, AddedDate = DateTime.UtcNow.AddDays(-200)
        },
        new()
        {
            Id = 18, Name = "Canon EOS R6 Mark II Boîtier Nu", Brand = "Canon",
            Category = "Photo & Vidéo", SubCategory = "Appareils Photo Hybrides",
            Price = 2499m, OriginalPrice = 2799m, Stock = 8,
            Rating = 4.8, ReviewCount = 876,
            Description = "Hybride Canon 24,2 Mpx avec AF Dual Pixel CMOS II, rafale 40 i/s et vidéo 4K HDR oversampled.",
            Ean = "4549292205367", IsNew = false, IsPromotion = true, AddedDate = DateTime.UtcNow.AddDays(-80)
        },
        // ── Gaming ────────────────────────────────────────────────────────────
        new()
        {
            Id = 19, Name = "Sony PlayStation 5 Console Slim Disc", Brand = "Sony",
            Category = "Gaming", SubCategory = "Consoles",
            Price = 449m, Stock = 23,
            Rating = 4.8, ReviewCount = 15678,
            Description = "Console PS5 Slim avec lecteur Blu-ray Ultra HD, SSD NVMe 1 To, DualSense, résolution 8K et Ray Tracing.",
            Ean = "711719577980", IsNew = false, IsPromotion = false, AddedDate = DateTime.UtcNow.AddDays(-90)
        },
        new()
        {
            Id = 20, Name = "Microsoft Xbox Series X 1 To", Brand = "Microsoft",
            Category = "Gaming", SubCategory = "Consoles",
            Price = 499m, Stock = 18,
            Rating = 4.7, ReviewCount = 8934,
            Description = "Console Xbox Series X avec SSD NVMe 1 To, résolution 8K, taux de rafraîchissement jusqu'à 120 fps et rétrocompatibilité totale.",
            Ean = "0889842640816", IsNew = false, IsPromotion = false, AddedDate = DateTime.UtcNow.AddDays(-120)
        },
        new()
        {
            Id = 21, Name = "Nintendo Switch OLED Blanc", Brand = "Nintendo",
            Category = "Gaming", SubCategory = "Consoles",
            Price = 319m, Stock = 45,
            Rating = 4.8, ReviewCount = 22345,
            Description = "Console hybride avec écran OLED 7 pouces, station TV améliorée, 64 Go de stockage interne et mode portable 4,5-9 heures d'autonomie.",
            Ean = "0045496453442", IsNew = false, IsPromotion = false, AddedDate = DateTime.UtcNow.AddDays(-300)
        },
        new()
        {
            Id = 22, Name = "Manette DualSense PS5 Blanc", Brand = "Sony",
            Category = "Gaming", SubCategory = "Accessoires",
            Price = 69m, OriginalPrice = 79m, Stock = 123,
            Rating = 4.7, ReviewCount = 34567,
            Description = "Manette DualSense avec retour haptique, gâchettes adaptatives, micro intégré et batterie rechargeable.",
            Ean = "711719827528", IsNew = false, IsPromotion = true, AddedDate = DateTime.UtcNow.AddDays(-200)
        },
        new()
        {
            Id = 23, Name = "The Legend of Zelda: Tears of the Kingdom Nintendo Switch", Brand = "Nintendo",
            Category = "Gaming", SubCategory = "Jeux Vidéo",
            Price = 49m, OriginalPrice = 69m, Stock = 234,
            Rating = 4.9, ReviewCount = 45678,
            Description = "Suite épique de Breath of the Wild. Explorez le royaume d'Hyrule et les îles célestes dans cette aventure ouverte extraordinaire.",
            Ean = "0045496478384", IsNew = false, IsPromotion = true, AddedDate = DateTime.UtcNow.AddDays(-400)
        },
        // ── Books ─────────────────────────────────────────────────────────────
        new()
        {
            Id = 24, Name = "Le Problème à Trois Corps - Liu Cixin", Brand = "Actes Sud",
            Category = "Livres", SubCategory = "Science-Fiction",
            Price = 9.90m, OriginalPrice = 11.50m, Stock = 456,
            Rating = 4.7, ReviewCount = 23456,
            Description = "Premier tome de la trilogie des Trois Corps. Une œuvre de science-fiction chinoise monumentale, Prix Hugo du Meilleur Roman 2015.",
            Ean = "9782330131067", IsNew = false, IsPromotion = true, AddedDate = DateTime.UtcNow.AddDays(-500)
        },
        new()
        {
            Id = 25, Name = "Clean Code: A Handbook of Agile Software Craftsmanship - Robert C. Martin", Brand = "Pearson",
            Category = "Livres", SubCategory = "Informatique",
            Price = 42m, Stock = 87,
            Rating = 4.6, ReviewCount = 12345,
            Description = "La référence absolue pour écrire du code propre et maintenable. Indispensable pour tout développeur professionnel.",
            Ean = "9780132350884", IsNew = false, IsPromotion = false, AddedDate = DateTime.UtcNow.AddDays(-700)
        },
        new()
        {
            Id = 26, Name = "Atomic Habits - James Clear (Édition Française)", Brand = "Flammarion",
            Category = "Livres", SubCategory = "Développement Personnel",
            Price = 19.90m, Stock = 234,
            Rating = 4.8, ReviewCount = 34567,
            Description = "Transformez vos habitudes et obtenez des résultats extraordinaires. La méthode révolutionnaire pour progresser 1% chaque jour.",
            Ean = "9782080274960", IsNew = false, IsPromotion = false, AddedDate = DateTime.UtcNow.AddDays(-365)
        },
        // ── Music (CDs) ───────────────────────────────────────────────────────
        new()
        {
            Id = 27, Name = "Daft Punk - Random Access Memories (Drumless Edition) CD", Brand = "Columbia",
            Category = "Musique", SubCategory = "CD",
            Price = 14.99m, Stock = 123,
            Rating = 4.9, ReviewCount = 8765,
            Description = "L'édition Drumless de l'album légendaire de Daft Punk, revisité pour révéler toutes ses harmonies.",
            Ean = "0194398821429", IsNew = false, IsPromotion = false, AddedDate = DateTime.UtcNow.AddDays(-200)
        },
        new()
        {
            Id = 28, Name = "Stromae - Multitude CD", Brand = "Mosaert",
            Category = "Musique", SubCategory = "CD",
            Price = 16.99m, OriginalPrice = 19.99m, Stock = 89,
            Rating = 4.7, ReviewCount = 5678,
            Description = "Troisième album studio de Stromae, une œuvre intime et universelle qui parle de vulnérabilité et de la condition humaine.",
            Ean = "5054197239205", IsNew = false, IsPromotion = true, AddedDate = DateTime.UtcNow.AddDays(-300)
        },
        // ── Movies ────────────────────────────────────────────────────────────
        new()
        {
            Id = 29, Name = "Oppenheimer 4K Blu-Ray + Blu-Ray", Brand = "Universal",
            Category = "Films & Séries", SubCategory = "Blu-Ray 4K",
            Price = 24.99m, OriginalPrice = 34.99m, Stock = 167,
            Rating = 4.8, ReviewCount = 12345,
            Description = "Le chef-d'œuvre de Christopher Nolan en 4K Ultra HD avec son Dolby Atmos pour une expérience cinéma chez soi.",
            Ean = "5053083268190", IsNew = false, IsPromotion = true, AddedDate = DateTime.UtcNow.AddDays(-180)
        },
        new()
        {
            Id = 30, Name = "Dune - Deuxième Partie Blu-Ray", Brand = "Warner Bros",
            Category = "Films & Séries", SubCategory = "Blu-Ray",
            Price = 19.99m, Stock = 234,
            Rating = 4.7, ReviewCount = 8765,
            Description = "La conclusion épique de l'adaptation de Denis Villeneuve. Paul Atreides mène les Fremen dans leur guerre sainte contre l'Empire.",
            Ean = "5051895485507", IsNew = false, IsPromotion = false, AddedDate = DateTime.UtcNow.AddDays(-100)
        },
        // ── Smart Home ────────────────────────────────────────────────────────
        new()
        {
            Id = 31, Name = "Amazon Echo Dot (5e génération) Anthracite", Brand = "Amazon",
            Category = "Maison Connectée", SubCategory = "Assistant Vocal",
            Price = 54.99m, OriginalPrice = 64.99m, Stock = 345,
            Rating = 4.4, ReviewCount = 45678,
            Description = "Enceinte connectée avec Alexa, son amélioré, capteur de température intégré et bague lumineuse multicolore.",
            Ean = "0840080595464", IsNew = false, IsPromotion = true, AddedDate = DateTime.UtcNow.AddDays(-250)
        },
        new()
        {
            Id = 32, Name = "Philips Hue Starter Kit E27 White & Color Ambiance", Brand = "Philips",
            Category = "Maison Connectée", SubCategory = "Éclairage",
            Price = 149m, OriginalPrice = 179m, Stock = 78,
            Rating = 4.5, ReviewCount = 23456,
            Description = "Kit démarrage avec bridge Hue et 3 ampoules couleur E27 pour créer l'ambiance parfaite dans votre salon.",
            Ean = "8718696728987", IsNew = false, IsPromotion = true, AddedDate = DateTime.UtcNow.AddDays(-300)
        },
        // ── Wearables ─────────────────────────────────────────────────────────
        new()
        {
            Id = 33, Name = "Apple Watch Series 10 46mm GPS Aluminium Noir Minuit", Brand = "Apple",
            Category = "Montres Connectées", SubCategory = "Apple Watch",
            Price = 449m, Stock = 56,
            Rating = 4.7, ReviewCount = 7890,
            Description = "Apple Watch la plus fine et la plus légère. Détection de l'apnée du sommeil, ECG, SpO2 et jusqu'à 18 heures d'autonomie.",
            Ean = "0195949441028", IsNew = true, IsPromotion = false, AddedDate = DateTime.UtcNow.AddDays(-12)
        },
        new()
        {
            Id = 34, Name = "Samsung Galaxy Watch 7 44mm LTE Silver", Brand = "Samsung",
            Category = "Montres Connectées", SubCategory = "Smartwatches",
            Price = 329m, OriginalPrice = 399m, Stock = 43,
            Rating = 4.5, ReviewCount = 3456,
            Description = "Galaxy Watch 7 avec puce Exynos W1000, analyse avancée du sommeil, BioActive Sensor et autonomie de 40 heures.",
            Ean = "8806095449555", IsNew = true, IsPromotion = true, AddedDate = DateTime.UtcNow.AddDays(-30)
        },
        // ── Accessories & Peripherals ─────────────────────────────────────────
        new()
        {
            Id = 35, Name = "Logitech MX Master 3S Souris Sans Fil Graphite", Brand = "Logitech",
            Category = "Informatique", SubCategory = "Souris",
            Price = 99m, OriginalPrice = 119m, Stock = 234,
            Rating = 4.8, ReviewCount = 34567,
            Description = "Souris haut de gamme avec capteur 8000 DPI, molette MagSpeed électromagnétique, ergonomie avancée et 70 jours d'autonomie.",
            Ean = "097855176189", IsNew = false, IsPromotion = true, AddedDate = DateTime.UtcNow.AddDays(-180)
        },
        new()
        {
            Id = 36, Name = "Samsung T7 Shield SSD Externe 1 To Beige", Brand = "Samsung",
            Category = "Informatique", SubCategory = "Stockage",
            Price = 89m, OriginalPrice = 109m, Stock = 189,
            Rating = 4.7, ReviewCount = 12345,
            Description = "SSD externe ultra-rapide (1050 Mo/s) résistant aux chocs, à la poussière et à l'eau (IP65) avec chiffrement AES 256 bits.",
            Ean = "8806094374490", IsNew = false, IsPromotion = true, AddedDate = DateTime.UtcNow.AddDays(-90)
        },
        new()
        {
            Id = 37, Name = "Apple MagSafe Chargeur Rapide 15W Blanc", Brand = "Apple",
            Category = "Accessoires", SubCategory = "Chargeurs",
            Price = 45m, Stock = 567,
            Rating = 4.5, ReviewCount = 23456,
            Description = "Chargeur MagSafe pour iPhone avec puissance de charge optimale de 15W grâce à l'alignement magnétique parfait.",
            Ean = "0194252049785", IsNew = false, IsPromotion = false, AddedDate = DateTime.UtcNow.AddDays(-400)
        },
        new()
        {
            Id = 38, Name = "Anker 737 Power Bank 140W 24000mAh", Brand = "Anker",
            Category = "Accessoires", SubCategory = "Batteries",
            Price = 129m, Stock = 78,
            Rating = 4.7, ReviewCount = 5678,
            Description = "Batterie externe haute capacité 24000 mAh avec charge rapide 140W, 3 ports de charge simultanés et affichage LED intelligent.",
            Ean = "0194644126781", IsNew = false, IsPromotion = false, AddedDate = DateTime.UtcNow.AddDays(-150)
        },
        // ── More Tech ─────────────────────────────────────────────────────────
        new()
        {
            Id = 39, Name = "DJI Mini 4 Pro Drone Seul", Brand = "DJI",
            Category = "Photo & Vidéo", SubCategory = "Drones",
            Price = 759m, Stock = 23,
            Rating = 4.8, ReviewCount = 3456,
            Description = "Drone compact moins de 249g, vidéo 4K/60fps HDR, omnidirectional obstacle sensing et autonomie 34 minutes.",
            Ean = "6941565967153", IsNew = true, IsPromotion = false, AddedDate = DateTime.UtcNow.AddDays(-60)
        },
        new()
        {
            Id = 40, Name = "GoPro HERO12 Black", Brand = "GoPro",
            Category = "Photo & Vidéo", SubCategory = "Caméras Action",
            Price = 299m, OriginalPrice = 399m, Stock = 56,
            Rating = 4.6, ReviewCount = 8765,
            Description = "Caméra d'action avec stabilisation HyperSmooth 6.0, vidéo 5.3K 60fps, étanche à 10m et batterie améliorée 2 heures.",
            Ean = "0818279028830", IsNew = false, IsPromotion = true, AddedDate = DateTime.UtcNow.AddDays(-120)
        },
        // ── Office & Peripherals ──────────────────────────────────────────────
        new()
        {
            Id = 41, Name = "Brother MFC-L3760CDW Imprimante Laser Couleur", Brand = "Brother",
            Category = "Informatique", SubCategory = "Imprimantes",
            Price = 349m, OriginalPrice = 429m, Stock = 12,
            Rating = 4.3, ReviewCount = 1234,
            Description = "Multifonctions laser couleur WiFi avec impression recto-verso auto, scanner ADF 50 feuilles et Ethernet.",
            Ean = "4977766819084", IsNew = false, IsPromotion = true, AddedDate = DateTime.UtcNow.AddDays(-90)
        },
        new()
        {
            Id = 42, Name = "LG UltraWide 34\" Curved Monitor QHD 100Hz", Brand = "LG",
            Category = "Informatique", SubCategory = "Écrans",
            Price = 449m, OriginalPrice = 599m, Stock = 17,
            Rating = 4.6, ReviewCount = 4567,
            Description = "Écran incurvé 34\" IPS QHD+ (3440x1440) avec HDR10, AMD FreeSync, USB-C 60W et 5ms de temps de réponse.",
            Ean = "8806091987990", IsNew = false, IsPromotion = true, AddedDate = DateTime.UtcNow.AddDays(-60)
        },
        // ── More Gaming ───────────────────────────────────────────────────────
        new()
        {
            Id = 43, Name = "Hogwarts Legacy PS5", Brand = "Warner Bros Interactive",
            Category = "Gaming", SubCategory = "Jeux Vidéo",
            Price = 39.99m, OriginalPrice = 79.99m, Stock = 345,
            Rating = 4.7, ReviewCount = 56789,
            Description = "Explorez l'univers magique de Poudlard dans les années 1800. Un RPG action-aventure ouvert magique.",
            Ean = "5051892226684", IsNew = false, IsPromotion = true, AddedDate = DateTime.UtcNow.AddDays(-400)
        },
        new()
        {
            Id = 44, Name = "Razer BlackShark V2 Pro Casque Gaming Sans Fil", Brand = "Razer",
            Category = "Gaming", SubCategory = "Accessoires",
            Price = 149m, OriginalPrice = 199m, Stock = 67,
            Rating = 4.5, ReviewCount = 7890,
            Description = "Casque gaming sans fil avec drivers TriForce Titanium 50mm, SmartSwitch et 70 heures d'autonomie.",
            Ean = "8886419378327", IsNew = false, IsPromotion = true, AddedDate = DateTime.UtcNow.AddDays(-150)
        },
        new()
        {
            Id = 45, Name = "Corsair K100 RGB Clavier Mécanique Cherry MX Speed", Brand = "Corsair",
            Category = "Gaming", SubCategory = "Accessoires",
            Price = 219m, OriginalPrice = 259m, Stock = 34,
            Rating = 4.7, ReviewCount = 5678,
            Description = "Clavier mécanique haut de gamme avec switches Cherry MX Speed, OPX Axle, molette iCUE et rétroéclairage RGB par touche.",
            Ean = "0840006616719", IsNew = false, IsPromotion = true, AddedDate = DateTime.UtcNow.AddDays(-200)
        },
        // ── More Books ────────────────────────────────────────────────────────
        new()
        {
            Id = 46, Name = "The Pragmatic Programmer - Andrew Hunt & David Thomas", Brand = "Addison-Wesley",
            Category = "Livres", SubCategory = "Informatique",
            Price = 45m, Stock = 123,
            Rating = 4.8, ReviewCount = 23456,
            Description = "Le guide indispensable du développeur pragmatique. De la théorie à la pratique pour créer des logiciels de qualité.",
            Ean = "9780135957059", IsNew = false, IsPromotion = false, AddedDate = DateTime.UtcNow.AddDays(-600)
        },
        new()
        {
            Id = 47, Name = "Fondation - Isaac Asimov (Intégrale)", Brand = "J'ai Lu",
            Category = "Livres", SubCategory = "Science-Fiction",
            Price = 34.90m, Stock = 78,
            Rating = 4.8, ReviewCount = 12345,
            Description = "L'intégrale du chef-d'œuvre d'Asimov. La saga de l'Empire Galactique et de la Fondation qui œuvre pour préserver la civilisation.",
            Ean = "9782290388341", IsNew = false, IsPromotion = false, AddedDate = DateTime.UtcNow.AddDays(-800)
        },
        new()
        {
            Id = 48, Name = "Microsoft Surface Pro 11 Snapdragon X Elite 16 Go / 512 Go", Brand = "Microsoft",
            Category = "Tablettes", SubCategory = "Tablettes Windows",
            Price = 1499m, Stock = 9,
            Rating = 4.5, ReviewCount = 567,
            Description = "Tablette-PC avec Snapdragon X Elite, 16 Go RAM, SSD 512 Go, écran PixelSense Flow 13\" 120Hz et autonomie 14 heures.",
            Ean = "0889842986716", IsNew = true, IsPromotion = false, AddedDate = DateTime.UtcNow.AddDays(-20)
        },
        new()
        {
            Id = 49, Name = "Dyson V15 Detect Absolute Aspirateur Sans Fil", Brand = "Dyson",
            Category = "Électroménager", SubCategory = "Aspirateurs",
            Price = 699m, OriginalPrice = 799m, Stock = 23,
            Rating = 4.6, ReviewCount = 8765,
            Description = "Aspirateur laser Dyson avec détection laser Fluffy, HEPA filtration, 60 min d'autonomie et écran LCD de performance.",
            Ean = "5025155069622", IsNew = false, IsPromotion = true, AddedDate = DateTime.UtcNow.AddDays(-180)
        },
        new()
        {
            Id = 50, Name = "Nespresso Vertuo Next Premium Titane + Aeroccino", Brand = "Nespresso",
            Category = "Électroménager", SubCategory = "Machines à Café",
            Price = 169m, OriginalPrice = 229m, Stock = 56,
            Rating = 4.5, ReviewCount = 12345,
            Description = "Machine Nespresso Vertuo avec technologie Centrifusion, reconnaissance de capsule par code-barres et mousseur lait Aeroccino 3.",
            Ean = "7630047641157", IsNew = false, IsPromotion = true, AddedDate = DateTime.UtcNow.AddDays(-250)
        }
    ];

    // ── Random product generator ───────────────────────────────────────────────
    private static readonly string[] _randomBrands = ["Sony", "Samsung", "Apple", "LG", "Logitech", "Razer", "ASUS", "Dell", "HP", "Lenovo", "Canon", "Nikon", "JBL", "Bose", "Philips", "Dyson", "Nintendo", "Microsoft", "Amazon", "Google"];
    private static readonly string[] _randomCategories = ["Téléphonie", "Informatique", "Audio", "Gaming", "TV & Vidéo", "Photo & Vidéo", "Maison Connectée", "Accessoires"];
    private static readonly string[] _randomAdjectives = ["Pro", "Elite", "Ultra", "Max", "Plus", "Premium", "Advanced", "Smart", "Slim", "Nano"];
    private static readonly string[] _randomColors = ["Noir", "Blanc", "Argent", "Bleu", "Rouge", "Gris", "Or", "Vert"];

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
            Description = $"Produit {brand} de haute qualité dans la catégorie {category}. Design moderne et performances exceptionnelles.",
            Ean = string.Concat(Enumerable.Range(0, 13).Select(_ => rng.Next(0, 10).ToString())), // Mock EAN — not a valid EAN-13 (no check digit)
            IsNew = rng.NextDouble() > 0.8,
            IsPromotion = hasPromo,
            AddedDate = DateTime.UtcNow.AddDays(-rng.Next(0, 730))
        };
    }
}
