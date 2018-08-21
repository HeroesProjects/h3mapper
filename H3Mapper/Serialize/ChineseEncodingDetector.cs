using System.Text;

namespace H3Mapper.Serialize
{
    public class ChineseEncodingDetector : IEncodingDetector
    {
        public Encoding TryMatchEncoding(byte[] text)
        {
            var waitsForSecondValidByte = false;
            foreach (var current in text)
            {
                if (waitsForSecondValidByte)
                {
                    waitsForSecondValidByte = false;
                    if (current < 0x40 || current == 0x7F) return null;

                    continue;
                }

                if (current <= 0x80) continue;
                // not valid
                if (current == 0xFF) return null;

                waitsForSecondValidByte = true;
            }

            if (waitsForSecondValidByte) return null;

            return Encoding.GetEncoding(936);
        }
    }
}