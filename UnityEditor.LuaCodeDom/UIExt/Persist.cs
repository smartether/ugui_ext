using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

namespace UnityEditor.UI.UIExt
{
    [UnityEditor.InitializeOnLoad]
    public class Persist
    {
        public static Dictionary<System.Type, int> ExportType2ID = new Dictionary<Type, int>(16)
        {
            {typeof(CanvasRenderer), 0 },
            {typeof(RectTransform), 1 },
            {typeof(Transform), 2 },
            {typeof(UnityEngine.UI.Text),3 },
            {typeof(UnityEngine.UI.Image), 4 },
            {typeof(UnityEngine.UI.RawImage), 5 },
            {typeof(UnityEngine.UI.Button),6 },
            {typeof(UnityEngine.UI.InputField), 7 },
            {typeof(UnityEngine.UI.Toggle),8 },
            {typeof(UnityEngine.UI.Scrollbar), 9 },
            {typeof(UnityEngine.UI.Slider), 10 }
        };

        public static List<System.Type> ForceExportType = new List<System.Type>(16)
        {
            typeof(UnityEngine.UI.Button),
        };

        private static Persist _instance = null;
        public static Persist Instance { get { return _instance = _instance ?? new Persist(); } }

        public Persist()
        {
            PrefabUtility.prefabInstanceUpdated += OnPrefabChanged;
        }

        ~Persist()
        {
            PrefabUtility.prefabInstanceUpdated -= OnPrefabChanged;
        }

        private bool isRigisted = false;

        [UnityEditor.InitializeOnLoadMethod]
        public static void StartOnLoad()
        {
            Persist.Instance.Init();
        }

        //private Dictionary<int, GameObject> m_InstanceId2PrefabMap = new Dictionary<int, GameObject>();
        //private List<GameObject> m_rootGameObjects = new List<GameObject>(16);
        string persistPath = Application.dataPath + "/Editor/UIExportConfig/";

        //用于编辑时同步变化
        private Dictionary<int, GameObject> m_prefabId2Go = new Dictionary<int, GameObject>(16);
        private Dictionary<int, PersistData> m_prefabId2Config = new Dictionary<int, PersistData>(16);

        //用于持久化配置
        private Dictionary<string, GameObject> m_prefabGUID2Go = new Dictionary<string, GameObject>(16);
        private Dictionary<string, PersistData> m_prefabGUID2Config = new Dictionary<string, PersistData>(16);

        public void Init()
        {
            m_prefabId2Go.Clear();
            m_prefabId2Config.Clear();
            m_prefabGUID2Go.Clear();
            m_prefabGUID2Config.Clear();
            EditorApplication.update += Update;
        }

        void Update()
        {
            //if (EditorApplication.isPlaying || EditorApplication.isCompiling) return;
            SyncSceneUI2Config();
        }

        void SyncSceneUI2Config()
        {
            var layer = LayerMask.NameToLayer("UI");
            var gos = GameObject.FindObjectsOfType<GameObject>();
            for (int i = 0, count = gos.Length; i < count; i++)
            {
                var go = gos[i];
                if (go.layer == layer)
                {
                    var prefab = PrefabUtility.GetPrefabParent(go);
                    if (prefab != null)
                    {
                        prefab = PrefabUtility.FindPrefabRoot(prefab as GameObject);

                        int id = prefab.GetInstanceID();
                        if (!m_prefabId2Go.ContainsKey(id))
                        {
                            m_prefabId2Go[id] = prefab as GameObject;
                            SyncProjectUI2Config(prefab as GameObject);
                            Debug.Log("$$ prefab:" + prefab.name + " id:" + id);
                        }

                    }
                }
            } 
        }

        void SyncProjectUI2Config(GameObject prefab)
        {
            string prefabPath = AssetDatabase.GetAssetPath(prefab);
            string GUID = AssetDatabase.AssetPathToGUID(prefabPath);
            if (!m_prefabGUID2Go.ContainsKey(GUID))
            {
                m_prefabGUID2Go[GUID] = prefab as GameObject;

                Debug.Log("$$ prepare load config GUID:" + GUID);
                string assetPath = UnityEditor.FileUtil.GetProjectRelativePath(persistPath);
                TextAsset config = AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath + GUID + ".txt");
                if (config != null)
                {
                    Debug.Log("$$ config loaded...");
                    PersistData persistData = new PersistData();
                    EditorJsonUtility.FromJsonOverwrite(config.text, persistData);
                    m_prefabGUID2Config[GUID] = persistData;

                    PrefabNode rootNode = new PrefabNode(prefab);
                    rootNode.AllNodes.ForEach(node =>
                    {
                        persistData.SyncIDWithPath(node.Path, node.Target.GetInstanceID());
                    });

                    Save(persistData);

                    string jsonValid = EditorJsonUtility.ToJson(persistData, true);
                    if (jsonValid == config.text)
                    {
                        Debug.Log("$$ config load success ...");
                        Debug.Log(config.text);
                    }
                }
                else
                {
                    PersistData persistData = new PersistData();
                    persistData.InstanceID = prefab.GetInstanceID();
                    persistData.GUID = GUID;
                    persistData.PATH = prefabPath;
                    persistData.PATH2 = persistPath + GUID + ".txt";

                    List<PersistData.ID2NodePath> id2NodePaths = new List<PersistData.ID2NodePath>(16);
                    List<PersistData.NodePath2NodeConfig> nodePath2NodeConfigs =
                        new List<PersistData.NodePath2NodeConfig>(16);

                    PrefabNode rootNode = new PrefabNode(prefab);
                    rootNode.AllNodes.ForEach(node =>
                    {
                        string path = node.Path;
                        PersistData.ID2NodePath id2Node = new PersistData.ID2NodePath()
                        {
                            ID = node.Target.GetInstanceID(),
                            PATH = path
                        };
                        id2NodePaths.Add(id2Node);

                        var nodeConfig = new PersistData.NodeConfig();
                        PersistData.NodePath2NodeConfig nodePath2NodeConfig = new PersistData.NodePath2NodeConfig() {Config = nodeConfig, NodePath = path};
                        nodePath2NodeConfigs.Add(nodePath2NodeConfig);
                    });

                    persistData.ID2NodePaths = id2NodePaths.ToArray();
                    persistData.NodePath2NodeConfigs = nodePath2NodeConfigs.ToArray();

                    m_prefabGUID2Config[GUID] = persistData;

                    string jsonStr = EditorJsonUtility.ToJson(persistData, true);

                    if (!System.IO.Directory.Exists(persistPath))
                    {
                        System.IO.Directory.CreateDirectory(persistPath);
                    }
                    System.IO.File.WriteAllText(persistPath + GUID + ".txt", jsonStr, Encoding.UTF8);

                    //jsonStr = JsonUtility.ToJson(persistData,true);
                    Debug.Log("$$ jsonStr: \n" + jsonStr);
                }
            }
        }
        
