using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Hackathon
{
    public partial class Giris : Window
    {
        private bool isPasswordShown = false;

        public Giris()
        {
            InitializeComponent();
        }

        #region Pencere Etkileşimleri

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtEmail.Focus();
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void textEmail_MouseDown(object sender, MouseButtonEventArgs e) { }
        private void txtEmail_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) { }
        private void textPassword_MouseDown(object sender, MouseButtonEventArgs e) { }
        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e) { }

        #endregion

        #region Buton Olay Yöneticileri

        private static string GetConnectionString()
        {
            return ConfigurationManager.AppSettings["DbConnectionString"];
        }

        private async void Button_Click(object sender, RoutedEventArgs e) // Metodu async yapmayı unutma!
        {
            string ad = txtEmail.Text.Trim();
            string girilenSifre = passwordBox.Password;

            if (string.IsNullOrWhiteSpace(ad) || string.IsNullOrWhiteSpace(girilenSifre))
            {
                MessageBox.Show("Lütfen kullanıcı adı ve şifre alanlarını doldurun.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            LoadingOverlay.Visibility = Visibility.Visible;

            try
            {
               
                bool girisBasarili = await Task.Run(() =>
                {
                    using (SqlConnection conn = new SqlConnection(GetConnectionString()))
                    {
                        try
                        {
                            conn.Open();
                            string query = "SELECT ID, SifreHash, SifreSalt, IlkGirisTamamlandi, IsAdmin FROM Kayit WHERE Ad = @Ad COLLATE Latin1_General_BIN2";
                            SqlCommand cmd = new SqlCommand(query, conn);
                            cmd.Parameters.AddWithValue("@Ad", ad);

                            using (SqlDataReader reader = cmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    int kullaniciId = (int)reader["ID"];
                                    byte[] dbHash = (byte[])reader["SifreHash"];
                                    byte[] dbSalt = (byte[])reader["SifreSalt"];
                                    bool ilkGirisTamamlandi = (bool)reader["IlkGirisTamamlandi"];
                                    bool isAdmin = (bool)reader["IsAdmin"];

                                    if (SifrelemeServisi.DogrulaSifre(girilenSifre, dbHash, dbSalt))
                                    {
                                        Dispatcher.Invoke(() =>
                                        {
                                            KullaniciOturumu.KullaniciAdi = ad;
                                            KullaniciOturumu.KullaniciID = kullaniciId;
                                            KullaniciOturumu.IsAdmin = isAdmin;
                                            KullaniciOturumu.Profil = VeritabaniServisi.GetKullaniciProfili(kullaniciId);

                                            if (!ilkGirisTamamlandi)
                                            {
                                                BilgiEkrani bilgiPenceresi = new BilgiEkrani(BilgiModu.IlkGirisUyarisi);
                                                bilgiPenceresi.Show();
                                                this.Close();
                                                VeritabaniServisi.UpdateIlkGirisDurumu(kullaniciId); 
                                            }
                                            else
                                            {
                                                MainWindow main = new MainWindow();
                                                main.Show();
                                                this.Close();
                                            }
                                        });
                                        return true; 
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Dispatcher.Invoke(() => MessageBox.Show("Veritabanı hatası: " + ex.Message, "Hata", MessageBoxButton.OK, MessageBoxImage.Error));
                        }
                    }
                    return false; 
                });

                if (!girisBasarili)
                {
                    MessageBox.Show("Kullanıcı adı veya şifre yanlış!", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Giriş sırasında beklenmedik bir hata oluştu: {ex.Message}", "Kritik Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Kayit kayitPenceresi = new Kayit();
            kayitPenceresi.Show();
            this.Close();
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

        private void SifremiUnuttum_Click(object sender, RoutedEventArgs e)
        {
            SifremiUnuttum sifremiUnuttumPenceresi = new SifremiUnuttum();
            sifremiUnuttumPenceresi.Show();
            this.Close();
        }

        //Şifre Göz Icon
        private void btnSifreGosterGizle_Click(object sender, RoutedEventArgs e)
        {
            isPasswordShown = !isPasswordShown;

            if (isPasswordShown)
            {
                passwordBox.Visibility = Visibility.Collapsed;
                textBoxPassword.Visibility = Visibility.Visible;
                textBoxPassword.Text = passwordBox.Password; // Metni senkronize et

              
                imgSifreIkon.Source = new BitmapImage(new Uri("Images/hidden_icon.png", UriKind.Relative));

                textBoxPassword.Focus();
                textBoxPassword.CaretIndex = textBoxPassword.Text.Length; // İmleci metnin sonuna taşı
            }
            else
            {
                textBoxPassword.Visibility = Visibility.Collapsed;
                passwordBox.Visibility = Visibility.Visible;
                passwordBox.Password = textBoxPassword.Text; // Metni senkronize et


                imgSifreIkon.Source = new BitmapImage(new Uri("Images/eye_icon.png", UriKind.Relative));

                passwordBox.Focus();
            }


        }

        private void passwordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (!isPasswordShown)
            {
                textBoxPassword.Text = passwordBox.Password;
            }
        }

        private void textBoxPassword_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (isPasswordShown)
            {
                passwordBox.Password = textBoxPassword.Text;
            }
        }
        #endregion
    }
}