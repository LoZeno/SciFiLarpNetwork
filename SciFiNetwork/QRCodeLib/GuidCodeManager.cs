using System;
using System.Drawing;
using System.Text;
using MessagingToolkit.QRCode.Codec;
using MessagingToolkit.QRCode.Codec.Data;

namespace QRCodeLib
{
    public static class GuidCodeManager
    {
        public static Image GetPictureFromText(string code)
		{
			QRCodeEncoder encoder = new QRCodeEncoder();
			encoder.QRCodeEncodeMode = QRCodeEncoder.ENCODE_MODE.BYTE; //ALPHA_NUMERIC, nonostante il nome, non va bene per codificare un GUID.
			encoder.QRCodeErrorCorrect = QRCodeEncoder.ERROR_CORRECTION.L;
			encoder.QRCodeVersion = 3;

			return encoder.Encode(code);
		}

		public static Image GetPictureFromGuid(Guid? uniqueCode)
		{
			return GetPictureFromText(uniqueCode.ToString());
		}

		public static string GetCodeFromPicture(Bitmap pic)
		{
			QRCodeDecoder decoder = new QRCodeDecoder();
			QRCodeBitmapImage image = new QRCodeBitmapImage(pic);
			return decoder.Decode(image);
		}

		public static string GetCodeFromPicture(Image pic)
		{
			Bitmap picture = new Bitmap(pic);
			return GetCodeFromPicture(picture);
		}
    }
}
