/**
 * ============================================================================
 * PRESENTATION LAYER CONTROLLER (UI APP ENTRY)
 * Single Page Application Router, Event Handlers, Dynamic DOM Rendering
 * ============================================================================
 */

import { DatabaseContext } from '../data/database.js';
import { LibraryService } from '../services/libraryService.js';
import { MaterialFactory } from '../patterns/designPatterns.js';

class AppController {
  constructor() {
    this.db = DatabaseContext.getInstance();
    this.service = new LibraryService();
    this.currentView = "dashboard";
    this.selectedBookForModal = null;
    this.selectedDeskCode = null;

    this.initElements();
    this.bindEvents();
    this.renderAll();

    // Mermaid UML başlat
    if (window.mermaid) {
      window.mermaid.initialize({ startOnLoad: true, theme: 'dark' });
    }
  }

  initElements() {
    this.userRoleSelector = document.getElementById("user-role-selector");
    this.roleBadgePill = document.getElementById("role-badge-pill");
    this.userDisplayName = document.getElementById("user-display-name");
    this.userDisplaySub = document.getElementById("user-display-sub");
    this.userAvatar = document.getElementById("user-avatar");
    this.dashGreetingName = document.getElementById("dash-greeting-name");
    this.globalSearchInput = document.getElementById("global-search-input");
    this.themeToggleBtn = document.getElementById("theme-toggle-btn");
    this.resetDbBtn = document.getElementById("reset-db-btn");
    this.navItems = document.querySelectorAll(".nav-menu .nav-item");
    this.viewSections = document.querySelectorAll(".view-section");
    this.toastContainer = document.getElementById("toast-container");
  }

