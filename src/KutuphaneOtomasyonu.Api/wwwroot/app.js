// Kütüphane Otomasyonu — tarayıcı arayüzü.
// Bağımlılıksız tek sayfa uygulaması; tüm veriler /api uç noktalarından gelir.
// Kullanıcı verisi DOM'a yalnızca metin olarak yazılır (innerHTML kullanılmaz).
"use strict";

// ---------- Yardımcılar ----------

/** Element oluşturur: h("a", { href: "#", onclick: f }, "metin", altElement) */
function h(etiket, ozellikler, ...cocuklar) {
  const el = document.createElement(etiket);
  for (const [ad, deger] of Object.entries(ozellikler || {})) {
    if (deger === null || deger === undefined || deger === false) continue;
    if (ad.startsWith("on") && typeof deger === "function") el.addEventListener(ad.slice(2), deger);
    else if (ad === "class") el.className = deger;
    else if (ad === "value") el.value = deger;
    else if (deger === true) el.setAttribute(ad, "");
    else el.setAttribute(ad, String(deger));
  }
  for (const cocuk of cocuklar.flat()) {
    if (cocuk === null || cocuk === undefined || cocuk === false) continue;
    el.append(cocuk instanceof Node ? cocuk : document.createTextNode(String(cocuk)));
  }
  return el;
}

/** İçeriği değiştirir. replaceChildren null/false değerleri metne çevirdiği için önce ayıklanır. */
function doldur(el, ...cocuklar) {
  el.replaceChildren(...cocuklar.flat().filter((c) => c !== null && c !== undefined && c !== false));
}

const tarih = (d) => (d ? new Date(d).toLocaleDateString("tr-TR") : "—");
const para = (n) => Number(n || 0).toLocaleString("tr-TR", { style: "currency", currency: "TRY" });

const ADLAR = {
  rol: { YardimciPersonel: "Yardımcı Personel", Kutuphaneci: "Kütüphaneci", Admin: "Yönetici" },
  uyeTipi: { Ogrenci: "Öğrenci", Akademisyen: "Akademisyen", Personel: "Personel", Misafir: "Misafir" },
  uyeDurumu: { Aktif: "Aktif", Pasif: "Pasif", Askida: "Askıda", Yasakli: "Yasaklı" },
  odunc: { Aktif: "Aktif", Iade: "İade edildi", Kayip: "Kayıp", Hasarli: "Hasarlı" },
  rezervasyon: { Bekliyor: "Bekliyor", Onaylandi: "Ayrıldı", Iptal: "İptal", Tamamlandi: "Tamamlandı", Suresi_Doldu: "Süresi doldu" },
  kitapDurumu: { Mukemmel: "Mükemmel", Iyi: "İyi", Orta: "Orta", Hasarli: "Hasarlı", Kayip: "Kayıp" },
};

function rozet(metin, tur) {
  return h("span", { class: "rozet " + (tur || "") }, metin);
}

function oduncRozeti(o) {
  if (o.durumu === "Aktif" && o.gecikmisMi) return rozet(`${o.gecikmeGunSayisi} gün gecikmiş`, "hata");
  const tur = { Aktif: "basari", Iade: "", Kayip: "hata", Hasarli: "uyari" }[o.durumu];
  return rozet(ADLAR.odunc[o.durumu] || o.durumu, tur);
}

function bildir(metin, tur = "basari") {
  const kutu = h("div", { class: "mesaj " + tur }, metin);
  document.getElementById("bildirim").append(kutu);
  setTimeout(() => kutu.remove(), tur === "hata" ? 7000 : 4000);
}

function tablo(basliklar, satirlar, bosMetin = "Kayıt yok.") {
  if (!satirlar.length) return h("p", { class: "bos" }, bosMetin);
  return h("div", { class: "tablo-kap" },
    h("table", {},
      h("thead", {}, h("tr", {}, basliklar.map((b) => h("th", { class: b.startsWith(">") ? "sag" : null }, b.replace(/^>/, ""))))),
      h("tbody", {}, satirlar)));
}

// ---------- API ----------

class ApiHatasi extends Error {
  constructor(durum, mesaj) {
    super(mesaj);
    this.durum = durum;
  }
}

async function api(yontem, yol, govde) {
  const secenekler = { method: yontem, headers: { Accept: "application/json" }, credentials: "same-origin" };
  if (govde !== undefined) {
    secenekler.headers["Content-Type"] = "application/json";
    secenekler.body = JSON.stringify(govde);
  }
  const yanit = await fetch(yol, secenekler);
  if (yanit.status === 204) return null;
  const metin = await yanit.text();
  const veri = metin ? JSON.parse(metin) : null;
  if (!yanit.ok) {
    if (yanit.status === 401 && oturum && !yol.startsWith("/api/oturum")) {
      oturum = null;
      menuyuCiz();
      location.hash = "#/giris";
    }
    const mesaj = (veri && (veri.detail || veri.title)) ||
      { 401: "Oturum açmanız gerekiyor.", 403: "Bu işlem için yetkiniz yok.", 429: "Çok fazla deneme. Lütfen biraz bekleyin." }[yanit.status] ||
      `İstek başarısız (${yanit.status}).`;
    throw new ApiHatasi(yanit.status, mesaj);
  }
  return veri;
}

