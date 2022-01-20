using Framework;
using System;
using Xunit;

namespace TestProject1
{
    public class UnitTest1
    {
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
                ToLanguage = LanguageEnum.��������,
                TranslateServer = TranslateServerEnum.Google
            };
            var v = apiClient.Execute(apiRequest).Result;

            //var v = youdaoFanyi.Fanyi("UnitTest1", "auto", "zh-CN", LanguageEnum.English, LanguageEnum.��������).Result;
            Assert.True(true);
        }
    }
}
