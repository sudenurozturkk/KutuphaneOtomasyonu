using KutuphaneOtomasyonu.Data;
using KutuphaneOtomasyonu.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace KutuphaneOtomasyonu.Services
{
    /// <summary>
    /// Bir kullanıcı işlemi için tek bir <see cref="KutuphaneDbContext"/> ve ona bağlı servisleri sağlar
    /// (iş birimi kapsamı). Sistem ayarları bir kez okunur; servisler ilk kullanımda oluşturulur.
    /// <code>using var kapsam = await ServisKapsami.OlusturAsync();
    /// var sonuc = await kapsam.Odunc.OduncVerAsync(...);</code>
    /// </summary>
    public sealed class ServisKapsami : IDisposable
    {
        private readonly bool _contextSahibi;
        private BildirimService? _bildirim;
        private OduncService? _odunc;
        private RezervasyonService? _rezervasyon;
        private KitapService? _kitap;
        private UyeService? _uye;
        private CuzdanService? _cuzdan;
        private BankoAramaService? _banko;
        private SalonRezervasyonService? _salon;
        private AkademikService? _akademik;
        private VeritabaniBakimService? _bakim;

        private ServisKapsami(KutuphaneDbContext context, SistemAyarlari ayarlar, bool contextSahibi)
        {
            Context = context;
            Ayarlar = ayarlar;
            _contextSahibi = contextSahibi;
        }

        /// <param name="context">Verilirse kapsam bu bağlamı kullanır ve kapatmaz (testler için).</param>
        public static async Task<ServisKapsami> OlusturAsync(KutuphaneDbContext? context = null)
        {
            var ctx = context ?? new KutuphaneDbContext();
            try
            {
                var ayarlar = await ctx.SistemAyarlari.AsNoTracking().FirstOrDefaultAsync() ?? new SistemAyarlari();
                return new ServisKapsami(ctx, ayarlar, contextSahibi: context == null);
            }
            catch
            {
                if (context == null) ctx.Dispose();
                throw;
            }
        }

        public KutuphaneDbContext Context { get; }
        public SistemAyarlari Ayarlar { get; }

        public BildirimService Bildirim => _bildirim ??= new BildirimService(Context);
        public OduncService Odunc => _odunc ??= new OduncService(Context, Bildirim, Ayarlar);
        public RezervasyonService Rezervasyon => _rezervasyon ??= new RezervasyonService(Context, Bildirim, Ayarlar);
        public KitapService Kitap => _kitap ??= new KitapService(Context);
        public UyeService Uye => _uye ??= new UyeService(Context);
        public CuzdanService Cuzdan => _cuzdan ??= new CuzdanService(Context);
        public BankoAramaService Banko => _banko ??= new BankoAramaService(Context);
        public SalonRezervasyonService Salon => _salon ??= new SalonRezervasyonService(Context);
        public AkademikService Akademik => _akademik ??= new AkademikService(Context);
        public VeritabaniBakimService Bakim => _bakim ??= new VeritabaniBakimService(Context);

        public void Dispose()
        {
            if (_contextSahibi) Context.Dispose();
        }
    }
}
