
using NUnit.Framework;
using HomeAutomation;

[TestFixture]
public class TestCenterKitchenLivingroom
{
    Center_kitchen_living_room_NG TestCenter = new Center_kitchen_living_room_NG("127.0.0.1", "0", "0", true);

    bool Result;
    [Test]
    public void Test_IsTestable()
    {
        Result = TestCenter.IsTestable();
        Assert.IsTrue(Result);
    }
}