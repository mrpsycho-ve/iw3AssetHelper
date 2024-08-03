using GDT;
using MyProg;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml.Linq;
using static GDT.GameDataTable;

namespace MyApp // Note: actual namespace depends on the project name.
{
    public class MaterialInfo
    {
        public int materialIndex;
        public string mapType;
        public string mapFileName;
    }
    public class Material
    {
        public string colorMap;
        public string normalMap;
        public string detailMap;
        public string cosinePowerMap;
        public string specColorMap;
    }
    public class xModel
    {
        public string filename;
        public string type;
        public string cosinePowerMap;
        public string specColorMap;
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            /*var configFile = new IniFile("config.ini");
            if (!configFile.KeyExists("TOOLS_PATH"))
            {
                configFile.Write("TOOLS_PATH", "C:\\");
            }

            string cod4Path = configFile.Read("TOOLS_PATH");*/

            string buf = null;

            foreach (string asset in args)
            {
                if (!File.Exists(asset)) return;
                string ext = Path.GetExtension(asset);
                bool bConverter;

                switch (ext)
                {
                    case ".xmodel_export":
                        {
                            handlexModel(asset);
                            break;
                        }
                    case ".xanim_export":
                        {
                            if (String.IsNullOrEmpty(buf))
                            {
                                Console.WriteLine("drag in anim model pussy");
                                buf = Console.ReadLine();
                            }


                            handlexAnim(asset, buf);
                            break;
                        }
                    case ".gdt":
                        {
                            Console.WriteLine("converter or linker");
                            buf = Console.ReadLine();
                            switch (buf)
                            {
                                case "converter": bConverter = true; break;
                                case "linker": bConverter = false; break;
                                default:
                                    {
                                        Console.WriteLine("invalid input, press any key to close");
                                        Console.ReadKey();
                                        return;
                                    }
                            }

                            Console.WriteLine("enter the type of assets you wanna do (xanim, xmodel, material, all)");
                            buf = Console.ReadLine();

                            switch (buf)
                            {
                                case "xmodel": break;
                                case "xanim": break;
                                case "material": break;
                                case "all": break;
                                default:
                                    {
                                        Console.WriteLine("invalid input, press any key to close");
                                        Console.ReadKey();
                                        return;
                                    }
                            }

                            if(bConverter)
                                handleGDT(asset, buf);
                            else
                                handleLinker(asset, buf);

                            break;
                        }
                    default:
                        {
                            Console.WriteLine("file \"" + ext + "\" does not have a recognized extension. (xmodel_export, gdt)");
                            return;
                        }
                }
            }
            File.AppendAllText("convert.bat", "\npause");
        }