/** Form gönderimini kilitler, hatayı formun içindeki mesaj alanında gösterir. */
function formIsle(form, isle) {
  const mesajAlani = h("div", { class: "gizli" });
  form.append(mesajAlani);
  form.addEventListener("submit", async (olay) => {
    olay.preventDefault();
    const dugme = form.querySelector("button[type=submit]");
    mesajAlani.className = "gizli";
    if (dugme) dugme.disabled = true;
    try {
      const sonuc = await isle(new FormData(form));
      if (sonuc) {
        mesajAlani.className = "mesaj basari";
        mesajAlani.textContent = sonuc;
      }
    } catch (hata) {
      mesajAlani.className = "mesaj hata";
      mesajAlani.textContent = hata.message;
    } finally {
      if (dugme) dugme.disabled = false;
    }
  });
  return form;
}

// ---------- Oturum ve menü ----------

let oturum = null;
const personelMi = () => oturum && !oturum.sifreDegistirmeli;
const kutuphaneciMi = () => personelMi() && (oturum.rol === "Kutuphaneci" || oturum.rol === "Admin");

function menuyuCiz() {
  const nav = document.getElementById("gezinti");
  const baglantilar = [["#/katalog", "Katalog"]];
  if (personelMi()) {
    baglantilar.push(["#/pano", "Pano"], ["#/banko", "Banko"], ["#/oduncler", "Ödünçler"],
      ["#/uyeler", "Üyeler"], ["#/rezervasyonlar", "Rezervasyonlar"]);
  }
  const rota = (location.hash.split("/")[1] || "katalog").split("?")[0];
  doldur(nav, ...[
    ...baglantilar.map(([adres, ad]) =>
      h("a", { href: adres, class: adres === "#/" + rota ? "aktif" : null }, ad)),
    oturum
      ? h("span", { class: "kullanici" }, `${oturum.adSoyad} · ${ADLAR.rol[oturum.rol] || oturum.rol}`)
      : null,
    oturum ? h("a", { href: "#/sifre", class: rota === "sifre" ? "aktif" : null }, "Şifre") : null,
    oturum
      ? h("button", { type: "button", onclick: cikisYap }, "Çıkış")
      : h("a", { href: "#/giris", class: rota === "giris" ? "aktif" : null }, "Personel girişi"),
  ]);
}

async function cikisYap() {
  try { await api("POST", "/api/oturum/cikis"); } catch { /* oturum zaten kapalı olabilir */ }
  oturum = null;
  bildir("Oturum kapatıldı.", "bilgi");
  location.hash = "#/katalog";
  menuyuCiz();
}

// ---------- Sayfalar ----------

const sayfalar = {};

sayfalar.katalog = async (alan) => {
  const parametreler = new URLSearchParams(location.hash.split("?")[1] || "");
  const arama = parametreler.get("q") || "";
  const kategori = parametreler.get("kategori") || "";
  const mevcut = parametreler.get("mevcut") === "1";
  const sayfa = Number(parametreler.get("sayfa") || 1);

  const [kategoriler, sonuc] = await Promise.all([
    api("GET", "/api/katalog/kategoriler"),
    api("GET", `/api/katalog?${new URLSearchParams({ arama, kategori, sadeceMevcut: mevcut, sayfa, boyut: 24 })}`),
  ]);

  const gonder = (yeniSayfa = 1) => {
    const p = new URLSearchParams();
    if (aramaKutusu.value.trim()) p.set("q", aramaKutusu.value.trim());
    if (kategoriSecimi.value) p.set("kategori", kategoriSecimi.value);
    if (mevcutKutusu.checked) p.set("mevcut", "1");
    if (yeniSayfa > 1) p.set("sayfa", yeniSayfa);
    location.hash = "#/katalog" + (p.toString() ? "?" + p : "");
  };

  const aramaKutusu = h("input", { type: "search", placeholder: "Başlık, yazar, ISBN veya yayınevi…", value: arama, "aria-label": "Katalogda ara" });
  const kategoriSecimi = h("select", { "aria-label": "Kategori" },
    h("option", { value: "" }, "Tüm kategoriler"),
    kategoriler.map((k) => h("option", { value: k, selected: k === kategori }, k)));
  const mevcutKutusu = h("input", { type: "checkbox", checked: mevcut });
  const form = h("form", { class: "arama-cubugu", onsubmit: (e) => { e.preventDefault(); gonder(); } },
    aramaKutusu, kategoriSecimi,
    h("label", { class: "onay" }, mevcutKutusu, "Yalnızca rafta olanlar"),
    h("button", { class: "dugme", type: "submit" }, "Ara"));

  const toplamSayfa = Math.max(1, Math.ceil(sonuc.toplam / sonuc.boyut));
  doldur(alan,
    h("div", { class: "sayfa-baslik" },
      h("div", {}, h("h1", {}, "Katalog"), h("p", { class: "alt-baslik" }, `${sonuc.toplam} kitap bulundu`))),
    form,
    sonuc.kayitlar.length
      ? h("div", { class: "izgara izgara-kitap" }, sonuc.kayitlar.map(kitapKarti))
      : h("p", { class: "bos" }, "Aramanızla eşleşen kitap yok."),
    toplamSayfa > 1
      ? h("div", { class: "sayfalama" },
          h("button", { class: "dugme ikincil kucuk", disabled: sayfa <= 1, onclick: () => gonder(sayfa - 1) }, "‹ Önceki"),
          `${sayfa} / ${toplamSayfa}`,
          h("button", { class: "dugme ikincil kucuk", disabled: sayfa >= toplamSayfa, onclick: () => gonder(sayfa + 1) }, "Sonraki ›"))
      : null);
};

