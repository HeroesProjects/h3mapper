using System.Linq;
using System.Text;

namespace H3Mapper.Serialize
{
    public class RussianEncodingDetector : IEncodingDetector
    {
        private static readonly byte[] Letters =
        {
            168, // Ё
            184, // ё
            192, // А
            193, // Б
            194, // В
            195, // Г
            196, // Д
            197, // Е
            198, // Ж
            199, // З
            200, // И
            201, // Й
            202, // К
            203, // Л
            204, // М
            205, // Н
            206, // О
            207, // П
            208, // Р
            209, // С
            210, // Т
            211, // У
            212, // Ф
            213, // Х
            214, // Ц
            215, // Ч
            216, // Ш
            217, // Щ
            218, // Ъ
            219, // Ы
            220, // Ь
            221, // Э
            222, // Ю
            223, // Я
            224, // а
            225, // б
            226, // в
            227, // г
            228, // д
            229, // е
            230, // ж
            231, // з
            232, // и
            233, // й
            234, // к
            235, // л
            236, // м
            237, // н
            238, // о
            239, // п
            240, // р
            241, // с
            242, // т
            243, // у
            244, // ф
            245, // х
            246, // ц
            247, // ч
            248, // ш
            249, // щ
            250, // ъ
            251, // ы
            252, // ь
            253, // э
            254, // ю
            255, // я
        };

        public Encoding TryMatchEncoding(byte[] text)
        {
            return text.Where(x => x > 127).All(x => Letters.Contains(x)) ? Encoding.GetEncoding(1251) : null;
        }
    }
}