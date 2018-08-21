using System.Linq;
using System.Text;

namespace H3Mapper.Serialize
{
    public class HungarianEncodingDetector : IEncodingDetector
    {
        private static readonly byte[] Letters = {
            193, // Á
            201, // É
            205, // Í
            211, // Ó
            213, // Ő
            214, // Ö
            218, // Ú
            219, // Ű
            220, // Ü
            225, // á
            233, // é
            237, // í
            243, // ó
            245, // ő
            246, // ö
            250, // ú
            251, // ű
            252, // ü
        };

        public Encoding TryMatchEncoding(byte[] text)
        {
            return text.Where(x => x > 127).All(x => Letters.Contains(x)) ? Encoding.GetEncoding(1250) : null;
        }
    }
}