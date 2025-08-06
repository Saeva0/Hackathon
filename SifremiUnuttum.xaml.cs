using System;
using System.Windows;
using System.Windows.Input;

namespace Hackathon
{
    public partial class SifremiUnuttum : Window
    {
        public SifremiUnuttum()
        {
            InitializeComponent();
            txtEmail.Focus();
        }

        private async void KodGonder_Click(object sender, RoutedEventArgs e)
        {
            string email = txtEmail.Text.Trim();
            if (string.IsNullOrEmpty(email))
            {
                MessageBox.Show("Lütfen e-posta adresinizi girin.", "Eksik Bilgi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            btnKodGonder.IsEnabled = false;
            btnKodGonder.Content = "Gönderiliyor...";

            bool sonuc = VeritabaniServisi.KaydetSifirlamaKodu(email, out string uretilenKod);

            if (sonuc)
            {
                try
                {
                    await EmailServisi.GonderSifirlamaKoduAsync(email, uretilenKod);
                    MessageBox.Show("Doğrulama kodu e-posta adresinize gönderildi. Lütfen gelen kutunuzu (ve spam klasörünü) kontrol edin.", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);

                    KodDogrulama kodDogrulamaPenceresi = new KodDogrulama(email);
                    kodDogrulamaPenceresi.Show();
                    this.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("E-posta gönderilirken bir hata oluştu. Lütfen internet bağlantınızı kontrol edin veya daha sonra tekrar deneyin.\n\nHata Detayı: " + ex.Message, "Gönderim Başarısız", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Bu e-posta adresiyle kayıtlı bir kullanıcı bulunamadı.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            btnKodGonder.IsEnabled = true;
            btnKodGonder.Content = "Doğrulama Kodu Gönder";
        }

       

        private void Geri_Click(object sender, RoutedEventArgs e)
        {
            Giris girisPenceresi = new Giris();
            girisPenceresi.Show();
            this.Close();
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}