  bindEvents() {
    // 1. View Navigation
    this.navItems.forEach(item => {
      item.addEventListener("click", () => {
        const targetView = item.getAttribute("data-view");
        this.navigateTo(targetView);
      });
    });

    // 2. User & Role Switcher
    this.userRoleSelector.addEventListener("change", (e) => {
      const selectedId = Number(e.target.value);
      this.db.currentUser = this.db.users.find(u => u.id === selectedId) || this.db.users[0];
      this.db.saveToStorage();
      this.showToast(`Profil değiştirildi: ${this.db.currentUser.name} (${this.db.currentUser.role})`, "info");
      this.renderAll();
    });

    // 3. Global Search Input
    this.globalSearchInput.addEventListener("input", (e) => {
      const query = e.target.value.toLowerCase().trim();
      this.renderCatalog(query);
      if (this.currentView !== "catalog" && query.length > 0) {
        this.navigateTo("catalog");
      }
    });

    // 4. Catalog Category Filter Pills
    const filterButtons = document.querySelectorAll("#catalog-category-filters .filter-btn");
    filterButtons.forEach(btn => {
      btn.addEventListener("click", () => {
        filterButtons.forEach(b => b.classList.remove("active"));
        btn.classList.add("active");
        this.renderCatalog();
      });
    });

    // Sadece stoktakiler filtresi
    document.getElementById("filter-only-available")?.addEventListener("change", () => {
      this.renderCatalog();
    });

    // 5. Dashboard Quick Links
    document.getElementById("btn-quick-catalog")?.addEventListener("click", () => this.navigateTo("catalog"));
    document.getElementById("btn-goto-borrows")?.addEventListener("click", () => this.navigateTo("borrows"));

    // 6. Theme Toggle (Koyu / Açık Mod)
    this.themeToggleBtn.addEventListener("click", () => {
      const current = document.documentElement.getAttribute("data-theme");
      const next = current === "light" ? "dark" : "light";
      document.documentElement.setAttribute("data-theme", next);
      this.showToast(`Görünüm teması: ${next === "light" ? "Açık Mod" : "Koyu Mod"}`, "info");
    });

    // 7. Reset Database
    this.resetDbBtn.addEventListener("click", () => {
      if (confirm("Kütüphane veritabanı fabrika ayarlarına sıfırlansın mı?")) {
        this.db.resetToDefault();
        this.showToast("Veritabanı ve örnek veriler başarıyla sıfırlandı!", "success");
        this.renderAll();
      }
    });

    // 8. Modal Kapatma Düğmeleri
    document.querySelectorAll("[data-close]").forEach(btn => {
      btn.addEventListener("click", () => {
        const modalId = btn.getAttribute("data-close");
        document.getElementById(modalId)?.classList.remove("open");
      });
    });

    // 9. Kitap Detay Modalı Aksiyonları
    document.getElementById("btn-modal-action-borrow")?.addEventListener("click", () => {
      if (!this.selectedBookForModal) return;
      try {
        const res = this.service.borrowMaterial(this.db.currentUser.id, this.selectedBookForModal.id);
        this.showToast(`'${res.material.title}' başarıyla ödünç alındı! Son teslim: ${new Date(res.dueDate).toLocaleDateString('tr-TR')}`, "success");
        document.getElementById("modal-book-detail")?.classList.remove("open");
        this.renderAll();
      } catch (err) {
        this.showToast(err.message, "error");
      }
    });

    document.getElementById("btn-modal-action-reserve")?.addEventListener("click", () => {
      if (!this.selectedBookForModal) return;
      try {
        const res = this.service.reserveMaterial(this.db.currentUser.id, this.selectedBookForModal.id);
        this.showToast(`'${this.selectedBookForModal.title}' için bekleme sırasına girdiniz! İade edildiğinde Observer tarafından bildirim alacaksınız.`, "warning");
        document.getElementById("modal-book-detail")?.classList.remove("open");
        this.renderAll();
      } catch (err) {
        this.showToast(err.message, "error");
      }
    });

    // 10. Yeni Materyal Ekleme Modalı (Factory Pattern)
    document.getElementById("btn-open-add-material")?.addEventListener("click", () => {
      document.getElementById("modal-add-material")?.classList.add("open");
    });

    document.getElementById("btn-submit-add-material")?.addEventListener("click", (e) => {
      e.preventDefault();
      const type = document.getElementById("new-mat-type").value;
      const title = document.getElementById("new-mat-title").value.trim();
      const author = document.getElementById("new-mat-author").value.trim();
      const isbn = document.getElementById("new-mat-isbn").value.trim();
      const year = Number(document.getElementById("new-mat-year").value) || 2026;
      const cat = document.getElementById("new-mat-cat").value.trim() || "Genel";
      const stock = Number(document.getElementById("new-mat-stock").value) || 1;
      const summary = document.getElementById("new-mat-summary").value.trim();

      if (!title || !author || !isbn) {
        this.showToast("Lütfen zorunlu alanları (Başlık, Yazar, ISBN) doldurunuz!", "error");
        return;
      }

      try {
        const newId = this.db.materials.length ? Math.max(...this.db.materials.map(m => m.id)) + 1 : 101;
        const newMaterial = MaterialFactory.createMaterial(type, {
          id: newId,
          isbn,
          title,
          author,
          publisher: "Üniversite Yayınları",
          publicationYear: year,
          category: cat,
          totalStock: stock,
          summary
        });

        this.db.materials.push(newMaterial);
        this.db.saveToStorage();
        this.showToast(`Yeni materyal başarıyla üretildi (${newMaterial.getTypeName()}): '${newMaterial.title}'`, "success");
        document.getElementById("modal-add-material")?.classList.remove("open");
        document.getElementById("form-add-material")?.reset();
        this.renderAll();
      } catch (err) {
        this.showToast(err.message, "error");
      }
    });

    // 11. Bakiye Yükleme Modalı
    document.getElementById("btn-open-topup")?.addEventListener("click", () => {
      document.getElementById("modal-topup-wallet")?.classList.add("open");
    });
    document.getElementById("btn-open-wallet-topup")?.addEventListener("click", () => {
      document.getElementById("modal-topup-wallet")?.classList.add("open");
    });

    document.getElementById("btn-confirm-topup")?.addEventListener("click", () => {
      const amount = Number(document.getElementById("topup-amount-input").value);
      try {
        const newBal = this.service.topupWallet(this.db.currentUser.id, amount);
        this.showToast(`Cüzdanınıza ${amount.toFixed(2)} TL yüklendi! Yeni bakiye: ${newBal.toFixed(2)} TL`, "success");
        document.getElementById("modal-topup-wallet")?.classList.remove("open");
        this.renderAll();
      } catch (err) {
        this.showToast(err.message, "error");
      }
    });

    // 12. Masa Rezervasyonu Onayı
    document.getElementById("btn-confirm-desk-booking")?.addEventListener("click", () => {
      if (!this.selectedDeskCode) return;
      const timeSlot = document.getElementById("desk-timeslot-select").value;
      const today = new Date().toISOString().split('T')[0];

      try {
        const booking = this.service.bookStudyDesk(this.db.currentUser.id, this.selectedDeskCode, timeSlot, today);
        this.showToast(`${booking.deskCode} masası (${booking.timeSlot}) adınıza başarıyla rezerve edildi!`, "success");
        this.selectedDeskCode = null;
        document.getElementById("btn-confirm-desk-booking").disabled = true;
        document.getElementById("selected-desk-label").textContent = "Lütfen yukarıdan bir masa seçiniz";
        this.renderDesks();
      } catch (err) {
        this.showToast(err.message, "error");
      }
    });

    document.getElementById("desk-timeslot-select")?.addEventListener("change", () => {
      this.renderDesks();
    });

    // 13. Görevli Barkod Masası İşlemleri
    document.getElementById("btn-barcode-borrow")?.addEventListener("click", () => {
      const uId = document.getElementById("barcode-user-select").value;
      const mId = document.getElementById("barcode-material-select").value;
      try {
        const res = this.service.borrowMaterial(uId, mId);
        this.showToast(`Barkodlu Ödünç Başarılı: ${res.user.name} -> '${res.material.title}'`, "success");
        this.renderAll();
      } catch (err) {
        this.showToast(err.message, "error");
      }
    });

    document.getElementById("btn-barcode-return")?.addEventListener("click", () => {
      const tId = document.getElementById("barcode-return-select").value;
      if (!tId) return;
      try {
        const res = this.service.returnMaterial(tId);
        let msg = `'${res.material.title}' teslim alındı ve stoğa eklendi.`;
        if (res.overdueDays > 0) {
          msg += ` (DİKKAT: ${res.overdueDays} gün gecikme! ${res.fineRecord.amount.toFixed(2)} TL ceza kesildi.)`;
        }
        if (res.notifications && res.notifications.length > 0) {
          msg += ` [Observer]: Sıradaki rezerve kullanıcıya bildirim iletildi!`;
        }
        this.showToast(msg, res.overdueDays > 0 ? "warning" : "success");
        this.renderAll();
      } catch (err) {
        this.showToast(err.message, "error");
      }
    });

    // 14. Mimari Alt Sekmeleri
    document.querySelectorAll("#arch-subtabs .filter-btn").forEach(btn => {
      btn.addEventListener("click", () => {
        document.querySelectorAll("#arch-subtabs .filter-btn").forEach(b => b.classList.remove("active"));
        btn.classList.add("active");
        const tabTarget = btn.getAttribute("data-tab");
        document.querySelectorAll(".arch-tab-content").forEach(c => c.style.display = "none");
        document.getElementById(`tab-${tabTarget}`).style.display = "block";
      });
    });

    // 15. E-Okuyucu Font Boyutu
    let currentFontSize = 15;
    document.getElementById("btn-font-inc")?.addEventListener("click", () => {
      currentFontSize = Math.min(22, currentFontSize + 2);
      document.getElementById("ereader-content-text").style.fontSize = `${currentFontSize}px`;
    });
    document.getElementById("btn-font-dec")?.addEventListener("click", () => {
      currentFontSize = Math.max(12, currentFontSize - 2);
      document.getElementById("ereader-content-text").style.fontSize = `${currentFontSize}px`;
    });
  }

