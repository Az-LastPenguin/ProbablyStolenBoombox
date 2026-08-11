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

namespace ProbablyStolenBoombox
{
    public record CassetteInfo(string id, string name, string composer, string sprite, float weight);

    public class Core : MelonMod
    {
        private string AudioFolderPath = Path.Combine(MelonEnvironment.ModsDirectory, "Boombox");

        private ProbabilityList<string> CassetPL;

        private ProbabilityItem<string> PITemplate;

        public override void OnLateInitializeMelon()
        {
            LoggerInstance.Msg("Initialized.");

            // If there is no Boombox folder, create one
            if (!Directory.Exists(AudioFolderPath))
                Directory.CreateDirectory(AudioFolderPath);
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

            // Create new loot table for dumping grounds
            // 1. Create new ProbabilityItem<LootTable>
            TableGroup dumpingTG = TableGroupMaster.tableGroups["dumpingGroundTG"];
            ProbabilityItem<LootTable> cassettePI = dumpingTG.tableGroup.m_ProbabilityItems.System_Collections_IList_get_Item(0).MemberwiseClone().Cast<ProbabilityItem<LootTable>>();
            cassettePI.m_DepletableItem = false;
            cassettePI.m_BaseProbability = 0.01f;

            // 2. Create the Loot Table
            LootTable cassetteLT = cassettePI.m_Value.MemberwiseClone().Cast<LootTable>();
            cassetteLT.name = "boomboxCassetteTable";
            cassettePI.m_Value = cassetteLT;

            // 3. Create the ProbabilityList for the Loot Table
            CassetPL = cassetteLT.table.MemberwiseClone().Cast<ProbabilityList<string>>();
            cassetteLT.table = CassetPL;

            // 4. Empty the ProbabilityList and make a template for ProbabilityItem<string>
            PITemplate = CassetPL.m_ProbabilityItems.System_Collections_IList_get_Item(0).MemberwiseClone().Cast<ProbabilityItem<string>>();
            CassetPL.m_ProbabilityItems.Clear();

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

            // Add handler for creating this cassette 
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

            // Add the cassette to the loottable
            ProbabilityItem<string> probItem = PITemplate.MemberwiseClone().Cast<ProbabilityItem<string>>();
            probItem.m_Value = cassetteId;
            probItem.m_BaseProbability = info.weight;

            CassetPL.m_ProbabilityItems.Add(probItem);
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