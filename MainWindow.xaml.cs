using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.UI.WebControls;
using System.Windows;
using System.Windows.Controls;


namespace Hackathon
{
    public class Ders
    {
        public string DersAdi { get; set; }
        public string IkonYolu { get; set; }
    }

    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        

        // Adım 2: Pencere yüklendiğinde bu metot otomatik olarak çalışır.
        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            
            if (!string.IsNullOrEmpty(KullaniciOturumu.KullaniciAdi))
            {
                txtHosgeldin.Text = $"Hoş Geldin, {KullaniciOturumu.KullaniciAdi}!";
            }

            if (KullaniciOturumu.IsAdmin)
            {
                btnAdminPanel.Visibility = Visibility.Visible;
            }
            else
            {
                btnAdminPanel.Visibility = Visibility.Collapsed; // Collapsed, yeri de kaplamaz.
            }

            // Adım 3: Ekranda gösterilecek derslerin listesini oluşturuyoruz.
           
            List<Ders> dersler = new List<Ders>
            {
                new Ders { DersAdi = "Türk Dili ve Edebiyatı", IkonYolu = "/Images/book_icon.png" },
                new Ders { DersAdi = "Matematik", IkonYolu = "/Images/math_icon.png" },
                new Ders { DersAdi = "Fizik", IkonYolu = "/Images/atom_icon.png" },
                new Ders { DersAdi = "Kimya", IkonYolu = "/Images/beaker_icon.png" },
                new Ders { DersAdi = "Biyoloji", IkonYolu = "/Images/dna_icon.png" },
                new Ders { DersAdi = "Tarih", IkonYolu = "/Images/scroll_icon.png" },
                new Ders { DersAdi = "Coğrafya", IkonYolu = "/Images/globe_icon.png" },
                new Ders { DersAdi = "Din Kültürü", IkonYolu = "/Images/mosque_icon.png" }, 
                new Ders { DersAdi = "İngilizce", IkonYolu = "/Images/language_icon.png" },
                new Ders { DersAdi = "Felsefe", IkonYolu = "/Images/philosophy_icon.png" }, 
                new Ders { DersAdi = "Diğer Yabancı Diller", IkonYolu = "/Images/languages_icon.png" },
                new Ders { DersAdi = "Diğerleri", IkonYolu = "/Images/other_icon.png" }
            };

            DerslerItemsControl.ItemsSource = dersler;
        }

        public static string GetConnectionString()
        {
            return ConfigurationManager.AppSettings["DbConnectionString"];
        }

       

        private void Button_Soru(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button tiklananButon)
            {
                string secilenDers = tiklananButon.Tag.ToString();

                if (secilenDers == "Diğerleri")
                {
                    bool digerleriBilgisiniGordu = false;

                    using (SqlConnection conn = new SqlConnection(GetConnectionString()))
                    {
                        try
                        {
                            conn.Open();
                           
                            string query = "SELECT DigerleriBilgisiniGordu FROM Kayit WHERE ID = @KullaniciID";
                            using (SqlCommand cmd = new SqlCommand(query, conn))
                            {
                          
                                cmd.Parameters.AddWithValue("@KullaniciID", KullaniciOturumu.KullaniciID);

                             
                                var result = cmd.ExecuteScalar();
                                if (result != null)
                                {
                                    digerleriBilgisiniGordu = (bool)result;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Veritabanı kontrolü sırasında hata: " + ex.Message);
                            return; 
                        }
                    }

                   
                    if (!digerleriBilgisiniGordu)
                    {
                      
                        BilgiEkrani bilgiSayfasi = new BilgiEkrani(BilgiModu.DigerleriBilgisi);
                        bilgiSayfasi.Show();
                        this.Close();
                    }
                    else 
                    {

                        SoruUretim su = new SoruUretim("Diğerleri");
                        su.Show();
                        this.Close();
                    }
                }
                else
                {
                    
                    SoruUretim su = new SoruUretim(secilenDers);
                    su.Show();
                    this.Close();
                }
            }
        }

        private void AyarlarButton_Click(object sender, RoutedEventArgs e)
        {
            Ayarlar ayarlarPenceresi = new Ayarlar();
            ayarlarPenceresi.Show();
            this.Close();
        }

        private void AdminPanelButton_Click(object sender, RoutedEventArgs e)
        {
            AdminPanel adminPenceresi = new AdminPanel();
            adminPenceresi.Show();
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

        private void GeriButton_Click(object sender, RoutedEventArgs e)
        {
            Giris girisPenceresi = new Giris();
            girisPenceresi.Show();
            this.Close();
        }

        private void TitleBar_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
            {
                this.DragMove();
            }
        }

    }
}