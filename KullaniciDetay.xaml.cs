using System.ComponentModel;
using System.Configuration;
using System.Data.SqlClient;
using System.Windows;

namespace Hackathon
{
 
    public class AdminKullaniciDetayModel : INotifyPropertyChanged
    {
        public int ID { get; set; }
        public string Ad { get; set; }
        public string Email { get; set; }
        public System.DateTime DogumTarihi { get; set; }

        private bool _isAdmin;
        public bool IsAdmin
        {
            get => _isAdmin;
            set { _isAdmin = value; OnPropertyChanged(nameof(IsAdmin)); }
        }

        public string EgitimDuzeyi { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }


    public partial class KullaniciDetay : Window
    {
        public AdminKullaniciDetayModel SecilenKullanici { get; set; }

       
        public KullaniciDetay(int kullaniciId)
        {
            InitializeComponent();
            this.DataContext = this;

           
            KullaniciBilgileriniYukle(kullaniciId);
        }

        private void KullaniciBilgileriniYukle(int kullaniciId)
        {
            string sql = "SELECT ID, Ad, Email, Yas, IsAdmin, EgitimDuzeyi FROM Kayit WHERE ID = @ID";

            try
            {
                using (SqlConnection conn = new SqlConnection(GetConnectionString()))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", kullaniciId);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                SecilenKullanici = new AdminKullaniciDetayModel
                                {
                                    ID = reader.GetInt32(reader.GetOrdinal("ID")),
                                    Ad = reader.GetString(reader.GetOrdinal("Ad")),
                                    Email = reader.GetString(reader.GetOrdinal("Email")),
                                    DogumTarihi = reader.GetDateTime(reader.GetOrdinal("Yas")),
                                    IsAdmin = reader.GetBoolean(reader.GetOrdinal("IsAdmin")),
                                    EgitimDuzeyi = reader.GetString(reader.GetOrdinal("EgitimDuzeyi"))
                                };
                            }
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Kullanıcı detayları yüklenirken hata: " + ex.Message);
                this.Close(); 
            }
        }

        private static string GetConnectionString()
        {
            return ConfigurationManager.AppSettings["DbConnectionString"];
        }

        private void Kaydet_Click(object sender, RoutedEventArgs e)
        {
           
            string sql = "UPDATE Kayit SET IsAdmin = @IsAdmin WHERE ID = @ID";

            try
            {
                using (SqlConnection conn = new SqlConnection(GetConnectionString()))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@IsAdmin", SecilenKullanici.IsAdmin);
                        cmd.Parameters.AddWithValue("@ID", SecilenKullanici.ID);
                        cmd.ExecuteNonQuery();
                    }
                }
                MessageBox.Show("Değişiklikler başarıyla kaydedildi!");
                this.DialogResult = true;
                this.Close();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Değişiklikler kaydedilirken hata: " + ex.Message);
            }
        }

        private void Iptal_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}