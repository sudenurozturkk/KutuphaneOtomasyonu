/**
 * ============================================================================
 * BUSINESS & DOMAIN SERVICES (İŞ MANTIĞI KATMANI)
 * Ödünç, İade, Rezervasyon, Ceza Stratejisi, Masa ve Öneri Motoru Servisleri
 * ============================================================================
 */

import { DatabaseContext } from '../data/database.js';
import { BorrowTransaction, FineRecord, Reservation, StudyDeskBooking } from '../core/models.js';
import { 
  FineContext, 
  StudentFineStrategy, 
  AcademicianFineStrategy, 
  VIPFacultyFineStrategy,
  MaterialSubject,
  UserNotificationObserver 
} from '../patterns/designPatterns.js';

export class LibraryService {
  constructor() {
    this.db = DatabaseContext.getInstance();
    this.materialSubject = new MaterialSubject();
    this.initObservers();
  }

  initObservers() {
    // Tüm kullanıcıları Observer olarak bağla
    this.db.users.forEach(user => {
      const observer = new UserNotificationObserver(user);
      // Bekleyen rezervasyonları bağla
      this.db.reservations
        .filter(r => r.userId === user.id && r.status === "PENDING")
        .forEach(r => {
          this.materialSubject.attach(r.materialId, observer);
        });
    });
  }

  // --- 1. ÖDÜNÇ ALMA MOTORU (CIRCULATION ENGINE) ---
  borrowMaterial(userId, materialId) {
    const user = this.db.users.find(u => u.id === Number(userId));
    const material = this.db.materials.find(m => m.id === Number(materialId));

    if (!user) throw new Error("Kullanıcı sistemde bulunamadı!");
    if (!material) throw new Error("Materyal sistemde bulunamadı!");

    // Kullanıcının ödünç alma hakkı var mı?
    if (!user.canBorrow()) {
      throw new Error(`Ödünç alma engellendi! Kullanıcı askıda veya gecikme borcu (${user.fineDebt.toFixed(2)} TL) 50 TL limitini aşıyor.`);
    }

    // Aktif ödünç sayısı limiti kontrolü (Polimorfik limit)
    const activeBorrows = this.db.transactions.filter(t => t.userId === user.id && t.status !== "RETURNED");
    if (activeBorrows.length >= user.getMaxBorrowLimit()) {
      throw new Error(`Maksimum ödünç limitine (${user.getMaxBorrowLimit()} kitap) ulaştınız! Yeni kitap alamazsınız.`);
    }

    // Stok kontrolü
    if (!material.isAvailable()) {
      throw new Error("Materyalin fiziksel kopyası kalmadı! Dilerseniz sıraya girerek rezerve edebilirsiniz.");
    }

    // Stok düşür
    material.decrementStock();

    // İşlem oluştur (Polimorfik süre: Lisans 15 gün, Doktora 28 gün, Hoca 45 gün vb.)
    const durationDays = user.getBorrowDurationDays();
    const newTransactionId = this.db.transactions.length ? Math.max(...this.db.transactions.map(t => t.id)) + 1 : 1001;
    const transaction = new BorrowTransaction(newTransactionId, user.id, material.id, new Date(), durationDays);
    
    this.db.transactions.push(transaction);
    user.totalBorrowedCount++;
    this.db.saveToStorage();

    return {
      transaction,
      user,
      material,
      dueDate: transaction.dueDate
    };
  }

