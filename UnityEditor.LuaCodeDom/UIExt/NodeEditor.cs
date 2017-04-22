using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.AnimatedValues;
using UnityEditor.UI.UIExt;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor.UI;

namespace UnityEditor.UI.UIExt
{

    [CustomEditor(typeof(UnityEngine.CanvasRenderer), true, isFallback = true)]
    public class NodeEditor : Editor
    {
        protected AnimBool m_ShowNativeSize;

        protected GameObject m_go = null;
        protected GameObject m_PrefabRoot = null;
        protected GameObject m_PrefabParent = null;
        protected int m_id = 0;

        protected PersistData m_PersistData = null;
        protected PersistData.NodeConfig m_NodeConfig = null;

        protected bool IsSubView = false;

        protected bool Valid = false;

        protected List<string> Type2Export = new List<string>(); 

        protected virtual void OnDisable()
        {
            Tools.hidden = false;
        }
        
        protected virtual void OnEnable()
        {
            try
            {
                var target = serializedObject.targetObject as CanvasRenderer;

                m_go = target.gameObject;
                bool IsPrefabInstance = PrefabUtility.GetPrefabType(target.gameObject) == PrefabType.PrefabInstance;

                Valid = m_go.activeInHierarchy && m_go.activeSelf && IsPrefabInstance && target.gameObject.layer == LayerMask.NameToLayer("UI");

                if (!Valid)
                    return;



                m_PrefabParent = PrefabUtility.GetPrefabParent(target.gameObject) as GameObject;
                m_id = m_PrefabParent.GetInstanceID();
                m_PrefabRoot = PrefabUtility.FindPrefabRoot(m_PrefabParent) as GameObject;

                m_PersistData = Persist.Instance.GetPersistDataWithPrefab(m_PrefabRoot);
                m_NodeConfig = m_PersistData.GetNodeConfigWithId(m_PrefabParent.GetInstanceID()).Config;


                m_ShowNativeSize = new AnimBool(false);
                m_ShowNativeSize.valueChanged.AddListener(Repaint);


            }
            catch (Exception e)
            {
                Debug.LogWarning("$$ OnEnable " + e.Message);
                Debug.Log("$$ OnEnable " + e.StackTrace);
            }

        }

        public override void OnInspectorGUI()
        {
            if (!Valid)
                return;

            try
            {
                Layout();
            }
            catch (Exception e)
            {
                Debug.LogWarning("$$ OnInspectorGUI " + e.Message);
                Debug.LogWarning("$$ OnInspectorGUI " + e.StackTrace);
            }
        }

        protected void SetShowNativeSize(bool show, bool instant)
        {
            if (instant)
                m_ShowNativeSize.value = show;
            else
                m_ShowNativeSize.target = show;
        }

        protected void Layout()
        {
            GUILayout.BeginVertical();
            {
                m_NodeConfig.IsSubView = GUILayout.Toggle(m_NodeConfig.IsSubView, "IsSubView");

                GUILayout.BeginHorizontal();
                {
                    m_NodeConfig.IsTemplate = GUILayout.Toggle(m_NodeConfig.IsTemplate && m_NodeConfig.IsSubView,
                        "IsTemplate");
                    if (m_NodeConfig.IsTemplate)
                    {
                        m_NodeConfig.TemplateName = GUILayout.TextField(m_NodeConfig.TemplateName);
                    }

                    GUILayout.EndHorizontal();
                }

                if (m_NodeConfig.IsTemplate && !m_NodeConfig.IsSubView)
                {
                    if (UnityEditor.EditorUtility.DisplayDialog("提示", "导出subView代码需要定义该节点为subview 是否定义为subview", "好的",
                        "取消"))
                    {
                        m_NodeConfig.IsSubView = true;
                        m_NodeConfig.IsTemplate = true;
                    }
                }

                if (m_NodeConfig.IsSubView)
                {
                    m_NodeConfig.ExportTypes = null;
                }
                else
                {
                    var coms = m_go.GetComponents<Component>();
                    List<string> exportTypes = new List<string>();
                    if (m_NodeConfig.ExportTypes != null)
                    {
                        exportTypes.AddRange(m_NodeConfig.ExportTypes);
                    }
                    for (int i = 0, count = coms.Length; i < count; i++)
                    {
                        var com = coms[i];
                        var comName = com.GetType().FullName;
                        if (comName.StartsWith("UnityEngine.UI") || comName.StartsWith("Game.uGUI.Widgets") || comName.StartsWith("UnityEngine.RectTransform") ||
                            comName.StartsWith("UnityEngine.Transform"))
                        {
                            bool isChecked = exportTypes.Contains(comName);
                            bool value = GUILayout.Toggle(isChecked, comName);
                            if (Persist.ForceExportType.Contains(com.GetType()))
                            {
                                value = true;
                            }
                            if (isChecked != value)
                            {
                                if (value)
                                {
                                    exportTypes.Add(comName);
                                }
                                else
                                {
                                    exportTypes.Remove(comName);
                                }
                            }
                        }
                    }

                    m_NodeConfig.ExportTypes = exportTypes.ToArray();
                }

                GUILayout.EndVertical();
            }
        }

    }
}