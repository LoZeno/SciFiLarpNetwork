using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace QRCodeLib.Test
{
    [TestFixture]
    public class GuidCodeManagerTest
    {
        [TestCase("ba0e5046-e9c4-45ac-bf79-47a9fb1581e9")]
        public void Test_WhenQrCodeIsPassed_ReturnsGUID(string guid)
        {
            Image qrcodeToTest = Image.FromFile(@"Resources\" + guid + ".png");
            var result = GuidCodeManager.GetCodeFromPicture(qrcodeToTest);
            Assert.AreEqual(guid, result);
        }

        [TestCase("ba0e5046-e9c4-45ac-bf79-47a9fb1581e9")]
        public void Test_WhenGuidIsPassed_ReturnsCorrectPicture(string guid)
        {
            var pictureToGet = Image.FromFile(@"Resources\" + guid + ".png");
            var newPicture = GuidCodeManager.GetPictureFromText(guid);
            Assert.AreEqual(pictureToGet.Height, newPicture.Height);
            Assert.AreEqual(pictureToGet.Width, newPicture.Width);
        }
    }
}
