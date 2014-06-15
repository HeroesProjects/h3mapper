using System.Text;

namespace H3Mapper
{
    public class MapHeader
    {
        public MapFormat Format { get; set; }
        public bool HasPlayers { get; set; }
        public int Size { get; set; }
        public bool HasSecondLevel { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int Difficulty { get; set; }
        public byte ExperienceLevelLimit { get; set; }
        public MapPlayer[] Players { get; set; }
        public VictoryCondition VictoryCondition { get; set; }
        public LossCondition LossCondition { get; set; }
        public int TeamCount { get; set; }
        public MapAllowedHeroes AllowedHeroes { get; set; }
        public DisposedHero[] DisposedHeroes { get; set; }
        public MapArtifacts AllowedArtifacts { get; set; }
        public MapSpellsAndAbilities AllowedSpellsAndAbilities { get; set; }
        public MapRumor[] Rumors { get; set; }
        public MapHeroDefinition[] PrefedinedHeroes { get; set; }
        public MapTerrain Terrain { get; set; }
        public CustomObject[] CustomObjects { get; set; }
        public MapObject[] Objects { get; set; }
        public TimedEvents[] Events { get; set; }

        public override string ToString()
        {
            var builder = new StringBuilder();
            builder.AppendLine("Format:  " + Format);
            builder.AppendLine("Has players: " + HasPlayers);
            builder.AppendLine("Size: " + Size);
            builder.AppendLine("Has second level: " + HasSecondLevel);
            builder.AppendLine("Name: " + Name);
            builder.AppendLine("Description: " + Description);
            builder.AppendLine("Difficulty: " + Difficulty);
            builder.AppendLine("Experience level limit: " + ExperienceLevelLimit);
            for (int i = 0; i < Players.Length; i++)
            {
                AppendPlayer(builder, Players[i]);
            }
            builder.AppendLine("Win : " + VictoryCondition);
            builder.AppendLine("Loss: " + LossCondition);
            builder.AppendLine("Heroes: " + AllowedHeroes);
            return builder.ToString();
        }

        private void AppendPlayer(StringBuilder builder, MapPlayer player)
        {
            builder.AppendLine("-- player");
            builder.AppendLine("Can human play: " + player.CanHumanPlay);
            builder.AppendLine("Can AI play: " + player.CanAIPlay);
            builder.AppendLine("AI tactic: " + player.AITactic);
            builder.AppendLine("P7: " + player.P7);
            builder.AppendLine("Has home town: " + player.HasHomeTown);
            builder.AppendLine("Allowed factions: " + player.AllowedFactions);
            builder.AppendLine("Is faction random: " + player.IsFactionRandom);
            builder.AppendLine("Has hometown: " + player.HasHomeTown);
            builder.AppendLine("Generate hero at main town: " + player.GenerateHeroAtMainTown);
            builder.AppendLine("Generate hero: " + player.GenerateHero);
            builder.AppendLine("Home town position: " + player.HomeTownPosition);
            builder.AppendLine("Has random hero: " + player.HasRandomHero);
            builder.AppendLine("Main custom hero id: " + player.MainCustomHeroId);
            builder.AppendLine("Main custom hero potrait id: " + player.MainCustomHeroPortraitId);
            builder.AppendLine("Main custom hero name: " + player.MainCustomHeroName);
            builder.AppendLine("Power placeholders: " + player.PowerPlaceholders);
            builder.AppendLine("Team Id: " + player.TeamId);
            foreach (var hero in player.Heroes)
            {
                builder.AppendLine("-Hero: " + hero);
            }
        }
    }
}