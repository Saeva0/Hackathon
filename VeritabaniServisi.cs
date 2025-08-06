using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;


namespace Hackathon
{
    public static class VeritabaniServisi
    {

        public static string GetConnectionString()
        {
            return ConfigurationManager.AppSettings["DbConnectionString"];
        }

        #region Kullanıcı Yönetimi

        public static KullaniciProfili GetKullaniciProfili(int kullaniciId) 
        {
            KullaniciProfili profil = null;

            string query = "SELECT Email, Yas, EgitimDuzeyi, OgrenimHedefi, PrefCoktanSecmeli, PrefDogruYanlis, PrefAcikUclu, PrefKodOrnegi FROM Kayit WHERE ID = @ID";

            using (var connection = new SqlConnection(GetConnectionString()))
            {
                using (var command = new SqlCommand(query, connection))
                {

                    command.Parameters.AddWithValue("@ID", kullaniciId);
                    try
                    {
                        connection.Open();
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                profil = new KullaniciProfili
                                {
                                    Email = reader["Email"].ToString(),
                                    Yas = reader.GetDateTime(reader.GetOrdinal("Yas")),
                                    EgitimDuzeyi = reader["EgitimDuzeyi"].ToString(),
                                    OgrenimHedefi = reader["OgrenimHedefi"].ToString(),
                                    PrefCoktanSecmeli = (bool)reader["PrefCoktanSecmeli"],
                                    PrefDogruYanlis = (bool)reader["PrefDogruYanlis"],
                                    PrefAcikUclu = (bool)reader["PrefAcikUclu"],
                                    PrefKodOrnegi = (bool)reader["PrefKodOrnegi"]
                                };
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("Profil okuma hatası: " + ex.Message);
                        return null;
                    }
                }
            }
            return profil;
        }

        #endregion

        #region Soru Yönetimi (Kullanıcı ID'sine Göre Güncellendi)

     
        public static List<string> GetirMevcutSoruMetinleri(int kullaniciId)
        {
            var sorular = new List<string>();
            string sql = "SELECT SoruMetni FROM Sorular WHERE KullaniciID = @KullaniciID";

            using (var baglanti = new SqlConnection(GetConnectionString()))
            {
                using (var komut = new SqlCommand(sql, baglanti))
                {
                    komut.Parameters.AddWithValue("@KullaniciID", kullaniciId);
                    try
                    {
                        baglanti.Open();
                        using (var okuyucu = komut.ExecuteReader())
                        {
                            while (okuyucu.Read())
                            {
                                sorular.Add(okuyucu["SoruMetni"].ToString());
                            }
                        }
                    }
                    catch (SqlException ex)
                    {
                        System.Diagnostics.Debug.WriteLine("Veritabanı okuma hatası: " + ex.Message);
                    }
                }
            }
            return sorular;
        }

