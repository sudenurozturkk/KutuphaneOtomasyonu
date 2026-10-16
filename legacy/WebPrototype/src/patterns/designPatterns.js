/**
 * ============================================================================
 * GOF DESIGN PATTERNS (YAZILIM TASARIM MİMARİSİ DESENLERİ)
 * Strategy, Observer, Factory Method, Singleton, State, Command Patterns
 * ============================================================================
 */

import { Book, EBook, Journal, Thesis, ShelfLocation } from '../core/models.js';

// ============================================================================
// 1. STRATEGY PATTERN (CEZA & SÜRE HESAPLAMA STRATEJİLERİ)
// ============================================================================

export class FineCalculationStrategy {
  calculateFine(overdueDays) {
    throw new Error("calculateFine() alt stratejide uygulanmalıdır!");
  }
}

export class StudentFineStrategy extends FineCalculationStrategy {
  calculateFine(overdueDays) {
    // Öğrenci: İlk 3 gün toleranslı, sonrası 5 TL/gün
    if (overdueDays <= 0) return 0;
    return overdueDays * 5.0;
  }
}

export class AcademicianFineStrategy extends FineCalculationStrategy {
  calculateFine(overdueDays) {
    // Akademisyen: Düşük ceza oranı, 2 TL/gün
    if (overdueDays <= 0) return 0;
    return overdueDays * 2.0;
  }
}

export class VIPFacultyFineStrategy extends FineCalculationStrategy {
  calculateFine(overdueDays) {
    // Onursal veya misafir araştırmacı: Ceza muafiyeti
    return 0.0;
  }
}

export class FineContext {
  constructor(strategy) {
    this.strategy = strategy;
  }

  setStrategy(strategy) {
    this.strategy = strategy;
  }

  executeCalculation(overdueDays) {
    return this.strategy.calculateFine(overdueDays);
  }
}

// ============================================================================
// 2. OBSERVER PATTERN (REZERVASYON & STOK BİLDİRİM SİSTEMİ)
// ============================================================================

export class Observer {
  update(event, data) {
    throw new Error("update() metodu implement edilmelidir!");
  }
}

export class UserNotificationObserver extends Observer {
  constructor(user) {
    super();
    this.user = user;
    this.notifications = [];
  }

  update(event, data) {
    const notification = {
      id: Date.now() + Math.random(),
      event,
      message: `Sn. ${this.user.name}, rezerve ettiğiniz '${data.title}' materyali kütüphaneye iade edildi ve adınıza 48 saat ayrıldı!`,
      date: new Date(),
      read: false
    };
    this.notifications.push(notification);
    console.log(`[Observer Bildirimi] ${this.user.name} kullanıcısına iletildi:`, notification.message);
    return notification;
  }
}

export class MaterialSubject {
  constructor() {
    this.observers = new Map(); // materialId -> Array<Observer>
  }

  attach(materialId, observer) {
    if (!this.observers.has(materialId)) {
      this.observers.set(materialId, []);
    }
    this.observers.get(materialId).push(observer);
  }

  detach(materialId, observer) {
    if (this.observers.has(materialId)) {
      const list = this.observers.get(materialId).filter(obs => obs !== observer);
      this.observers.set(materialId, list);
    }
  }

  notify(materialId, materialData) {
    if (this.observers.has(materialId)) {
      const list = this.observers.get(materialId);
      const notificationsSent = [];
      list.forEach(observer => {
        const notif = observer.update("MATERIAL_AVAILABLE", materialData);
        notificationsSent.push(notif);
      });
      // Bildirim gönderildikten sonra kuyruktaki ilk kişiyi ayırtmış sayabiliriz
      return notificationsSent;
    }
    return [];
  }
}

// ============================================================================
// 3. FACTORY METHOD PATTERN (MATERYAL ÜRETİM FABRİKASI)
// ============================================================================

