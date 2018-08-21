using H3Mapper.DataModel;
using H3Mapper.MapModel;

namespace H3Mapper.Analysis
{
    public class MapValidator : IMapValidator
    {
        private readonly IMapValidator[] validators;

        public MapValidator(IdMappings maps)
        {
            validators = new IMapValidator[]
            {
                new MapObjectTemplateValidator(maps),
                new MapObjectSubIdValidator()
            };
        }

        public void Validate(H3Map map)
        {
            foreach (var validator in validators)
            {
                validator.Validate(map);
            }
        }
    }
}