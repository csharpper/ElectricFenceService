namespace ElectricFenceService.Http
{
    public class SortSource
    {
        public string Name { get; private set; }
        public string Setting { get; private set; }
        public SortSource(string name, string setting)
        {
            Name = name;
            Setting = setting;
        }
    }
}