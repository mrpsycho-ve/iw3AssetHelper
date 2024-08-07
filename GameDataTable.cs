using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace GDT;

public class GameDataTable
{
    public class Asset
    {
        public Dictionary<object, object> Properties = new Dictionary<object, object>();

        public string Name { get; set; }

        public string Type { get; set; }

        public bool Derivative { get; set; }

        public object this[string key]
        {
            get
            {
                if (!Properties.TryGetValue(key, out var value))
                {
                    return null;
                }
                return value;
            }
            set
            {
                Properties[key] = value;
            }
        }

        public Asset()
        {
        }

        public Asset(string name, string type)
        {
            Name = name;
            Type = type;
        }
    }

    public static Tuple<string, string, string>[] LODGDTKeys = new Tuple<string, string, string>[8]
    {
        new Tuple<string, string, string>("filename", "highLodDist", "autogenHighLod"),
        new Tuple<string, string, string>("mediumLod", "mediumLodDist", "autogenMediumLod"),
        new Tuple<string, string, string>("lowLod", "lowLodDist", "autogenLowLod"),
        new Tuple<string, string, string>("lowestLod", "lowestLodDist", "autogenLowestLod"),
        new Tuple<string, string, string>("lod4File", "lod4Dist", "autogenLod4"),
        new Tuple<string, string, string>("lod5File", "lod5Dist", "autogenLod5"),
        new Tuple<string, string, string>("lod6File", "lod6Dist", "autogenLod6"),
        new Tuple<string, string, string>("lod7File", "lod7Dist", "autogenLod7")
    };

    public Dictionary<string, Dictionary<string, Asset>> Assets { get; set; }

    public Asset this[string type, string assetName]
    {
        get
        {
            lock (this)
            {
                if (Assets.TryGetValue(type, out var value) && value.TryGetValue(assetName, out var value2))
                {
                    return value2;
                }
                return null;
            }
        }
        set
        {
            lock (this)
            {
                if (!Assets.TryGetValue(type, out var value2))
                {
                    value2 = new Dictionary<string, Asset>();
                    Assets[type] = value2;
                }
                value2[assetName] = value;
            }
        }
    }

    public Asset this[string assetName]
    {
        get
        {
            lock (this)
            {
                foreach (KeyValuePair<string, Dictionary<string, Asset>> asset in Assets)
                {
                    if (asset.Value.TryGetValue(assetName, out var value))
                    {
                        return value;
                    }
                }
                return null;
            }
        }
    }

    public GameDataTable()
    {
        Assets = new Dictionary<string, Dictionary<string, Asset>>();
    }

    public GameDataTable(string fileName)
    {
        Load(fileName);
    }

    /// <summary>
    /// Attempts to locate the asset in the lists
    /// </summary>
    /// <param name="name">Name of the asset</param>
    /// <returns>Asset</returns>
    public Asset FindAsset(string name)
    {
        foreach (var list in Assets)
            if (list.Value.TryGetValue(name, out var value))
                return value;

        return null;
    }

    /// <summary>
    /// Attempts to locate the asset in the lists
    /// </summary>
    /// <param name="name">Name of the asset</param>
    /// <returns>Asset</returns>
    public void GetAllAssetsOfType(string type)
    {
        foreach (var list in Assets)
            if (list.Value.TryGetValue(type, out var value))
                Console.WriteLine(value);

        //return null;
    }

    public bool ContainsAsset(string type, string name)
    {
        if (Assets.TryGetValue(type, out var value) && value.ContainsKey(name))
        {
            return true;
        }
        return false;
    }

    public void Save(string fileName)
    {
        using StreamWriter streamWriter = new StreamWriter(fileName);
        streamWriter.WriteLine("{");
        foreach (KeyValuePair<string, Dictionary<string, Asset>> asset in Assets)
        {
            foreach (KeyValuePair<string, Asset> item in asset.Value)
            {
                bool derivative = item.Value.Derivative;
                streamWriter.WriteLine("\t\"{0}\" {2} \"{1}\" {3}", item.Key, item.Value.Type + (derivative ? "" : ".gdf"), derivative ? "[" : "(", derivative ? "]" : ")");
                streamWriter.WriteLine("\t{");
                foreach (KeyValuePair<object, object> property in item.Value.Properties)
                {
                    streamWriter.WriteLine("\t\t\"{0}\" \"{1}\"", property.Key, property.Value);
                }
                streamWriter.WriteLine("\t}");
            }
        }
        streamWriter.WriteLine("}");
    }

    public void Load(string fileName)
    {
        Assets = new Dictionary<string, Dictionary<string, Asset>>();
        List<Asset> list = new List<Asset>();
        string[] array = File.ReadAllLines(fileName);
        int num = -1;
        Asset asset = null;
        for (int i = 0; i < array.Length; i++)
        {
            string text = array[i].Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }
            if (text == "{" && num == -1)
            {
                num = 0;
                continue;
            }
            if (text == "{" && num == 2)
            {
                num = 3;
                continue;
            }
            if (text == "}" && num == 3)
            {
                if (asset.Derivative)
                {
                    list.Add(asset);
                }
                else
                {
                    this[asset.Type, asset.Name] = asset;
                }
                num = 0;
                continue;
            }
            if (text == "}" && num == 0)
            {
                num = 4;
                break;
            }
            switch (num)
            {
                case 0:
                    {
                        MatchCollection matchCollection = Regex.Matches(text, "\\(([^\\)]*)\\)");
                        asset = new Asset
                        {
                            Derivative = (matchCollection.Count == 0)
                        };
                        if (matchCollection.Count == 0)
                        {
                            matchCollection = Regex.Matches(text, "\\[([^\\)]*)\\]");
                        }
                        if (matchCollection.Count == 0)
                        {
                            throw new ArgumentException($"Parse error on line: {i}. Expecting Parentheses for GDF or Derived Asset.");
                        }
                        matchCollection = Regex.Matches(text, "\"([^\"]*)\"");
                        if (matchCollection.Count < 2)
                        {
                            throw new ArgumentException($"Parse error on line: {i}. Expecting Asset Name and GDF/Parent.");
                        }
                        asset.Name = matchCollection[0].Value.Replace("\"", "");
                        asset.Type = matchCollection[1].Value.Replace("\"", "").Replace(".gdf", "");
                        num = 2;
                        break;
                    }
                case 3:
                    {
                        MatchCollection matchCollection = Regex.Matches(text, "\"([^\"]*)\"");
                        if (matchCollection.Count < 2)
                        {
                            throw new ArgumentException($"Parse error on line: {i}. Expecting Setting Group.");
                        }
                        asset[matchCollection[0].Value.Replace("\"", "")] = matchCollection[1].Value.Replace("\"", "");
                        break;
                    }
                default:
                    throw new ArgumentException($"Parse error on line: {i}. Unexpected line {text}.");
            }
        }
        if (num != 4)
        {
            throw new ArgumentException($"Expecting EOF Bracket.");
        }
        foreach (Asset item in list)
        {
            Asset asset2 = this[item.Type];
            if (asset2 != null)
            {
                this[asset2.Type, item.Name] = item;
            }
        }
    }
}
