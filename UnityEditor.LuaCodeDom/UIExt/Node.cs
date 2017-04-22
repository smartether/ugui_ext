using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.UI.UIExt
{
    public interface INode
    {
        INode Root { get; }
        INode Parent { get; }

        List<INode> Children { get; }
    }
    /// <summary>
    /// 创建一个rootnode 会自动向下渗透所有子节点信息
    /// </summary>
    public class Node<T> : INode
    {
        protected T m_target;

        protected Node<T> m_parent = null;

        protected List<INode> m_children = new List<INode>(8);

        public virtual INode Parent
        {
            get { return m_parent;}
        }

        public T Target
        {
            get { return m_target; }
        }

        public virtual List<INode> Children
        {
            get
            {
                return new List<INode>(m_children);
            }
        }

        public bool IsRoot => Parent == null;

        public virtual INode Root
        {
            get
            {
                INode root = this;
                INode super = this;
                int maxDepth = 10000;
                while (super.Parent != null && maxDepth>0)
                {
                    super = super.Parent;
                    maxDepth--;
                }
                root = super;
                return root;
            }
        }

        //public bool IsBoundary
        //{
        //    get { return Children.Count == 0; }
        //}

        public bool IsBoundary => Children.Count == 0;


        public Node(T target, Node<T> parent = null)
        {
            m_target = target;
            m_parent = parent;
        }

    }
}