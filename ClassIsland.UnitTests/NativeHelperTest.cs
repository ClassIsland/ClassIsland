using ClassIsland.Core;
using ClassIsland.Core.Helpers.Native;

namespace ClassIsland.UnitTests;

[TestClass]
public class NativeHelperTest
{
    [TestMethod]
    public void TestIsOccupiedTrue()
    {
        var v = NativeWindowHelper.IsOccupied(Environment.ProcessPath!);
        Assert.IsTrue(v);
    }

    [TestMethod]
    public void TestIsOccupiedFalse()
    {
        File.WriteAllText("./test.txt", "test");
        var v = NativeWindowHelper.IsOccupied("./test.txt");
        Assert.IsFalse(v);
    }
}