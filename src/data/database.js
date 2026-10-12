/**
 * ============================================================================
 * DATA ACCESS & PERSISTENCE LAYER (SINGLETON PATTERN)
 * LocalStorage senkronizasyonu, Mock Veri Başlatıcı ve Repository Yönetimi
 * ============================================================================
 */

import { Student, Academician, Librarian, Book, EBook, Journal, Thesis, ShelfLocation, BorrowTransaction, FineRecord, StudyDeskBooking } from '../core/models.js';

export class DatabaseContext {
  static instance = null;

  constructor() {
    if (DatabaseContext.instance) {
      return DatabaseContext.instance;
    }
    
    this.STORAGE_KEY = "KUTUPHANE_DB_V1";
    this.users = [];
    this.materials = [];
    this.transactions = [];
    this.reservations = [];
    this.fines = [];
    this.deskBookings = [];
    this.currentUser = null;
    
    this.loadFromStorage();
    DatabaseContext.instance = this;
  }

  static getInstance() {
    if (!DatabaseContext.instance) {
      DatabaseContext.instance = new DatabaseContext();
    }
    return DatabaseContext.instance;
  }

  loadFromStorage() {
    const raw = localStorage.getItem(this.STORAGE_KEY);
    if (raw) {
      try {
        const parsed = JSON.parse(raw);
        this.deserialize(parsed);
        return;
      } catch (err) {
        console.warn("Mevcut veri çözümlenemedi, varsayılan veri seti yükleniyor...", err);
      }
    }
    this.seedInitialData();
    this.saveToStorage();
  }

  saveToStorage() {
    const dataToSave = {
      users: this.users,
      materials: this.materials,
      transactions: this.transactions,
      reservations: this.reservations,
      fines: this.fines,
      deskBookings: this.deskBookings,
      currentUserId: this.currentUser ? this.currentUser.id : 1
    };
    localStorage.setItem(this.STORAGE_KEY, JSON.stringify(dataToSave));
  }

  deserialize(data) {
    // 1. Kullanıcıları polimorfik olarak ayağa kaldır
    this.users = (data.users || []).map(u => {
      let userObj;
      if (u.role === "Ogrenci") {
        userObj = new Student(u.id, u.tcNo, u.name, u.email, u.phone, u.studentNo, u.department, u.year, u.degreeLevel);
      } else if (u.role === "Akademisyen") {
        userObj = new Academician(u.id, u.tcNo, u.name, u.email, u.phone, u.title, u.faculty, u.roomNo);
      } else {
        userObj = new Librarian(u.id, u.tcNo, u.name, u.email, u.phone, u.employeeId, u.shift);
      }
      userObj.walletBalance = u.walletBalance || 0;
      userObj.fineDebt = u.fineDebt || 0;
      userObj.totalBorrowedCount = u.totalBorrowedCount || 0;
      userObj.barcode = u.barcode;
      return userObj;
    });

    // 2. Materyalleri polimorfik olarak ayağa kaldır
    this.materials = (data.materials || []).map(m => {
      let mat;
      if (m.pageCount) {
        const shelf = m.shelfLocation ? new ShelfLocation(m.shelfLocation.building, m.shelfLocation.floor, m.shelfLocation.aisle, m.shelfLocation.shelfNo) : new ShelfLocation();
        mat = new Book(m.id, m.isbn, m.title, m.author, m.publisher, m.publicationYear, m.category, m.totalStock, m.pageCount, m.coverType, shelf, m.summary);
      } else if (m.fileSizeMB) {
        mat = new EBook(m.id, m.isbn, m.title, m.author, m.publisher, m.publicationYear, m.category, m.fileSizeMB, m.format, m.contentUrl, m.summary);
      } else if (m.issn) {
        mat = new Journal(m.id, m.issn, m.title, m.publisher, m.publicationYear, m.category, m.issueNumber, m.frequency, m.totalStock, m.summary);
      } else {
        mat = new Thesis(m.id, m.title, m.author, m.advisor, m.university, m.department, m.degreeLevel, m.publicationYear, m.summary);
      }
      mat.availableStock = m.availableStock !== undefined ? m.availableStock : m.totalStock;
      mat.timesBorrowed = m.timesBorrowed || 0;
      return mat;
    });

    // 3. İşlemleri ayağa kaldır
    this.transactions = (data.transactions || []).map(t => {
      const trans = new BorrowTransaction(t.id, t.userId, t.materialId, new Date(t.borrowDate), 15);
      trans.dueDate = new Date(t.dueDate);
      trans.returnDate = t.returnDate ? new Date(t.returnDate) : null;
      trans.status = t.status;
      trans.renewalCount = t.renewalCount || 0;
      return trans;
    });

    this.reservations = data.reservations || [];
    this.fines = data.fines || [];
    this.deskBookings = data.deskBookings || [];

    const activeId = data.currentUserId || 1;
    this.currentUser = this.users.find(u => u.id === activeId) || this.users[0];
  }