  navigateTo(viewId) {
    this.currentView = viewId;
    this.navItems.forEach(item => {
      if (item.getAttribute("data-view") === viewId) item.classList.add("active");
      else item.classList.remove("active");
    });

    this.viewSections.forEach(section => {
      if (section.id === `view-${viewId}`) section.classList.add("active");
      else section.classList.remove("active");
    });

    // İlgili görünümü tazeleyin
    if (viewId === "catalog") this.renderCatalog();
    else if (viewId === "borrows") this.renderBorrows();
    else if (viewId === "desks") this.renderDesks();
    else if (viewId === "wallet") this.renderWallet();
    else if (viewId === "librarian") this.renderLibrarianStation();
    else if (viewId === "analytics") this.renderAnalytics();
  }

  renderAll() {
    this.renderHeaderAndUser();
    this.renderDashboard();
    this.renderCatalog();
    this.renderBorrows();
    this.renderDesks();
    this.renderWallet();
    this.renderLibrarianStation();
    this.renderAnalytics();
  }

  // Header & Profil Bilgileri
  renderHeaderAndUser() {
    const user = this.db.currentUser;
    if (!user) return;

    this.userRoleSelector.value = user.id;
    this.roleBadgePill.textContent = user.role;
    this.userDisplayName.textContent = user.name;
    this.dashGreetingName.textContent = user.name.split(" ")[0];
    
    let subInfo = user.role;
    if (user.role === "Ogrenci") subInfo = `${user.department} (${user.degreeLevel})`;
    else if (user.role === "Akademisyen") subInfo = `${user.title} | ${user.faculty}`;
    else if (user.role === "Kutuphaneci") subInfo = `${user.employeeId} | Görevli`;

    this.userDisplaySub.textContent = subInfo;
    this.userAvatar.textContent = user.name.split(" ").map(n => n[0]).join("").slice(0, 2);

    // Sidebar rozetleri
    const activeBorrows = this.db.transactions.filter(t => t.userId === user.id && t.status !== "RETURNED");
    document.getElementById("nav-borrow-count").textContent = activeBorrows.length;

    const debtPill = document.getElementById("nav-debt-count");
    if (user.fineDebt > 0) {
      debtPill.style.display = "inline-block";
      debtPill.textContent = `${user.fineDebt.toFixed(2)} TL`;
    } else {
      debtPill.style.display = "none";
    }
  }