export class MaterialFactory {
  static createMaterial(type, data) {
    switch (type.toUpperCase()) {
      case "BOOK":
      case "KITAP": {
        const shelf = new ShelfLocation(
          data.building || "Merkez Kütüphane",
          data.floor || 1,
          data.aisle || "A-1",
          data.shelfNo || "10"
        );
        return new Book(
          data.id,
          data.isbn,
          data.title,
          data.author,
          data.publisher,
          data.publicationYear,
          data.category,
          data.totalStock || 1,
          data.pageCount || 200,
          data.coverType || "Karton Kapak",
          shelf,
          data.summary || ""
        );
      }
      case "EBOOK":
      case "EKITAP":
        return new EBook(
          data.id,
          data.isbn,
          data.title,
          data.author,
          data.publisher,
          data.publicationYear,
          data.category,
          data.fileSizeMB || 5.4,
          data.format || "PDF",
          data.contentUrl || "",
          data.summary || ""
        );
      case "JOURNAL":
      case "DERGI":
        return new Journal(
          data.id,
          data.issn,
          data.title,
          data.publisher,
          data.publicationYear,
          data.category,
          data.issueNumber || 1,
          data.frequency || "Aylık",
          data.totalStock || 2,
          data.summary || ""
        );
      case "THESIS":
      case "TEZ":
        return new Thesis(
          data.id,
          data.title,
          data.author,
          data.advisor || "Prof. Dr. Bilinmiyor",
          data.university || "Üniversite Kütüphanesi",
          data.department || "Bilgisayar Mühendisliği",
          data.degreeLevel || "Doktora",
          data.publicationYear || 2024,
          data.summary || ""
        );
      default:
        throw new Error(`Geçersiz Materyal Tipi: '${type}'. Fabrika nesneyi üretemedi!`);
    }
  }
}

// ============================================================================
// 4. STATE PATTERN (MATERYAL YAŞAM DÖNGÜSÜ DURUM DESENİ)
// ============================================================================

export class MaterialState {
  handleBorrow(context) { throw new Error("handleBorrow() ezilmeli"); }
  handleReturn(context) { throw new Error("handleReturn() ezilmeli"); }
  getStatusName() { return "Bilinmeyen Durum"; }
}

export class AvailableState extends MaterialState {
  handleBorrow(context) {
    if (context.material.availableStock <= 1) {
      context.setState(new BorrowedState());
    }
    context.material.decrementStock();
    return true;
  }
  handleReturn(context) {
    context.material.incrementStock();
    return true;
  }
  getStatusName() { return "Rafta Mevcut"; }
}

export class BorrowedState extends MaterialState {
  handleBorrow(context) {
    throw new Error("Materyal stokta kalmadı! Yalnızca rezervasyon yapılabilir.");
  }
  handleReturn(context) {
    context.material.incrementStock();
    context.setState(new AvailableState());
    return true;
  }
  getStatusName() { return "Tümü Ödünçte"; }
}

export class MaterialStateContext {
  constructor(material) {
    this.material = material;
    this.state = material.isAvailable() ? new AvailableState() : new BorrowedState();
  }

  setState(state) {
    this.state = state;
  }

  borrow() {
    return this.state.handleBorrow(this);
  }

  returnBook() {
    return this.state.handleReturn(this);
  }

  getStatus() {
    return this.state.getStatusName();
  }
}

// ============================================================================
// 5. COMMAND PATTERN (İŞLEM GERİ ALMA & DENETİM KAYDI)
// ============================================================================

export class Command {
  execute() { throw new Error("execute() uygulanmalıdır"); }
  undo() { throw new Error("undo() uygulanmalıdır"); }
}

export class BorrowCommand extends Command {
  constructor(borrowService, userId, materialId) {
    super();
    this.borrowService = borrowService;
    this.userId = userId;
    this.materialId = materialId;
    this.transactionResult = null;
  }

  execute() {
    this.transactionResult = this.borrowService.borrowMaterial(this.userId, this.materialId);
    return this.transactionResult;
  }

  undo() {
    if (this.transactionResult) {
      this.borrowService.returnMaterial(this.transactionResult.id);
      return true;
    }
    return false;
  }
}
