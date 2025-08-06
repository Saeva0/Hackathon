using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Hackathon
{
    // Kullanıcı Yönetimi ekranında göstereceğimiz model
    public class AdminKullaniciModel
    {
        public int ID { get; set; }
        public string Ad { get; set; }
        public string Email { get; set; }
        public System.DateTime KayitTarihi { get; set; }
        public bool IsAdmin { get; set; }
    }

    // İçerik Yönetimi ekranında göstereceğimiz soru modeli
    public class AdminSoruModel
    {
        public int ID { get; set; }
        public string AnaKonu { get; set; }
        public string SoruMetni { get; set; }
        public string SoruTipi { get; set; }
        public string Zorluk { get; set; }
    }


    public partial class AdminPanel : Window, INotifyPropertyChanged
    {
        // Kullanıcı Yönetimi için listeler
        private List<AdminKullaniciModel> _tumKullanicilar;
        public List<AdminKullaniciModel> Kullanicilar { get; set; }

        // İçerik Yönetimi için listeler
        private List<AdminSoruModel> _tumSorular;
        public List<AdminSoruModel> Sorular { get; set; }

        public AdminPanel()
        {
            InitializeComponent();
            this.DataContext = this;
        }

        private void AdminPanel_Loaded(object sender, RoutedEventArgs e)
        {
            Dashboard_Click(null, null);
        }

        #region Menü Tıklama Olayları

        private void Dashboard_Click(object sender, RoutedEventArgs e)
        {
            // Dashboard'u göster, diğerlerini gizle
            DashboardGrid.Visibility = Visibility.Visible;
            KullaniciYonetimiGrid.Visibility = Visibility.Collapsed;
            IcerikYonetimiGrid.Visibility = Visibility.Collapsed;

            // İstatistikleri veritabanından çek ve ekrana yazdır.
            var stats = VeritabaniServisi.GetDashboardStats();

            // Toplam İstatistikler
            txtToplamKullanici.Text = stats.kullaniciSayisi.ToString("N0");
            txtToplamSoru.Text = stats.soruSayisi.ToString("N0");
            txtToplamCevap.Text = stats.cevapSayisi.ToString("N0");

            // Günlük İstatistikler
            txtBugunKaydolanlar.Text = stats.bugunKaydolanlar.ToString("N0");
            txtBugunCozulenler.Text = stats.bugunCozulenler.ToString("N0");
        }

        private void KullaniciYonetimi_Click(object sender, RoutedEventArgs e)
        {
            DashboardGrid.Visibility = Visibility.Collapsed;
            KullaniciYonetimiGrid.Visibility = Visibility.Visible;
            IcerikYonetimiGrid.Visibility = Visibility.Collapsed;

            if (_tumKullanicilar == null) { KullanicilariYukle(); }
        }

        private void IcerikYonetimi_Click(object sender, RoutedEventArgs e)
        {
            DashboardGrid.Visibility = Visibility.Collapsed;
            KullaniciYonetimiGrid.Visibility = Visibility.Collapsed;
            IcerikYonetimiGrid.Visibility = Visibility.Visible;

            if (_tumSorular == null) { SorulariYukle(); }
        }

        private void AnaMenuDon_Click(object sender, RoutedEventArgs e)
        {
            MainWindow anaMenu = new MainWindow();
            anaMenu.Show();
            this.Close();
        }

        #endregion

        #region Veri Yükleme Metotları

        private void KullanicilariYukle()
        {
            _tumKullanicilar = new List<AdminKullaniciModel>();

            string sql = "SELECT ID, Ad, Email, KayitTarihi, IsAdmin FROM Kayit ORDER BY ID DESC";

            try
            {
                using (var conn = new SqlConnection(VeritabaniServisi.GetConnectionString()))
                {
                    conn.Open();
                    using (var cmd = new SqlCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        // 3. Veritabanından gelen her satır için döngüye gir.
                        while (reader.Read())
                        {
                            // 4. Okunan her kullanıcıyı listeye ekle.
                            _tumKullanicilar.Add(new AdminKullaniciModel
                            {
                                ID = reader.GetInt32(0),
                                Ad = reader.GetString(1),
                                Email = reader.GetString(2),
                                KayitTarihi = reader.GetDateTime(3), // Artık doğru sütunu okuyoruz.
                                IsAdmin = reader.GetBoolean(4)
                            });
                        }
                    }
                }

                Kullanicilar = new List<AdminKullaniciModel>(_tumKullanicilar);
                OnPropertyChanged(nameof(Kullanicilar));
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Kullanıcılar yüklenirken bir hata oluştu: " + ex.Message);
            }
        }

        private void SorulariYukle()
        {
            _tumSorular = new List<AdminSoruModel>();
            string sql = "SELECT ID, AnaKonu, SoruMetni, SoruTipi, Zorluk FROM Sorular ORDER BY ID DESC";
            try
            {
                using (var conn = new SqlConnection(VeritabaniServisi.GetConnectionString()))
                {
                    conn.Open();
                    using (var cmd = new SqlCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            _tumSorular.Add(new AdminSoruModel
                            {
                                ID = (int)reader["ID"],
                                AnaKonu = reader["AnaKonu"].ToString(),
                                SoruMetni = reader["SoruMetni"].ToString(),
                                SoruTipi = reader["SoruTipi"].ToString(),
                                Zorluk = reader["Zorluk"].ToString()
                            });
                        }
                    }
                }
                Sorular = new List<AdminSoruModel>(_tumSorular);
                OnPropertyChanged(nameof(Sorular));
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Sorular yüklenirken bir hata oluştu: " + ex.Message);
            }
        }

        #endregion

        #region Olay Yöneticileri (Events)

        private void txtKullaniciAra_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_tumKullanicilar == null) return;
            string aramaMetni = txtKullaniciAra.Text.ToLower();

            if (string.IsNullOrWhiteSpace(aramaMetni))
            {
                Kullanicilar = _tumKullanicilar;
            }
            else
            {
                Kullanicilar = _tumKullanicilar.Where(k =>
                    k.Ad.ToLower().Contains(aramaMetni) ||
                    k.Email.ToLower().Contains(aramaMetni)
                ).ToList();
            }
            OnPropertyChanged(nameof(Kullanicilar));
        }

        private void KullanicilarDataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (KullanicilarDataGrid.SelectedItem is AdminKullaniciModel secilenKullanici)
            {
                KullaniciDetay detayPenceresi = new KullaniciDetay(secilenKullanici.ID);
                bool? sonuc = detayPenceresi.ShowDialog();
                if (sonuc == true)
                {
                    KullanicilariYukle();
                }
            }
        }

        private void txtSoruFiltre_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_tumSorular == null) return;
            string aramaMetni = txtSoruFiltre.Text.ToLower();

            if (string.IsNullOrWhiteSpace(aramaMetni))
            {
                Sorular = _tumSorular;
            }
            else
            {
                Sorular = _tumSorular.Where(s =>
                    s.AnaKonu.ToLower().Contains(aramaMetni) ||
                    s.SoruMetni.ToLower().Contains(aramaMetni)
                ).ToList();
            }
            OnPropertyChanged(nameof(Sorular));
        }

        private void btnSoruSil_Click(object sender, RoutedEventArgs e)
        {
            if (SorularDataGrid.SelectedItem is AdminSoruModel secilenSoru)
            {
                MessageBoxResult result = MessageBox.Show(
                    $"Emin misiniz?\n\nID: {secilenSoru.ID}\nSoru: {(secilenSoru.SoruMetni.Length > 70 ? secilenSoru.SoruMetni.Substring(0, 70) : secilenSoru.SoruMetni)}...\n\nBu soru ve ilişkili tüm cevaplar kalıcı olarak silinecektir!",
                    "Silme Onayı",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    VeritabaniServisi.SoruSil(secilenSoru.ID);
                    SorulariYukle(); // Listeyi yenile
                }
            }
            else
            {
                MessageBox.Show("Lütfen silmek için bir soru seçin.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            if (propertyName == "Kullanicilar")
            {
                KullanicilarDataGrid.ItemsSource = Kullanicilar;
            }
            else if (propertyName == "Sorular")
            {
                SorularDataGrid.ItemsSource = Sorular;
            }

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}