  // Dashboard Görünümü
  renderDashboard() {
    const user = this.db.currentUser;
    const activeBorrows = this.db.transactions.filter(t => t.userId === user.id && t.status !== "RETURNED");
    const overdueCount = activeBorrows.filter(t => t.isOverdue() || t.status === "OVERDUE").length;

    document.getElementById("stat-active-borrows").textContent = activeBorrows.length;
    document.getElementById("stat-limit-text").textContent = `Kota: ${user.getMaxBorrowLimit()} Kitap`;
    document.getElementById("stat-overdue-count").textContent = overdueCount;
    
    const overdueAlert = document.getElementById("stat-overdue-alert");
    if (overdueCount > 0) {
      overdueAlert.textContent = `⚠️ ${overdueCount} kitap gecikmede!`;
      overdueAlert.className = "stat-trend negative";
    } else {
      overdueAlert.textContent = "Tüm teslimler zamanında";
      overdueAlert.className = "stat-trend positive";
    }

    document.getElementById("stat-fine-debt").textContent = `${user.fineDebt.toFixed(2)} TL`;
    const debtStatus = document.getElementById("stat-debt-status");
    if (user.fineDebt > 0) {
      debtStatus.textContent = "Ödenmemiş borç var!";
      debtStatus.className = "stat-trend negative";
    } else {
      debtStatus.textContent = "Borcunuz bulunmamaktadır";
      debtStatus.className = "stat-trend positive";
    }

    document.getElementById("stat-wallet-balance").textContent = `${user.walletBalance.toFixed(2)} TL`;

    // Dashboard Ödünç Tablosu
    const tbody = document.getElementById("dash-borrows-tbody");
    tbody.innerHTML = "";

    if (activeBorrows.length === 0) {
      tbody.innerHTML = `<tr><td colspan="6" style="text-align: center; color: var(--text-dim); padding: 24px;">Şu anda üzerinizde aktif ödünç kitap bulunmuyor.</td></tr>`;
    } else {
      activeBorrows.forEach(t => {
        const mat = this.db.materials.find(m => m.id === t.materialId);
        const overdue = t.isOverdue() || t.status === "OVERDUE";
        const overdueDays = t.getOverdueDays();
        const diffDays = Math.ceil((new Date(t.dueDate) - new Date()) / (1000 * 60 * 60 * 24));

        const tr = document.createElement("tr");
        tr.innerHTML = `
          <td><strong>${mat ? mat.title : 'Bilinmeyen'}</strong></td>
          <td>${new Date(t.borrowDate).toLocaleDateString('tr-TR')}</td>
          <td>${new Date(t.dueDate).toLocaleDateString('tr-TR')}</td>
          <td>
            ${overdue ? `<span style="color: var(--danger); font-weight: 700;">${overdueDays} gün gecikti!</span>` : `<span style="color: var(--success);">${diffDays} gün kaldı</span>`}
          </td>
          <td>
            <span class="status-pill ${overdue ? 'overdue' : 'active'}">${overdue ? 'GECİKTİ' : 'Ödünçte'}</span>
          </td>
          <td>
            <button class="btn btn-secondary btn-sm" onclick="window.app.handleRenew(${t.id})" ${overdue ? 'disabled title="Geciken kitap online uzatılamaz"' : ''}>+ Süre Uzat</button>
            <button class="btn btn-primary btn-sm" onclick="window.app.handleReturn(${t.id})">İade Et</button>
          </td>
        `;
        tbody.appendChild(tr);
      });
    }

    // Öneri Motoru Kartları
    const recGrid = document.getElementById("dash-recommendations-grid");
    recGrid.innerHTML = "";
    const recs = this.service.getRecommendationsForUser(user.id);
    recs.forEach(book => {
      recGrid.appendChild(this.createBookCard(book));
    });
  }