function kitapKarti(k) {
  const mevcut = k.mevcutAdet > 0;
  return h("a", { class: "kart kitap-kart", href: `#/kitap/${k.id}` },
    h("h3", {}, k.baslik),
    h("span", { class: "yazar" }, k.yazar),
    h("div", {}, rozet(mevcut ? `${k.mevcutAdet}/${k.toplamAdet} rafta` : "Tümü ödünçte", mevcut ? "basari" : "uyari")),
    h("span", { class: "meta" }, [k.kategori, k.yayinYili || null, k.rafKonumu ? `Raf ${k.rafKonumu}` : null].filter(Boolean).join(" · ")));
}

sayfalar.kitap = async (alan, id) => {
  const { kitap, nushalar } = await api("GET", `/api/katalog/${encodeURIComponent(id)}`);
  const bilgiler = [
    ["Yazar", kitap.yazar], ["ISBN", kitap.isbn], ["Yayınevi", kitap.yayinevi], ["Yayın yılı", kitap.yayinYili || "—"],
    ["Kategori", [kitap.kategori, kitap.altKategori].filter(Boolean).join(" / ")], ["Dil", kitap.dil],
    ["Sayfa", kitap.sayfaSayisi || "—"], ["Raf", kitap.rafKonumu || "—"],
  ];

  const nushaSatirlari = nushalar.map((n) => h("tr", {},
    h("td", {}, n.barkod),
    h("td", {}, rozet(n.durumMetni, { Rafta: "basari", Oduncte: "vurgu", Hasarli: "uyari", Kayip: "hata" }[n.durum])),
    h("td", {}, n.rafKonumu || "—")));

  doldur(alan,
    h("div", { class: "sayfa-baslik" },
      h("div", {}, h("h1", {}, kitap.baslik), h("p", { class: "alt-baslik" }, kitap.yazar)),
      h("a", { class: "dugme ikincil", href: "#/katalog" }, "‹ Kataloğa dön")),
    h("div", { class: "izgara izgara-2" },
      h("section", { class: "kart" },
        h("h2", {}, "Kitap bilgileri"),
        h("dl", { class: "tanim" }, bilgiler.flatMap(([a, d]) => [h("dt", {}, a), h("dd", {}, d)])),
        kitap.aciklama ? h("p", { class: "alt-baslik" }, kitap.aciklama) : null),
      h("section", { class: "kart" },
        h("h2", {}, `Nüshalar — ${kitap.mevcutAdet}/${kitap.toplamAdet} rafta`),
        tablo(["Barkod", "Durum", "Raf"], nushaSatirlari, "Nüsha kaydı yok."),
        personelMi() ? rezervasyonFormu(kitap) : null)));
};

function rezervasyonFormu(kitap) {
  const form = h("form", {},
    h("h2", {}, "Rezervasyon ekle"),
    h("label", {}, "Üye (öğrenci no, e-posta veya ad)", h("input", { name: "uye", required: true, autocomplete: "off" })),
    h("div", {}, h("button", { class: "dugme", type: "submit" }, "Sıraya ekle")));
  return formIsle(form, async (veri) => {
    const uyeId = await tekUyeBul(veri.get("uye"));
    const r = await api("POST", "/api/rezervasyonlar", { kitapId: kitap.id, uyeId });
    form.reset();
    return `${r.uyeAdSoyad} sıraya eklendi (${r.siraNumarasi}. sıra).`;
  });
}

