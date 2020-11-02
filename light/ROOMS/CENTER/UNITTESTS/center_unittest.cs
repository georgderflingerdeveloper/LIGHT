using HomeAutomation;
using HomeAutomation.HardConfig_Collected;
using Moq;
using NUnit.Framework;
using Quartz;

namespace CenterUnitTest
{
    public static class TestConstants
	{
		static decimal TestTransaction                                = 00001;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2211:NonConstantFieldsShouldNotBeVisible")]
        public static string DatagrammButtonRightUpside_Pressed       = TestTransaction.ToString()     + "_" + LivingRoomWestIOAssignment.LivWestDigInputs.indDigitalInputButtonMainUpLeft.ToString() + "_" + "true";
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2211:NonConstantFieldsShouldNotBeVisible")]
        public static string DatagrammButtonRightUpside_Released      = (TestTransaction+1).ToString() + "_" + LivingRoomWestIOAssignment.LivWestDigInputs.indDigitalInputButtonMainUpLeft.ToString() + "_" + "false";
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2211:NonConstantFieldsShouldNotBeVisible")]
        public static string DatagrammButtonRightUpside_PressedTwice  = (TestTransaction+2).ToString() + "_" + LivingRoomWestIOAssignment.LivWestDigInputs.indDigitalInputButtonMainUpLeft.ToString() + "_" + "true";
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2211:NonConstantFieldsShouldNotBeVisible")]
        public static string DatagrammButtonRightUpside_ReleasedTwice = (TestTransaction+3).ToString() + "_" + LivingRoomWestIOAssignment.LivWestDigInputs.indDigitalInputButtonMainUpLeft.ToString() + "_" + "false";
    }

    [TestFixture]
	public class CenterTests
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        Center_kitchen_living_room_NG TestCenter;


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        [SetUp]
        public void Init()
        {
        }

        //[Test]
        //public void TestLigthOutsideIsOn()
        //{
        //    TestCenter.ResetDeviceController();

        //    TestCenter.TestBackdoor_UdpReceiver(TestConstants.DatagrammButtonRightUpside_Pressed);
        //    TestCenter.TestBackdoor_UdpReceiver(TestConstants.DatagrammButtonRightUpside_Released);

        //    Assert.True(TestCenter.ReferenceDigitalOutputState[CenterOutsideIODevices.indDigitalOutputLightsOutside]);
        //}

        //[Test]
        //public void TestLigthOutsideIsOff()
        //{
        //    TestCenter.ResetDeviceController();

        //    // press once
        //    TestCenter.TestBackdoor_UdpReceiver(TestConstants.DatagrammButtonRightUpside_Pressed);
        //    TestCenter.TestBackdoor_UdpReceiver(TestConstants.DatagrammButtonRightUpside_Released);
        //    // press twice
        //    TestCenter.TestBackdoor_UdpReceiver(TestConstants.DatagrammButtonRightUpside_PressedTwice);
        //    TestCenter.TestBackdoor_UdpReceiver(TestConstants.DatagrammButtonRightUpside_ReleasedTwice);

        //    Assert.False(TestCenter.ReferenceDigitalOutputState[CenterOutsideIODevices.indDigitalOutputLightsOutside]);
        //}

        //[Test]
        //public void TestTurnAllLightsOn()
        //{
        //    TestCenter.ResetDeviceController( );

        //    TestCenter.TestBackdoor_UdpReceiver( ComandoString.TURN_ALL_LIGHTS_ON );

        //    Assert.True( TestCenter.ReferenceDigitalOutputState[KitchenCenterIoDevices.indDigitalOutputFirstKitchen] );
        //    Assert.True( TestCenter.ReferenceDigitalOutputState[KitchenCenterIoDevices.indDigitalOutputFrontLight_1] );

        //}

        [TearDown]
        public void Cleanup()
        {
            TestCenter = null;
        }
    }
}