  // Katalog Görünümü
  renderCatalog(searchQuery = "") {
    const container = document.getElementById("catalog-books-grid");
    if (!container) return;
    container.innerHTML = "";

    const activeFilterBtn = document.querySelector("#catalog-category-filters .filter-btn.active");
    const categoryFilter = activeFilterBtn ? activeFilterBtn.getAttribute("data-filter") : "ALL";
    const onlyAvailable = document.getElementById("filter-only-available")?.checked;

    const filtered = this.db.materials.filter(m => {
      // 1. Kategori filtresi
      if (categoryFilter !== "ALL") {
        if (categoryFilter === "E-Kitap" && !m.fileSizeMB) return false;
        if (categoryFilter === "Akademik Tez" && !m.advisor) return false;
        if (categoryFilter !== "E-Kitap" && categoryFilter !== "Akademik Tez" && m.category !== categoryFilter) return false;
      }

      // 2. Stok filtresi
      if (onlyAvailable && !m.isAvailable()) return false;

      // 3. Arama filtresi
      if (searchQuery) {
        const matchTitle = m.title.toLowerCase().includes(searchQuery);
        const matchAuthor = m.author && m.author.toLowerCase().includes(searchQuery);
        const matchIsbn = m.isbn && m.isbn.toLowerCase().includes(searchQuery);
        const matchCat = m.category && m.category.toLowerCase().includes(searchQuery);
        return matchTitle || matchAuthor || matchIsbn || matchCat;
      }

      return true;
    });

    if (filtered.length === 0) {
      container.innerHTML = `<div style="grid-column: 1 / -1; text-align: center; padding: 48px; color: var(--text-dim);">Arama kriterlerine uygun kütüphane materyali bulunamadı.</div>`;
      return;
    }

    filtered.forEach(book => {
      container.appendChild(this.createBookCard(book));
    });
  }

  // Kitap Kartı Üretici (UI Component)
  createBookCard(book) {
    const card = document.createElement("div");
    card.className = "book-card";

    let icon = "📚";
    if (book.fileSizeMB) icon = "💻";
    else if (book.issn) icon = "📰";
    else if (book.advisor) icon = "🎓";

    const isAvail = book.isAvailable();
    const stockBadgeClass = isAvail ? "available" : "out";
    const stockBadgeText = book.fileSizeMB ? "Dijital" : (isAvail ? `${book.availableStock} / ${book.totalStock} Adet` : "Tükendi");

    card.innerHTML = `
      <div class="book-cover">
        <span class="book-badge-type">${book.getTypeName()}</span>
        <span class="book-badge-stock ${stockBadgeClass}">${stockBadgeText}</span>
        <div class="book-cover-placeholder">${icon}</div>
      </div>
      <div class="book-details">
        <div class="book-category">${book.category}</div>
        <h4 class="book-title">${book.title}</h4>
        <div class="book-author">${book.author}</div>
        <div class="book-meta">
          <span>${book.publicationYear}</span>
          <div class="book-location">
            <span>📍 ${book.shelfLocation ? book.shelfLocation.aisle + '-' + book.shelfLocation.shelfNo : 'Online / Dijital'}</span>
          </div>
        </div>
        <div class="book-actions">
          <button class="btn btn-secondary btn-sm btn-block" onclick="window.app.openBookModal(${book.id})">İncele & İşlem</button>
        </div>
      </div>
    `;
    return card;
  }