async function tekUyeBul(giris) {
  const sonuc = await api("GET", `/api/uyeler?${new URLSearchParams({ arama: giris.trim(), boyut: 5 })}`);
  if (sonuc.toplam === 0) throw new Error("Bu bilgiyle üye bulunamadı.");
  if (sonuc.toplam > 1) throw new Error(`${sonuc.toplam} üye eşleşti; öğrenci numarası gibi daha belirgin bir bilgi girin.`);
  return sonuc.kayitlar[0].id;
}

sayfalar.giris = async (alan) => {
  const form = h("form", { class: "kart" },
    h("h1", {}, "Personel girişi"),
    h("p", { class: "alt-baslik" }, "Katalog herkese açıktır; ödünç, üye ve rezervasyon işlemleri için giriş yapın."),
    h("label", {}, "Kullanıcı adı", h("input", { name: "kullaniciAdi", required: true, autocomplete: "username" })),
    h("label", {}, "Şifre", h("input", { name: "sifre", type: "password", required: true, autocomplete: "current-password" })),
    h("button", { class: "dugme", type: "submit" }, "Giriş yap"));
  formIsle(form, async (veri) => {
    oturum = await api("POST", "/api/oturum/giris", { kullaniciAdi: veri.get("kullaniciAdi"), sifre: veri.get("sifre") });
    menuyuCiz();
    if (oturum.sifreDegistirmeli) {
      bildir("Güvenliğiniz için şifrenizi değiştirmeniz gerekiyor.", "bilgi");
      location.hash = "#/sifre";
    } else {
      bildir(`Hoş geldiniz, ${oturum.adSoyad}.`);
      location.hash = "#/pano";
    }
  });
  doldur(alan, h("div", { class: "giris-kap" }, form));
  form.querySelector("input").focus();
};

sayfalar.sifre = async (alan) => {
  if (!oturum) { location.hash = "#/giris"; return; }
  const form = h("form", { class: "kart" },
    h("h1", {}, "Şifre değiştir"),
    oturum.sifreDegistirmeli
      ? h("p", { class: "mesaj bilgi" }, "Varsayılan veya kısa bir şifreyle giriş yaptınız. Devam etmek için yeni bir şifre belirleyin.")
      : null,
    h("label", {}, "Mevcut şifre", h("input", { name: "mevcut", type: "password", required: true, autocomplete: "current-password" })),
    h("label", {}, "Yeni şifre (en az 8 karakter)", h("input", { name: "yeni", type: "password", required: true, minlength: 8, autocomplete: "new-password" })),
    h("label", {}, "Yeni şifre (tekrar)", h("input", { name: "tekrar", type: "password", required: true, minlength: 8, autocomplete: "new-password" })),
    h("button", { class: "dugme", type: "submit" }, "Şifreyi değiştir"));
  formIsle(form, async (veri) => {
    if (veri.get("yeni") !== veri.get("tekrar")) throw new Error("Yeni şifreler eşleşmiyor.");
    oturum = await api("POST", "/api/oturum/sifre", { mevcutSifre: veri.get("mevcut"), yeniSifre: veri.get("yeni") });
    menuyuCiz();
    bildir("Şifreniz değiştirildi.");
    location.hash = "#/pano";
  });
  doldur(alan, h("div", { class: "giris-kap" }, form));
};

sayfalar.pano = async (alan) => {
  const p = await api("GET", "/api/pano");
  const sayi = (deger, etiket, tur) => h("div", { class: "kart sayi-kart " + (tur || "") },
    h("div", { class: "deger" }, deger), h("div", { class: "etiket" }, etiket));

  doldur(alan,
    h("div", { class: "sayfa-baslik" },
      h("div", {}, h("h1", {}, "Pano"), h("p", { class: "alt-baslik" }, new Date().toLocaleDateString("tr-TR", { dateStyle: "full" }))),
      h("div", { class: "dugmeler" },
        h("a", { class: "dugme", href: "#/banko" }, "Barkod bankosu"),
        kutuphaneciMi() ? h("a", { class: "dugme ikincil", href: "#/kitap-ekle" }, "Kitap ekle") : null,
        kutuphaneciMi() ? h("a", { class: "dugme ikincil", href: "#/uye-ekle" }, "Üye ekle") : null)),
    h("div", { class: "izgara izgara-sayi" },
      sayi(p.aktifOdunc, "Aktif ödünç"),
      sayi(p.gecikmis, "Gecikmiş", p.gecikmis ? "hata" : ""),
      sayi(`${p.mevcutKopya}/${p.toplamKopya}`, "Rafta / toplam nüsha"),
      sayi(p.toplamKitap, "Kitap"),
      sayi(p.aktifUye, "Aktif üye"),
      sayi(p.cezaliUye, "Cezalı üye", p.cezaliUye ? "uyari" : ""),
      sayi(`${p.bugunOdunc} / ${p.bugunIade}`, "Bugün ödünç / iade"),
      sayi(para(p.odenmemisCeza), "Ödenmemiş ceza", p.odenmemisCeza > 0 ? "uyari" : "")),
    h("section", { class: "kart" },
      h("h2", {}, "İade tarihi en yakın ödünçler"),
      oduncTablosu(p.yaklasanIadeler, false)));
};