        /// <summary>
        /// 保存UI组件导出配置
        /// </summary>
        /// <param name="ui"></param>
        public void Save(PersistData persistData, bool merge = false)
        {
            //var prefabType = PrefabUtility.GetPrefabType(ui);
            //GameObject prefab = ui;
            //if (PrefabType.PrefabInstance == prefabType)
            //{
            //    prefab = PrefabUtility.GetPrefabParent(ui) as GameObject;
            //}
            string jsonStr = EditorJsonUtility.ToJson(persistData, true);

            if (!System.IO.Directory.Exists(persistPath))
            {
                System.IO.Directory.CreateDirectory(persistPath);
            }

            string jsonFilePath = persistPath + persistData.GUID + ".txt";
            if (merge && System.IO.File.Exists(jsonFilePath) && UnityEditor.EditorUtility.DisplayDialog("UI工具提示", "导出配置已经存在 手动合并或者覆盖","合并","覆盖"))
            {
                string jsonFileTmpPath = System.IO.Path.GetTempPath() + "/UIExportConfig/";
                if (!System.IO.Directory.Exists(jsonFileTmpPath))
                {
                    System.IO.Directory.CreateDirectory(jsonFileTmpPath);
                }
                System.IO.File.WriteAllText(jsonFileTmpPath + persistData.GUID + ".txt", jsonStr, Encoding.UTF8);
                System.Diagnostics.Process.Start("TortoiseMerge"," /base:" + jsonFileTmpPath + persistData.GUID + ".txt" + " /mine:" + jsonFilePath + " " + jsonFilePath);
            }
            else
            {
                System.IO.File.WriteAllText(jsonFilePath, jsonStr, Encoding.UTF8);
            }

        }

        void OnPrefabChanged(GameObject go)
        {
            //int id = go.GetInstanceID();
            //string prefabPath = AssetDatabase.GetAssetPath(go);
            //string prefabGUID = AssetDatabase.AssetPathToGUID(prefabPath);
            go = PrefabUtility.GetPrefabParent(go) as GameObject;
            PrefabNode rootNode = new PrefabNode(go);

            //Debug.Log("$$ boundary count:" + rootNode.AllBoundaries.Count);
            /*
            rootNode.AllBoundaries.ForEach(node =>
            {
                var modifications = PrefabUtility.GetPropertyModifications(node.Target);
                
                foreach (var propertyModification in modifications)
                {
                    Debug.Log("$$ name:" + node.Target.name + " " + propertyModification.propertyPath + " changed with:" + propertyModification.value);
                }
            });
            */

            string path = AssetDatabase.GetAssetPath(go);
            string GUID = AssetDatabase.AssetPathToGUID(path);
            Assert.IsTrue(m_prefabGUID2Config.ContainsKey(GUID), "$$ UI Prefab path: " + path +" is not in manage..");
            if (m_prefabGUID2Config.ContainsKey(GUID))
            {
                var persistData = m_prefabGUID2Config[GUID];
                persistData.PATH = path;
                rootNode.AllNodes.ForEach(node =>
                {
                    persistData.SyncPathWithID(node.Target.GetInstanceID(), node.Path);
                });

                List<string> path2Remove = new List<string>();

                foreach (var nodePath in persistData.ID2NodePaths)
                {
                    bool exist = rootNode.AllNodes.Exists(node => node.Path == nodePath.PATH);
                    if (!exist)
                    {
                        Debug.Log("$$ path to be remove: " + nodePath.PATH);
                        path2Remove.Add(nodePath.PATH);
                    }
                }

                path2Remove.ForEach(s =>
                {
                    persistData.RemoveNodePathWithPath(s);
                });
                
                Save(persistData, true);
            }

        }

        public PersistData GetPersistDataWithPrefab(GameObject go)
        {
            string path = AssetDatabase.GetAssetPath(go);
            string GUID = AssetDatabase.AssetPathToGUID(path);
            if (m_prefabGUID2Config.ContainsKey(GUID))
            {
                return m_prefabGUID2Config[GUID];
            }

            return null;
        }
    }
}