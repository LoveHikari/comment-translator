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
            YoudaoFanyi youdaoFanyi = new YoudaoFanyi();
            var v = youdaoFanyi.Fanyi("���", "auto", "en", LanguageEnum.��������, LanguageEnum.English).Result;
            Assert.True(true);
        }
    }
}