        // Yeni soruları GİRİŞ YAPAN KULLANICIYA ait olarak kaydetme
        public static List<QuizModel> KaydetVeGetirSorular(List<QuizModel> sorular, string sinavTipi, string zorluk, string anaKonu, int kullaniciId)
        {
           
            string sql = @"
        INSERT INTO Sorular (KullaniciID, SoruTipi, Zorluk, AnaKonu, SoruMetni, SeceneklerMetni, DogruCevap) 
        VALUES (@KullaniciID, @SoruTipi, @Zorluk, @AnaKonu, @SoruMetni, @SeceneklerMetni, @DogruCevap);
        SELECT SCOPE_IDENTITY();";

            using (var baglanti = new SqlConnection(GetConnectionString()))
            {
                try
                {
                    baglanti.Open();
                    foreach (var soru in sorular)
                    {
                        using (var komut = new SqlCommand(sql, baglanti))
                        {
                            
                            string seceneklerMetni = null;
                            if (soru.Secenekler != null && soru.Secenekler.Any())
                            {
                                var sb = new StringBuilder();
                                var siraliSecenekler = soru.Secenekler.OrderBy(kv => kv.Key);
                                foreach (var secenek in siraliSecenekler)
                                {
                                    sb.Append($"{secenek.Key}) {secenek.Value}");
                                    if (secenek.Key != siraliSecenekler.Last().Key)
                                    {
                                        sb.Append("~|~");
                                    }
                                }
                                seceneklerMetni = sb.ToString();
                            }

                        
                            komut.Parameters.AddWithValue("@KullaniciID", kullaniciId);
                            komut.Parameters.AddWithValue("@SoruTipi", sinavTipi);
                            komut.Parameters.AddWithValue("@Zorluk", zorluk);
                            komut.Parameters.AddWithValue("@AnaKonu", anaKonu);
                            komut.Parameters.AddWithValue("@SoruMetni", soru.SoruMetni);
                            komut.Parameters.AddWithValue("@SeceneklerMetni", (object)seceneklerMetni ?? DBNull.Value);
                            komut.Parameters.AddWithValue("@DogruCevap", soru.DogruCevap);

                            object newId = komut.ExecuteScalar();
                            if (newId != null)
                            {
                                soru.ID = Convert.ToInt32(newId);
                            }
                          
                        }
                    }
                }
                catch (SqlException ex)
                {
                    System.Diagnostics.Debug.WriteLine("Veritabanı yazma hatası: " + ex.Message);
                }
            }

            
            return sorular;
        }
        public static void KaydetCevap(int kullaniciId, int soruId, string verilenCevap, bool sonucDogruMu)
        {
            string sql = @"
                INSERT INTO KullaniciCevaplari (KullaniciID, SoruID, VerilenCevap, SonucDogruMu, CevaplamaTarihi)
                VALUES (@KullaniciID, @SoruID, @VerilenCevap, @SonucDogruMu, @CevaplamaTarihi)";

            using (var baglanti = new SqlConnection(GetConnectionString()))
            {
                using (var komut = new SqlCommand(sql, baglanti))
                {
                    komut.Parameters.AddWithValue("@KullaniciID", kullaniciId);
                    komut.Parameters.AddWithValue("@SoruID", soruId);
                    komut.Parameters.AddWithValue("@VerilenCevap", verilenCevap);
                    komut.Parameters.AddWithValue("@SonucDogruMu", sonucDogruMu);
                    komut.Parameters.AddWithValue("@CevaplamaTarihi", DateTime.Now);

                    try
                    {
                        baglanti.Open();
                        komut.ExecuteNonQuery();
                    }
                    catch (SqlException ex)
                    {
                        System.Diagnostics.Debug.WriteLine("Cevap kaydetme hatası: " + ex.Message);
                    }
                }
            }
        }

        public static void UpdateKullaniciProfili(int kullaniciId, KullaniciProfili profil)
        {
            string sql = @"
        UPDATE Kayit 
        SET 
            Email = @Email, 
            Yas = @Yas, 
            EgitimDuzeyi = @EgitimDuzeyi, 
            OgrenimHedefi = @OgrenimHedefi, 
            PrefCoktanSecmeli = @PrefCoktanSecmeli,
            PrefDogruYanlis = @PrefDogruYanlis,
            PrefAcikUclu = @PrefAcikUclu,
            PrefKodOrnegi = @PrefKodOrnegi
        WHERE ID = @KullaniciID";

            using (var baglanti = new SqlConnection(GetConnectionString()))
            {
                using (var komut = new SqlCommand(sql, baglanti))
                {
                   
                    komut.Parameters.AddWithValue("@Email", profil.Email);
                    komut.Parameters.AddWithValue("@Yas", profil.Yas);
                    komut.Parameters.AddWithValue("@EgitimDuzeyi", profil.EgitimDuzeyi);
                    komut.Parameters.AddWithValue("@OgrenimHedefi", profil.OgrenimHedefi);
                    komut.Parameters.AddWithValue("@PrefCoktanSecmeli", profil.PrefCoktanSecmeli);
                    komut.Parameters.AddWithValue("@PrefDogruYanlis", profil.PrefDogruYanlis);
                    komut.Parameters.AddWithValue("@PrefAcikUclu", profil.PrefAcikUclu);
                    komut.Parameters.AddWithValue("@PrefKodOrnegi", profil.PrefKodOrnegi);

                
                    komut.Parameters.AddWithValue("@KullaniciID", kullaniciId);

                    try
                    {
                        baglanti.Open();
                        komut.ExecuteNonQuery();
                    }
                    catch (SqlException ex)
                    {
                     
                        throw new Exception("Veritabanı güncelleme işlemi sırasında bir hata oluştu.", ex);
                    }
                }
            }
        }

