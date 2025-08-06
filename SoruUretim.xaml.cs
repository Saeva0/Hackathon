using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Hackathon
{
    public partial class SoruUretim : Window
    {
        private readonly string _secilenDers;

        public SoruUretim(string secilenDers)
        {
            InitializeComponent();
            _secilenDers = secilenDers;
            txtAnaKonu.Text = _secilenDers + " - ";
            txtAnaKonu.Focus();
            txtAnaKonu.CaretIndex = txtAnaKonu.Text.Length;
        }

        private async void Button_SoruOlustur_Click(object sender, RoutedEventArgs e)
        {
            // 1. Kullanıcı Girdilerini Kontrol Et
            if (string.IsNullOrWhiteSpace(txtAnaKonu.Text) || cmbSinavTipi.SelectedItem == null)
            {
                MessageBox.Show("Lütfen 'Ana Konu' ve 'Sınav Tipi' alanlarını doldurun.", "Eksik Bilgi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!_secilenDers.Contains("Diğer") && !txtAnaKonu.Text.Trim().StartsWith(_secilenDers))
            {
                MessageBoxResult result = MessageBox.Show(
                    $"Girdiğiniz konu '{_secilenDers}' dersiyle ilgili görünmüyor. Yine de devam etmek istiyor musunuz?",
                    "Konu Uyarısı",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);
                if (result == MessageBoxResult.No) { return; }
            }

            // 2. Arayüzü Bekleme Moduna Al
            LoadingOverlay.Visibility = Visibility.Visible;
            MainContent.IsEnabled = false;

            try
            {
                int aktifKullaniciId = KullaniciOturumu.KullaniciID;
                if (aktifKullaniciId == 0) { throw new Exception("Geçerli bir kullanıcı oturumu bulunamadı."); }

                // 1. Kullanıcının isteklerini değişkenlere ata
                int toplamIstenenSoru = GetSelectedQuestionCount();
                string sinavTipi = (cmbSinavTipi.SelectedItem as ComboBoxItem).Content.ToString();
                string zorluk = GetSelectedDifficulty();
                string anaKonu = txtAnaKonu.Text.Trim();

                // 2. Veritabanındaki soru havuzunu kontrol et
                List<QuizModel> havuzdanGelenSorular = VeritabaniServisi.GetirHavuzdanSorular(anaKonu, sinavTipi, zorluk, aktifKullaniciId, toplamIstenenSoru);

                int eksikSoruSayisi = toplamIstenenSoru - havuzdanGelenSorular.Count;
                List<QuizModel> tumSorularListesi = new List<QuizModel>(havuzdanGelenSorular);

                // 3. Eksik soru varsa, sadece eksik olanlar için API'ye git
                if (eksikSoruSayisi > 0)
                {
                  
                    List<string> mevcutSorular = VeritabaniServisi.GetirMevcutSoruMetinleri(0);
                    List<string> guncelMevcutSorular = mevcutSorular.Concat(tumSorularListesi.Select(q => q.SoruMetni)).ToList();

                    GeminiModel kullanilacakModel = new List<string> { "Matematik", "Fizik", "Kimya", "Biyoloji" }.Contains(_secilenDers) ? GeminiModel.GeminiPro : GeminiModel.GeminiFlash;
                    GeminiClient gemini = new GeminiClient();

                    string fullPrompt = BuildPrompt(guncelMevcutSorular, eksikSoruSayisi); 
                    List<QuizModel> apiDanGelenSorular = await gemini.GenerateQuestionsAsync(fullPrompt, kullanilacakModel);

                    if (apiDanGelenSorular != null && apiDanGelenSorular.Any())
                    {
                        var kaydedilmisSorular = VeritabaniServisi.KaydetVeGetirSorular(apiDanGelenSorular, sinavTipi, zorluk, anaKonu, aktifKullaniciId);
                        tumSorularListesi.AddRange(kaydedilmisSorular); 
                    }
                }

                // 4. Sonuçları işle ve testi başlat
                if (tumSorularListesi.Count > 0)
                {
                  
                    DuzeltArtArdaSiklari(tumSorularListesi);
                    if (tumSorularListesi.Count > toplamIstenenSoru)
                    {
                        tumSorularListesi = tumSorularListesi.Take(toplamIstenenSoru).ToList();
                    }

                    VeritabaniServisi.IsaretleCozulenSorular(aktifKullaniciId, tumSorularListesi);

                    this.Hide();
                    Sorular yeniPencere = new Sorular(anaKonu, tumSorularListesi, sinavTipi, zorluk, tumSorularListesi.Count);
                    yeniPencere.ShowDialog();
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Ne veritabanında ne de yapay zekada uygun soru bulunamadı.", "Hata", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Sorular oluşturulurken bir hata oluştu: {ex.Message}", "İşlem Başarısız", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
                MainContent.IsEnabled = true;
            }
        }

        private void Button_Geri_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mw = new MainWindow();
            mw.Show();
            this.Close();
        }

      
        private string BuildPrompt(List<string> mevcutSorular, int partiSoruSayisi)
        {
            KullaniciProfili profil = KullaniciOturumu.Profil;
            string zorluk = GetSelectedDifficulty();
            string sinavTipi = (cmbSinavTipi.SelectedItem as ComboBoxItem)?.Content.ToString();

            StringBuilder promptBuilder = new StringBuilder();
            promptBuilder.AppendLine("Aşağıdaki bilgilere dayanarak bir test hazırla.");
            promptBuilder.AppendLine();
          
            promptBuilder.AppendLine($"**KESİN KURAL: Bu test tam olarak {partiSoruSayisi} sorudan oluşmalıdır.**");

            promptBuilder.AppendLine();
            promptBuilder.AppendLine("--- KULLANICI PROFİLİ ---");
            promptBuilder.AppendLine($" - Eğitim Düzeyi: {profil.EgitimDuzeyi}");
            promptBuilder.AppendLine($" - Öğrenim Hedefi: {profil.OgrenimHedefi}");
            promptBuilder.AppendLine("--- TEST İSTEĞİ ---");
            promptBuilder.AppendLine($" - Ders: {_secilenDers}");
            promptBuilder.AppendLine($" - Konu: {txtAnaKonu.Text.Trim()}");
            if (!string.IsNullOrWhiteSpace(txtAltKonular.Text)) { promptBuilder.AppendLine($" - Alt Konular: {txtAltKonular.Text.Trim()}"); }
            promptBuilder.AppendLine($" - Sınav Formatı: {sinavTipi}");
            promptBuilder.AppendLine($" - Soru Sayısı: {partiSoruSayisi}");
            promptBuilder.AppendLine($" - Zorluk: {zorluk}");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("--- EK FORMAT KURALLARI ---");

            if (sinavTipi.Contains("Çoktan Seçmeli"))
            {
                promptBuilder.AppendLine("**Bu testteki tüm sorular YKS (Yükseköğretim Kurumları Sınavı) formatında, paragraf tabanlı ve analiz gerektiren tipte olmalıdır.**");
                promptBuilder.Append(" - **Soru Metni:** ");
                switch (zorluk)
                {
                    case "Zor":
                        promptBuilder.AppendLine("Soru metni, **kesinlikle 5 ila 6 paragraftan oluşan**, son derece detaylı ve derinlemesine analiz gerektiren bir yapıda olmalıdır.");
                        break;
                    case "Orta":
                        promptBuilder.AppendLine("Soru metni, **tam olarak 3 paragraftan oluşan**, okuduğunu anlama ve yorumlama becerilerini ölçen bir yapıda olmalıdır.");
                        break;
                    case "Kolay":
                        promptBuilder.AppendLine("Soru metni, **bir veya en fazla iki paragraftan oluşan**, konunun ana fikrini anlamayı hedefleyen, daha kısa ve net bir yapıda olmalıdır.");
                        break;
                    default:
                        promptBuilder.AppendLine("Soru metni, okuduğunu anlama ve yorumlama becerisini ölçecek uzunlukta, birkaç paragraftan oluşmalıdır.");
                        break;
                }
                promptBuilder.AppendLine(" - **Sözel Alan Şıkları:** Türkçe, Edebiyat, Tarih, Felsefe gibi sözel alanlarda cevap şıkları da uzun, anlamlı ve güçlü çeldiriciler içeren cümleler olmalıdır. Tek kelimelik şıklardan kaçın.");
                promptBuilder.AppendLine(" - **Sayısal Alan Şıkları:** Matematik, Fizik gibi sayısal alanlarda soru bir senaryo içerebilir, ancak cevap şıkları daha net ve sonuç odaklı olabilir.");
                promptBuilder.AppendLine(" - **ŞIK DAĞILIMI KESİN KURALI:** Üreteceğin testin tamamında, doğru cevapların şıklara (A, B, C, D, E) dağılımı **dengeli ve tamamen rastgele** olmak ZORUNDADIR. Testin tamamında doğru cevapların büyük bir çoğunluğunun tek bir şıkta (örneğin tüm cevapların E olması gibi) toplanması KESİNLİKLE YASAKTIR. Ayrıca, aynı doğru cevap şıkkı art arda 3 defadan fazla tekrar edemez.");
            }
            else if (sinavTipi.Contains("Doğru/Yanlış"))
            {
                promptBuilder.AppendLine("**Bu testteki tüm sorular kesinlikle Doğru/Yanlış formatında olmalıdır.**");
                promptBuilder.AppendLine(" - **JSON Kuralı:** Üreteceğin JSON nesnelerinin tamamında `secenekler` alanı `null` olmalıdır.");
                promptBuilder.AppendLine(" - **Cevap Kuralı:** `dogru_cevap` alanı, cümlenin doğruluğuna göre sadece ve sadece `\"Doğru\"` veya `\"Yanlış\"` metnini içermelidir.");
            }
            else if (sinavTipi.Contains("Açık Uçlu") || sinavTipi.Contains("Genel Tekrar"))
            {
                promptBuilder.AppendLine("**Bu testteki tüm sorular kesinlikle açık uçlu olmalıdır. Üreteceğin JSON nesnelerinin tamamında `secenekler` alanı `null` olmalıdır. Asla çoktan seçmeli şık kullanma.**");
            }
            if (mevcutSorular != null && mevcutSorular.Any())
            {
                promptBuilder.AppendLine();
                promptBuilder.AppendLine("--- ÖNEMLİ KURAL: DAHA ÖNCE SORULAN SORULAR ---");
                promptBuilder.AppendLine("**Aşağıdaki listede daha önce sorulmuş sorular yer almaktadır. LÜTFEN bu sorulara ANLAMSAL olarak ÇOK BENZER veya AYNI olan sorular üretmekten KAÇIN.**");
                foreach (var soru in mevcutSorular)
                {
                    promptBuilder.AppendLine($"- {soru}");
                }
            }
            return promptBuilder.ToString();
        }

        private int GetSelectedQuestionCount()
        {
            foreach (RadioButton rb in FindVisualChildren<RadioButton>(this)) { if (rb.GroupName == "SoruSayisi" && rb.IsChecked == true && int.TryParse(rb.Content.ToString(), out int count)) return count; }
            return 10;
        }
        private string GetSelectedDifficulty()
        {
            foreach (RadioButton rb in FindVisualChildren<RadioButton>(this)) { if (rb.GroupName == "Zorluk" && rb.IsChecked == true) return rb.Content.ToString(); }
            return "Orta";
        }
        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T t) { yield return t; }
                    foreach (T childOfChild in FindVisualChildren<T>(child)) { yield return childOfChild; }
                }
            }
        }

        private Window GetParentWindow()
        {
            return Window.GetWindow(this);
        }

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

        private void TitleBar_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
            {
                this.DragMove();
            }
        }

        // Verilen soru listesinde art arda 3'ten fazla aynı doğru şık varsa, bunları programatik olarak düzeltir.

        private void DuzeltArtArdaSiklari(List<QuizModel> sorular)
        {
           
            if (sorular == null || sorular.Count < 4)
            {
                return;
            }

         
            for (int i = 3; i < sorular.Count; i++)
            {
               
                if (sorular[i].DogruCevap == sorular[i - 1].DogruCevap &&
                    sorular[i].DogruCevap == sorular[i - 2].DogruCevap &&
                    sorular[i].DogruCevap == sorular[i - 3].DogruCevap)
                {
                  
                    QuizModel ihlalEdenSoru = sorular[i];
                    string eskiDogruSik = ihlalEdenSoru.DogruCevap;

            
                    var digerSiklar = new List<string> { "A", "B", "C", "D", "E" };
                    string yeniDogruSik = digerSiklar.FirstOrDefault(s => s != eskiDogruSik); 

                   
                    if (yeniDogruSik == null || ihlalEdenSoru.Secenekler == null) continue;

                   
                    string orjinalDogruCevapMetni = ihlalEdenSoru.Secenekler[eskiDogruSik];
                    string digerSikMetni = ihlalEdenSoru.Secenekler[yeniDogruSik];

                   
                    ihlalEdenSoru.Secenekler[yeniDogruSik] = orjinalDogruCevapMetni;
                    ihlalEdenSoru.Secenekler[eskiDogruSik] = digerSikMetni;

                    ihlalEdenSoru.DogruCevap = yeniDogruSik;
                }
            }
        }


    }
}