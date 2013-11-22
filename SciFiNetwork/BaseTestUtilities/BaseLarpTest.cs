using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataMappings;
using NUnit.Framework;

namespace BaseTestUtilities
{
    [TestFixture]
    public abstract class BaseLarpTest
    {
        [TestFixtureSetUp]
        public void CreateInMemoryDatabase()
        {
            using (var unitOfWork = new gamedata2Entities())
            {
                unitOfWork.Database.Create();
            }
        }
    }
}