  // Kitap Detay Modalı Açıcı
  openBookModal(bookId) {
    const book = this.db.materials.find(m => m.id === Number(bookId));
    if (!book) return;

    this.selectedBookForModal = book;
    document.getElementById("m-book-title").textContent = book.title;
    document.getElementById("m-book-heading").textContent = book.title;
    document.getElementById("m-book-author").textContent = book.author;
    document.getElementById("m-book-category").textContent = book.category;
    document.getElementById("m-book-summary").textContent = book.summary;
    document.getElementById("m-book-meta").textContent = `${book.publicationYear} | ${book.publisher || 'Üniversite Basımevi'}`;
    document.getElementById("m-book-shelf").textContent = book.shelfLocation ? book.shelfLocation.getFormatted() : "Dijital Kaynak (Fiziksel raf gerektirmez)";

    const isAvail = book.isAvailable();
    const stockBadge = document.getElementById("m-book-stock-badge");
    const btnBorrow = document.getElementById("btn-modal-action-borrow");
    const btnReserve = document.getElementById("btn-modal-action-reserve");

    if (isAvail) {
      stockBadge.className = "book-badge-stock available";
      stockBadge.textContent = book.fileSizeMB ? "Sınırsız Dijital Stok" : `${book.availableStock} / ${book.totalStock} Adet Mevcut`;
      btnBorrow.style.display = "inline-flex";
      btnReserve.style.display = "none";
    } else {
      stockBadge.className = "book-badge-stock out";
      stockBadge.textContent = "Tükendi (Tümü Ödünçte)";
      btnBorrow.style.display = "none";
      btnReserve.style.display = "inline-flex";
    }

    document.getElementById("modal-book-detail")?.classList.add("open");
  }

  // Ödünçlerim Görünümü
  renderBorrows() {
    const user = this.db.currentUser;
    const userBorrows = this.db.transactions.filter(t => t.userId === user.id);
    const activeCount = userBorrows.filter(t => t.status !== "RETURNED").length;

    document.getElementById("active-borrows-count-pill").textContent = `${activeCount} Aktif Ödünç`;

    const tbody = document.getElementById("borrows-table-tbody");
    tbody.innerHTML = "";

    if (userBorrows.length === 0) {
      tbody.innerHTML = `<tr><td colspan="8" style="text-align: center; color: var(--text-dim); padding: 24px;">Henüz bir ödünç hareketiniz bulunmuyor.</td></tr>`;
      return;
    }

    userBorrows.forEach(t => {
      const mat = this.db.materials.find(m => m.id === t.materialId);
      const isReturned = t.status === "RETURNED";
      const isOverdue = t.isOverdue() || t.status === "OVERDUE";

      let statusBadge = `<span class="status-pill active">Ödünçte</span>`;
      if (isReturned) statusBadge = `<span class="status-pill returned">İade Edildi</span>`;
      else if (isOverdue) statusBadge = `<span class="status-pill overdue">GECİKTİ</span>`;

      const tr = document.createElement("tr");
      tr.innerHTML = `
        <td>#${t.id}</td>
        <td><strong>${mat ? mat.title : 'Bilinmeyen'}</strong></td>
        <td>${mat ? mat.category : '-'}</td>
        <td>${new Date(t.borrowDate).toLocaleDateString('tr-TR')}</td>
        <td>${new Date(t.dueDate).toLocaleDateString('tr-TR')}</td>
        <td>${t.renewalCount} / 2</td>
        <td>${statusBadge}</td>
        <td>
          ${!isReturned ? `
            <button class="btn btn-secondary btn-sm" onclick="window.app.handleRenew(${t.id})" ${isOverdue ? 'disabled title="Geciken kitap online uzatılamaz"' : ''}>Süre Uzat</button>
            <button class="btn btn-primary btn-sm" onclick="window.app.handleReturn(${t.id})">İade Et</button>
          ` : `<span style="font-size: 12px; color: var(--text-dim);">${new Date(t.returnDate).toLocaleDateString('tr-TR')}</span>`}
        </td>
      `;
      tbody.appendChild(tr);
    });
  }

