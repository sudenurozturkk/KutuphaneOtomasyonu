namespace KutuphaneOtomasyonu.Core
{
    /// <summary>
    /// Kitabın fiziksel konumunu temsil eden sınıf (Kompozisyon ilişkisiyle Kitap içinde yer alır)
    /// </summary>
    public class RafKonumu
    {
        public string Salon { get; set; }
        public int Kat { get; set; }
        public string Koridor { get; set; }
        public string RafNo { get; set; }

        public RafKonumu(string salon = "Merkez Kütüphane", int kat = 1, string koridor = "A-1", string rafNo = "10")
        {
            Salon = salon;
            Kat = kat;
            Koridor = koridor;
            RafNo = rafNo;
        }

        public string TamKonum()
        {
            return $"{Salon} | Kat {Kat} | Koridor {Koridor} | Raf #{RafNo}";
        }

        public override string ToString() => TamKonum();
    }
}
