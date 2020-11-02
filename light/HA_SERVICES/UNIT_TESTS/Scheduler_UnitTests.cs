using NUnit.Compatibility;
using NUnit.Framework;
using Scheduler;

namespace Communication.HA_SERVICES.UNIT_TESTS
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores")]
    [TestFixture]
    public class Scheduler_UnitTests
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        [SetUp]
        public void Init()
        {
        }

        string TestDays                               = "Mon,Tue,Wed,Thu,Fri,Sat,Sun";
        string TestTimePointZeroSeconds               = "04:30:00";
        string ExpectedResultOfCroneStringZeroSeconds = "0 30 04 ? * Mon,Tue,Wed,Thu,Fri,Sat,Sun";

        string TestTimePoint = "04:30:33";
        string ExpectedResultOfCroneString = "33 30 04 ? * Mon,Tue,Wed,Thu,Fri,Sat,Sun";

        string TestTimePointWrongSecond = "04:30:60";
        string TestTimePointWrongMinute = "04:60:00";
        string TestTimePointWrongHour   = "25:00:00";


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores")]
        [Test]
        public void TestCroneTimePoint_Any()
        {
            string CroneResult = MyCroneConverter.GetPointOfTime( TestTimePoint, TestDays );
            Assert.AreEqual( ExpectedResultOfCroneString, CroneResult );
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores")]
        [Test]
        public void TestCroneTimePoint_ZeroSeconds()
        {
            string CroneResult = MyCroneConverter.GetPointOfTime( TestTimePointZeroSeconds, TestDays );
            Assert.AreEqual( ExpectedResultOfCroneStringZeroSeconds, CroneResult );
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores")]
        [Test]
        public void TestCroneTimePoint_WithWrongSecondTimeFormat()
        {
            string CroneResult = MyCroneConverter.GetPointOfTime( TestTimePointWrongSecond, TestDays );
            Assert.AreEqual( QuartzApplicationMessages.MessageWrongTimeFormat, CroneResult );
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores")]
        [Test]
        public void TestCroneTimePoint_WithWrongMinuteTimeFormat()
        {
            string CroneResult = MyCroneConverter.GetPointOfTime( TestTimePointWrongMinute, TestDays );
            Assert.AreEqual( QuartzApplicationMessages.MessageWrongTimeFormat, CroneResult );
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores")]
        [Test]
        public void TestCroneTimePoint_WithWrongHourTimeFormat()
        {
            string CroneResult = MyCroneConverter.GetPointOfTime( TestTimePointWrongHour, TestDays );
            Assert.AreEqual( QuartzApplicationMessages.MessageWrongTimeFormat, CroneResult );
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        [TearDown]
        public void Cleanup()
        {
        }

    }
}
