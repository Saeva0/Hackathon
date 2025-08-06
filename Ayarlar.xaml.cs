using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace Hackathon
{
    public partial class Ayarlar : Window
    {
        public Ayarlar()
        {
            InitializeComponent();
        }

        #region Pencere Yönetimi ve Veri Yükleme

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadUserProfile();
        }

        private int CalculateAge(DateTime dogumTarihi)
        {
            var today = DateTime.Today;
            var age = today.Year - dogumTarihi.Year;
            if (dogumTarihi.Date > today.AddYears(-age)) age--;
            return age;
        }


        private void LoadUserProfile()
        {
            // Oturum ve profil bilgilerinin geçerli olduğunu kontrol et.
            if (KullaniciOturumu.Profil == null || string.IsNullOrEmpty(KullaniciOturumu.KullaniciAdi))
            {
                MessageBox.Show("Kullanıcı profili yüklenemedi. Lütfen tekrar giriş yapın.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
                return;
            }

            txtKullaniciAdiProfil.Text = KullaniciOturumu.KullaniciAdi;
            txtEmailProfil.Text = KullaniciOturumu.Profil.Email;

            int yas = CalculateAge(KullaniciOturumu.Profil.Yas);
            txtYasProfil.Text = yas.ToString();

            txtYasProfil.IsReadOnly = true;
            txtYasProfil.Opacity = 0.7;


            SetComboBoxSelection(cmbEgitimProfil, KullaniciOturumu.Profil.EgitimDuzeyi);
            SetComboBoxSelection(cmbHedefProfil, KullaniciOturumu.Profil.OgrenimHedefi);

            chkCoktanSecmeliProfil.IsChecked = KullaniciOturumu.Profil.PrefCoktanSecmeli;
            chkDogruYanlisProfil.IsChecked = KullaniciOturumu.Profil.PrefDogruYanlis;
            chkAcikUcluProfil.IsChecked = KullaniciOturumu.Profil.PrefAcikUclu;
            chkKodOrnegiProfil.IsChecked = KullaniciOturumu.Profil.PrefKodOrnegi;

        }

        private void SetComboBoxSelection(ComboBox comboBox, string value)
        {
            foreach (ComboBoxItem item in comboBox.Items)
            {
                if (item.Content.ToString() == value)
                {
                    comboBox.SelectedItem = item;
                    return;
                }
            }
        }

        #endregion

        #region Sol Menü Tıklama Olayları

        private void Profil_Click(object sender, RoutedEventArgs e)
        {
            ProfilGrid.Visibility = Visibility.Visible;
            GecmisSonuclarGrid.Visibility = Visibility.Collapsed;
            HesapSilGrid.Visibility = Visibility.Collapsed;
        }

        private void Gecmis_Click(object sender, RoutedEventArgs e)
        {
            ProfilGrid.Visibility = Visibility.Collapsed;
            HesapSilGrid.Visibility = Visibility.Collapsed;
            GecmisSonuclarGrid.Visibility = Visibility.Visible;

            GecmisSonuclariYukle(); 
        }

        private void HesapSil_Click(object sender, RoutedEventArgs e)
        {
            ProfilGrid.Visibility = Visibility.Collapsed;
            GecmisSonuclarGrid.Visibility = Visibility.Collapsed;
            HesapSilGrid.Visibility = Visibility.Visible;
        }

        private void AnaMenu_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mw = new MainWindow();
            mw.Show();
            this.Close();
        }

        #endregion

        #region Kaydetme İşlemi


        private void Kaydet_Click(object sender, RoutedEventArgs e)
        {

            if (string.IsNullOrWhiteSpace(txtEmailProfil.Text) ||
        cmbEgitimProfil.SelectedItem == null ||
        cmbHedefProfil.SelectedItem == null)
            {
                MessageBox.Show("Lütfen tüm alanları doğru bir şekilde doldurun.", "Geçersiz Veri", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!txtEmailProfil.Text.Contains("@") || !txtEmailProfil.Text.Contains("."))
            {
                MessageBox.Show("Lütfen geçerli bir e-posta adresi girin.", "Geçersiz E-posta", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (cmbEgitimProfil.SelectedItem == null || cmbHedefProfil.SelectedItem == null)
            {
                MessageBox.Show("Lütfen tüm alanları doğru bir şekilde doldurun.", "Geçersiz Veri", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            KullaniciProfili guncelProfil = new KullaniciProfili
            {
                Email = txtEmailProfil.Text.Trim(),
                Yas = KullaniciOturumu.Profil.Yas,
                EgitimDuzeyi = (cmbEgitimProfil.SelectedItem as ComboBoxItem).Content.ToString(),
                OgrenimHedefi = (cmbHedefProfil.SelectedItem as ComboBoxItem).Content.ToString(),
                PrefCoktanSecmeli = chkCoktanSecmeliProfil.IsChecked ?? false,
                PrefDogruYanlis = chkDogruYanlisProfil.IsChecked ?? false,
                PrefAcikUclu = chkAcikUcluProfil.IsChecked ?? false,
                PrefKodOrnegi = chkKodOrnegiProfil.IsChecked ?? false
            };

            try
            {
                VeritabaniServisi.UpdateKullaniciProfili(KullaniciOturumu.KullaniciID, guncelProfil);

                KullaniciOturumu.Profil = guncelProfil;

                MessageBox.Show("Profil bilgileriniz başarıyla güncellendi!", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Bilgiler güncellenirken bir hata oluştu: " + ex.Message, "Veritabanı Hatası", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Arayüz Etkileşimleri

        private void txtYas_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !int.TryParse(e.Text, out _); 
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) this.DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private Window GetParentWindow()
        {
            return Window.GetWindow(this);
        }


        //Hesap Silme

        private void HesabiSilOnay_Click(object sender, RoutedEventArgs e)
        {
            string girilenSifre = txtSifreOnay.Password;

            if (string.IsNullOrEmpty(girilenSifre))
            {
                MessageBox.Show("İşleme devam etmek için şifrenizi girmelisiniz.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                bool sifreDogru = VeritabaniServisi.CheckSifre(KullaniciOturumu.KullaniciAdi, girilenSifre);

                if (!sifreDogru)
                {
                    MessageBox.Show("Girdiğiniz şifre yanlış. Lütfen tekrar deneyin.", "Hatalı Şifre", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                MessageBoxResult result = MessageBox.Show(
                    "Şifre doğrulandı.\n\nHesabınızı kalıcı olarak silmek istediğinizden emin misiniz? Bu işlem geri alınamaz!",
                    "Hesabı Silme Onayı",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    VeritabaniServisi.DeleteKullaniciById(KullaniciOturumu.KullaniciID);

                    MessageBox.Show("Hesabınız başarıyla silindi.", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);


                    Giris giris = new Giris();
                    giris.Show();
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("İşlem sırasında beklenmedik bir hata oluştu: " + ex.Message, "Kritik Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Sağ üstteki barlar
        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            Window parentWindow = GetParentWindow();
            if (parentWindow != null)
            {
                parentWindow.WindowState = WindowState.Minimized;
            }
        }

        private void Maximize_Click(object sender, RoutedEventArgs e)
        {
            Window parentWindow = GetParentWindow();
            if (parentWindow != null)
            {
                parentWindow.WindowState = parentWindow.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Window parentWindow = GetParentWindow();
            if (parentWindow != null)
            {
                parentWindow.Close();
            }
        }
        #endregion

        #region Geçmiş Sonuçlar Filtreleme

        private List<TestOzet> _tumTestler;

        private async void GecmisSonuclariYukle()
        {
            txtYukleniyor.Visibility = Visibility.Visible;
            TestlerListBox.Visibility = Visibility.Collapsed; 

            cmbDersFiltre.ItemsSource = null;
            TestDetayDataGrid.ItemsSource = null;
            txtDetaySec.Visibility = Visibility.Collapsed;


            try
            {
                // 1. Veritabanından verileri ASENKRON olarak çek. 
                _tumTestler = await VeritabaniServisi.GetirGecmisTestlerAsync(KullaniciOturumu.KullaniciID);


                // 2. Ders filtresi ComboBox'ını doldur.
                var mevcutDersler = _tumTestler.Select(t => t.AnaKonu.Split('-')[0].Trim()).Distinct().ToList();

                cmbDersFiltre.Items.Clear();
                cmbDersFiltre.Items.Add(new ComboBoxItem { Content = "Tüm Dersler" });
                foreach (var ders in mevcutDersler)
                {
                    cmbDersFiltre.Items.Add(new ComboBoxItem { Content = ders });
                }
                cmbDersFiltre.SelectedIndex = 0;

                FiltreleriUygula();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Geçmiş sonuçlar yüklenirken bir hata oluştu: " + ex.Message);
                txtGecmisYok.Text = "Sonuçlar yüklenemedi.";
                txtGecmisYok.Visibility = Visibility.Visible;
            }
            finally
            {
             
                txtYukleniyor.Visibility = Visibility.Collapsed;
                TestlerListBox.Visibility = Visibility.Visible;
            }
        }

        private void FiltreleriUygula()
        {
            IEnumerable<TestOzet> filtrelenmisListe = _tumTestler;

            if (dpTarihFiltre.SelectedDate.HasValue)
            {
                DateTime secilenTarih = dpTarihFiltre.SelectedDate.Value.Date;
                filtrelenmisListe = filtrelenmisListe.Where(t => t.Tarih.Date == secilenTarih);
            }

            if (cmbDersFiltre.SelectedItem is ComboBoxItem secilenItem && secilenItem.Content.ToString() != "Tüm Dersler")
            {
                string secilenDers = secilenItem.Content.ToString();
                filtrelenmisListe = filtrelenmisListe.Where(t => t.AnaKonu.Trim().StartsWith(secilenDers));
            }

            var sonuclar = filtrelenmisListe.ToList();

            var viewSource = (CollectionViewSource)this.FindResource("GroupedTests");
            viewSource.Source = sonuclar;

            // Arayüzü güncelle
            if (sonuclar.Any())
            {
                txtGecmisYok.Visibility = Visibility.Collapsed;
                TestDetayDataGrid.ItemsSource = null; 
                txtDetaySec.Visibility = Visibility.Visible;

                TestlerListBox.SelectedIndex = 0;
                GosterTestDetaylari(TestlerListBox.SelectedItem as TestOzet);
            }
            else
            {
                txtGecmisYok.Text = "Seçilen filtrelere uygun sonuç bulunamadı.";
                txtGecmisYok.Visibility = Visibility.Visible;
                TestDetayDataGrid.ItemsSource = null;
                txtDetaySec.Visibility = Visibility.Collapsed;
            }
        }

        // Tarih veya ders seçimi değiştiğinde bu metot tetiklenir.
        private void Filtre_Degisti(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded || _tumTestler == null) return;

            FiltreleriUygula();
        }


        private void FiltreTemizle_Click(object sender, RoutedEventArgs e)
        {
            dpTarihFiltre.SelectedDate = null;
            cmbDersFiltre.SelectedIndex = 0;
        }

        #endregion

        private void TestlerListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            btnGecmisTekrarTesti.Visibility = Visibility.Collapsed;

            if (TestlerListBox.SelectedItem is TestOzet secilenTest)
            {
                TestDetayDataGrid.ItemsSource = secilenTest.Detaylar;
                txtDetaySec.Visibility = Visibility.Collapsed;

                bool yanlisVarMi = secilenTest.Detaylar.Any(detay => !detay.SonucDogruMu);

                if (yanlisVarMi)
                {
                    btnGecmisTekrarTesti.Visibility = Visibility.Visible;
                }
            }
            else
            {
                TestDetayDataGrid.ItemsSource = null;
                txtDetaySec.Visibility = Visibility.Visible;
            }
        }

        private void GecmisTekrarTesti_Click(object sender, RoutedEventArgs e)
        {
            if (TestlerListBox.SelectedItem is TestOzet secilenTest)
            {
                var yanlisCevapDetaylari = secilenTest.Detaylar.Where(d => !d.SonucDogruMu);

                List<QuizModel> yanlisSorular = new List<QuizModel>();

                foreach (var detay in yanlisCevapDetaylari)
                {
                    QuizModel tamSoru = VeritabaniServisi.GetSoruById(detay.SoruID);

                    if (tamSoru != null)
                    {
                        yanlisSorular.Add(tamSoru);
                    }
                }

                if (!yanlisSorular.Any())
                {
                    MessageBox.Show("Tekrar teste uygun çoktan seçmeli yanlış soru bulunamadı.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                Random rng = new Random();
                foreach (var soru in yanlisSorular)
                {
                    if (soru.Secenekler == null || soru.Secenekler.Count < 2) continue;

                    string orjinalDogruCevapMetni = soru.Secenekler[soru.DogruCevap];
                    var cevaplar = soru.Secenekler.Values.ToList();
                    cevaplar = cevaplar.OrderBy(a => rng.Next()).ToList();

                    var yeniSecenekler = new Dictionary<string, string>();
                    var siklar = soru.Secenekler.Keys.ToList();
                    for (int i = 0; i < siklar.Count; i++)
                    {
                        yeniSecenekler[siklar[i]] = cevaplar[i];
                    }

                    soru.Secenekler = yeniSecenekler;
                    soru.DogruCevap = yeniSecenekler.FirstOrDefault(x => x.Value == orjinalDogruCevapMetni).Key;
                }

                Sorular yeniTestPenceresi = new Sorular(
                    secilenTest.AnaKonu + " (Yanlışlar Tekrar)",
                    yanlisSorular,
                    secilenTest.TestTipi,
                    "Orta",
                    yanlisSorular.Count);

                yeniTestPenceresi.Show();
                this.Close();
            }
        }

        private void GosterTestDetaylari(TestOzet secilenTest)
        {
            if (secilenTest != null)
            {

                TestDetayDataGrid.ItemsSource = secilenTest.Detaylar;
                txtDetaySec.Visibility = Visibility.Collapsed;
            }
            else
            {
                TestDetayDataGrid.ItemsSource = null;
                txtDetaySec.Visibility = Visibility.Visible;
            }
        }

        private async void btnNedenYanlis_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button clickedButton)
            {
                clickedButton.IsEnabled = false;
                clickedButton.Content = "Analiz ediliyor...";

                if (clickedButton.DataContext is TestDetay selectedDetail)
                {
                    try
                    {
                        var geminiClient = new GeminiClient();
                        string analysisResult = await geminiClient.AnalyzeWrongAnswerAsync(selectedDetail);

                        MessageBox.Show(analysisResult, "Yapay Zeka Açıklaması", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (System.Exception ex)
                    {
                        MessageBox.Show("Analiz sırasında bir hata oluştu: " + ex.Message, "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    finally
                    {
                        clickedButton.IsEnabled = true;
                        clickedButton.Content = "Neden Yanlış?";
                    }
                }
            }
        }
    }
}