        //Hesap Silme

        public static bool CheckSifre(string kullaniciAdi, string girilenSifre)
        {
            
            string sql = "SELECT SifreHash, SifreSalt FROM Kayit WHERE Ad = @Ad";

            using (var baglanti = new SqlConnection(GetConnectionString()))
            using (var komut = new SqlCommand(sql, baglanti))
            {
                komut.Parameters.AddWithValue("@Ad", kullaniciAdi);
                try
                {
                    baglanti.Open();
                    using (var reader = komut.ExecuteReader())
                    {
                        if (reader.Read()) 
                        {
                            byte[] dbHash = (byte[])reader["SifreHash"];
                            byte[] dbSalt = (byte[])reader["SifreSalt"];

                          
                            return SifrelemeServisi.DogrulaSifre(girilenSifre, dbHash, dbSalt);
                        }
                        return false; 
                    }
                }
                catch (SqlException ex)
                {
                    Debug.WriteLine("Şifre kontrol hatası: " + ex.Message);
                    return false;
                }
            }
        }


        public static void DeleteKullaniciById(int kullaniciId)
        {

            string sql = @"
        DELETE FROM KullaniciCevaplari WHERE KullaniciID = @kullaniciId;
        DELETE FROM CozulenSorular WHERE KullaniciID = @kullaniciId;
        DELETE FROM Sorular WHERE KullaniciID = @kullaniciId;
        DELETE FROM Kayit WHERE Id = @kullaniciId;";

            using (var baglanti = new SqlConnection(GetConnectionString()))
            {
                using (var komut = new SqlCommand(sql, baglanti))
                {
                    komut.Parameters.AddWithValue("@kullaniciId", kullaniciId);

                    try
                    {
                        baglanti.Open();
                        
                        komut.ExecuteNonQuery();
                    }
                    catch (SqlException ex)
                    {
                        Debug.WriteLine("Kullanıcı silme hatası: " + ex.Message);
                       
                        MessageBox.Show("Hesap silinirken bir veritabanı hatası oluştu.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        public static List<TestOzet> GetirGecmisTestler(int kullaniciId)
        {
            var tumCevaplar = new List<TestDetay>();

        
            string sql = @"
        SELECT 
            s.ID AS SoruID, 
            c.VerilenCevap, c.SonucDogruMu, c.CevaplamaTarihi,
            s.SoruMetni, s.DogruCevap, s.SoruTipi, s.AnaKonu
        FROM KullaniciCevaplari c
        JOIN Sorular s ON c.SoruID = s.ID
        WHERE c.KullaniciID = @KullaniciID 
        ORDER BY s.AnaKonu, c.CevaplamaTarihi DESC";
           

            try
            {
                using (var conn = new SqlConnection(GetConnectionString()))
                {
                    conn.Open();
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@KullaniciID", kullaniciId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                tumCevaplar.Add(new TestDetay
                                {
                                    SoruID = Convert.ToInt32(reader["SoruID"]),
                                    AnaKonu = reader["AnaKonu"].ToString(),
                                    SoruMetni = reader["SoruMetni"].ToString(),
                                    VerilenCevap = reader["VerilenCevap"].ToString(),
                                    DogruCevap = reader["DogruCevap"].ToString(),
                                    SonucDogruMu = (bool)reader["SonucDogruMu"],
                                    Tarih = (DateTime)reader["CevaplamaTarihi"],
                                    TestTipi = reader["SoruTipi"].ToString()
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Geçmiş sonuçlar yüklenirken hata: " + ex.Message);
                MessageBox.Show("Geçmiş sonuçlar yüklenirken bir veritabanı hatası oluştu. Detaylar için logları kontrol edin.", "Veritabanı Hatası", MessageBoxButton.OK, MessageBoxImage.Error);
                return new List<TestOzet>();
            }

            // Gruplama mantığı (değişiklik yok)
            var testGruplari = tumCevaplar
     .GroupBy(c => new { c.AnaKonu, c.Tarih.Date, c.TestTipi })
     .Select(g => new TestOzet
     {
         AnaKonu = g.Key.AnaKonu,
         Tarih = g.Key.Date,
         TestTipi = g.Key.TestTipi,
         ToplamSoru = g.Count(),
         DogruSayisi = g.Count(c => c.SonucDogruMu),
         Detaylar = g.ToList()
     
     })
     .OrderBy(o => o.AnaKonu).ThenByDescending(o => o.Tarih)
     .ToList();

            return testGruplari;
        }

        public static Task<List<TestOzet>> GetirGecmisTestlerAsync(int kullaniciId)
        {

            return Task.Run(() => GetirGecmisTestler(kullaniciId));
        }

        /// Verilen bir Soru ID'sine göre, tam soru detaylarını bir QuizModel nesnesi olarak döndürür.
     
        public static QuizModel GetSoruById(int soruId)
        {
            QuizModel soru = null;
            string sql = "SELECT ID, SoruMetni, SeceneklerMetni, DogruCevap FROM Sorular WHERE ID = @SoruID";

            using (var conn = new SqlConnection(GetConnectionString()))
            {
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@SoruID", soruId);
                    try
                    {
                        conn.Open();
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                soru = new QuizModel
                                {
                                    ID = (int)reader["ID"],
                                    SoruMetni = reader["SoruMetni"].ToString(),
                                    DogruCevap = reader["DogruCevap"].ToString()
                                };

                                // Seçenekleri veritabanındaki metinden Dictionary'e geri dönüştür.
                                string seceneklerMetni = reader["SeceneklerMetni"] as string;
                                if (!string.IsNullOrEmpty(seceneklerMetni))
                                {
                                    var secenekler = new Dictionary<string, string>();
                                    // Metni "~|~" karakterine göre böl. Örnek: "A) Cevap 1~|~B) Cevap 2"
                                    var parcalar = seceneklerMetni.Split(new[] { "~|~" }, StringSplitOptions.RemoveEmptyEntries);
                                    foreach (var parca in parcalar)
                                    {
                                        // Her parçayı ilk ")" karakterine göre ikiye böl.
                                        int ayiriciIndex = parca.IndexOf(')');
                                        if (ayiriciIndex > 0)
                                        {
                                            string key = parca.Substring(0, ayiriciIndex).Trim();
                                            string value = parca.Substring(ayiriciIndex + 1).Trim();
                                            secenekler[key] = value;
                                        }
                                    }
                                    soru.Secenekler = secenekler;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"GetSoruById hatası: {ex.Message}");
                    }
                }
            }
            return soru;
        }

        // Belirtilen kriterlere uyan ve kullanıcının DAHA ÖNCE ÇÖZMEDİĞİ soruları veritabanından çeker.
       
        public static List<QuizModel> GetirHavuzdanSorular(string anaKonu, string sinavTipi, string zorluk, int kullaniciId, int istenenSoruSayisi)
        {
            var sorular = new List<QuizModel>();
           
            string sql = @"
        SELECT TOP (@IstenenSayi) ID, SoruMetni, SeceneklerMetni, DogruCevap 
        FROM Sorular
        WHERE 
            AnaKonu = @AnaKonu AND 
            SoruTipi = @SoruTipi AND 
            Zorluk = @Zorluk AND
            ID NOT IN (SELECT SoruID FROM CozulenSorular WHERE KullaniciID = @KullaniciID)
        ORDER BY NEWID()"; // Rastgele sırala

            using (var conn = new SqlConnection(GetConnectionString()))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@IstenenSayi", istenenSoruSayisi);
                cmd.Parameters.AddWithValue("@AnaKonu", anaKonu);
                cmd.Parameters.AddWithValue("@SoruTipi", sinavTipi);
                cmd.Parameters.AddWithValue("@Zorluk", zorluk);
                cmd.Parameters.AddWithValue("@KullaniciID", kullaniciId);

                try
                {
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            // GetSoruById'deki mantığı burada tekrar kullanabiliriz.
                            var soru = new QuizModel { /* ... Okuma kodları ... */ };
                            // ... (reader'dan QuizModel'e dönüşüm kodunu buraya ekleyin)
                            sorular.Add(soru);
                        }
                    }
                }
                catch (Exception ex) { Debug.WriteLine($"Havuzdan soru çekme hatası: {ex.Message}"); }
            }
            return sorular;
        }

      
        // Bir testteki soruların, kullanıcı tarafından çözüldü olarak işaretlenmesini sağlar.
       
        public static void IsaretleCozulenSorular(int kullaniciId, List<QuizModel> sorular)
        {
            if (sorular == null || !sorular.Any()) return;

            var sqlBuilder = new StringBuilder();
            sqlBuilder.AppendLine("INSERT INTO CozulenSorular (KullaniciID, SoruID) VALUES");

            var parametreler = new List<SqlParameter>();
            for (int i = 0; i < sorular.Count; i++)
            {
                sqlBuilder.Append($"(@KullaniciID{i}, @SoruID{i})");
                if (i < sorular.Count - 1) sqlBuilder.Append(",");

                parametreler.Add(new SqlParameter($"@KullaniciID{i}", kullaniciId));
                parametreler.Add(new SqlParameter($"@SoruID{i}", sorular[i].ID));
            }

            using (var conn = new SqlConnection(GetConnectionString()))
            using (var cmd = new SqlCommand(sqlBuilder.ToString(), conn))
            {
                cmd.Parameters.AddRange(parametreler.ToArray());
                try
                {
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex) { Debug.WriteLine($"Çözülen sorular işaretlenemedi: {ex.Message}"); }
            }
        }

        // Şifremi Unuttum

        public static bool KaydetSifirlamaKodu(string email, out string kod)
        {
            kod = new Random().Next(100000, 999999).ToString();
            var gecerlilikTarihi = DateTime.Now.AddMinutes(10);

          
            string sql = "UPDATE Kayit SET SifreSifirlamaKodu = @Kod, SifirlamaKoduGecerlilikTarihi = @Tarih WHERE Email = @Email";

            using (var conn = new SqlConnection(GetConnectionString()))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@Kod", kod);
                cmd.Parameters.AddWithValue("@Tarih", gecerlilikTarihi);
                cmd.Parameters.AddWithValue("@Email", email);
                try
                {
                    conn.Open();
                    int etkilenenSatir = cmd.ExecuteNonQuery();
                    return etkilenenSatir > 0;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Sıfırlama kodu kaydedilemedi: {ex.Message}");
                    return false;
                }
            }
        }



        public static bool DogrulaSifirlamaKodu(string email, string kod)
        {
           
            string sql = "SELECT COUNT(1) FROM Kayit WHERE Email = @Email AND SifreSifirlamaKodu = @Kod AND SifirlamaKoduGecerlilikTarihi > @SimdikiZaman";
          

            using (var conn = new SqlConnection(GetConnectionString()))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@Email", email);
                cmd.Parameters.AddWithValue("@Kod", kod);
                cmd.Parameters.AddWithValue("@SimdikiZaman", DateTime.Now);
                try
                {
                    conn.Open();
                    int sayi = Convert.ToInt32(cmd.ExecuteScalar());
                    return sayi > 0;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Kod doğrulama hatası: {ex.Message}");
                    return false;
                }
            }
        }

        public static bool GuncelleSifre(string email, string yeniSifre)
        {
            var (sifreHash, sifreSalt) = SifrelemeServisi.HashSifre(yeniSifre);

           
            string sql = "UPDATE Kayit SET SifreHash = @Hash, SifreSalt = @Salt, SifreSifirlamaKodu = NULL, SifirlamaKoduGecerlilikTarihi = NULL WHERE Email = @Email";

            using (var conn = new SqlConnection(GetConnectionString()))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@Hash", sifreHash);
                cmd.Parameters.AddWithValue("@Salt", sifreSalt);
                cmd.Parameters.AddWithValue("@Email", email);
                try
                {
                    conn.Open();
                    int etkilenenSatir = cmd.ExecuteNonQuery();
                    return etkilenenSatir > 0;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Şifre güncelleme hatası: {ex.Message}");
                    return false;
                }
            }
        }

