using NUnit.Framework;
using HomeAutomation;
using HomeAutomation.HardConfig;

namespace CenterUnitTest
{
	public static class TestConstants
	{
		static decimal TestTransaction                                = 00001;
		public static string DatagrammButtonRightUpside_Pressed       = TestTransaction.ToString()     + "_" + LivingRoomWestIOAssignment.LivWestDigInputs.indDigitalInputButtonMainUpLeft.ToString() + "_" + "true";
		public static string DatagrammButtonRightUpside_Released      = (TestTransaction+1).ToString() + "_" + LivingRoomWestIOAssignment.LivWestDigInputs.indDigitalInputButtonMainUpLeft.ToString() + "_" + "false";
        public static string DatagrammButtonRightUpside_PressedTwice  = (TestTransaction+2).ToString() + "_" + LivingRoomWestIOAssignment.LivWestDigInputs.indDigitalInputButtonMainUpLeft.ToString() + "_" + "true";
        public static string DatagrammButtonRightUpside_ReleasedTwice = (TestTransaction+3).ToString() + "_" + LivingRoomWestIOAssignment.LivWestDigInputs.indDigitalInputButtonMainUpLeft.ToString() + "_" + "false";
    }

    [TestFixture]
	public class CenterTests
	{
		Center_kitchen_living_room_NG TestCenter;

        [SetUp]
        public void Init()
        {
            TestCenter = new Center_kitchen_living_room_NG("0", "0", "0", true);
        }

        [Test]
        public void TestLigthOutsideIsOn()
        {
            TestCenter.ResetDeviceController();

            TestCenter.TestBackdoor_UdpReceiver(TestConstants.DatagrammButtonRightUpside_Pressed);
            TestCenter.TestBackdoor_UdpReceiver(TestConstants.DatagrammButtonRightUpside_Released);

            Assert.True(TestCenter.ReferenceDigitalOutputState[CenterOutsideIODevices.indDigitalOutputLightsOutside]);
        }

        [Test]
        public void TestLigthOutsideIsOff()
        {
            TestCenter.ResetDeviceController();

            // press once
            TestCenter.TestBackdoor_UdpReceiver(TestConstants.DatagrammButtonRightUpside_Pressed);
            TestCenter.TestBackdoor_UdpReceiver(TestConstants.DatagrammButtonRightUpside_Released);
            // press twice
            TestCenter.TestBackdoor_UdpReceiver(TestConstants.DatagrammButtonRightUpside_PressedTwice);
            TestCenter.TestBackdoor_UdpReceiver(TestConstants.DatagrammButtonRightUpside_ReleasedTwice);

            Assert.False(TestCenter.ReferenceDigitalOutputState[CenterOutsideIODevices.indDigitalOutputLightsOutside]);
        }

        [TearDown]
        public void Cleanup()
        {
            TestCenter = null;
        }
    }
}

