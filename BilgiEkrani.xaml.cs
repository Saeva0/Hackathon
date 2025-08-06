using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Input;

namespace Hackathon
{
    public enum BilgiModu
    {
        IlkGirisUyarisi,
        DigerleriBilgisi
    }

    public partial class BilgiEkrani : Window
    {
        private readonly BilgiModu _mevcutMod;

        public BilgiEkrani(BilgiModu mod)
        {
            InitializeComponent();
            _mevcutMod = mod;
            this.Loaded += BilgiEkrani_Loaded;
        }

        private void BilgiEkrani_Loaded(object sender, RoutedEventArgs e)
        {
            IcerigiAyarla();
        }

        private void IcerigiAyarla()
        {
            switch (_mevcutMod)
            {
                case BilgiModu.IlkGirisUyarisi:
                    txtBaslik.Text = "Hoş Geldiniz & Önemli Uyarı";
                    txtIcerik.Text = "Lütfen unutmayın: Bu uygulama yapay zeka tarafından desteklenmektedir. " +
                                     "Üretilen soruların ve cevapların doğruluğuna %100 güvenilmemelidir.\n\n" +
                                     "Bilgileri her zaman güvenilir kaynaklardan teyit etmenizi öneririz.";
                    btnOnay.Content = "Anladım, Devam Et";
                    break;

                case BilgiModu.DigerleriBilgisi:
                    txtBaslik.Text = "Diğerleri Kategorisi Hakkında";
                    txtIcerik.Text = "Bu kategori, belirtilen ana dersler dışındaki tüm konular için genel bir soru üretme alanıdır.\n\n" +
                                     "Buradan yazılım, sanat tarihi, müzik veya aklınıza gelebilecek herhangi bir alanla ilgili sorular üretebilirsiniz.";
                    btnOnay.Content = "Anladım, Devam Et";
                    break;
            }
        }

        public static string GetConnectionString()
        {
            return ConfigurationManager.AppSettings["DbConnectionString"];
        }
        private void AnladimButton_Click(object sender, RoutedEventArgs e)
        {
            switch (_mevcutMod)
            {
                case BilgiModu.IlkGirisUyarisi:
                    MainWindow anaSayfa = new MainWindow();
                    anaSayfa.Show();
                    this.Close();
                    break;

                case BilgiModu.DigerleriBilgisi:

                    using (SqlConnection conn = new SqlConnection(GetConnectionString()))
                    {
                        try
                        {
                            conn.Open();
                            string updateQuery = "UPDATE Kayit SET DigerleriBilgisiniGordu = 1 WHERE ID = @KullaniciID";
                            using (SqlCommand updateCmd = new SqlCommand(updateQuery, conn))
                            {
                                updateCmd.Parameters.AddWithValue("@KullaniciID", KullaniciOturumu.KullaniciID);
                                updateCmd.ExecuteNonQuery(); // Güncelleme komutunu çalıştır.
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Bilgi durumu güncellenirken hata: " + ex.Message);
                            // Hata olsa bile devam etmeyi seçebiliriz.
                        }
                    }

                    SoruUretim su = new SoruUretim("Diğerleri");
                    su.Show();
                    this.Close();
                    break;
            }
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }
    }
}