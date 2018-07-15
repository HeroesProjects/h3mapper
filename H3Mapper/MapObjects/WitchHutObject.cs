using H3Mapper.Flags;

namespace H3Mapper.MapObjects
{
    public class WitchHutObject : MapObject<ObjectVariantType>
    {
        public WitchHutObject(int typeRawValue) : base(typeRawValue)
        {
        }

        public SecondarySkillType[] AllowedSkills { get; set; }
    }
}