  seedInitialData() {
    // Zengin Kullanıcı Listesi
    const u1 = new Student(1, "12345678901", "Ahmet Yılmaz", "ahmet.yilmaz@ogr.edu.tr", "0532 111 2233", "2024105021", "Bilgisayar Mühendisliği", 3, "Lisans");
    u1.walletBalance = 65.0;
    
    const u2 = new Student(2, "98765432109", "Zeynep Kaya", "zeynep.kaya@ogr.edu.tr", "0544 222 3344", "2023108044", "Yazılım Mühendisliği", 4, "Yüksek Lisans");
    u2.walletBalance = 120.0;
    
    const u3 = new Student(3, "45678912305", "Canan Demir", "canan.demir@ogr.edu.tr", "0555 333 4455", "2022109012", "Yapay Zeka Mühendisliği", 2, "Doktora");
    u3.walletBalance = 40.0;
    
    const u4 = new Academician(4, "11223344556", "Doç. Dr. Mehmet Demir", "mehmet.demir@edu.tr", "0505 444 5566", "Doç. Dr.", "Mühendislik Fakültesi", "B-204");
    u4.walletBalance = 250.0;

    const u5 = new Librarian(5, "99887766554", "Ayşe Yurt (Görevli)", "ayse.yurt@lib.edu.tr", "0530 999 8877", "LIB-8041", "Gündüz Vardiyası");

    this.users = [u1, u2, u3, u4, u5];
    this.currentUser = u1;

    // Zengin Üniversite Kütüphanesi Materyal Kataloğu (Yazılım Tasarım Mimarisi, Sistemler, vb.)
    this.materials = [
      new Book(101, "978-0132350884", "Clean Code (Temiz Kod)", "Robert C. Martin", "Prentice Hall", 2008, "Yazılım Mühendisliği", 4, 464, "Karton Kapak", new ShelfLocation("Merkez Kütüphane", 2, "C-4", "12"), "Çevik yazılım ustalığının ve temiz kodlama prensiplerinin rehberi."),
      new Book(102, "978-0201633610", "Design Patterns (Tasarım Desenleri)", "Erich Gamma, Richard Helm, Ralph Johnson, John Vlissides", "Addison-Wesley", 1994, "Yazılım Mimarisi", 3, 395, "Ciltli", new ShelfLocation("Merkez Kütüphane", 2, "C-4", "14"), "Nesne yönelimli yazılım tasarımının efsanevi GoF desenleri başucu kitabı."),
      new Book(103, "978-0134757599", "Refactoring: Improving the Design of Existing Code", "Martin Fowler", "Addison-Wesley", 2018, "Yazılım Mimarisi", 2, 448, "Karton Kapak", new ShelfLocation("Merkez Kütüphane", 2, "C-5", "03"), "Mevcut kodun tasarımını ve mimarisini adım adım iyileştirme sanatı."),
      new Book(104, "978-0262033848", "Introduction to Algorithms (CLRS)", "Thomas H. Cormen, Charles E. Leiserson, Ronald L. Rivest, Clifford Stein", "MIT Press", 2009, "Algoritmalar", 5, 1312, "Ciltli", new ShelfLocation("Merkez Kütüphane", 1, "A-2", "01"), "Dünya çapında üniversitelerin algoritma standardı olan kapsamlı referans kitap."),
      new Book(105, "978-0133591620", "Modern Operating Systems", "Andrew S. Tanenbaum, Herbert Bos", "Pearson", 2014, "İşletim Sistemleri", 3, 1136, "Ciltli", new ShelfLocation("Merkez Kütüphane", 1, "B-1", "08"), "Modern işletim sistemlerinin mimarisi, çekirdek yapıları ve süreç yönetimi."),
      new Book(106, "978-0136042594", "Artificial Intelligence: A Modern Approach", "Stuart Russell, Peter Norvig", "Pearson", 2020, "Yapay Zeka", 3, 1152, "Ciltli", new ShelfLocation("Merkez Kütüphane", 3, "D-1", "15"), "Yapay zeka teorisi ve modern ajan mimarilerinin ana kaynağı."),
      new Book(107, "978-0078022159", "Database System Concepts", "Abraham Silberschatz, Henry F. Korth", "McGraw-Hill", 2019, "Veritabanı Sistemleri", 4, 1376, "Karton Kapak", new ShelfLocation("Merkez Kütüphane", 1, "B-3", "04"), "İlişkisel veritabanları, SQL mimarisi, indeksleme ve sorgu optimizasyonu."),
      new Book(108, "978-0132143011", "Computer Networks", "Andrew S. Tanenbaum, David J. Wetherall", "Pearson", 2011, "Ağ Mimarisi", 3, 960, "Karton Kapak", new ShelfLocation("Merkez Kütüphane", 1, "A-5", "09"), "OSI katmanları, TCP/IP protokolleri ve ağ güvenliği."),
      
      // E-Kitaplar
      new EBook(201, "978-1491950357", "Building Microservices: Designing Fine-Grained Systems", "Sam Newman", "O'Reilly Media", 2021, "Bulut & Mimari", 14.8, "PDF", "https://example.com/ebooks/microservices.pdf", "Mikroservis sistemleri, servis sınırları, olay tabanlı mimariler."),
      new EBook(202, "978-1492040798", "Designing Data-Intensive Applications", "Martin Kleppmann", "O'Reilly Media", 2017, "Veri Mimarisi", 22.1, "EPUB", "https://example.com/ebooks/ddia.epub", "Güvenilir, ölçeklenebilir ve sürdürülebilir veri sistemlerinin arka planı."),

      // Süreli Yayınlar (Dergiler)
      new Journal(301, "0098-5589", "IEEE Transactions on Software Engineering", "IEEE Computer Society", 2025, "Akademik Dergi", 12, "Aylık", 2, "Yazılım mühendisliği teorisi ve pratikleri üzerine hakemli uluslararası yayın."),
      new Journal(302, "0001-0782", "Communications of the ACM", "Association for Computing Machinery", 2026, "Bilgisayar Bilimleri", 4, "Aylık", 3, "Hesaplama dünyasındaki en son teknolojik atılımlar ve felsefi tartışmalar."),

      // Akademik Tezler
      new Thesis(401, "Dağıtık Mikroservis Mimarilerinde Hata Toleransı ve Dayanıklılık Örüntüleri", "Yük. Müh. Ali Kemal Çelik", "Prof. Dr. Ahmet Turan", "İstanbul Teknik Üniversitesi", "Bilgisayar Mühendisliği", "Doktora", 2025, "Kritik görev dağıtık sistemlerinde Circuit Breaker ve Bulkhead desenlerinin performans analizi.")
    ];

    // Örnek Başlangıç Ödünçleri
    const t1 = new BorrowTransaction(1001, 1, 101, new Date(Date.now() - 5 * 24 * 60 * 60 * 1000), 15);
    const t2 = new BorrowTransaction(1002, 1, 104, new Date(Date.now() - 18 * 24 * 60 * 60 * 1000), 15); // Gecikmiş!
    t2.status = "OVERDUE";

    const t3 = new BorrowTransaction(1003, 2, 102, new Date(Date.now() - 2 * 24 * 60 * 60 * 1000), 21);

    this.transactions = [t1, t2, t3];

    // Stokları düşür
    this.materials.find(m => m.id === 101).decrementStock();
    this.materials.find(m => m.id === 104).decrementStock();
    this.materials.find(m => m.id === 102).decrementStock();

    // Örnek Ceza (t2 işlemi için 3 gün gecikme: 3 * 5 = 15 TL)
    const f1 = new FineRecord(5001, 1002, 1, 15.0, 3);
    this.fines = [f1];
    u1.fineDebt = 15.0;

    // Örnek Masa Rezervasyonları
    this.deskBookings = [
      new StudyDeskBooking(1, 1, "D-104 (Prizli / Sessiz)", "09:00 - 12:00"),
      new StudyDeskBooking(2, 2, "ROOM-B (Grup Çalışma)", "13:00 - 16:00")
    ];
  }

  resetToDefault() {
    localStorage.removeItem(this.STORAGE_KEY);
    this.seedInitialData();
    this.saveToStorage();
    return true;
  }
}
