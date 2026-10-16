using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Threading.Tasks;

namespace KutuphaneOtomasyonu.Data
{
    /// <summary>
    /// Servislerin iç içe çağrılabilmesi için transaction yardımcıları.
    /// SQLite'ta Microsoft.Data.Sqlite varsayılan olarak <c>BEGIN IMMEDIATE</c> kullanır: yazma kilidi
    /// transaction başında alınır, böylece "oku → kontrol et → yaz" adımları başka bir bağlantı/işlemle
    /// iç içe geçemez.
    /// </summary>
    public static class VeritabaniIslemi
    {
        /// <summary>
        /// Bağlamda açık bir transaction yoksa yenisini başlatır; varsa <c>null</c> döner ve
        /// commit/rollback sorumluluğu dıştaki çağırana kalır.
        /// </summary>
        public static async Task<IDbContextTransaction?> BaslatAsync(KutuphaneDbContext context) =>
            context.Database.CurrentTransaction == null
                ? await context.Database.BeginTransactionAsync()
                : null;
    }
}