        public static bool CheckSifreByEmail(string email, string girilenSifre)
        {
         
            string sql = "SELECT SifreHash, SifreSalt FROM Kayit WHERE Email = @Email";
          

            using (var baglanti = new SqlConnection(GetConnectionString()))
            using (var komut = new SqlCommand(sql, baglanti))
            {
                komut.Parameters.AddWithValue("@Email", email); // Parametre adı da @Email oldu.
                try
                {
                    baglanti.Open();
                    using (var reader = komut.ExecuteReader())
                    {
                        if (reader.Read()) // Kullanıcı bulunduysa
                        {
                            byte[] dbHash = (byte[])reader["SifreHash"];
                            byte[] dbSalt = (byte[])reader["SifreSalt"];

                            return SifrelemeServisi.DogrulaSifre(girilenSifre, dbHash, dbSalt);
                        }
                        return false; // Kullanıcı bulunamadı.
                    }
                }
                catch (SqlException ex)
                {
                    Debug.WriteLine("E-postaya göre şifre kontrol hatası: " + ex.Message);
                    return false;
                }
            }
        }

        public static void UpdateIlkGirisDurumu(int kullaniciId)
        {
           
            if (kullaniciId <= 0) return;

            string sql = "UPDATE Kayit SET IlkGirisTamamlandi = 1 WHERE ID = @KullaniciID";

            using (var conn = new SqlConnection(GetConnectionString()))
            {
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@KullaniciID", kullaniciId);
                    try
                    {
                        conn.Open();
                        cmd.ExecuteNonQuery(); 
                    }
                    catch (Exception ex)
                    {
                       
                        System.Diagnostics.Debug.WriteLine($"IlkGirisTamamlandi bayrağı güncellenemedi: {ex.Message}");
                    }
                }
            }
        }

