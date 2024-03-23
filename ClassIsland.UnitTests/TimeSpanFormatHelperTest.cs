using ClassIsland.Helpers;

namespace ClassIsland.UnitTests;

[TestClass]
public class TimeSpanFormatHelperTest
{
    [TestMethod]
    public void TestFormat()
    {
        TimeSpan[] ts = [
            new(11, 45, 14),
            new(25, 11, 10),
            new(00, 00, 01),
            new(00, 10, 00),
        ];
        string[] result =
        [
            "11小时45分钟14秒",
            "25小时11分钟10秒",
            "1秒",
            "10分钟",
        ];

        for (var i = 0; i < ts.Length; i++)
        {
            Assert.AreEqual(TimeSpanFormatHelper.Format(ts[i]), result[i]);
        }
    }
}