  // --- 2. İADE ALMA & REZERVASYON TETİKLEYİCİSİ (OBSERVER PATTERN) ---
  returnMaterial(transactionId) {
    const transaction = this.db.transactions.find(t => t.id === Number(transactionId));
    if (!transaction) throw new Error("Ödünç kaydı bulunamadı!");
    if (transaction.status === "RETURNED") throw new Error("Bu kitap zaten iade edilmiş!");

    const user = this.db.users.find(u => u.id === transaction.userId);
    const material = this.db.materials.find(m => m.id === transaction.materialId);

    transaction.returnBook();
    material.incrementStock();

    // Gecikme Kontrolü & Ceza Stratejisi (STRATEGY PATTERN)
    let fineRecord = null;
    const overdueDays = transaction.getOverdueDays();
    if (overdueDays > 0) {
      let strategy = new StudentFineStrategy();
      if (user.role === "Akademisyen") strategy = new AcademicianFineStrategy();
      else if (user.role === "Kutuphaneci") strategy = new VIPFacultyFineStrategy();

      const fineContext = new FineContext(strategy);
      const fineAmount = fineContext.executeCalculation(overdueDays);

      if (fineAmount > 0) {
        const fineId = this.db.fines.length ? Math.max(...this.db.fines.map(f => f.id)) + 1 : 5001;
        fineRecord = new FineRecord(fineId, transaction.id, user.id, fineAmount, overdueDays);
        this.db.fines.push(fineRecord);
        user.fineDebt += fineAmount;
      }
    }

    // Rezerve eden varsa OBSERVER PATTERN İLE BİLDİR
    const notifications = this.materialSubject.notify(material.id, material);

    this.db.saveToStorage();

    return {
      transaction,
      material,
      user,
      overdueDays,
      fineRecord,
      notifications
    };
  }

  // --- 3. ONLİNE SÜRE UZATMA (RENEWAL) ---
  renewBorrow(transactionId) {
    const transaction = this.db.transactions.find(t => t.id === Number(transactionId));
    if (!transaction) throw new Error("Ödünç kaydı bulunamadı!");
    
    // Rezervasyon var mı kontrolü (Başkası sıradaysa süre uzatılamaz!)
    const hasReservation = this.db.reservations.some(r => r.materialId === transaction.materialId && r.status === "PENDING");
    if (hasReservation) {
      throw new Error("Bu kitap başka bir okuyucu tarafından rezerve edildiği için süresi uzatılamaz!");
    }

    const newDueDate = transaction.renew(7); // +7 gün uzat
    this.db.saveToStorage();
    return newDueDate;
  }

  // --- 4. REZERVASYON KUYRUĞUNA GİRME ---
  reserveMaterial(userId, materialId) {
    const user = this.db.users.find(u => u.id === Number(userId));
    const material = this.db.materials.find(m => m.id === Number(materialId));

    if (!user || !material) throw new Error("Kullanıcı veya materyal bulunamadı!");
    if (material.isAvailable()) {
      throw new Error("Bu kitap şu anda rafta mevcut! Rezervasyon yerine doğrudan ödünç alabilirsiniz.");
    }

    const alreadyReserved = this.db.reservations.some(r => r.userId === user.id && r.materialId === material.id && r.status === "PENDING");
    if (alreadyReserved) {
      throw new Error("Bu materyal için zaten bekleme kuyruğundasınız!");
    }

    const newResId = this.db.reservations.length ? Math.max(...this.db.reservations.map(r => r.id)) + 1 : 1;
    const reservation = new Reservation(newResId, user.id, material.id);
    this.db.reservations.push(reservation);

    // Observer'a kaydet
    const observer = new UserNotificationObserver(user);
    this.materialSubject.attach(material.id, observer);

    this.db.saveToStorage();
    return reservation;
  }

  // --- 5. CEZA ÖDEME & SANAL CÜZDAN ---
  payFine(fineId, userId) {
    const fine = this.db.fines.find(f => f.id === Number(fineId));
    const user = this.db.users.find(u => u.id === Number(userId));

    if (!fine) throw new Error("Ceza kaydı bulunamadı!");
    if (fine.isPaid) throw new Error("Bu ceza zaten ödenmiş!");
    if (!user) throw new Error("Kullanıcı bulunamadı!");

    if (user.walletBalance < fine.amount) {
      throw new Error(`Yetersiz bakiye! Cüzdanınızda ${user.walletBalance.toFixed(2)} TL var, gereken: ${fine.amount.toFixed(2)} TL. Lütfen bakiye yükleyiniz.`);
    }

    user.payDebt(fine.amount);
    const receiptCode = fine.pay();
    this.db.saveToStorage();

    return {
      receiptCode,
      amount: fine.amount,
      remainingBalance: user.walletBalance
    };
  }