  // 2D Salon & Masa Görünümü
  renderDesks() {
    const timeSlot = document.getElementById("desk-timeslot-select").value;
    const today = new Date().toISOString().split('T')[0];

    const desksGrid = document.getElementById("floor-desks-grid");
    const roomsGrid = document.getElementById("floor-rooms-grid");
    desksGrid.innerHTML = "";
    roomsGrid.innerHTML = "";

    // 16 Bireysel Masa
    for (let i = 1; i <= 16; i++) {
      const deskCode = `D-${100 + i}`;
      const isOccupied = this.db.deskBookings.some(b => b.deskCode === deskCode && b.timeSlot === timeSlot && b.date === today && b.status === "CONFIRMED");
      const isSelected = this.selectedDeskCode === deskCode;

      const slot = document.createElement("div");
      slot.className = `desk-slot ${isOccupied ? 'occupied' : (isSelected ? 'selected' : 'free')}`;
      slot.innerHTML = `
        <div>🖥️</div>
        <div class="desk-name">${deskCode}</div>
        <div class="desk-features">${i % 2 === 0 ? 'Prizli' : 'Pencere Kenarı'}</div>
      `;

      if (!isOccupied) {
        slot.addEventListener("click", () => {
          this.selectedDeskCode = deskCode;
          document.getElementById("selected-desk-label").textContent = `Seçilen Masa: ${deskCode} (${timeSlot})`;
          document.getElementById("btn-confirm-desk-booking").disabled = false;
          this.renderDesks();
        });
      }

      desksGrid.appendChild(slot);
    }

    // 4 Grup Çalışma Odası
    ["ROOM-A", "ROOM-B", "ROOM-C", "ROOM-D"].forEach(roomCode => {
      const isOccupied = this.db.deskBookings.some(b => b.deskCode === roomCode && b.timeSlot === timeSlot && b.date === today && b.status === "CONFIRMED");
      const isSelected = this.selectedDeskCode === roomCode;

      const slot = document.createElement("div");
      slot.className = `desk-slot ${isOccupied ? 'occupied' : (isSelected ? 'selected' : 'free')}`;
      slot.style.minHeight = "90px";
      slot.innerHTML = `
        <div style="font-size: 20px;">👥</div>
        <div class="desk-name">${roomCode}</div>
        <div class="desk-features">Akıllı Tahta & 6 Kişilik</div>
      `;

      if (!isOccupied) {
        slot.addEventListener("click", () => {
          this.selectedDeskCode = roomCode;
          document.getElementById("selected-desk-label").textContent = `Seçilen Grup Odası: ${roomCode} (${timeSlot})`;
          document.getElementById("btn-confirm-desk-booking").disabled = false;
          this.renderDesks();
        });
      }

      roomsGrid.appendChild(slot);
    });
  }

  // Cüzdan & Ceza Görünümü
  renderWallet() {
    const user = this.db.currentUser;
    document.getElementById("card-user-name").textContent = user.name;
    document.getElementById("card-user-role").textContent = user.role === "Ogrenci" ? `Öğrenci No: ${user.studentNo}` : (user.role === "Akademisyen" ? user.title : user.employeeId);
    document.getElementById("card-barcode-text").textContent = user.barcode;

    document.getElementById("wallet-balance-big").textContent = `${user.walletBalance.toFixed(2)} TL`;
    document.getElementById("wallet-debt-big").textContent = `${user.fineDebt.toFixed(2)} TL`;

    const userFines = this.db.fines.filter(f => f.userId === user.id);
    const tbody = document.getElementById("fines-table-tbody");
    tbody.innerHTML = "";

    if (userFines.length === 0) {
      tbody.innerHTML = `<tr><td colspan="7" style="text-align: center; color: var(--text-dim); padding: 24px;">Kayıtlı herhangi bir ceza borcunuz bulunmamaktadır. Tebrikler!</td></tr>`;
      return;
    }

    userFines.forEach(f => {
      const tr = document.createElement("tr");
      tr.innerHTML = `
        <td>#${f.id}</td>
        <td>İşlem #${f.transactionId}</td>
        <td>${f.overdueDays} Gün</td>
        <td><strong>${f.amount.toFixed(2)} TL</strong></td>
        <td>${new Date(f.createdDate).toLocaleDateString('tr-TR')}</td>
        <td>
          <span class="status-pill ${f.isPaid ? 'returned' : 'overdue'}">${f.isPaid ? 'Ödendi' : 'Ödenmedi'}</span>
        </td>
        <td>
          ${!f.isPaid ? `
            <button class="btn btn-success btn-sm" onclick="window.app.handlePayFine(${f.id})">💳 Online Öde</button>
          ` : `<span style="font-size: 11px; color: var(--text-dim); font-family: monospace;">${f.receiptCode}</span>`}
        </td>
      `;
      tbody.appendChild(tr);
    });
  }