function oduncTablosu(oduncler, islemli, yenile) {
  const satirlar = oduncler.map((o) => h("tr", {},
    h("td", {}, h("a", { href: `#/kitap/${o.kitapId}` }, o.kitapBaslik), o.nushaBarkod ? h("div", { class: "alt-baslik" }, o.nushaBarkod) : null),
    h("td", {}, o.uyeAdSoyad ? h("a", { href: `#/uye/${o.uyeId}` }, o.uyeAdSoyad) : "—"),
    h("td", {}, tarih(o.oduncTarihi)),
    h("td", {}, tarih(o.planlananIadeTarihi)),
    h("td", {}, oduncRozeti(o)),
    islemli
      ? h("td", { class: "sag" }, o.durumu === "Aktif"
          ? h("div", { class: "dugmeler" },
              h("button", { class: "dugme ikincil kucuk", onclick: () => oduncIslemi(`/api/oduncler/${o.id}/uzat`, undefined, yenile),
                disabled: o.gecikmisMi || o.uzatmaSayisi >= o.maxUzatmaSayisi, title: `Uzatma ${o.uzatmaSayisi}/${o.maxUzatmaSayisi}` }, "Uzat"),
              h("button", { class: "dugme kucuk", onclick: () => oduncIslemi(`/api/oduncler/${o.id}/iade`, { durum: "Iyi" }, yenile) }, "İade al"))
          : null)
      : null));
  return tablo(["Kitap", "Üye", "Veriliş", "İade tarihi", "Durum", ...(islemli ? [">İşlem"] : [])], satirlar, "Ödünç kaydı yok.");
}

async function oduncIslemi(yol, govde, yenile) {
  try {
    const sonuc = await api("POST", yol, govde === undefined ? {} : govde);
    bildir(sonuc.mesaj || "İşlem tamamlandı.");
    if (yenile) await yenile();
  } catch (hata) {
    bildir(hata.message, "hata");
  }
}

sayfalar.oduncler = async (alan) => {
  let durum = "aktif";
  const liste = h("div", {});
  const sekme = (ad, deger) => h("button", {
    type: "button", class: durum === deger ? "aktif" : null,
    onclick: async () => { durum = deger; await yenile(); },
  }, ad);
  const sekmeler = h("div", { class: "sekmeler" });

  async function yenile() {
    doldur(sekmeler, sekme("Aktif", "aktif"), sekme("Gecikmiş", "gecikmis"));
    const veriler = await api("GET", `/api/oduncler?durum=${durum}`);
    doldur(liste, oduncTablosu(veriler, true, yenile));
  }

  doldur(alan,
    h("div", { class: "sayfa-baslik" }, h("div", {}, h("h1", {}, "Ödünçler"),
      h("p", { class: "alt-baslik" }, "Hasarlı veya kayıp iadeler için Barkod bankosunu kullanın."))),
    sekmeler, liste);
  await yenile();
};

sayfalar.banko = async (alan) => {
  const odunc = h("form", { class: "kart" },
    h("h2", {}, "Ödünç ver"),
    h("label", {}, "Nüsha barkodu", h("input", { name: "barkod", required: true, autocomplete: "off", placeholder: "KB0000010001" })),
    h("label", {}, "Üye (T.C./YKN, pasaport, öğrenci no veya ad)", h("input", { name: "uye", required: true, autocomplete: "off" })),
    h("div", {}, h("button", { class: "dugme", type: "submit" }, "Ödünç ver")));
  formIsle(odunc, async (veri) => {
    const o = await api("POST", "/api/oduncler", { nushaBarkodu: veri.get("barkod"), uye: veri.get("uye") });
    odunc.reset();
    odunc.querySelector("input").focus();
    return `"${o.kitapBaslik}" ${o.uyeAdSoyad} adına verildi. İade tarihi: ${tarih(o.planlananIadeTarihi)}.`;
  });

  const iade = h("form", { class: "kart" },
    h("h2", {}, "İade al"),
    h("label", {}, "Nüsha barkodu", h("input", { name: "barkod", required: true, autocomplete: "off" })),
    h("label", {}, "Kitabın durumu",
      h("select", { name: "durum" }, Object.entries(ADLAR.kitapDurumu).map(([d, ad]) => h("option", { value: d, selected: d === "Iyi" }, ad)))),
    h("label", {}, "Not (isteğe bağlı)", h("input", { name: "notlar", autocomplete: "off" })),
    h("div", {}, h("button", { class: "dugme", type: "submit" }, "İade al")));
  formIsle(iade, async (veri) => {
    const s = await api("POST", "/api/oduncler/iade-barkod",
      { barkod: veri.get("barkod"), durum: veri.get("durum"), notlar: veri.get("notlar") || null });
    iade.reset();
    iade.querySelector("input").focus();
    return `"${s.odunc.kitapBaslik}" — ${s.mesaj}`;
  });

  doldur(alan,
    h("div", { class: "sayfa-baslik" }, h("div", {}, h("h1", {}, "Barkod bankosu"),
      h("p", { class: "alt-baslik" }, "Barkod okuyucu klavye gibi çalışır: alana odaklanıp okutun."))),
    h("div", { class: "izgara izgara-2" }, odunc, iade));
  odunc.querySelector("input").focus();
};

