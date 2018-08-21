using System.Linq;
using System.Text;

namespace H3Mapper.Serialize
{
    public class GermanEncodingDetector : IEncodingDetector
    {
        private static readonly byte[] Letters =
        {
            133, // …
            196, // Ä
            214, // Ö
            220, // Ü
            223, // ß
            228, // ä
            246, // ö
            252 // ü
        };

        public Encoding TryMatchEncoding(byte[] text)
        {
            return text.Where(x => x > 127).All(x => Letters.Contains(x)) ? Encoding.GetEncoding(1252) : null;
        }
    }
}