using System;
using System.Collections.Generic;

namespace Hackathon
{
    // OTURUM BİLGİLERİNİ TUTAN STATİK SINIF 
    public static class KullaniciOturumu
    {
        public static string KullaniciAdi { get; set; }
        public static int KullaniciID { get; set; }
        public static KullaniciProfili Profil { get; set; }

        public static bool IsAdmin { get; set; }
    }

    // KULLANICI PROFİL MODELİ 
    public class KullaniciProfili
    {
        public string EgitimDuzeyi { get; set; }
        public string OgrenimHedefi { get; set; }
        public DateTime Yas { get; set; } 
        public bool PrefCoktanSecmeli { get; set; }
        public bool PrefDogruYanlis { get; set; }
        public bool PrefAcikUclu { get; set; }
        public bool PrefKodOrnegi { get; set; }
        public string Email { get; set; }
    }


    public class TestOzet
    {
        public string AnaKonu { get; set; }
        public string TestTipi { get; set; }
        public DateTime Tarih { get; set; }
        public int DogruSayisi { get; set; }
        public int ToplamSoru { get; set; }
        public List<TestDetay> Detaylar { get; set; } = new List<TestDetay>();
        public string TarihString => Tarih.ToString("dd.MM.yyyy");
        public string SkorString => $"{DogruSayisi}/{ToplamSoru}";
    }

    public class TestDetay
    {
        public string AnaKonu { get; set; } 
        public string SoruMetni { get; set; }
        public string VerilenCevap { get; set; }
        public string DogruCevap { get; set; }
        public bool SonucDogruMu { get; set; }

        public DateTime Tarih { get; set; }
        public string TestTipi { get; set; }

        public int SoruID { get; set; }
    }
}