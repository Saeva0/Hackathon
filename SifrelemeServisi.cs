using System;
using System.Security.Cryptography;
using System.Text;

namespace Hackathon
{
    public static class SifrelemeServisi
    {
      
        public static (byte[] hash, byte[] salt) HashSifre(string sifre)
        {
            // 1. Rastgele bir tuz (salt) oluştur.
            byte[] salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            // 2. Şifreyi ve tuzu birleştirip hash'le.
            using (var sha256 = SHA256.Create())
            {

                byte[] sifreBytes = Encoding.UTF8.GetBytes(sifre);

               
                byte[] birlesikBytes = new byte[sifreBytes.Length + salt.Length];
                Buffer.BlockCopy(sifreBytes, 0, birlesikBytes, 0, sifreBytes.Length);
                Buffer.BlockCopy(salt, 0, birlesikBytes, sifreBytes.Length, salt.Length);

                byte[] hash = sha256.ComputeHash(birlesikBytes);


                return (hash, salt);
            }
        }

        // Girilen şifrenin, veritabanındaki hash ile eşleşip eşleşmediğini doğrular.
        public static bool DogrulaSifre(string girilenSifre, byte[] veritabaniHash, byte[] veritabaniSalt)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] sifreBytes = Encoding.UTF8.GetBytes(girilenSifre);

                byte[] birlesikBytes = new byte[sifreBytes.Length + veritabaniSalt.Length];
                Buffer.BlockCopy(sifreBytes, 0, birlesikBytes, 0, sifreBytes.Length);
                Buffer.BlockCopy(veritabaniSalt, 0, birlesikBytes, sifreBytes.Length, veritabaniSalt.Length);

                byte[] yeniHash = sha256.ComputeHash(birlesikBytes);

                if (yeniHash.Length != veritabaniHash.Length) return false;
                for (int i = 0; i < yeniHash.Length; i++)
                {
                    if (yeniHash[i] != veritabaniHash[i]) return false;
                }
                return true;
            }
        }
    }
}