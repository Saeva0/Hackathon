using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media; // Brushes için

namespace Hackathon
{
    public partial class YeniSifre : Window
    {
        private readonly string _email;

        public YeniSifre(string email)
        {
            InitializeComponent();
            _email = email;
        }

        private bool SifreGecerliMi(string sifre, out string hataMesaji)
        {
            hataMesaji = string.Empty;

            if (sifre.Length < 8)
            {
                hataMesaji = "Şifre en az 8 karakter uzunluğunda olmalıdır.";
                return false;
            }
            if (!sifre.Any(char.IsUpper))
            {
                hataMesaji = "Şifre en az bir büyük harf içermelidir.";
                return false;
            }
            if (!sifre.Any(char.IsLower))
            {
                hataMesaji = "Şifre en az bir küçük harf içermelidir.";
                return false;
            }
            if (!sifre.Any(char.IsDigit))
            {
                hataMesaji = "Şifre en az bir rakam içermelidir.";
                return false;
            }
            if (!sifre.Any(c => !char.IsLetterOrDigit(c)))
            {
                hataMesaji = "Şifre en az bir özel karakter içermelidir (ör: !, ?, *, ., _).";
                return false;
            }

            return true;
        }

        private void Guncelle_Click(object sender, RoutedEventArgs e)
        {
            pbSifreYeni.BorderBrush = SystemColors.ControlDarkBrush;
            pbSifreYeniTekrar.BorderBrush = SystemColors.ControlDarkBrush;
            txtSifreHataMesaji.Visibility = Visibility.Collapsed;

            string yeniSifre = pbSifreYeni.Password;
            string yeniSifreTekrar = pbSifreYeniTekrar.Password;

            if (string.IsNullOrWhiteSpace(yeniSifre) || string.IsNullOrWhiteSpace(yeniSifreTekrar))
            {
                MessageBox.Show("Lütfen tüm alanları doldurun.", "Hata");
                return;
            }

            if (yeniSifre != yeniSifreTekrar)
            {
                pbSifreYeni.BorderBrush = Brushes.Red;
                pbSifreYeniTekrar.BorderBrush = Brushes.Red;
                MessageBox.Show("Girdiğiniz şifreler uyuşmuyor.", "Hata");
                return;
            }

            if (!SifreGecerliMi(yeniSifre, out string sifreHataMesaji))
            {
                txtSifreHataMesaji.Text = sifreHataMesaji;
                txtSifreHataMesaji.Visibility = Visibility.Visible;
                pbSifreYeni.BorderBrush = Brushes.Red;
                return;
            }

            try
            {
                bool ayniSifreMi = VeritabaniServisi.CheckSifreByEmail(_email, yeniSifre);

                if (ayniSifreMi)
                {
                    MessageBox.Show("Yeni şifreniz eski şifrenizle aynı olamaz. Lütfen farklı bir şifre belirleyin.", "Güvenlik Uyarısı", MessageBoxButton.OK, MessageBoxImage.Warning);
                    pbSifreYeni.Password = string.Empty;
                    pbSifreYeniTekrar.Password = string.Empty;
                    pbSifreYeni.Focus();
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Eski şifre kontrol edilirken bir hata oluştu: " + ex.Message, "Hata");
                return;
            }

            bool sonuc = VeritabaniServisi.GuncelleSifre(_email, yeniSifre);

            if (sonuc)
            {
                MessageBox.Show("Şifreniz başarıyla güncellendi! Giriş ekranına yönlendiriliyorsunuz.", "Başarılı");

                Giris girisPenceresi = new Giris();
                girisPenceresi.Show();
                this.Close();
            }
            else
            {
                MessageBox.Show("Şifre güncellenirken bir hata oluştu.", "Hata");
            }
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
            Giris gr = new Giris();
            gr.Show();
            this.Close();

        }
    }
}