        public static void handlexModel(string xModel)
        {
            GameDataTable gameDataTable = new GameDataTable();

            if (File.Exists("gdt.gdt"))
                gameDataTable.Load("gdt.gdt");

            using (StreamReader sr = new StreamReader(xModel))
            {
                while (sr.Peek() >= 0)
                {
                    string str;
                    string imgFolder;
                    string strBuffer;
                    string[] strArray;
                    string[] fileArray;
                    str = sr.ReadLine();
                    Material material = new Material();

                    if (!str.StartsWith("MATERIAL"))
                        continue;

                    strArray = str.Split(' ');

                    imgFolder = System.IO.Path.GetDirectoryName(xModel) + "\\_images";
                    strBuffer = imgFolder + "\\" + strArray[2].Trim('"');
                    //Console.Write(imgFolder + "\n");

                    if (System.IO.Path.Exists(strBuffer)) Console.WriteLine(strBuffer);

                    fileArray = Directory.GetFiles(strBuffer);

                    foreach (string file in fileArray)
                    {
                        if (System.IO.Path.GetFileNameWithoutExtension(file).EndsWith("_c")) material.colorMap = file.Substring(file.IndexOf("\\model_export") + 1).Replace("\\", "\\\\");
                        if (System.IO.Path.GetFileNameWithoutExtension(file).EndsWith("_d")) material.detailMap = file.Substring(file.IndexOf("\\model_export") + 1).Replace("\\", "\\\\");
                        if (System.IO.Path.GetFileNameWithoutExtension(file).EndsWith("_n")) material.normalMap = file.Substring(file.IndexOf("\\model_export") + 1).Replace("\\", "\\\\");
                        if (System.IO.Path.GetFileNameWithoutExtension(file).EndsWith("_s")) material.specColorMap = file.Substring(file.IndexOf("\\model_export") + 1).Replace("\\", "\\\\");
                        if (System.IO.Path.GetFileNameWithoutExtension(file).EndsWith("_g")) material.cosinePowerMap = file.Substring(file.IndexOf("\\model_export") + 1).Replace("\\", "\\\\");
                    }

                    GameDataTable.Asset asset = new GameDataTable.Asset(strArray[2].Trim('"'), "material");
                    asset["colorMap"] = material.colorMap;
                    asset["detailMap"] = material.detailMap;
                    asset["cosinePowerMap"] = material.cosinePowerMap;
                    asset["normalMap"] = material.normalMap;
                    asset["specColorMap"] = material.specColorMap;
                    asset["template"] = "material.template";
                    asset["materialType"] = "model phong";
                    asset["surfaceType"] = "<none>";

                    File.AppendAllText("convert.bat", "\nconverter -nocachedownload -nopause -single \"material\" " + strArray[2]);

                    gameDataTable["material", asset.Name] = asset;

                    GameDataTable.Asset xModelAsset = new GameDataTable.Asset(Path.GetFileNameWithoutExtension(xModel), "xmodel");
                    xModelAsset["filename"] = xModel.Substring(xModel.IndexOf("export\\") + 7).Replace("\\", "\\\\");
                    xModelAsset["type"] = Path.GetFileNameWithoutExtension(xModel).StartsWith("vm_") ? "animated" : "rigid";

                    File.AppendAllText("convert.bat", "\nconverter -nocachedownload -nopause -single \"xmodel\" " + xModelAsset.Name);
                    gameDataTable["xmodel", xModelAsset.Name] = xModelAsset;
                }

                gameDataTable.Save(Path.GetFileNameWithoutExtension(xModel) + ".gdt");
            }
        }
        public static void handlexAnim(string xAnim, string animModel)
        {
            GameDataTable gameDataTable = new GameDataTable();

            if (File.Exists("gdt.gdt"))
                gameDataTable.Load("gdt.gdt");

            GameDataTable.Asset xAnimAsset = new GameDataTable.Asset(Path.GetFileNameWithoutExtension(xAnim), "xanim");
            xAnimAsset["filename"] = xAnim.Substring(xAnim.IndexOf("export\\") + 7).Replace("\\", "\\\\");
            xAnimAsset["model"] = animModel.Substring(xAnim.IndexOf("export\\") + 7).Replace("\\", "\\\\");

            if (xAnim.Contains("idle") || xAnim.Contains("loop"))
                xAnimAsset["looping"] = "1";

            xAnimAsset["type"] = "relative";
            xAnimAsset["angleError"] = "0.0001";
            xAnimAsset["useBones"] = "0";
            xAnimAsset["translationError"] = "0.0001";

            File.AppendAllText("convert.bat", "\nconverter -nocachedownload -nopause -single \"xmodel\" " + xAnimAsset.Name);
            gameDataTable["xanim", xAnimAsset.Name] = xAnimAsset;
            gameDataTable.Save("gdt.gdt");
        }

        public static void handleGDT(string gdt, string type)
        {
            Console.WriteLine("starting " + type + " asset conversion of GDT " + Path.GetFileNameWithoutExtension(gdt));
            GameDataTable gameDataTable = new GameDataTable(gdt);
            string buffer;
            /*string[] bufferArray = gamePath.Split("\\");

            File.WriteAllText("convert.bat", bufferArray[0] + "\n");
            File.AppendAllText("convert.bat", "cd " + gamePath + "\\bin");*/

            foreach (KeyValuePair<string, Dictionary<string, Asset>> asset in gameDataTable.Assets)
            {
                if(asset.Key != type) continue;
                foreach (KeyValuePair<string, Asset> item in asset.Value)
                {
                    File.AppendAllText("convert.bat", "\nconverter -nocachedownload -nopause -single \"" + type + "\" " + item.Key);
                }
            }
        }
        public static void handleLinker(string gdt, string type)
        {
            Console.WriteLine("starting " + type + " asset list building of GDT " + Path.GetFileNameWithoutExtension(gdt));
            GameDataTable gameDataTable = new GameDataTable(gdt);
            string buffer;
            /*string[] bufferArray = gamePath.Split("\\");*/

            File.WriteAllText("assetlist.csv", "//" + Path.GetFileNameWithoutExtension(gdt) + " assetlist" + "\n");

            foreach (KeyValuePair<string, Dictionary<string, Asset>> asset in gameDataTable.Assets)
            {
                if (asset.Key != type) continue;
                foreach (KeyValuePair<string, Asset> item in asset.Value)
                {
                    File.AppendAllText("assetlist.csv", "\n" + type + "," + item.Key);
                }
            }
        }
    }
}