sayfalar.uyeler = async (alan) => {
  const parametreler = new URLSearchParams(location.hash.split("?")[1] || "");
  const arama = parametreler.get("q") || "";
  const sonuc = await api("GET", `/api/uyeler?${new URLSearchParams({ arama, boyut: 50 })}`);
  const kutu = h("input", { type: "search", value: arama, placeholder: "Ad, e-posta, öğrenci no…", "aria-label": "Üye ara" });

  const satirlar = sonuc.kayitlar.map((u) => h("tr", { class: "satir-link", onclick: () => { location.hash = `#/uye/${u.id}`; } },
    h("td", {}, h("a", { href: `#/uye/${u.id}` }, u.adSoyad)),
    h("td", {}, u.ogrenciNo || "—"),
    h("td", {}, ADLAR.uyeTipi[u.uyeTipi] || u.uyeTipi),
    h("td", {}, rozet(ADLAR.uyeDurumu[u.durumu] || u.durumu, u.durumu === "Aktif" ? "basari" : "uyari")),
    h("td", { class: "sag" }, u.kalanCeza > 0 ? rozet(para(u.kalanCeza), "hata") : "—")));

  doldur(alan,
    h("div", { class: "sayfa-baslik" },
      h("div", {}, h("h1", {}, "Üyeler"), h("p", { class: "alt-baslik" }, `${sonuc.toplam} üye`)),
      kutuphaneciMi() ? h("a", { class: "dugme", href: "#/uye-ekle" }, "Üye ekle") : null),
    h("form", { class: "arama-cubugu", onsubmit: (e) => {
      e.preventDefault();
      location.hash = "#/uyeler" + (kutu.value.trim() ? "?q=" + encodeURIComponent(kutu.value.trim()) : "");
    } }, kutu, h("span"), h("span"), h("button", { class: "dugme", type: "submit" }, "Ara")),
    tablo(["Ad Soyad", "Öğrenci no", "Tip", "Durum", ">Borç"], satirlar, "Üye bulunamadı."),
    sonuc.toplam > sonuc.kayitlar.length ? h("p", { class: "alt-baslik" }, `İlk ${sonuc.kayitlar.length} kayıt gösteriliyor; aramayı daraltın.`) : null);
};

sayfalar.uye = async (alan, id) => {
  const u = await api("GET", `/api/uyeler/${encodeURIComponent(id)}`);
  const bilgiler = [
    ["Tip", ADLAR.uyeTipi[u.uyeTipi]], ["Durum", ADLAR.uyeDurumu[u.durumu]], ["E-posta", u.email || "—"],
    ["Telefon", u.telefon || "—"], ["T.C./YKN", u.tcKimlikNo || "—"], ["Pasaport", u.pasaportNo || "—"],
    ["Öğrenci no", u.ogrenciNo || "—"], ["Bölüm", [u.bolum, u.fakulte].filter(Boolean).join(", ") || "—"],
    ["Ödünç hakkı", `${u.maxOduncAdet} kitap / ${u.maxOduncSuresi} gün`], ["Üyelik bitişi", tarih(u.uyelikBitisTarihi)],
    ["Kalan borç", para(u.kalanCeza)],
  ];

  let cezaFormu = null;
  if (kutuphaneciMi() && u.kalanCeza > 0) {
    cezaFormu = h("form", {},
      h("h2", {}, "Ceza ödemesi al"),
      h("label", {}, "Tutar (₺)", h("input", { name: "tutar", type: "number", step: "0.01", min: "0.01", max: u.kalanCeza, value: u.kalanCeza, required: true })),
      h("div", {}, h("button", { class: "dugme", type: "submit" }, "Ödemeyi kaydet")));
    formIsle(cezaFormu, async (veri) => {
      await api("POST", `/api/uyeler/${u.id}/ceza-ode`, { tutar: Number(veri.get("tutar")) });
      bildir("Ödeme kaydedildi.");
      await sayfalar.uye(alan, id);
    });
  }

  const rezervasyonSatirlari = u.rezervasyonlar.map((r) => h("tr", {},
    h("td", {}, h("a", { href: `#/kitap/${r.kitapId}` }, r.kitapBaslik)),
    h("td", {}, tarih(r.rezervasyonTarihi)),
    h("td", {}, rozet(ADLAR.rezervasyon[r.durumu] || r.durumu))));

  doldur(alan,
    h("div", { class: "sayfa-baslik" },
      h("div", {}, h("h1", {}, `${u.ad} ${u.soyad}`.trim() || "Anonim üye"),
        h("p", { class: "alt-baslik" }, u.anonimMi ? "Bu üye KVKK talebiyle anonimleştirildi." : `Üye #${u.id}`)),
      h("a", { class: "dugme ikincil", href: "#/uyeler" }, "‹ Üyeler")),
    h("div", { class: "izgara izgara-2" },
      h("section", { class: "kart" },
        h("h2", {}, "Üye bilgileri"),
        u.kisiselVeriMaskeli ? h("p", { class: "mesaj bilgi" }, "Kimlik ve telefon bilgileri rolünüz gereği maskelenmiştir (KVKK).") : null,
        h("dl", { class: "tanim" }, bilgiler.flatMap(([a, d]) => [h("dt", {}, a), h("dd", {}, d)])),
        cezaFormu),
      h("section", { class: "kart" }, h("h2", {}, "Rezervasyonlar"), tablo(["Kitap", "Tarih", "Durum"], rezervasyonSatirlari, "Rezervasyon yok."))),
    h("section", { class: "kart" }, h("h2", {}, "Ödünç geçmişi (son 20)"), oduncTablosu(u.oduncler, false)));
};