  // Görevli Barkod Masası Görünümü
  renderLibrarianStation() {
    const userSelect = document.getElementById("barcode-user-select");
    const matSelect = document.getElementById("barcode-material-select");
    const returnSelect = document.getElementById("barcode-return-select");

    userSelect.innerHTML = this.db.users.map(u => `<option value="${u.id}">${u.name} (${u.role} - ${u.barcode})</option>`).join("");
    matSelect.innerHTML = this.db.materials.filter(m => m.isAvailable()).map(m => `<option value="${m.id}">${m.title} [Stok: ${m.availableStock}]</option>`).join("");

    const activeLoans = this.db.transactions.filter(t => t.status !== "RETURNED");
    if (activeLoans.length === 0) {
      returnSelect.innerHTML = `<option value="">Aktif ödünç bulunmuyor</option>`;
    } else {
      returnSelect.innerHTML = activeLoans.map(t => {
        const u = this.db.users.find(usr => usr.id === t.userId);
        const m = this.db.materials.find(mat => mat.id === t.materialId);
        return `<option value="${t.id}">İşlem #${t.id} - ${u ? u.name : 'Üye'} : ${m ? m.title : 'Kitap'}</option>`;
      }).join("");
    }
  }

  // Yönetici Analitiği
  renderAnalytics() {
    const stats = this.service.getSystemAnalytics();
    document.getElementById("admin-kpi-titles").textContent = stats.totalTitles;
    document.getElementById("admin-kpi-copies").textContent = stats.totalPhysicalCopies;
    document.getElementById("admin-kpi-overdue").textContent = stats.overdueCount;
    document.getElementById("admin-kpi-fines").textContent = `${stats.totalFinesCollected.toFixed(2)} TL`;
  }

  // İşlem Tetikleyicileri (Global Erişilebilir)
  handleRenew(transactionId) {
    try {
      const newDate = this.service.renewBorrow(transactionId);
      this.showToast(`Süre başarıyla 7 gün uzatıldı! Yeni son teslim: ${new Date(newDate).toLocaleDateString('tr-TR')}`, "success");
      this.renderAll();
    } catch (err) {
      this.showToast(err.message, "error");
    }
  }

  handleReturn(transactionId) {
    try {
      const res = this.service.returnMaterial(transactionId);
      let msg = `'${res.material.title}' başarıyla iade edildi.`;
      if (res.overdueDays > 0) {
        msg += ` (${res.overdueDays} gün gecikme: ${res.fineRecord.amount.toFixed(2)} TL ceza yansıtıldı)`;
      }
      if (res.notifications && res.notifications.length > 0) {
        msg += ` [Observer]: Rezerve eden sıradaki kullanıcıya bildirim gitti!`;
      }
      this.showToast(msg, res.overdueDays > 0 ? "warning" : "success");
      this.renderAll();
    } catch (err) {
      this.showToast(err.message, "error");
    }
  }

  handlePayFine(fineId) {
    try {
      const res = this.service.payFine(fineId, this.db.currentUser.id);
      this.showToast(`Ceza tahsil edildi! Dekont: ${res.receiptCode}. Kalan bakiye: ${res.remainingBalance.toFixed(2)} TL`, "success");
      this.renderAll();
    } catch (err) {
      this.showToast(err.message, "error");
    }
  }

  // Toast Bildirim Sistemi
  showToast(message, type = "info") {
    const toast = document.createElement("div");
    toast.className = `toast ${type}`;

    let icon = "ℹ️";
    if (type === "success") icon = "✅";
    else if (type === "error") icon = "❌";
    else if (type === "warning") icon = "⚠️";

    toast.innerHTML = `<span>${icon}</span><span>${message}</span>`;
    this.toastContainer.appendChild(toast);

    setTimeout(() => {
      toast.style.opacity = "0";
      toast.style.transform = "translateX(50px)";
      setTimeout(() => toast.remove(), 300);
    }, 4500);
  }
}

// Uygulamayı başlat
window.addEventListener("DOMContentLoaded", () => {
  window.app = new AppController();
});
