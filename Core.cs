using Il2Cpp;
using Il2CppInterop.Runtime;
using Il2CppRNGNeeds;
using MelonLoader;
using MelonLoader.Utils;
using System.Collections;
using System.Text.Json;
using UnityEngine;
using UnityEngine.Networking;

[assembly: MelonInfo(typeof(ProbablyStolenBoombox.Core), "ProbablyStolenBoombox", "1.0.0", "LastPenguin", null)]
[assembly: MelonGame("Questing Goose Studio", "Probably Stolen")]
[assembly: HarmonyDontPatchAll]

namespace ProbablyStolenBoombox
{
    public record CassetteInfo(string id, string name, string composer, string sprite, float weight);

    public class Core : MelonMod
    {
        private string AudioFolderPath = Path.Combine(MelonEnvironment.ModsDirectory, "Boombox");

        private ProbabilityList<string> CassettePL;

        private ProbabilityItem<string> PITemplate;


        private MelonPreferences_Category boomboxCategory;
        private MelonPreferences_Entry<float> cassettePoolWeight;
        private MelonPreferences_Entry<bool> smallCassettes;

        public override void OnLateInitializeMelon()
        {
            // If there is no Boombox folder, create one
            if (!Directory.Exists(AudioFolderPath))
                Directory.CreateDirectory(AudioFolderPath);

            boomboxCategory = MelonPreferences.CreateCategory("Boombox");
            cassettePoolWeight = boomboxCategory.CreateEntry<float>("Cassette Pool Weight", 0.01f);
            smallCassettes = boomboxCategory.CreateEntry<bool>("Make Cassetes Small", true);
            MelonPreferences.Save();

            // Make sprites small if needed & patch cassette creation to make them small
            if (smallCassettes.Value)
            {
                for (int i = 1; i < 11; i++)
                {
                    string cassetteId = $"cassette{i}";
                    Sprite oldSprite = RenderHandler.LoadFromAtlas("Items/items_amenities", cassetteId);
                    Sprite newSprite = Sprite.Create(oldSprite.texture, oldSprite.rect, oldSprite.pivot, 200);
                    RenderHandler.atlasCache["Items/items_amenities"][cassetteId] = newSprite;
                }

                HarmonyInstance.PatchAll(typeof(Patches));
            }

            LoggerInstance.Msg("Probably Stolen Boombox Initialized.");
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            if (sceneName != "InventoryScene")
                return;

            MelonCoroutines.Start(CreateCassettes());
        }

        private IEnumerator CreateCassettes()
        {
            if (AudioDirectory.activeDirectory == null)
                yield return null;

            LoggerInstance.Msg("Creating Cassettes.");

            // Create new loot table for dumping grounds
            // 1. Create new ProbabilityItem<LootTable>
            TableGroup dumpingTG = TableGroupMaster.tableGroups["dumpingGroundTG"];
            // I couldn't for the love of god figure out why i can't create ProbabilityItem without cloning. If i don't clone, it instead becomes the type T and sometimes even crashes
            ProbabilityItem<LootTable> cassettePI = dumpingTG.tableGroup.m_ProbabilityItems[0].MemberwiseClone().Cast<ProbabilityItem<LootTable>>();
            cassettePI.m_DepletableItem = false;
            cassettePI.m_BaseProbability = cassettePoolWeight.Value;

            // 2. Create the Loot Table
            LootTable cassetteLT = new LootTable();
            cassetteLT.name = "boomboxCassetteTable";
            cassettePI.m_Value = cassetteLT;

            // 3. Create the ProbabilityList for the Loot Table
            CassettePL = new ProbabilityList<string>();
            cassetteLT.table = CassettePL;

            // 4. Empty the ProbabilityList and make a template for ProbabilityItem<string>
            PITemplate = dumpingTG.tableGroup.m_ProbabilityItems[0].m_Value.table.m_ProbabilityItems[0].MemberwiseClone().Cast<ProbabilityItem<string>>();
            CassettePL.m_ProbabilityItems.Clear();

            // 5. Add the ProbabilityItem<LootTable> to the tablegroup
            dumpingTG.tableGroup.m_ProbabilityItems.Add(cassettePI);

            // Create cassettes
            // Get only recognized file formats
            List<string> allFiles = Directory.GetFiles(AudioFolderPath).Where(f => GetAudioType(f) != AudioType.UNKNOWN).ToList();

            // Go through all of them
            foreach (string file in allFiles)
            {
                // Try to get meta file
                string metaPath = Path.ChangeExtension(file, ".txt");
                if (!File.Exists(metaPath))
                    continue;

                CassetteInfo info = JsonSerializer.Deserialize<CassetteInfo>(File.ReadAllText(metaPath));

                // Create the cassettes
                MelonCoroutines.Start(CreateCassette(file, info));
            }
        }

        private IEnumerator CreateCassette(string filePath, CassetteInfo info)
        {
            string cassetteId = $"cassette_{info.id}";

            // 1. Load the audio file
            string URI = $"file://{filePath}";

            AudioType type = GetAudioType(filePath);

            UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(URI, type);
            yield return request.SendWebRequest();

            if (request.isHttpError || request.isNetworkError)
            {
                Debug.LogError("Error loading audio: " + request.error);

                yield break;
            }

            AudioClip clip = DownloadHandlerAudioClip.GetContent(request);

            request.Dispose();

            // Add audio to the audio directory
            AudioDirectory.activeDirectory.audioAssetDict[cassetteId] = clip;

            // 2. Add handler for creating this cassette 
            if (!DirectoryMaster.singletonInstance.TryGetValue(Il2CppType.Of<GameItem>(), out var gameItemDirs))
                yield break;

            MiscItemDirectory miscItemDir = gameItemDirs.Find(new System.Func<Il2CppSystem.Object, bool>(d => d.GetIl2CppType() == Il2CppType.Of<MiscItemDirectory>())).Cast<MiscItemDirectory>();

            miscItemDir.factoryDictionary[cassetteId] = new System.Func<GameItem>(() =>
            {
                GameItem cassette = MiscItemDirectory.MusicCassette(cassetteId, info.name, info.sprite, "", state: 2);
                cassette.longDescription = $"Composer: {info.composer}";
                cassette.shortDescription = $"Composer: {info.composer}";

                return cassette;
            });

            // 3. Add the cassette to the lootTable
            ProbabilityItem<string> probItem = PITemplate.MemberwiseClone().Cast<ProbabilityItem<string>>();
            probItem.m_Value = cassetteId;
            probItem.m_BaseProbability = info.weight;

            CassettePL.m_ProbabilityItems.Add(probItem);

            LoggerInstance.Msg($"Cassette {info.id} was created.");
        }

        private AudioType GetAudioType(string path)
        {
            string ext = Path.GetExtension(path).ToLower();
            switch (ext)
            {
                case ".wav": return AudioType.WAV;
                case ".ogg": return AudioType.OGGVORBIS;
                case ".mp3": return AudioType.MPEG;
                default: return AudioType.UNKNOWN;
            }
        }
    }
}