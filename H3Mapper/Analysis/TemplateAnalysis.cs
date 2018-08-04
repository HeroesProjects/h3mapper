using System;
using System.Collections.Generic;

namespace H3Mapper.Analysis
{
    public class TemplateAnalysis
    {
        private readonly ISet<TemplateData> data = new HashSet<TemplateData>(TemplateData.TemplateDataComparer);

        public void Analyse(H3Map map)
        {
            foreach (var mapObject in map.Objects)
            {
                data.Add(new TemplateData(mapObject.Template));
            }
        }

        public void PrintAll()
        {
            foreach (var templateData in data)
            {
                Console.WriteLine(templateData);
            }
        }
    }
}