sayfalar.rezervasyonlar = async (alan) => {
  let durum = "Bekliyor";
  const liste = h("div", {});
  const sekmeler = h("div", { class: "sekmeler" });

  async function yenile() {
    doldur(sekmeler, ...["Bekliyor", "Onaylandi", "Tamamlandi", "Iptal", "Suresi_Doldu"].map((d) =>
      h("button", { type: "button", class: d === durum ? "aktif" : null, onclick: async () => { durum = d; await yenile(); } },
        ADLAR.rezervasyon[d])));
    const veriler = await api("GET", `/api/rezervasyonlar?durum=${durum}`);
    doldur(liste, tablo(["Kitap", "Üye", "Tarih", "Geçerlilik", "Sıra", ">İşlem"], veriler.map((r) => h("tr", {},
      h("td", {}, h("a", { href: `#/kitap/${r.kitapId}` }, r.kitapBaslik)),
      h("td", {}, h("a", { href: `#/uye/${r.uyeId}` }, r.uyeAdSoyad)),
      h("td", {}, tarih(r.rezervasyonTarihi)),
      h("td", {}, tarih(r.gecerlilikTarihi)),
      h("td", {}, r.siraNumarasi),
      h("td", { class: "sag" }, (r.durumu === "Bekliyor" || r.durumu === "Onaylandi")
        ? h("div", { class: "dugmeler" },
            r.durumu === "Onaylandi"
              ? h("button", { class: "dugme kucuk", onclick: () => oduncIslemi(`/api/rezervasyonlar/${r.id}/odunce-donustur`, undefined, yenile) }, "Ödünç ver")
              : null,
            h("button", { class: "dugme ikincil kucuk", onclick: async () => {
              try { await api("POST", `/api/rezervasyonlar/${r.id}/iptal`, {}); bildir("Rezervasyon iptal edildi."); await yenile(); }
              catch (hata) { bildir(hata.message, "hata"); }
            } }, "İptal"))
        : null))), "Bu durumda rezervasyon yok."));
  }

  doldur(alan,
    h("div", { class: "sayfa-baslik" }, h("div", {}, h("h1", {}, "Rezervasyonlar"),
      h("p", { class: "alt-baslik" }, "Yeni rezervasyon kitabın sayfasından eklenir. \"Ayrıldı\": iade edilen nüsha üyeye ayrılmıştır."))),
    sekmeler, liste);
  await yenile();
};

sayfalar["kitap-ekle"] = async (alan) => {
  if (!kutuphaneciMi()) throw new ApiHatasi(403, "Kitap eklemek için Kütüphaneci yetkisi gerekir.");
  const alanlar = [
    ["baslik", "Başlık", { required: true }], ["yazar", "Yazar", { required: true }], ["isbn", "ISBN (10 veya 13 hane)", {}],
    ["yayinevi", "Yayınevi", {}], ["yayinYili", "Yayın yılı", { type: "number", min: 1450, max: 2100 }],
    ["kategori", "Kategori", { required: true }], ["altKategori", "Alt kategori", {}], ["dil", "Dil", { value: "Türkçe" }],
    ["sayfaSayisi", "Sayfa sayısı", { type: "number", min: 0 }], ["toplamAdet", "Nüsha sayısı", { type: "number", min: 1, value: 1, required: true }],
    ["rafKonumu", "Raf", {}], ["satinAlmaFiyati", "Fiyat (₺)", { type: "number", min: 0, step: "0.01" }],
  ];
  const form = h("form", { class: "kart" },
    h("div", { class: "form-satir" }, alanlar.map(([ad, etiket, ek]) => h("label", {}, etiket, h("input", { name: ad, ...ek })))),
    h("label", {}, "Açıklama", h("textarea", { name: "aciklama", rows: 3 })),
    h("div", {}, h("button", { class: "dugme", type: "submit" }, "Kitabı ekle")));
  formIsle(form, async (veri) => {
    const govde = Object.fromEntries(veri.entries());
    for (const sayisal of ["yayinYili", "sayfaSayisi", "toplamAdet", "satinAlmaFiyati"]) govde[sayisal] = Number(govde[sayisal] || 0);
    const k = await api("POST", "/api/kitaplar", govde);
    bildir(`"${k.baslik}" ${k.toplamAdet} nüsha ile eklendi.`);
    location.hash = `#/kitap/${k.id}`;
  });
  doldur(alan, h("div", { class: "sayfa-baslik" }, h("h1", {}, "Kitap ekle")), form);
};

