using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UnityEditor.UI.UIExt
{
    [CustomPreview(typeof(GameObject))]
    class NodePreview : ObjectPreview
    {
        private const float kLabelWidth = 110;
        private const float kValueWidth = 100;

        class Styles
        {
            public GUIStyle labelStyle = new GUIStyle(EditorStyles.label);
            public GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel);

            public Styles()
            {
                Color fontColor = new Color(0.7f, 0.7f, 0.7f);
                labelStyle.padding.right += 4;
                labelStyle.normal.textColor = fontColor;
                headerStyle.padding.right += 4;
                headerStyle.normal.textColor = fontColor;
            }
        }

        private GUIContent m_Title;
        private Styles m_Styles = new Styles();

        #region ExportConfig
        protected GameObject m_go = null;
        protected GameObject m_PrefabRoot = null;
        protected GameObject m_PrefabParent = null;
        protected int m_id = 0;

        protected PersistData m_PersistData = null;
        protected PersistData.NodeConfig m_NodeConfig = null;

        protected bool IsSubView = false;

        protected bool Valid = false;

        #endregion

        public override void OnPreviewSettings()
        {
            base.OnPreviewSettings();
        }
        
        public override void Initialize(UnityEngine.Object[] targets)
        {
            base.Initialize(targets);

            GameObject go = target as GameObject;
            m_go = go;
            bool IsPrefabInstance = PrefabUtility.GetPrefabType(m_go) == PrefabType.PrefabInstance;

            Valid = m_go.activeInHierarchy && m_go.activeSelf && IsPrefabInstance && m_go.layer == LayerMask.NameToLayer("UI");




            m_PrefabParent = PrefabUtility.GetPrefabParent(m_go) as GameObject;
            m_id = m_PrefabParent.GetInstanceID();
            m_PrefabRoot = PrefabUtility.FindPrefabRoot(m_PrefabParent) as GameObject;

            m_PersistData = Persist.Instance.GetPersistDataWithPrefab(m_PrefabRoot);
            m_NodeConfig = m_PersistData.GetNodeConfigWithId(m_PrefabParent.GetInstanceID()).Config;


        }

        public override GUIContent GetPreviewTitle()
        {
            if (m_Title == null)
            {
                m_Title = new GUIContent("View Export Code Config");
            }
            return m_Title;
        }

        public override bool HasPreviewGUI()
        {
            GameObject go = target as GameObject;
            if (!go || !go.activeSelf || !go.activeInHierarchy)
                return false;

            if (m_Targets.Length > 1)
                return false;

            if (!Valid)
                return false;

            return go.GetComponent(typeof(RectTransform)) != null;
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            Layout();
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

        public void OnPreviewGUI_old(Rect r, GUIStyle background)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            if (m_Styles == null)
                m_Styles = new Styles();

            GameObject go = target as GameObject;
            RectTransform rect = go.transform as RectTransform;
            if (rect == null)
                return;

            // Apply padding
            RectOffset previewPadding = new RectOffset(-5, -5, -5, -5);
            r = previewPadding.Add(r);

            // Prepare rects for columns
            r.height = EditorGUIUtility.singleLineHeight;
            Rect labelRect = r;
            Rect valueRect = r;
            Rect sourceRect = r;
            labelRect.width = kLabelWidth;
            valueRect.xMin += kLabelWidth;
            valueRect.width = kValueWidth;
            sourceRect.xMin += kLabelWidth + kValueWidth;

            // Headers
            GUI.Label(labelRect, "Property", m_Styles.headerStyle);
            GUI.Label(valueRect, "Value", m_Styles.headerStyle);
            GUI.Label(sourceRect, "Source", m_Styles.headerStyle);
            labelRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            valueRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            sourceRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            // Prepare reusable variable for out argument
            ILayoutElement source = null;

            // Show properties

            ShowProp(ref labelRect, ref valueRect, ref sourceRect, "Min Width", LayoutUtility.GetLayoutProperty(rect, e => e.minWidth, 0, out source).ToString(), source);
            ShowProp(ref labelRect, ref valueRect, ref sourceRect, "Min Height", LayoutUtility.GetLayoutProperty(rect, e => e.minHeight, 0, out source).ToString(), source);
            ShowProp(ref labelRect, ref valueRect, ref sourceRect, "Preferred Width", LayoutUtility.GetLayoutProperty(rect, e => e.preferredWidth, 0, out source).ToString(), source);
            ShowProp(ref labelRect, ref valueRect, ref sourceRect, "Preferred Height", LayoutUtility.GetLayoutProperty(rect, e => e.preferredHeight, 0, out source).ToString(), source);

            float flexible = 0;

            flexible = LayoutUtility.GetLayoutProperty(rect, e => e.flexibleWidth, 0, out source);
            ShowProp(ref labelRect, ref valueRect, ref sourceRect, "Flexible Width", flexible > 0 ? ("enabled (" + flexible.ToString() + ")") : "disabled", source);
            flexible = LayoutUtility.GetLayoutProperty(rect, e => e.flexibleHeight, 0, out source);
            ShowProp(ref labelRect, ref valueRect, ref sourceRect, "Flexible Height", flexible > 0 ? ("enabled (" + flexible.ToString() + ")") : "disabled", source);

            if (!rect.GetComponent<LayoutElement>())
            {
                Rect noteRect = new Rect(labelRect.x, labelRect.y + 10, r.width, EditorGUIUtility.singleLineHeight);
                GUI.Label(noteRect, "Add a LayoutElement to override values.", m_Styles.labelStyle);
            }
        }

        private void ShowProp(ref Rect labelRect, ref Rect valueRect, ref Rect sourceRect, string label, string value, ILayoutElement source)
        {
            GUI.Label(labelRect, label, m_Styles.labelStyle);
            GUI.Label(valueRect, value, m_Styles.labelStyle);
            GUI.Label(sourceRect, source == null ? "none" : source.GetType().Name, m_Styles.labelStyle);
            labelRect.y += EditorGUIUtility.singleLineHeight;
            valueRect.y += EditorGUIUtility.singleLineHeight;
            sourceRect.y += EditorGUIUtility.singleLineHeight;
        }
    }
}