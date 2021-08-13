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
            GoogleFanyi youdaoFanyi = new ();
            var v = youdaoFanyi.Fanyi("你好", "auto", "en", LanguageEnum.简体中文, LanguageEnum.English).Result;
            Assert.True(true);
        }
    }
}
