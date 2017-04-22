using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace UnityEditor.UI.UIExt
{
    public class PrefabNode : Node<GameObject>
    {

        private List<PrefabNode> m_boundaries = null;
        private List<PrefabNode> m_allNodes = null;

        public PrefabNode(GameObject target, PrefabNode parent = null) : base(target, parent)
        {
            if (IsRoot)
            {
                m_boundaries = new List<PrefabNode>();
                m_allNodes = new List<PrefabNode>();
            }

            if (AllNodes.Exists(node => node.Path == this.Path))
            {
                m_target.name = m_target.name + UnityEngine.Random.Range(0, 10000).ToString();
                UnityEditor.EditorUtility.DisplayDialog("UI工具提示", "有相同路径 即将重命名为" + Path, "好的");
            }
            AllNodes.Add(this);

            if (m_target.transform.childCount == 0)
            {
                PrefabNode root = this;
                PrefabNode super = this;
                while (super.Parent != null)
                {
                    super = super.Parent;
                }
                root = super;
                
                //add boundaries node to root
                root.m_boundaries.Add(this);
            }
            foreach (Transform child in m_target.transform)
            {
                var node = new PrefabNode(child.gameObject, this);
                m_children.Add(node);
            }
            
        }
        
        public string Path
        {
            get
            {
                List<string> pathNodes = new List<string>(8);
                pathNodes.Add(m_target.name);
                PrefabNode parent = m_parent as PrefabNode;
                int loopRemain = 10000;
                while (parent != null && loopRemain > 0)
                {
                    pathNodes.Add(parent.m_target.name);
                    parent = parent.m_parent as PrefabNode;
                    loopRemain--;
                }
                pathNodes.Reverse();
                string path = string.Join("/", pathNodes.ToArray());
                return path;
            }
        }

        public new PrefabNode Parent
        {
            get
            {
                return base.Parent as PrefabNode;
            }
        }

        public new List<PrefabNode> Children
        {
            get
            {
                var lst = base.Children.ConvertAll(input => input as PrefabNode);
                return lst;
            }
        }
        
        public List<PrefabNode> AllBoundaries
        {
            get
            {
                if (IsRoot)
                {
                    return m_boundaries;
                }
                else
                {
                    PrefabNode root = Root as PrefabNode;
                    return root.AllBoundaries;
                }
            }
        }

        public List<PrefabNode> AllNodes
        {
            get
            {
                if (IsRoot)
                {
                    return m_allNodes;
                }
                else
                {
                    PrefabNode root = Root as PrefabNode;
                    return root.m_allNodes;
                }
            }
        }
        
    }
}