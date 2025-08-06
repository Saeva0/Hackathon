using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace Hackathon
{
    public partial class Sorular : Window
    {
        #region Sınıf Değişkenleri

        private readonly GeminiClient _geminiClient = new GeminiClient();
        private readonly List<QuizModel> _sorularListesi;
        private readonly string _sinavTipi;
        private readonly string _anaKonu;
        private int _mevcutSoruIndex = 0;
        private int _dogruSayisi = 0;
        private int _yanlisSayisi = 0;
        private bool _isClosingProgrammatically = false;
        private DispatcherTimer _timer;
        private TimeSpan _elapsedTime = TimeSpan.Zero;
        private bool _isTimerRunning = false;

        private readonly string _zorluk;
        private readonly int _soruSayisi;

        #endregion

        #region Pencere Yönetimi

        public Sorular()
        {
            InitializeComponent();
        }

        public Sorular(string anaKonu, List<QuizModel> sorularListesi, string sinavTipi, string zorluk, int soruSayisi)
        {
            InitializeComponent();
            _sorularListesi = sorularListesi;
            _sinavTipi = sinavTipi;
            _anaKonu = anaKonu;
            _zorluk = zorluk;
            _soruSayisi = soruSayisi;

            this.Title = $"Test Ekranı: {anaKonu} ({_sinavTipi})";



            // Sınav tipine göre doğru paneli görünür yap
            switch (_sinavTipi)
            {
               
                case "Açık Uçlu":
                    OpenEndedPanel.Visibility = Visibility.Visible;
                    break;

                case "Doğru/Yanlış":
                    TrueFalsePanel.Visibility = Visibility.Visible;
                    break;

               
                case "Boşluk Doldurma":
                    FillInTheBlankPanel.Visibility = Visibility.Visible;
                    break;

               
                case "Çoktan Seçmeli Test":
                case "YKS / TYT / AYT Formatı":
                default:
                    MultipleChoicePanel.Visibility = Visibility.Visible;
                    break;
            }

            InitializeTimer();
            SoruyuGoster(_mevcutSoruIndex);
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (_isClosingProgrammatically) { return; }

            MessageBoxResult result = MessageBox.Show(
                "Testten çıkmak istediğinize emin misiniz? İlerlemeniz kaydedilmeyecek.",
                "Çıkışı Onayla",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _timer.Stop();
            }
            else
            {
                e.Cancel = true;
            }

            MainWindow mw = new MainWindow();
            mw.Show();
        }

        #endregion

        #region Soru ve Arayüz Yönetimi

        private void SoruyuGoster(int index)
        {
            if (index >= _sorularListesi.Count)
            {
                SkoruGoster();
                return;
            }

            ResetPanels();
            QuizModel mevcutSoru = _sorularListesi[index];
            txtSoruNumarasi.Text = $"Soru {index + 1} / {_sorularListesi.Count}";

            switch (_sinavTipi)
            {
                case "Açık Uçlu":
                    txtSoruMetni.Text = mevcutSoru.SoruMetni;
                    txtOpenEndedAnswer.Text = "";
                    break;

                case "Doğru/Yanlış":
                    
                    txtSoruMetni.Text = mevcutSoru.SoruMetni;
                    break;

                case "Boşluk Doldurma":
                    txtSoruMetni.Visibility = Visibility.Collapsed;
                    var parts = mevcutSoru.SoruMetni.Split(new[] { "[BOŞLUK]" }, StringSplitOptions.None);
                    txtFillBlankPart1.Text = parts.Length > 0 ? parts[0] : "";
                    txtFillBlankPart2.Text = parts.Length > 1 ? parts[1] : "";
                    txtFillBlankAnswer.Text = "";
                    break;

                case "Çoktan Seçmeli Test":
                case "YKS / TYT / AYT Formatı":
                default:
                    txtSoruMetni.Text = mevcutSoru.SoruMetni;
                    if (mevcutSoru.Secenekler == null || mevcutSoru.Secenekler.Count < 5)
                    { _mevcutSoruIndex++; SoruyuGoster(_mevcutSoruIndex); return; }
                    btnSikA.Content = $"A) {mevcutSoru.Secenekler["A"]}";
                    btnSikB.Content = $"B) {mevcutSoru.Secenekler["B"]}";
                    btnSikC.Content = $"C) {mevcutSoru.Secenekler["C"]}";
                    btnSikD.Content = $"D) {mevcutSoru.Secenekler["D"]}";
                    btnSikE.Content = $"E) {mevcutSoru.Secenekler["E"]}";
                    break;
            }

            MainScrollViewer.ScrollToTop();
        }

     

        private void SkoruGoster()
        {
            _isClosingProgrammatically = true;
            QuestionPanel.Visibility = Visibility.Collapsed;
            ScoreScreen.Visibility = Visibility.Visible;
            txtDogruSayisi.Text = _dogruSayisi.ToString();
            txtYanlisSayisi.Text = _yanlisSayisi.ToString();
            _timer.Stop();
            TimerPanel.Visibility = Visibility.Collapsed;

        
            if (_soruSayisi >= 30 && _zorluk == "Zor")
            {
                btnAnaliz.Visibility = Visibility.Visible;
            }

          
            if (_yanlisSayisi > 0)
            {
                btnTekrarCoz.Visibility = Visibility.Visible;
            }
        }

        #endregion

        #region Buton Olay Yöneticileri

        private async void AnswerButton_Click(object sender, RoutedEventArgs e)
        {
            MultipleChoicePanel.IsEnabled = false;
            btnIpucu.IsEnabled = false;
            Button tiklananButon = sender as Button;

           
            string secilenCevap = tiklananButon.Tag.ToString();
            QuizModel mevcutSoru = _sorularListesi[_mevcutSoruIndex]; // Mevcut soruyu bir değişkene al
            bool sonucDogruMu = (secilenCevap == mevcutSoru.DogruCevap);


            mevcutSoru.KullaniciCevabi = secilenCevap;
            mevcutSoru.IsCorrect = sonucDogruMu;
            


            int kullaniciId = KullaniciOturumu.KullaniciID;
            int soruId = mevcutSoru.ID;
            VeritabaniServisi.KaydetCevap(kullaniciId, soruId, secilenCevap, sonucDogruMu);

           
            Border border = (Border)tiklananButon.Template.FindName("border", tiklananButon);
            if (sonucDogruMu)
            {
                _dogruSayisi++;
                border.BorderBrush = new SolidColorBrush(Colors.LawnGreen);
            }
            else
            {
                _yanlisSayisi++;
                border.BorderBrush = new SolidColorBrush(Colors.Red);
                Button dogruButon = (Button)FindName("btnSik" + mevcutSoru.DogruCevap);
                if (dogruButon != null)
                {
                    Border dogruBorder = (Border)dogruButon.Template.FindName("border", dogruButon);
                    dogruBorder.BorderBrush = new SolidColorBrush(Colors.LawnGreen);
                }
            }
            await GoToNextQuestionAfterDelay();
        }

        private async void CheckAnswerButton_Click(object sender, RoutedEventArgs e)
        {
            btnCheckFillBlank.IsEnabled = false;

           
            string userAnswer = txtFillBlankAnswer.Text.Trim();
            QuizModel mevcutSoru = _sorularListesi[_mevcutSoruIndex];
            bool sonucDogruMu = string.Equals(userAnswer, mevcutSoru.DogruCevap, StringComparison.OrdinalIgnoreCase);

            
            mevcutSoru.KullaniciCevabi = userAnswer;
            mevcutSoru.IsCorrect = sonucDogruMu;
            

            
            int kullaniciId = KullaniciOturumu.KullaniciID;
            int soruId = mevcutSoru.ID;
            VeritabaniServisi.KaydetCevap(kullaniciId, soruId, userAnswer, sonucDogruMu);

           
            if (sonucDogruMu)
            {
                _dogruSayisi++;
                txtFillBlankAnswer.Background = Brushes.LightGreen;
            }
            else
            {
                _yanlisSayisi++;
                txtFillBlankAnswer.Background = Brushes.LightCoral;
                txtFillBlankAnswer.Text += $" (Doğru: {mevcutSoru.DogruCevap})";
            }
            await GoToNextQuestionAfterDelay();
        }

     
        private void btnShowAnswer_Click(object sender, RoutedEventArgs e)
        {
            btnShowAnswer.Visibility = Visibility.Collapsed;
            txtOpenEndedAnswer.IsReadOnly = true;
            btnIpucu.IsEnabled = false;

            string correctAnswer = _sorularListesi[_mevcutSoruIndex].DogruCevap;
            txtCorrectAnswer.Text = correctAnswer;
            CorrectAnswerPanel.Visibility = Visibility.Visible;

            EvaluationPanel.Visibility = Visibility.Visible;
        }

        private async void EvaluationButton_Click(object sender, RoutedEventArgs e)
        {
            EvaluationPanel.IsEnabled = false;

           
            string evaluation = (sender as Button).Tag.ToString();
            bool sonucDogruMu = (evaluation == "Dogru");
            string userAnswer = txtOpenEndedAnswer.Text;
            QuizModel mevcutSoru = _sorularListesi[_mevcutSoruIndex];

           
            mevcutSoru.KullaniciCevabi = userAnswer;
            mevcutSoru.IsCorrect = sonucDogruMu;

            int kullaniciId = KullaniciOturumu.KullaniciID;
            int soruId = mevcutSoru.ID;
            VeritabaniServisi.KaydetCevap(kullaniciId, soruId, userAnswer, sonucDogruMu);

           
            if (sonucDogruMu)
            {
                _dogruSayisi++;
                txtOpenEndedAnswer.BorderBrush = Brushes.LawnGreen;
                txtOpenEndedAnswer.BorderThickness = new Thickness(2);
            }
            else
            {
                _yanlisSayisi++;
                txtOpenEndedAnswer.BorderBrush = Brushes.Red;
                txtOpenEndedAnswer.BorderThickness = new Thickness(2);
            }

            await GoToNextQuestionAfterDelay();
        }

        private async void btnIpucu_Click(object sender, RoutedEventArgs e)
        {
            if (txtIpucu.Visibility == Visibility.Visible) { txtIpucu.Visibility = Visibility.Collapsed; btnIpucu.Content = "İpucu İste"; return; }
            btnIpucu.IsEnabled = false; btnIpucu.Content = "Hazırlanıyor..."; txtIpucu.Text = "Yapay zekadan ipucu isteniyor, lütfen bekleyin..."; txtIpucu.Visibility = Visibility.Visible;
            try
            {
                QuizModel mevcutSoru = _sorularListesi[_mevcutSoruIndex];
                string hint = await _geminiClient.GenerateHintAsync(mevcutSoru);
                txtIpucu.Text = hint;
                btnIpucu.Content = "İpucu Gizle";
            }
            catch (Exception ex) { txtIpucu.Text = $"Bir hata oluştu: {ex.Message}"; btnIpucu.Content = "Tekrar Dene"; }
            finally { btnIpucu.IsEnabled = true; }
        }

        private void KapatButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mw = new MainWindow();
            mw.Show();
            this.Close();
        }

        private void btnStartStopTimer_Click(object sender, RoutedEventArgs e)
        {
            if (_isTimerRunning) { _timer.Stop(); btnStartStopTimer.Content = "Devam Et"; }
            else { _timer.Start(); btnStartStopTimer.Content = "Durdur"; }
            _isTimerRunning = !_isTimerRunning;
        }

        #endregion

        #region Yardımcı Metodlar

        private async Task GoToNextQuestionAfterDelay()
        {
            await Task.Delay(1500);
            _mevcutSoruIndex++;
            SoruyuGoster(_mevcutSoruIndex);
        }

        private void ResetPanels()
        {
            txtSoruMetni.Foreground = Brushes.White;
            txtFillBlankPart1.Foreground = Brushes.White;
            txtFillBlankPart2.Foreground = Brushes.White;

           
            foreach (Button btn in MultipleChoicePanel.Children.OfType<Button>())
            {
                var border = btn.Template.FindName("border", btn) as Border;
                if (border != null) { border.BorderBrush = Brushes.Transparent; }
            }
            MultipleChoicePanel.IsEnabled = true;

            btnDogru.IsEnabled = true;
            btnYanlis.IsEnabled = true;
           
            txtFillBlankAnswer.Background = SystemColors.WindowBrush;
            txtFillBlankAnswer.Foreground = SystemColors.ControlTextBrush;
            btnCheckFillBlank.IsEnabled = true;

            txtIpucu.Visibility = Visibility.Collapsed;
            btnIpucu.Content = "İpucu İste";
            btnIpucu.IsEnabled = true;

            btnShowAnswer.Visibility = Visibility.Visible;
            CorrectAnswerPanel.Visibility = Visibility.Collapsed;
            EvaluationPanel.Visibility = Visibility.Collapsed;
            EvaluationPanel.IsEnabled = true;
            txtOpenEndedAnswer.IsReadOnly = false;
            txtOpenEndedAnswer.BorderBrush = Brushes.Transparent;
        }

        private void InitializeTimer()
        {
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += DispatcherTimer_Tick;
        }

        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            _elapsedTime = _elapsedTime.Add(TimeSpan.FromSeconds(1));
            txtTimerDisplay.Text = _elapsedTime.ToString(@"mm\:ss");
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

        private void AnalizButton_Click(object sender, RoutedEventArgs e)
        {
            Analiz analizPenceresi = new Analiz(_sorularListesi, _dogruSayisi, _yanlisSayisi, _anaKonu); // <-- _anaKonu'yu EKLE
            analizPenceresi.Show();
            this.Close();
        }

      

        private void TekrarCoz_Click(object sender, RoutedEventArgs e)
        {
           
            List<QuizModel> yanlisSorular = _sorularListesi.Where(q => !q.IsCorrect).ToList();

            Random rng = new Random();
           
            foreach (var soru in yanlisSorular)
            {
                if (soru.Secenekler == null || soru.Secenekler.Count == 0) continue;

                var siklar = soru.Secenekler.Keys.ToList();
                var cevaplar = soru.Secenekler.Values.ToList();
                string orjinalDogruCevapMetni = soru.Secenekler[soru.DogruCevap];

              
                cevaplar = cevaplar.OrderBy(a => rng.Next()).ToList();

                var yeniSecenekler = new Dictionary<string, string>();
                for (int i = 0; i < siklar.Count; i++)
                {
                    yeniSecenekler[siklar[i]] = cevaplar[i];
                }

                string yeniDogruSik = yeniSecenekler.FirstOrDefault(x => x.Value == orjinalDogruCevapMetni).Key;

                soru.Secenekler = yeniSecenekler;
                soru.DogruCevap = yeniDogruSik;
            }

           
            Sorular yeniTestPenceresi = new Sorular(_anaKonu + " (Tekrar)", yanlisSorular, _sinavTipi, _zorluk, yanlisSorular.Count);
            yeniTestPenceresi.Show();

            _isClosingProgrammatically = true;
            this.Close();
        }

      

        private async void TrueFalse_Click(object sender, RoutedEventArgs e)
        {
           
            btnDogru.IsEnabled = false;
            btnYanlis.IsEnabled = false;

            Button tiklananButon = sender as Button;

          
            string secilenCevap = tiklananButon.Tag.ToString();
            QuizModel mevcutSoru = _sorularListesi[_mevcutSoruIndex];
            bool sonucDogruMu = (secilenCevap == mevcutSoru.DogruCevap);

           
            mevcutSoru.KullaniciCevabi = secilenCevap;
            mevcutSoru.IsCorrect = sonucDogruMu;

            VeritabaniServisi.KaydetCevap(KullaniciOturumu.KullaniciID, mevcutSoru.ID, secilenCevap, sonucDogruMu);

           
            if (sonucDogruMu)
            {
                _dogruSayisi++;
                txtSoruMetni.Foreground = Brushes.LawnGreen;
            }
            else
            {
                _yanlisSayisi++;
                txtSoruMetni.Foreground = Brushes.Red; 
            }
            await GoToNextQuestionAfterDelay();
        }
        #endregion
    }
}