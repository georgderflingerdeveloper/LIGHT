using BASIC_COMPONENTS;
using Moq;
using NUnit.Framework;
using System.Timers;



namespace BASIC_CONTROL_LOGIC
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "lightstepcontrol")]
    [TestFixture]
    public class Unittest_lightstepcontrol
    {
        const uint NumberOfDevices = 16;
        const double TimeNext = 40;
        const double LittleDelay = 20;
        const uint NumberOfOnOffTests = 2;
        devicestepcontrol StepControl;
        ITimer timernext = new Timer_(TimeNext);

        void SignalChangeOnOff()
        {
            StepControl.WatchForInputValueChange(true);
            StepControl.WatchForInputValueChange(false);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores")]
        [Test]
        public void Test_DeviceValueSwitch()
        {
            StepControl = new devicestepcontrol(NumberOfDevices, timernext);

            for (uint i = 0; i < NumberOfOnOffTests; i++)
            {
                SignalChangeOnOff(); // f.e. emulation of pushing / pulling a button
                Assert.AreEqual(true, StepControl.Value);
                SignalChangeOnOff();
                Assert.AreEqual(false, StepControl.Value);
                StepControl.Reset();
                Assert.AreEqual((System.UInt32)0, StepControl.Number);
            }
        }


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores")]
        [Test]
        public void Test_StartDeviceValueWhenSwitchingToNextElement()
        {
            Mock<ITimer> MockTimer = new Mock<ITimer>();

            StepControl = new devicestepcontrol(NumberOfDevices, MockTimer.Object);
            StepControl.EStep += StepControl_EStep;

            StepControl.WatchForInputValueChange(true);
            MockTimer.Raise(timer => timer.Elapsed += null, new System.EventArgs() as ElapsedEventArgs);
            StepControl.WatchForInputValueChange(false);
            StepControl.Reset();
        }
        int EventRaisedCounter = 1;
        private void StepControl_EStep(uint number, bool value)
        {
            switch (EventRaisedCounter)
            {
                case 1:
                    Assert.AreEqual((System.UInt32)0, StepControl.Number);
                    Assert.AreEqual(false, StepControl.Value);
                    break;
                case 2:
                    Assert.AreEqual((System.UInt32)1, StepControl.Number);
                    Assert.AreEqual(false, StepControl.Value);
                    break;
            }
            EventRaisedCounter++;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores")]
        [Test]
        public void Test_DeviceValueWhenSwitchingToNextElement()
        {
            Mock<ITimer> MockTimer = new Mock<ITimer>();

            StepControl = new devicestepcontrol(NumberOfDevices, MockTimer.Object);
            StepControl.EStep += StepControl_EStepMulitple;

            for (uint i = 0; i < NumberOfDevices; i++)
            {
                StepControl.WatchForInputValueChange(true);
                MockTimer.Raise(timer => timer.Elapsed += null, new System.EventArgs() as ElapsedEventArgs);
                StepControl.WatchForInputValueChange(false);
            }
            StepControl.Reset();
        }

        int EventRaisedCounter_multi = 1;
        private void StepControl_EStepMulitple(uint number, bool value)
        {
            if ((EventRaisedCounter_multi % 3) == 0)
            {
                return;
            }

            if ((EventRaisedCounter_multi % 2) != 0)
            {
                Assert.AreEqual((System.UInt32)EventRaisedCounter_multi - 1, StepControl.Number);
                Assert.AreEqual(false, StepControl.Value);
            }
            else if ((EventRaisedCounter_multi % 2) == 0)
            {
                Assert.AreEqual((System.UInt32)EventRaisedCounter_multi - 1, StepControl.Number);
                Assert.AreEqual(false, StepControl.Value);
            }
            EventRaisedCounter_multi++;
        }
    }
}

