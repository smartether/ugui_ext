using System;
using System.Collections.Generic;
using UnityEngine;


namespace UnityEditor.UI.UIExt
{
    //整个预知件对应的配置信息
    [System.Serializable]
    public class PersistData
    {   
        //每个节点上的附加配置
        [System.Serializable]
        public class NodeConfig
        {
            //是否为子view
            public bool IsSubView = false;
            public bool IsTemplate = false;
            public string TemplateName = string.Empty;
            public string[] ExportTypes;

        }
        [System.Serializable]
        public class ID2NodePath
        {
            [NonSerialized]
            public int ID;
            public string PATH;
        }
        [System.Serializable]
        public class NodePath2NodeConfig
        {
            public string NodePath;
            public NodeConfig Config;
        }

        public int InstanceID;
        public string GUID;
        public string PATH;

        public string PATH2;
        
        //map instanceId to nodepath
       
        public ID2NodePath[] ID2NodePaths;
        
        public NodePath2NodeConfig[] NodePath2NodeConfigs;
        

        //sync id on prefab awake 
        public void SyncIDWithPath(string path, int id)
        {
            var nodePath = GetNodePathWithPath(path);
            //may be node delete without sync
            if (nodePath == null)
            {
                UnityEditor.EditorUtility.DisplayDialog("UI工具提示", "预知件节点:" + path + " 被删除后没有同步到配置中 即将自动修复 可能会有一些配置需要重新设置", "好的");
                return;
            }
            nodePath.ID = id;
        }

        //sync path on prefab changed
        public void SyncPathWithID(int id, string path)
        {
            ID2NodePath nodePath = GetNodePathWithID(id);

            //may be a new node
            if (nodePath == null)
            {
                nodePath = new ID2NodePath() {ID = id, PATH = path};
                List<ID2NodePath> id2NodePath = new List<ID2NodePath>(ID2NodePaths);
                id2NodePath.Add(nodePath);
                ID2NodePaths = id2NodePath.ToArray();
            }
            var nodeConfig = GetNodeConfigWithPath(nodePath.PATH);
            if (nodeConfig == null)
            {
                nodeConfig = new NodePath2NodeConfig() {NodePath = path, Config = new NodeConfig() };
                List<NodePath2NodeConfig> nodePath2NodeConfigs=new List<NodePath2NodeConfig>(NodePath2NodeConfigs);
                nodePath2NodeConfigs.Add(nodeConfig);
                NodePath2NodeConfigs = nodePath2NodeConfigs.ToArray();
            }
  

            nodeConfig.NodePath = path;
            nodePath.PATH = path;
        }

        public void RemoveNodePathWithPath(string path)
        {
            List<ID2NodePath> nodePaths = new List<ID2NodePath>(ID2NodePaths);
            nodePaths.RemoveAll(id2NodePath => id2NodePath.PATH == path);
            ID2NodePaths = nodePaths.ToArray();

            List<NodePath2NodeConfig> nodePath2NodeConfigs = new List<NodePath2NodeConfig>(NodePath2NodeConfigs);
            nodePath2NodeConfigs.RemoveAll(config => config.NodePath == path);
            NodePath2NodeConfigs = nodePath2NodeConfigs.ToArray();
        }

        public ID2NodePath GetNodePathWithPath(string path)
        {
            for (int i = 0, cout = ID2NodePaths.Length; i < cout; i++)
            {
                if (ID2NodePaths[i].PATH == path)
                {
                    return ID2NodePaths[i];
                }
            }
            return default(ID2NodePath);
        }

       
        public ID2NodePath GetNodePathWithID(int id)
        {
            for (int i = 0,cout = ID2NodePaths.Length; i < cout; i++)
            {
                if (ID2NodePaths[i].ID == id)
                {
                    return ID2NodePaths[i];
                }
            }
            return default(ID2NodePath);
        }

        public NodePath2NodeConfig GetNodeConfigWithPath(string path)
        {
            for (int i = 0, cout = NodePath2NodeConfigs.Length; i < cout; i++)
            {
                if (NodePath2NodeConfigs[i].NodePath == path)
                {
                    return NodePath2NodeConfigs[i];
                }
            }
            return default(NodePath2NodeConfig);
        }

        public NodePath2NodeConfig GetNodeConfigWithId(int id)
        {
            var path = GetNodePathWithID(id).PATH;
            return GetNodeConfigWithPath(path);
        }
    }
}