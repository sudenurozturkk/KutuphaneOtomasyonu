/**
 * ============================================================================
 * CORE DOMAIN ENTITIES & VALUE OBJECTS (DOMAIN LAYER)
 * Yazılım Tasarım Mimarisi: Kalıtım, Polimorfizm, Kapsülleme, Kompozisyon
 * ============================================================================
 */

// --- 1. SHELF LOCATION (KOMPOZİSYON İLE KİTABA BAĞLI) ---
export class ShelfLocation {
  constructor(building = "Merkez Kütüphane", floor = 1, aisle = "A-1", shelfNo = "10") {
    this.building = building;
    this.floor = floor;
    this.aisle = aisle;
    this.shelfNo = shelfNo;
  }

  getFormatted() {
    return `${this.building} | Kat ${this.floor} | Koridor ${this.aisle} | Raf #${this.shelfNo}`;
  }
}

// --- 2. KULLANICI HİYERARŞİSİ (INHERITANCE & POLYMORPHISM) ---
export class User {
  constructor(id, tcNo, name, email, phone, role, barcode = "") {
    if (new.target === User) {
      throw new TypeError("Abstract 'User' sınıfından doğrudan nesne üretilemez!");
    }
    this.id = id;
    this.tcNo = tcNo;
    this.name = name;
    this.email = email;
    this.phone = phone;
    this.role = role;
    this.barcode = barcode || `USR-${id}-${Math.floor(1000 + Math.random() * 9000)}`;
    this.walletBalance = 0.0;
    this.totalBorrowedCount = 0;
    this.fineDebt = 0.0;
    this.isSuspended = false;
  }

  // Polimorfik Metodlar (Alt sınıflar ezecek)
  getMaxBorrowLimit() {
    throw new Error("getMaxBorrowLimit() alt sınıfta uygulanmalı!");
  }

  getBorrowDurationDays() {
    throw new Error("getBorrowDurationDays() alt sınıfta uygulanmalı!");
  }

  getDailyFineRate() {
    throw new Error("getDailyFineRate() alt sınıfta uygulanmalı!");
  }

  canBorrow() {
    return !this.isSuspended && this.fineDebt < 50.0;
  }

  addBalance(amount) {
    if (amount > 0) {
      this.walletBalance += amount;
      return true;
    }
    return false;
  }

  payDebt(amount) {
    if (amount > 0 && this.walletBalance >= amount) {
      this.walletBalance -= amount;
      this.fineDebt = Math.max(0, this.fineDebt - amount);
      return true;
    }
    return false;
  }
}

export class Student extends User {
  constructor(id, tcNo, name, email, phone, studentNo, department, year, degreeLevel = "Lisans") {
    super(id, tcNo, name, email, phone, "Ogrenci");
    this.studentNo = studentNo;
    this.department = department;
    this.year = year;
    this.degreeLevel = degreeLevel; // Ön Lisans, Lisans, Yüksek Lisans, Doktora
  }

  getMaxBorrowLimit() {
    if (this.degreeLevel === "Doktora") return 8;
    if (this.degreeLevel === "Yüksek Lisans") return 6;
    return 4; // Lisans / Ön Lisans
  }

  getBorrowDurationDays() {
    if (this.degreeLevel === "Doktora") return 28;
    if (this.degreeLevel === "Yüksek Lisans") return 21;
    return 15;
  }

  getDailyFineRate() {
    return 5.0; // Öğrenci günlük gecikme cezası: 5 TL
  }
}

export class Academician extends User {
  constructor(id, tcNo, name, email, phone, title, faculty, roomNo) {
    super(id, tcNo, name, email, phone, "Akademisyen");
    this.title = title; // Prof. Dr., Doç. Dr., Dr. Öğr. Üyesi, Araş. Gör.
    this.faculty = faculty;
    this.roomNo = roomNo;
  }

  getMaxBorrowLimit() {
    return 15; // Akademisyenler için yüksek limit
  }

  getBorrowDurationDays() {
    return 45; // 45 gün ödünç hakkı
  }

  getDailyFineRate() {
    return 2.0; // Akademisyen gecikme katsayısı daha düşük
  }
}

export class Librarian extends User {
  constructor(id, tcNo, name, email, phone, employeeId, shift = "Gündüz") {
    super(id, tcNo, name, email, phone, "Kutuphaneci");
    this.employeeId = employeeId;
    this.shift = shift;
  }

  getMaxBorrowLimit() {
    return 10;
  }

  getBorrowDurationDays() {
    return 30;
  }

  getDailyFineRate() {
    return 0.0; // Personel muaf
  }
}

// --- 3. MATERYAL HİYERARŞİSİ (INHERITANCE & POLYMORPHISM) ---
export class Material {
  constructor(id, isbn, title, author, publisher, publicationYear, category, totalStock = 1, summary = "") {
    if (new.target === Material) {
      throw new TypeError("Abstract 'Material' sınıfından doğrudan nesne üretilemez!");
    }
    this.id = id;
    this.isbn = isbn;
    this.title = title;
    this.author = author;
    this.publisher = publisher;
    this.publicationYear = publicationYear;
    this.category = category;
    this.totalStock = totalStock;
    this.availableStock = totalStock;
    this.summary = summary || "Bu materyale ait özet açıklama henüz eklenmemiştir.";
    this.timesBorrowed = 0;
  }

  isAvailable() {
    return this.availableStock > 0;
  }

  decrementStock() {
    if (this.availableStock > 0) {
      this.availableStock--;
      this.timesBorrowed++;
      return true;
    }
    return false;
  }

  incrementStock() {
    if (this.availableStock < this.totalStock) {
      this.availableStock++;
      return true;
    }
    return false;
  }

