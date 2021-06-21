using System;
using Framework;
using Xunit;

namespace TestProject1
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            //var request = new ApiRequest()
            //{
            //    TranslateServer = TranslateServerEnum.Google,
            //    FromLanguage = LanguageEnum.English,
            //    ToLanguage = LanguageEnum.¼òÌåÖÐÎÄ,
            //    Body = "TranslateServer"
            //};
            //ApiClient apiClient = new ApiClient();
            //var v = apiClient.Execute(request).Result;
            var v = System.Globalization.CultureInfo.InstalledUICulture.Name;
            var v1 = System.Globalization.CultureInfo.CurrentCulture.EnglishName;
            Assert.True(true);
        }
    }
}
