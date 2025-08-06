using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Hackathon
{
    public partial class Analiz : Window
    {
        private readonly List<QuizModel> _analizEdilecekSorular;
        private readonly int _toplamDogru;
        private readonly int _toplamYanlis;
        private readonly string _anaKonu;
        private readonly GeminiClient _geminiClient = new GeminiClient();

        public Analiz(List<QuizModel> sorularListesi, int dogruSayisi, int yanlisSayisi, string anaKonu)
        {
            InitializeComponent();
            _analizEdilecekSorular = sorularListesi;
            _toplamDogru = dogruSayisi;
            _toplamYanlis = yanlisSayisi;
            _anaKonu = anaKonu;
            this.Loaded += AnalizPenceresi_Loaded;
        }


        private void AnalizPenceresi_Loaded(object sender, RoutedEventArgs e)
        {
            ArayuzuDoldur();
        }

        private void ArayuzuDoldur()
        {
            if (_analizEdilecekSorular == null || !_analizEdilecekSorular.Any()) return;

            txtAnalizDogru.Text = _toplamDogru.ToString();
            txtAnalizYanlis.Text = _toplamYanlis.ToString();
            int toplamSoru = _toplamDogru + _toplamYanlis;
            if (toplamSoru > 0)
            {
                double basariOrani = (double)_toplamDogru / toplamSoru * 100;
                txtAnalizBasari.Text = $"%{basariOrani:F0}";
            }
            else
            {
                txtAnalizBasari.Text = "%0";
            }
            txtAnalizKonu.Text = "Konu: " + _anaKonu;
        }


        private async Task YapayZekaAnaliziAl()
        {
            try
            {
                string analizPrompt = BuildAnalysisPrompt();

                txtAITavsiye.Text = "";
                txtAITavsiye.FontStyle = FontStyles.Normal; 

                await _geminiClient.StreamAnalysisAsync(analizPrompt, chunk =>
                {
                  
                    Dispatcher.Invoke(() =>
                    {
                        txtAITavsiye.Text += chunk; 
                    });
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    txtAITavsiye.Text = $"Analiz alınırken bir hata oluştu: {ex.Message}";
                });
            }
        }

        private string BuildAnalysisPrompt()
        {
            var promptBuilder = new StringBuilder();

            // 1. Rol ve Görevi Daha Detaylı Tanımla
            promptBuilder.AppendLine("Sen, alanında uzman, pedagojik formasyona sahip bir öğretmen ve motive edici bir eğitim koçusun.");
            promptBuilder.AppendLine("Bir öğrencinin az önce tamamladığı testin sonuçlarını, güçlü ve zayıf yönlerini ortaya çıkaracak şekilde derinlemesine analiz edeceksin.");
            promptBuilder.AppendLine("Cevabın; yapıcı, teşvik edici, kişiselleştirilmiş ve eyleme dönük tavsiyeler içermelidir.");
            promptBuilder.AppendLine();

            // 2. Analiz Kurallarını Daha Detaylı ve Yapısal Hale Getir
            promptBuilder.AppendLine("--- ANALİZ ÇIKTISI İÇİN KESİN KURALLAR ---");
            promptBuilder.AppendLine("Cevabını aşağıdaki 4 ana başlık altında, Markdown formatını kullanarak yapılandır:");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("### 1. Genel Performans Özeti");
            promptBuilder.AppendLine("   - Öğrencinin genel başarısını (doğru/yanlış sayısı) dikkate alarak kısa ve pozitif bir giriş yap.");
            promptBuilder.AppendLine("   - Testin ana konusundaki genel hakimiyetini değerlendir (ör: '... konusunda temel bilgilere sahipsin' veya '... konusunda derin bir anlayış sergiledin').");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("### 2. Güçlü Yönlerin");
            promptBuilder.AppendLine("   - **(Eğer varsa)** Öğrencinin doğru cevapladığı sorulara dayanarak, hangi alt konularda veya soru tiplerinde başarılı olduğunu belirt. Bu bölümü atlama, pozitif pekiştirme çok önemlidir.");
            promptBuilder.AppendLine("   - Örnek: 'Özellikle neden-sonuç ilişkisi kurmanı gerektiren sorularda başarılı olduğun görülüyor.'");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("### 3. Geliştirilebilecek Alanlar ve Kök Neden Analizi");
            promptBuilder.AppendLine("   - Yanlış cevaplanan soruları analiz ederek ortak temaları ve bilgi eksikliklerini belirle. Sadece 'bu konuda yanlış yaptın' deme, hatanın **kök nedenini** tahmin et.");
            promptBuilder.AppendLine("   - **Kök Neden Örnekleri:** Kavram yanılgısı mı? Tarih ezberi eksikliği mi? Okuduğunu yanlış anlama mı? Dikkatsizlik hatası mı?");
            promptBuilder.AppendLine("   - Örnek: 'Yanlışların, özellikle iki olay arasındaki kronolojik sıralamayı karıştırmandan kaynaklandığı görülüyor. Bu bir bilgi eksikliğinden çok, olayları bir zaman çizelgesine oturtma pratiği eksikliğini gösteriyor olabilir.'");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("### 4. Kişiselleştirilmiş Eylem Planı ve İleriye Yönelik Adımlar");
            promptBuilder.AppendLine("   - Geliştirilecek alanlara yönelik **3 adet somut, uygulanabilir ve çeşitli** tavsiye sun.");
            promptBuilder.AppendLine("   - **Tavsiye Çeşitliliği:** Sadece 'konu tekrarı yap' deme. Farklı öğrenme stillerine hitap et (Ör: bir video izleme, bir makale okuma, bir zihin haritası oluşturma, pratik bir problem çözme).");
            promptBuilder.AppendLine("   - **Pozitif Kapanış:** Öğrenciyi motive edecek ve bir sonraki adımı atmaya teşvik edecek güçlü bir kapanış cümlesi kur. (ör: 'Bu alanlara odaklanarak bir sonraki testte çok daha başarılı olacağına eminim!')");
            promptBuilder.AppendLine();

            // 3. Veri Bölümü
            promptBuilder.AppendLine("--- ANALİZ EDİLECEK VERİLER ---");
            promptBuilder.AppendLine($"**Test Konusu:** {_anaKonu}");
            promptBuilder.AppendLine($"**Test Skoru:** {_toplamDogru} Doğru, {_toplamYanlis} Yanlış");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("**Öğrencinin Yanlış Cevapladığı Sorular ve Doğru Cevapları:**");

            int sayac = 1;
            foreach (var soru in _analizEdilecekSorular.Where(s => !s.IsCorrect))
            {
                promptBuilder.AppendLine($"{sayac}. **Soru:** {soru.SoruMetni}");
                promptBuilder.AppendLine($"   **Kullanıcının Cevabı:** {soru.KullaniciCevabi}");
                promptBuilder.AppendLine($"   **Doğru Cevap:** {soru.DogruCevap}");
                promptBuilder.AppendLine();
                sayac++;
            }

            // Eğer hiç yanlış yoksa, özel bir tebrik mesajı ekle.
            if (_toplamYanlis == 0)
            {
                promptBuilder.AppendLine("Öğrenci bu testteki tüm soruları doğru cevapladı.");
                promptBuilder.AppendLine("Lütfen bu mükemmel performansı öven, başarısının nedenlerini analiz eden (ör: konuya tam hakimiyet, dikkatli okuma) ve bir sonraki adımlar için (ör: daha zor bir konuda kendini test etme) motive edici bir tebrik mesajı yaz.");
            }

            return promptBuilder.ToString();
        }

        private void AnaMenuButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mw = new MainWindow();
            mw.Show();
            this.Close();
        }


        private async void AnalizBaslat_Click(object sender, RoutedEventArgs e)
        {
            btnAnalizBaslat.IsEnabled = false;
            btnAnalizBaslat.Content = "Analiz Ediliyor...";
            txtAITavsiye.Text = "";
            txtAITavsiye.FontStyle = FontStyles.Normal;

            await YapayZekaAnaliziAl();

            btnAnalizBaslat.Visibility = Visibility.Collapsed;
        }
    }
}