  getTypeName() {
    return "Materyal";
  }
}

export class Book extends Material {
  constructor(id, isbn, title, author, publisher, publicationYear, category, totalStock, pageCount, coverType = "Karton Kapak", shelfLocation = null, summary = "") {
    super(id, isbn, title, author, publisher, publicationYear, category, totalStock, summary);
    this.pageCount = pageCount;
    this.coverType = coverType;
    this.shelfLocation = shelfLocation || new ShelfLocation();
  }

  getTypeName() {
    return "Basılı Kitap";
  }
}

export class EBook extends Material {
  constructor(id, isbn, title, author, publisher, publicationYear, category, fileSizeMB, format = "PDF", contentUrl = "", summary = "") {
    super(id, isbn, title, author, publisher, publicationYear, category, 9999, summary); // E-Kitapta fiziksel stok sınırı yoktur
    this.fileSizeMB = fileSizeMB;
    this.format = format;
    this.contentUrl = contentUrl || "#";
    this.downloadCount = 0;
  }

  getTypeName() {
    return "E-Kitap (Dijital)";
  }

  isAvailable() {
    return true; // Her zaman erişilebilir
  }
}

export class Journal extends Material {
  constructor(id, issn, title, publisher, publicationYear, category, issueNumber, frequency = "Aylık", totalStock = 3, summary = "") {
    super(id, issn, title, "Kolektif / Editör Heyeti", publisher, publicationYear, category, totalStock, summary);
    this.issn = issn;
    this.issueNumber = issueNumber;
    this.frequency = frequency;
  }

  getTypeName() {
    return "Süreli Yayın / Dergi";
  }
}

export class Thesis extends Material {
  constructor(id, title, author, advisor, university, department, degreeLevel = "Doktora", publicationYear = 2025, summary = "") {
    super(id, `THS-${id}`, title, author, university, publicationYear, "Akademik Tez", 1, summary);
    this.advisor = advisor;
    this.university = university;
    this.department = department;
    this.degreeLevel = degreeLevel;
  }

  getTypeName() {
    return "Akademik Tez";
  }
}

// --- 4. ÖDÜNÇ İŞLEMİ (TRANSACTION) ---
export class BorrowTransaction {
  constructor(id, userId, materialId, borrowDate = new Date(), durationDays = 15) {
    this.id = id;
    this.userId = userId;
    this.materialId = materialId;
    this.borrowDate = new Date(borrowDate);
    
    // Son teslim tarihi hesapla
    const due = new Date(this.borrowDate);
    due.setDate(due.getDate() + durationDays);
    this.dueDate = due;
    
    this.returnDate = null;
    this.status = "ACTIVE"; // ACTIVE, RETURNED, OVERDUE
    this.renewalCount = 0;
    this.maxRenewals = 2;
  }

  isOverdue() {
    if (this.status === "RETURNED") return false;
    return new Date() > this.dueDate;
  }

  getOverdueDays() {
    const compareDate = this.returnDate ? new Date(this.returnDate) : new Date();
    if (compareDate <= this.dueDate) return 0;
    const diffTime = Math.abs(compareDate - this.dueDate);
    return Math.ceil(diffTime / (1000 * 60 * 60 * 24));
  }

  renew(extraDays = 7) {
    if (this.renewalCount >= this.maxRenewals) {
      throw new Error(`Maksimum süre uzatma limitine (${this.maxRenewals} kez) ulaşıldı!`);
    }
    if (this.isOverdue()) {
      throw new Error("Gecikmiş kitapların süresi online uzatılamaz! Lütfen kütüphaneye teslim ediniz.");
    }
    this.dueDate.setDate(this.dueDate.getDate() + extraDays);
    this.renewalCount++;
    return this.dueDate;
  }

  returnBook() {
    this.returnDate = new Date();
    this.status = "RETURNED";
  }
}

// --- 5. REZERVASYON (HOLD) ---
export class Reservation {
  constructor(id, userId, materialId, requestDate = new Date()) {
    this.id = id;
    this.userId = userId;
    this.materialId = materialId;
    this.requestDate = new Date(requestDate);
    this.status = "PENDING"; // PENDING, NOTIFIED, COMPLETED, CANCELLED
    this.notifiedDate = null;
    this.expiryDate = null;
  }

  notifyAvailable(hoursValid = 48) {
    this.status = "NOTIFIED";
    this.notifiedDate = new Date();
    const expiry = new Date();
    expiry.setHours(expiry.getHours() + hoursValid);
    this.expiryDate = expiry;
  }
}

// --- 6. CEZA KAYDI (FINE RECORD) ---
export class FineRecord {
  constructor(id, transactionId, userId, amount, overdueDays) {
    this.id = id;
    this.transactionId = transactionId;
    this.userId = userId;
    this.amount = amount;
    this.overdueDays = overdueDays;
    this.isPaid = false;
    this.createdDate = new Date();
    this.paymentDate = null;
    this.receiptCode = `REC-${id}-${Math.floor(100000 + Math.random() * 900000)}`;
  }

  pay() {
    this.isPaid = true;
    this.paymentDate = new Date();
    return this.receiptCode;
  }
}

// --- 7. ÇALIŞMA ODASI & MASA REZERVASYONU ---
export class StudyDeskBooking {
  constructor(id, userId, deskCode, timeSlot, date = new Date().toISOString().split('T')[0]) {
    this.id = id;
    this.userId = userId;
    this.deskCode = deskCode; // Örn: D-101, ROOM-A
    this.timeSlot = timeSlot; // Örn: "09:00 - 12:00", "13:00 - 16:00"
    this.date = date;
    this.status = "CONFIRMED"; // CONFIRMED, CANCELLED
    this.createdAt = new Date();
  }
}
