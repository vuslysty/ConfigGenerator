using ConfigGenerator.ConfigInfrastructure;

namespace ConfigGenerator;

public class Configs : ConfigsBase
{
    private readonly General _general = new();
    public General General => GetConfig(_general);

    private readonly Bots _bots = new();
    public Bots Bots => GetConfig(_bots);

    private readonly Rarity _rarity = new();
    public Rarity Rarity => GetConfig(_rarity);
}

public class General : ValueConfigTable
{
    /// <summary>
    /// It's health
    /// </summary>
    public float health { get; }

    public string playerName { get; private set; }
    
    public int testInt { get; private set; }
    public float testFloat { get; private set; }
    public bool testBool { get; private set; }
    public string test { get; private set; }
    public string test2 { get; private set; }
    public string test3 { get; private set; }
}

public class Bots : DatabaseConfigTable<Bots.Item, int>
{
    public class Item : IConfigTableItem<int>
    {
        public int Id { get; private set; }
        public int Index { get; private set; }

        /// <summary>
        /// Here is name
        /// </summary>
        public string Name { get; private set; }
        
        /// <summary>
        /// Comment for Height
        /// </summary>
        public float Height { get; private set; }
        
        public Rarity.Item Rarity { get; private set; }
        public int Tier { get; private set; }
    }
}

public class Rarity : DatabaseConfigTable<Rarity.Item, string>
{
    public class Item : IConfigTableItem<string>
    {
        public string Id { get; private set; }
        public int Index { get; private set; }
        public string ShortHand { get; private set; }
        public int ColorHexCode { get; private set; }
    }
}

public class Rarity3 : DatabaseConfigTable<Rarity3.Item, string>
{
    public class Item : IConfigTableItem<string>
    {
        public string Id { get; private set; }
        public int Index { get; private set; }
        public string ShortHand { get; private set; }
        public int ColorHexCode { get; private set; }
        public int ColorHexCode2 { get; private set; }
        public int ColorHexCode3 { get; private set; }
    }
}