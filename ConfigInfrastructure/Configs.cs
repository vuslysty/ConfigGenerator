// using System;
// using System.Collections.Generic;
// using ConfigGenerator.ConfigInfrastructure;
//
// namespace YourNamespace
// {
//     public class Configs2 : ConfigsBase
//     {
//         private static Configs2 _instance;
//         
//         private readonly General _general = new General();
//         public static General General => GetConfigs()._general;
//
//         private readonly Bots _bots = new Bots();
//         public Bots Bots => GetConfigs()._bots;
//
//         private readonly Rarity _rarity = new Rarity();
//         public Rarity Rarity => GetConfigs()._rarity;
//
//         private readonly Tiers _tiers = new Tiers();
//         public Tiers Tiers => GetConfigs()._tiers;
//
//         private readonly Rarity3 _rarity3 = new Rarity3();
//         public Rarity3 Rarity3 => GetConfigs()._rarity3;
//
//         private readonly Rarity2 _rarity2 = new Rarity2();
//         public Rarity2 Rarity2 => GetConfigs()._rarity2;
//
//         private readonly Rarity4 _rarity4 = new Rarity4();
//         public Rarity4 Rarity4 => GetConfigs()._rarity4;
//
//         private readonly Quests _quests = new Quests();
//         public Quests Quests => GetConfigs()._quests;
//         
//         public static void Init(string jsonData)
//         {
//             if (_instance == null)
//             {
//                 _instance = new Configs();
//             }
//             
//             _instance.Initialize(jsonData);
//         }
//
//         public static void Init(List<TableData> tables)
//         {
//             if (_instance == null)
//             {
//                 _instance = new Configs();
//             }
//             
//             _instance.Initialize(tables);
//         }
//
//         private static Configs GetConfigs()
//         {
//             if (_instance == null)
//             {
//                 _instance = new Configs();
//             }
//
//             if (!_instance._initialized)
//             {
//                 Console.WriteLine($"{nameof(Configs)} is not initialized!");
//             }
//
//             return _instance;
//         }
//     }
//
//     public class General : ValueConfigTable
//     {
//         /// <summary>
//         /// It's health
//         /// </summary>
//         public float health { get; private set; }
//         public string playerName { get; private set; }
//         public int testInt { get; private set; }
//         public float testFloat { get; private set; }
//         public bool testBool { get; private set; }
//         /// <summary>
//         /// Just test 1
//         /// </summary>
//         public string test { get; private set; }
//         /// <summary>
//         /// Just test 2
//         /// </summary>
//         public string test2 { get; private set; }
//         /// <summary>
//         /// Just test 3
//         /// </summary>
//         public string test3 { get; private set; }
//     }
//
//     public class Bots : DatabaseConfigTable<Bots.Item, int>
//     {
//         public class Item : IConfigTableItem<int>
//         {
//             public int Id { get; private set; }
//             public int Index { get; private set; }
//             /// <summary>
//             /// Here is name
//             /// </summary>
//             public string Name { get; private set; }
//             /// <summary>
//             /// Comment for Height
//             /// </summary>
//             public float Height { get; private set; }
//             /// <summary>
//             /// It is rarity of bot
//             /// </summary>
//             public Rarity.Item Rarity { get; private set; }
//             public int Tier { get; private set; }
//         }
//     }
//
//     public class Rarity : DatabaseConfigTable<Rarity.Item, string>
//     {
//         public class Item : IConfigTableItem<string>
//         {
//             public string Id { get; private set; }
//             public int Index { get; private set; }
//             public string ShortHand { get; private set; }
//             public int ColorHexCode { get; private set; }
//         }
//     }
//
//     public class Tiers : DatabaseConfigTable<Tiers.Item, string>
//     {
//         public class Item : IConfigTableItem<string>
//         {
//             public string Id { get; private set; }
//             public int Index { get; private set; }
//         }
//     }
//
//     public class Rarity3 : DatabaseConfigTable<Rarity3.Item, string>
//     {
//         public class Item : IConfigTableItem<string>
//         {
//             public string Id { get; private set; }
//             public int Index { get; private set; }
//             /// <summary>
//             /// We can add comments
//             /// </summary>
//             public string ShortHand { get; private set; }
//             /// <summary>
//             /// Color for rarity in hex format
//             /// </summary>
//             public int ColorHexCode { get; private set; }
//         }
//     }
//
//     public class Rarity2 : DatabaseConfigTable<Rarity2.Item, string>
//     {
//         public class Item : IConfigTableItem<string>
//         {
//             public string Id { get; private set; }
//             public int Index { get; private set; }
//             public string ShortHand { get; private set; }
//             public int ColorHexCode { get; private set; }
//         }
//     }
//
//     public class Rarity4 : DatabaseConfigTable<Rarity4.Item, string>
//     {
//         public class Item : IConfigTableItem<string>
//         {
//             public string Id { get; private set; }
//             public int Index { get; private set; }
//             public string ShortHand { get; private set; }
//             public int ColorHexCode { get; private set; }
//         }
//     }
//
//     public class Quests : DatabaseConfigTable<Quests.Item, string>
//     {
//         public class Item : IConfigTableItem<string>
//         {
//             public string Id { get; private set; }
//             public int Index { get; private set; }
//             public string Name { get; private set; }
//             public int Experience { get; private set; }
//         }
//     }
// }