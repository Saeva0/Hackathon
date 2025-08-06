using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace Hackathon
{
    public partial class KodDogrulama : Window
    {
        private readonly string _email;
        private TextBox[] _codeBoxes;

        private DispatcherTimer _timer;
        private int _kalanSure = 60;

        public KodDogrulama(string email)
        {
            InitializeComponent();
            _email = email;
            txtBilgi.Text = $"{_email} adresine gönderilen 6 haneli kodu girin.";
            _codeBoxes = new TextBox[] { txt1, txt2, txt3, txt4, txt5, txt6 };
            txt1.Focus();

            // Pencere açılır açılmaz geri sayımı başlat
            GeriSayimiBaslat();
        }

        private void GeriSayimiBaslat()
        {
            _kalanSure = 60;
            btnTekrarGonder.IsEnabled = false;
            btnTekrarGonder.Content = "Tekrar Kod Gönder";

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;
            _timer.Start();

            txtGeriSayim.Text = $"Yeni kod istemek için {_kalanSure} saniye bekleyin.";
            txtGeriSayim.Opacity = 1; 
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            _kalanSure--;
            txtGeriSayim.Text = $"Yeni kod istemek için {_kalanSure} saniye bekleyin.";

            if (_kalanSure <= 0)
            {
                _timer.Stop();
                btnTekrarGonder.IsEnabled = true;
                txtGeriSayim.Opacity = 0; 
            }
        }

        private async void TekrarGonder_Click(object sender, RoutedEventArgs e)
        {
            btnTekrarGonder.IsEnabled = false;
            btnTekrarGonder.Content = "Gönderiliyor...";

            bool sonuc = VeritabaniServisi.KaydetSifirlamaKodu(_email, out string yeniKod);

            if (sonuc)
            {
                try
                {
                    await EmailServisi.GonderSifirlamaKoduAsync(_email, yeniKod);
                    MessageBox.Show("Yeni doğrulama kodu e-posta adresinize gönderildi.", "Başarılı");

                    GeriSayimiBaslat();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("E-posta gönderilirken bir hata oluştu: " + ex.Message, "Hata");
                }
            }
            else
            {
                MessageBox.Show("Kayıtlı e-posta adresi bulunamadı.", "Hata");
            }

            if (!sonuc)
            {
                btnTekrarGonder.IsEnabled = true;
                btnTekrarGonder.Content = "Tekrar Kod Gönder";
            }
        }
        private void Dogrula_Click(object sender, RoutedEventArgs e)
        {
            string girilenKod = string.Concat(_codeBoxes.Select(box => box.Text));

            if (girilenKod.Length != 6)
            {
                MessageBox.Show("Lütfen 6 haneli kodu eksiksiz girin.", "Geçersiz Kod", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            btnDogrula.IsEnabled = false;
            btnDogrula.Content = "Doğrulanıyor...";

            bool kodDogruMu = VeritabaniServisi.DogrulaSifirlamaKodu(_email, girilenKod);

            if (kodDogruMu)
            {
                MessageBox.Show("Kod doğrulandı! Şimdi yeni şifrenizi belirleyebilirsiniz.", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);

                YeniSifre yeniSifrePenceresi = new YeniSifre(_email);
                yeniSifrePenceresi.Show();
                this.Close();
            }
            else
            {
                MessageBox.Show("Girdiğiniz kod yanlış veya süresi dolmuş. Lütfen tekrar deneyin.", "Doğrulama Başarısız", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            btnDogrula.IsEnabled = true;
            btnDogrula.Content = "Kodu Doğrula";
        }

        // --- YENİ VE AKILLI OTOMATİK ATLAMA/DOLDURMA MANTIĞI ---

        private void CodeBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !e.Text.All(char.IsDigit);
        }

        private void CodeBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox currentBox = sender as TextBox;

          
            if (currentBox.Text.Length == 1)
            {
                FocusNavigationDirection direction = FocusNavigationDirection.Next;
                TraversalRequest request = new TraversalRequest(direction);
                currentBox.MoveFocus(request);
            }
        }

        private void CodeBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            TextBox currentBox = sender as TextBox;

           
            if (e.Key == Key.Back && string.IsNullOrEmpty(currentBox.Text))
            {
                FocusNavigationDirection direction = FocusNavigationDirection.Previous;
                TraversalRequest request = new TraversalRequest(direction);
                UIElement focusedElement = Keyboard.FocusedElement as UIElement;
                if (focusedElement != null)
                {
                    focusedElement.MoveFocus(request);
                }
            }

            if (e.Key == Key.V && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
               
                string pastedText = Clipboard.GetText();

               
                if (!string.IsNullOrEmpty(pastedText) && pastedText.Length == 6 && pastedText.All(char.IsDigit))
                {
                    
                    e.Handled = true;

                   
                    for (int i = 0; i < _codeBoxes.Length; i++)
                    {
                        _codeBoxes[i].Text = pastedText[i].ToString();
                    }

                    
                    _codeBoxes.Last().Focus();
                    _codeBoxes.Last().SelectAll();
                }
            }
        }

       
        private void Border_MouseDown(object sender, MouseButtonEventArgs e) { if (e.ChangedButton == MouseButton.Left) { this.DragMove(); } }
        private void Minimize_Click(object sender, RoutedEventArgs e) { this.WindowState = WindowState.Minimized; }
        private void Close_Click(object sender, RoutedEventArgs e) { Giris gr = new Giris(); gr.Show(); this.Close(); }
    }
}