sayfalar["uye-ekle"] = async (alan) => {
  if (!kutuphaneciMi()) throw new ApiHatasi(403, "Üye eklemek için Kütüphaneci yetkisi gerekir.");
  const form = h("form", { class: "kart" },
    h("div", { class: "form-satir" },
      h("label", {}, "Ad", h("input", { name: "ad", required: true })),
      h("label", {}, "Soyad", h("input", { name: "soyad", required: true })),
      h("label", {}, "E-posta", h("input", { name: "email", type: "email", required: true })),
      h("label", {}, "Telefon", h("input", { name: "telefon", type: "tel" })),
      h("label", {}, "T.C. / Yabancı Kimlik No", h("input", { name: "tcKimlikNo", inputmode: "numeric", maxlength: 11 })),
      h("label", {}, "Pasaport No (yabancı uyruklu)", h("input", { name: "pasaportNo", maxlength: 20 })),
      h("label", {}, "Öğrenci / sicil no", h("input", { name: "ogrenciNo" })),
      h("label", {}, "Üye tipi", h("select", { name: "uyeTipi" }, Object.entries(ADLAR.uyeTipi).map(([d, ad]) => h("option", { value: d }, ad)))),
      h("label", {}, "Bölüm", h("input", { name: "bolum" })),
      h("label", {}, "Fakülte", h("input", { name: "fakulte" })),
      h("label", {}, "Üyelik bitişi", h("input", { name: "uyelikBitisTarihi", type: "date" }))),
    h("label", {}, "Adres", h("textarea", { name: "adres", rows: 2 })),
    h("p", { class: "alt-baslik" }, "T.C./YKN veya pasaport numarasından en az biri zorunludur. Ödünç kotası üye tipine göre atanır."),
    h("div", {}, h("button", { class: "dugme", type: "submit" }, "Üyeyi kaydet")));
  formIsle(form, async (veri) => {
    const govde = Object.fromEntries(veri.entries());
    govde.uyelikBitisTarihi = govde.uyelikBitisTarihi || null;
    const u = await api("POST", "/api/uyeler", govde);
    bildir(`${u.adSoyad} kaydedildi.`);
    location.hash = `#/uye/${u.id}`;
  });
  doldur(alan, h("div", { class: "sayfa-baslik" }, h("h1", {}, "Üye ekle")), form);
};

// ---------- Yönlendirme ----------

const PERSONEL_SAYFALARI = new Set(["pano", "banko", "oduncler", "uyeler", "uye", "rezervasyonlar", "kitap-ekle", "uye-ekle"]);

async function yonlendir() {
  const alan = document.getElementById("icerik");
  const [yol] = location.hash.replace(/^#\/?/, "").split("?");
  const [rota = "katalog", parametre] = yol.split("/");
  document.getElementById("gezinti").classList.remove("acik");
  menuyuCiz();

  if (PERSONEL_SAYFALARI.has(rota) && !personelMi()) {
    location.hash = oturum ? "#/sifre" : "#/giris";
    return;
  }
  const sayfa = sayfalar[rota] || sayfalar.katalog;
  doldur(alan, h("p", { class: "yukleniyor" }, "Yükleniyor…"));
  try {
    await sayfa(alan, parametre && decodeURIComponent(parametre));
  } catch (hata) {
    doldur(alan, h("div", { class: "mesaj hata" }, hata.message || "Beklenmeyen bir hata oluştu."));
  }
  alan.focus({ preventScroll: true });
}

async function basla() {
  const menuDugme = document.getElementById("menuDugme");
  menuDugme.addEventListener("click", () => {
    const acik = document.getElementById("gezinti").classList.toggle("acik");
    menuDugme.setAttribute("aria-expanded", String(acik));
  });
  try {
    oturum = await api("GET", "/api/oturum/ben");
  } catch {
    oturum = null;
  }
  window.addEventListener("hashchange", yonlendir);
  await yonlendir();
}

basla();
