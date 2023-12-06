using Framework;
using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Xunit;
using Xunit.Abstractions;

namespace TestProject1
{
    public class UnitTest1
    {
        private ITestOutputHelper _output;

        public UnitTest1(ITestOutputHelper output)
        {
            _output = output;
        }
        [Fact]
        public void Test1()
        {
            //GoogleFanyi youdaoFanyi = new ();
            ApiClient apiClient = new ApiClient();
            var apiRequest = new ApiRequest()
            {
                Body = @"The minimum requirement for a class to be considered a valid package for VisualStudio
                            The minimum requirement for a class to be considered a valid package for Visual Studio",
                FromLanguage = LanguageEnum.Auto,
                ToLanguage = LanguageEnum.简体中文,
                TranslateServer = TranslateServerEnum.Google
            };
            var v = apiClient.Execute(apiRequest).Result;

            //var v = youdaoFanyi.Fanyi("UnitTest1", "auto", "zh-CN", LanguageEnum.English, LanguageEnum.简体中文).Result;
            Assert.True(true);
        }
        [Fact]
        public void Test2()
        {
            string o = "ydsecret://query/key/B*RGygVywfNBwpmBaZg*WT7SIOUP2T0C9WHMZN39j^DAdaZhAnxvGcCY6VYFwnHl";
            string n = "ydsecret://query/iv/C@lZe2YzHtZ2CYgaXKSVfsb7Y4QWHjITPPZ0nQp87fBeJ!Iv6v^6fvi2WN@bYpJ4";
            var a = MD5.Create().ComputeHash(System.Text.Encoding.GetEncoding("utf-8").GetBytes(o));
            var r = MD5.Create().ComputeHash(System.Text.Encoding.GetEncoding("utf-8").GetBytes(n));
            var s = "_jsUyA02rwkOJ4enKX7c4dhd7CjvGkcKfbRx0BjNGW-zSaX3gF8OxVTwytdyFMaJGqwne0VpU7tZaPPeKSfsoZMbywIm3HC5rdddC7OmZvVMhfMVbH9auckpHG7WLAPGGeGmtEmEIAvt3tIE3CReFmOehnwxRfUO7ruKJEgysCnBG6pV3NUf0N3I-NtTURsJqq2dIRegdxylZYPy2tj9OA==";

            //using var aes = Aes.Create();
            //aes.Mode = CipherMode.CBC;
            //aes.Padding = PaddingMode.PKCS7;
            //aes.KeySize = 128;
            //aes.Key = a;
            //aes.IV = r;

            //ICryptoTransform cTransform = aes.CreateDecryptor();
            s = s.Replace("-", "+").Replace("_", "/");
            //byte[] bytes = Base64UrlEncoder.DecodeBytes(s);
            //byte[] resultArray = cTransform.TransformFinalBlock(bytes, 0, bytes.Length);
            //var hex = System.Text.Encoding.GetEncoding("utf-8").GetString(resultArray);
            using Aes aes = Aes.Create();
                aes.KeySize = 128;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = a;
                aes.IV = r;

                ICryptoTransform decryptor = aes.CreateDecryptor();
                byte[] encryptedBytes = Convert.FromBase64String(s);
                byte[] decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);

                string decryptedText = Encoding.UTF8.GetString(decryptedBytes);
                Console.WriteLine(decryptedText);
            

            Assert.True(true);
        }

        [Fact]
        public void Test3()
        {
            string o = "ydsecret://query/key/B*RGygVywfNBwpmBaZg*WT7SIOUP2T0C9WHMZN39j^DAdaZhAnxvGcCY6VYFwnHl";
            string n = "ydsecret://query/iv/C@lZe2YzHtZ2CYgaXKSVfsb7Y4QWHjITPPZ0nQp87fBeJ!Iv6v^6fvi2WN@bYpJ4";
            string t = "_jsUyA02rwkOJ4enKX7c4dhd7CjvGkcKfbRx0BjNGW-zSaX3gF8OxVTwytdyFMaJGqwne0VpU7tZaPPeKSfsoZMbywIm3HC5rdddC7OmZvVMhfMVbH9auckpHG7WLAPGGeGmtEmEIAvt3tIE3CReFmOehnwxRfUO7ruKJEgysCnBG6pV3NUf0N3I-NtTURsJqq2dIRegdxylZYPy2tj9OA==";

            byte[] a = new byte[16];
            using (MD5 md5 = MD5.Create()) {
                byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(o));
                Buffer.BlockCopy(hash, 0, a, 0, hash.Length);
            }
            var a1 = MD5.Create().ComputeHash(System.Text.Encoding.GetEncoding("utf-8").GetBytes(o));
            byte[] r = new byte[16];
            using (MD5 md5 = MD5.Create()) {
                byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(n));
                Buffer.BlockCopy(hash, 0, r, 0, hash.Length);
            }
            var r1 = MD5.Create().ComputeHash(System.Text.Encoding.GetEncoding("utf-8").GetBytes(n));
            using (Aes aes = Aes.Create()) {
                aes.KeySize = 128;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = a1;
                aes.IV = r1;

                ICryptoTransform decryptor = aes.CreateDecryptor();
                byte[] encryptedBytes = Base64UrlEncoder.DecodeBytes(t);
                byte[] decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);

                string decryptedText = Encoding.UTF8.GetString(decryptedBytes);
                Console.WriteLine(decryptedText);
            }
        }
    }
}