  topupWallet(userId, amount) {
    const user = this.db.users.find(u => u.id === Number(userId));
    if (!user) throw new Error("Kullanıcı bulunamadı!");
    if (amount <= 0) throw new Error("Geçerli bir yükleme tutarı giriniz!");

    user.addBalance(amount);
    this.db.saveToStorage();
    return user.walletBalance;
  }

  // --- 6. ÇALIŞMA MASASI & SALON REZERVASYONU ---
  bookStudyDesk(userId, deskCode, timeSlot, date) {
    const user = this.db.users.find(u => u.id === Number(userId));
    if (!user) throw new Error("Kullanıcı bulunamadı!");

    // Çakışma kontrolü
    const isOccupied = this.db.deskBookings.some(b => 
      b.deskCode === deskCode && 
      b.timeSlot === timeSlot && 
      b.date === date && 
      b.status === "CONFIRMED"
    );

    if (isOccupied) {
      throw new Error(`Seçilen masa (${deskCode}) belirtilen saat diliminde doludur! Lütfen başka bir masa seçiniz.`);
    }

    const newBookingId = this.db.deskBookings.length ? Math.max(...this.db.deskBookings.map(b => b.id)) + 1 : 1;
    const booking = new StudyDeskBooking(newBookingId, user.id, deskCode, timeSlot, date);
    this.db.deskBookings.push(booking);
    this.db.saveToStorage();

    return booking;
  }

  cancelDeskBooking(bookingId) {
    const booking = this.db.deskBookings.find(b => b.id === Number(bookingId));
    if (!booking) throw new Error("Rezervasyon kaydı bulunamadı!");
    booking.status = "CANCELLED";
    this.db.saveToStorage();
    return true;
  }

  // --- 7. AKILLI ÖNERİ MOTORU (RECOMMENDATION ENGINE) ---
  getRecommendationsForUser(userId) {
    const user = this.db.users.find(u => u.id === Number(userId));
    if (!user) return this.db.materials.slice(0, 4);

    // Kullanıcının daha önce ödünç aldığı kategoriler
    const userBorrowedMatIds = this.db.transactions
      .filter(t => t.userId === user.id)
      .map(t => t.materialId);

    const preferredCategories = this.db.materials
      .filter(m => userBorrowedMatIds.includes(m.id))
      .map(m => m.category);

    if (preferredCategories.length > 0) {
      return this.db.materials
        .filter(m => preferredCategories.includes(m.category) && !userBorrowedMatIds.includes(m.id))
        .slice(0, 4);
    }

    // Varsayılan: En çok ödünç alınan popüler materyaller
    return [...this.db.materials]
      .sort((a, b) => b.timesBorrowed - a.timesBorrowed)
      .slice(0, 4);
  }

  // --- 8. SİSTEM ANALİTİĞİ & METRİKLER (KPI & ANALYTICS) ---
  getSystemAnalytics() {
    const totalMaterials = this.db.materials.reduce((acc, m) => acc + m.totalStock, 0);
    const activeBorrows = this.db.transactions.filter(t => t.status !== "RETURNED");
    const overdueBorrows = activeBorrows.filter(t => t.isOverdue() || t.status === "OVERDUE");
    const totalFinesCollected = this.db.fines.filter(f => f.isPaid).reduce((acc, f) => acc + f.amount, 0);
    const pendingFinesDebt = this.db.users.reduce((acc, u) => acc + u.fineDebt, 0);

    return {
      totalTitles: this.db.materials.length,
      totalPhysicalCopies: totalMaterials,
      activeBorrowsCount: activeBorrows.length,
      overdueCount: overdueBorrows.length,
      totalRegisteredUsers: this.db.users.length,
      totalFinesCollected,
      pendingFinesDebt,
      deskReservationsToday: this.db.deskBookings.filter(b => b.status === "CONFIRMED").length
    };
  }
}
