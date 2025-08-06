using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Hackathon
{
    public partial class Kayit : Window
    {
        private bool isPasswordShown = false;
        public Kayit() { InitializeComponent(); }
        private void Border_MouseDown(object sender, MouseButtonEventArgs e) { this.DragMove(); }
        private void textBoxPassword_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (isPasswordShown)
            {
                passwordBox.Password = textBoxPassword.Text;
            }
        }

        private bool SifreGecerliMi(string sifre, out string hataMesaji)
        {
            hataMesaji = string.Empty;
            if (sifre.Length < 8) { hataMesaji = "Şifre en az 8 karakter olmalıdır."; return false; }
            if (!sifre.Any(char.IsUpper)) { hataMesaji = "Şifre en az bir büyük harf içermelidir."; return false; }
            if (!sifre.Any(char.IsLower)) { hataMesaji = "Şifre en az bir küçük harf içermelidir."; return false; }
            if (!sifre.Any(char.IsDigit)) { hataMesaji = "Şifre en az bir rakam içermelidir."; return false; }
            if (!sifre.Any(c => !char.IsLetterOrDigit(c))) { hataMesaji = "Şifre en az bir özel karakter içermelidir."; return false; }
            return true;
        }

        public static string GetConnectionString()
        {
            return ConfigurationManager.AppSettings["DbConnectionString"];
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string anaSifre = isPasswordShown ? textBoxPassword.Text : passwordBox.Password;
            string tekrarSifre = isPasswordShown ? txtSifreTekrarAcik.Text : txtSifreTekrar.Password;

            passwordBox.BorderBrush = new SolidColorBrush(Color.FromRgb(30, 60, 114));
            txtSifreTekrar.BorderBrush = new SolidColorBrush(Color.FromRgb(30, 60, 114));
            textBoxPassword.BorderBrush = new SolidColorBrush(Color.FromRgb(30, 60, 114)); 
            txtSifreTekrarAcik.BorderBrush = new SolidColorBrush(Color.FromRgb(30, 60, 114)); 
            txtSifreHataMesaji.Visibility = Visibility.Collapsed;

            if (string.IsNullOrWhiteSpace(txtKullaniciAdi.Text) ||
                string.IsNullOrWhiteSpace(txtEmail.Text) ||
                string.IsNullOrWhiteSpace(anaSifre) || 
                dpDogumTarihi.SelectedDate == null ||
                cmbEgitim.SelectedItem == null ||
                cmbHedef.SelectedItem == null ||
                string.IsNullOrWhiteSpace(tekrarSifre)) 
            {
                MessageBox.Show("Lütfen tüm zorumlu alanları doldurun.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (anaSifre != tekrarSifre) 
            {
                passwordBox.BorderBrush = Brushes.Red;
                txtSifreTekrar.BorderBrush = Brushes.Red;
                textBoxPassword.BorderBrush = Brushes.Red;
                txtSifreTekrarAcik.BorderBrush = Brushes.Red; 
                MessageBox.Show("Şifreler uyuşmuyor.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!SifreGecerliMi(anaSifre, out string sifreHataMesaji)) 
            {
                txtSifreHataMesaji.Text = sifreHataMesaji;
                txtSifreHataMesaji.Visibility = Visibility.Visible;
                MessageBox.Show(sifreHataMesaji, "Zayıf Şifre", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DateTime dogumTarihi = dpDogumTarihi.SelectedDate.Value;

            using (SqlConnection conn = new SqlConnection(GetConnectionString()))
            {
                try
                {
                    conn.Open();

                    string kontrolSorgu = "SELECT COUNT(*) FROM Kayit WHERE Ad = @Ad COLLATE Latin1_General_BIN2";
                    SqlCommand kontrolCmd = new SqlCommand(kontrolSorgu, conn);
                    kontrolCmd.Parameters.AddWithValue("@Ad", txtKullaniciAdi.Text);
                    int mevcutKayitSayisi = (int)kontrolCmd.ExecuteScalar();
                    if (mevcutKayitSayisi > 0)
                    {
                        MessageBox.Show("Bu kullanıcı adıyla zaten bir kayıt mevcut!", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                  
                    var (sifreHash, sifreSalt) = SifrelemeServisi.HashSifre(anaSifre); 

                    string sql = @"INSERT INTO Kayit 
                       (Ad, Email, SifreHash, SifreSalt, Yas, EgitimDuzeyi, OgrenimHedefi, 
                        PrefCoktanSecmeli, PrefDogruYanlis, PrefAcikUclu, PrefKodOrnegi) 
                   VALUES 
                       (@Ad, @Email, @SifreHash, @SifreSalt, @Yas, @EgitimDuzeyi, @OgrenimHedefi, 
                        @PrefCoktanSecmeli, @PrefDogruYanlis, @PrefAcikUclu, @PrefKodOrnegi)";

                    SqlCommand cmd = new SqlCommand(sql, conn);

                    cmd.Parameters.AddWithValue("@Ad", txtKullaniciAdi.Text);
                    cmd.Parameters.AddWithValue("@Email", txtEmail.Text);
                    cmd.Parameters.AddWithValue("@SifreHash", sifreHash);
                    cmd.Parameters.AddWithValue("@SifreSalt", sifreSalt);
                    cmd.Parameters.AddWithValue("@Yas", dogumTarihi);
                    cmd.Parameters.AddWithValue("@EgitimDuzeyi", cmbEgitim.Text);
                    cmd.Parameters.AddWithValue("@OgrenimHedefi", cmbHedef.Text);
                    cmd.Parameters.AddWithValue("@PrefCoktanSecmeli", chkCoktanSecmeli.IsChecked == true);
                    cmd.Parameters.AddWithValue("@PrefDogruYanlis", chkDogruYanlis.IsChecked == true);
                    cmd.Parameters.AddWithValue("@PrefAcikUclu", chkAcikUclu.IsChecked == true);
                    cmd.Parameters.AddWithValue("@PrefKodOrnegi", chkKodOrnegi.IsChecked == true);

                    int result = cmd.ExecuteNonQuery();

                    if (result > 0)
                    {
                        MessageBox.Show("Kayıt başarıyla eklendi! Giriş ekranına yönlendiriliyorsunuz.", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                        Giris GirisPenceresi = new Giris();
                        GirisPenceresi.Show();
                        this.Close();
                    }
                    else
                    {
                        MessageBox.Show("Kayıt eklenemedi.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Veritabanı Hatası: " + ex.Message, "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void passwordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (!isPasswordShown)
            {
                textBoxPassword.Text = passwordBox.Password;
            }

            string sifre = passwordBox.Password;
            int guc = 0;
            if (sifre.Length >= 8) guc++;
            if (sifre.Any(char.IsUpper)) guc++;
            if (sifre.Any(char.IsLower)) guc++;
            if (sifre.Any(char.IsDigit)) guc++;
            if (sifre.Any(c => !char.IsLetterOrDigit(c))) guc++;

            pbSifreGucu.Value = guc;
            switch (guc)
            {
                case 0: case 1: case 2: pbSifreGucu.Foreground = Brushes.Red; break;
                case 3: pbSifreGucu.Foreground = Brushes.Orange; break;
                case 4: pbSifreGucu.Foreground = Brushes.YellowGreen; break;
                case 5: pbSifreGucu.Foreground = Brushes.LawnGreen; break;
            }
        }

        private void SifreGoster_Checked(object sender, RoutedEventArgs e)
        {
            isPasswordShown = true;

            passwordBox.Visibility = Visibility.Collapsed;
            textBoxPassword.Visibility = Visibility.Visible;
            txtSifreTekrar.Visibility = Visibility.Collapsed;
            txtSifreTekrarAcik.Visibility = Visibility.Visible;

            textBoxPassword.Text = passwordBox.Password;
            txtSifreTekrarAcik.Text = txtSifreTekrar.Password;
        }

        private void SifreGoster_Unchecked(object sender, RoutedEventArgs e)
        {
            isPasswordShown = false;

            textBoxPassword.Visibility = Visibility.Collapsed;
            passwordBox.Visibility = Visibility.Visible;
            txtSifreTekrarAcik.Visibility = Visibility.Collapsed;
            txtSifreTekrar.Visibility = Visibility.Visible;

            passwordBox.Password = textBoxPassword.Text;
            txtSifreTekrar.Password = txtSifreTekrarAcik.Text;
        }



        private Window GetParentWindow() { return Window.GetWindow(this); }
        private void Minimize_Click(object sender, RoutedEventArgs e) { Window parentWindow = GetParentWindow(); if (parentWindow != null) { parentWindow.WindowState = WindowState.Minimized; } }
        private void Maximize_Click(object sender, RoutedEventArgs e) { Window parentWindow = GetParentWindow(); if (parentWindow != null) { parentWindow.WindowState = parentWindow.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized; } }
        private void Close_Click(object sender, RoutedEventArgs e) { Window parentWindow = GetParentWindow(); if (parentWindow != null) { parentWindow.Close(); } }
        private void GeriButton_Click(object sender, RoutedEventArgs e) { Giris girisEkrani = new Giris(); girisEkrani.Show(); this.Close(); }
    }
}