        public static (int kullaniciSayisi, int soruSayisi, int cevapSayisi, int bugunKaydolanlar, int bugunCozulenler) GetDashboardStats()
        {
            int kullaniciSayisi = 0;
            int soruSayisi = 0;
            int cevapSayisi = 0;
            int bugunKaydolanlar = 0;
            int bugunCozulenler = 0;

                    string sql = @"
                SELECT (SELECT COUNT(*) FROM Kayit);
                SELECT (SELECT COUNT(*) FROM Sorular);
                SELECT (SELECT COUNT(*) FROM KullaniciCevaplari);
                SELECT (SELECT COUNT(*) FROM Kayit WHERE CONVERT(date, KayitTarihi) = CONVERT(date, GETDATE()));
                SELECT (SELECT COUNT(*) FROM KullaniciCevaplari WHERE CONVERT(date, CevaplamaTarihi) = CONVERT(date, GETDATE()));
            ";

            try
            {
                using (var conn = new SqlConnection(GetConnectionString()))
                {
                    conn.Open();
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read()) { kullaniciSayisi = reader.GetInt32(0); }

                         
                            if (reader.NextResult() && reader.Read()) { soruSayisi = reader.GetInt32(0); }

                          
                            if (reader.NextResult() && reader.Read()) { cevapSayisi = reader.GetInt32(0); }

                          
                            if (reader.NextResult() && reader.Read()) { bugunKaydolanlar = reader.GetInt32(0); }

                            
                            if (reader.NextResult() && reader.Read()) { bugunCozulenler = reader.GetInt32(0); }
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Dashboard istatistikleri alınırken hata: " + ex.Message);
            }

            return (kullaniciSayisi, soruSayisi, cevapSayisi, bugunKaydolanlar, bugunCozulenler);
        }

        public static void SoruSil(int soruId)
        {
           
            string sql = @"
        DELETE FROM KullaniciCevaplari WHERE SoruID = @SoruID;
        DELETE FROM CozulenSorular WHERE SoruID = @SoruID;
        DELETE FROM Sorular WHERE ID = @SoruID;
    ";

            try
            {
                using (var conn = new SqlConnection(GetConnectionString()))
                {
                    conn.Open();
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@SoruID", soruId);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Soru silinirken bir veritabanı hatası oluştu: " + ex.Message, "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }
} 