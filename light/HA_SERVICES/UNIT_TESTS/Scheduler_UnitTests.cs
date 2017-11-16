using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Moq;
using Scheduler;

namespace Communication.HA_SERVICES.UNIT_TESTS
{
    [TestFixture]
    public class Scheduler_UnitTests
    {
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


        [Test]
        public void TestCroneTimePoint_Any()
        {
            string CroneResult = MyCroneConverter.GetPointOfTime( TestTimePoint, TestDays );
            Assert.AreEqual( ExpectedResultOfCroneString, CroneResult );
        }

        [Test]
        public void TestCroneTimePoint_ZeroSeconds()
        {
            string CroneResult = MyCroneConverter.GetPointOfTime( TestTimePointZeroSeconds, TestDays );
            Assert.AreEqual( ExpectedResultOfCroneStringZeroSeconds, CroneResult );
        }

        [Test]
        public void TestCroneTimePoint_WithWrongSecondTimeFormat()
        {
            string CroneResult = MyCroneConverter.GetPointOfTime( TestTimePointWrongSecond, TestDays );
            Assert.AreEqual( ExpectedResultOfCroneStringZeroSeconds, CroneResult );
        }


        [TearDown]
        public void Cleanup()
        {
        }

    }
}
