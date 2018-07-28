using H3Mapper.Flags;

namespace H3Mapper.MapObjects
{
    public class MagicShrineObject : MapObject
    {
        public Identifier Spell { get; set; }
        public MagicShrineSpellLevel SpellLevel { get; set; }
        public ShrineType Type { get; set; }
    }
}