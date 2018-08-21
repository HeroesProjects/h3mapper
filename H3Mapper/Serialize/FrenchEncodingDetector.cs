using System.Linq;
using System.Text;

namespace H3Mapper.Serialize
{
    public class FrenchEncodingDetector : IEncodingDetector
    {
        private static readonly byte[] Letters =
        {
            133, // …
            159, // Ÿ
            192, // À
            194, // Â
            199, // Ç
            200, // È
            201, // É
            202, // Ê
            203, // Ë
            206, // Î
            207, // Ï
            212, // Ô
            217, // Ù
            219, // Û
            220, // Ü
            224, // à
            226, // â
            231, // ç
            232, // è
            233, // é
            234, // ê
            235, // ë
            238, // î
            239, // ï
            244, // ô
            249, // ù
            251, // û
            252, // ü
            255 // ÿ
        };

        public Encoding TryMatchEncoding(byte[] text)
        {
            return text.Where(x => x > 127).All(x => Letters.Contains(x)) ? Encoding.GetEncoding(1252) : null;
        }
    }
}