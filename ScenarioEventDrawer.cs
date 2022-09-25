using UnityEngine;
using System.Collections;

using UnityEditor;
using UnityEngine.Events;
using UnityEditorInternal;
using UnityEngine.UIElements;
using System.Reflection;
using System;
using Helmeton.Base;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Object = UnityEngine.Object;
using UnityEngine.Pool;

namespace Helmeton.Experimental.ScenarioSystem2
{
    [CustomPropertyDrawer(typeof(ScenarioEvent), true)]
    public class ScenarioEventDrawer : PropertyDrawer
    {
        #region [changable options]
        private string _scenarioEventListPropertyName = "ScenarioEventList";
        private string _objectTypePropertyName = "ObjectType";
        private string _targetIndexPropertyName = "TargetIndex";
        private string _methodNamePropertyName = "MethodName";
        private string _argumentsTypesPropertyName = "ArgumentsTypes";

        private string _argumentsPropertyName = "Arguments";
        private string _intValuePropertyName = "IntValue";
        private string _floatValuePropertyName = "FloatValue";
        private string _stringValuePropertyName = "StringValue";
        private string _boolValuePropertyName = "BoolValue";
        private string _vctor2ValuePropertyName = "Vector2Value";
        private string _vctor3ValuePropertyName = "Vector3Value";
        private string _vctor4ValuePropertyName = "Vector4Value";

        private string _scenarioDataPropertyName = "_data";
        private string _charactersPropertyName = "Characters";
        private string _itemsPropertyName = "Items";
        private string _triggersPropertyName = "Triggers";
        private string _ikPropertyName = "IK";
        private string _camerasPropertyName = "Cameras";

        private float _inMiddleOffset = 10f;
        private float _singleLineHeight = 20f;
        #endregion

        #region [non-changable options]
        private ReorderableList _scenarioEventsList;
        private int _lastSelectedIndex;
        private Dictionary<string, State> _states = new Dictionary<string, State>();

        private SerializedObject _serializedObject;
        private SerializedProperty _serializedProperty;

        public static SerializedObject ScenarioSerializedObject;

        private bool _initialized;

        private SerializedProperty _charactersProperty;
        private SerializedProperty _itemsProperty;
        private SerializedProperty _triggersProperty;
        private SerializedProperty _ikProperty;
        private SerializedProperty _camerasProperty;

        #endregion

        protected class State
        {
            internal ReorderableList ReorderableList;
            public int lastSelectedIndex;
        }

        private State GetState(SerializedProperty property)
        {
            State state;
            string key = property.propertyPath;
            _states.TryGetValue(key, out state);
            // ensure the cached SerializedProperty is synchronized (case 974069)
            if (state == null || state.ReorderableList.serializedProperty.serializedObject != property.serializedObject)
            {
                if (state == null)
                    state = new State();

                state.ReorderableList =
                    InitScenarioEventsList(property);

                _states[key] = state;
            }
            return state;
        }

        private State RestoreState(SerializedProperty property)
        {
            State state = GetState(property);

            _serializedProperty = state.ReorderableList.serializedProperty;
            _serializedObject = _serializedProperty.serializedObject;
            _scenarioEventsList = state.ReorderableList;
            _lastSelectedIndex = state.lastSelectedIndex;
            //_scenarioEventsList.index = _lastSelectedIndex;

            return state;
        }

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            //_serializedProperty = property;
            //_serializedObject = property.serializedObject;

            //State state = RestoreState(property);

            OnGUI(rect);
            //state.lastSelectedIndex = _lastSelectedIndex;
        }

        public void OnGUI(Rect rect)
        {
            if (_scenarioEventsList != null)
            {
                var oldIdentLevel = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 0;
                _scenarioEventsList.DoList(rect);
                EditorGUI.indentLevel = oldIdentLevel;
            }
        }

        protected ReorderableList InitScenarioEventsList(SerializedProperty property)
        {
            var listProperty = _serializedProperty.FindPropertyRelative(_scenarioEventListPropertyName);
            var list = new ReorderableList(listProperty.serializedObject, listProperty, true, true, true, true)
            {
                drawHeaderCallback = DrawHeader,

                drawElementCallback = DrawEvent,

                elementHeightCallback = CalcElementHeight,
            };

            return list;
        }

        protected void DrawHeader(Rect rect)
        {
            var labelStyle = new GUIStyle();
            labelStyle.fontSize = 14;
            labelStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            labelStyle.alignment = TextAnchor.MiddleCenter;
            GUI.Label(rect, "Scenario Event", labelStyle);
        }

        protected void DrawEvent(Rect rect, int index, bool isActive, bool isFocused)
        {
            if (_serializedProperty == null || _serializedProperty.arraySize == 0) return;

            SerializedProperty element = _serializedProperty.GetArrayElementAtIndex(index);

            EditorGUI.BeginProperty(rect, GUIContent.none, element);

            // Calculate rects

            float objectTypeRectWidth = rect.width * 1 / 3;
            float targetIndexRectWidth = rect.width * 2 / 3;
            float methodRectWidth = rect.width;
            float argumentRectWidth = rect.width;

            var objectTypeRect = new Rect(rect.x, rect.y + _inMiddleOffset, objectTypeRectWidth, _singleLineHeight);
            var targetRect = new Rect(rect.x + objectTypeRectWidth, rect.y + _inMiddleOffset, targetIndexRectWidth, _singleLineHeight);
            var methodRect = new Rect(rect.x, rect.y + _singleLineHeight + _inMiddleOffset, methodRectWidth, _singleLineHeight);
            var argumentsRect = new Rect(rect.x, rect.y + _singleLineHeight * 2 + _inMiddleOffset, argumentRectWidth, _scenarioEventsList.elementHeight);

            // Draw fields - pass GUIContent.none to each so they are drawn without labels
            var objectTypeProperty = element.FindPropertyRelative(_objectTypePropertyName);
            EditorGUI.PropertyField(objectTypeRect, objectTypeProperty, GUIContent.none);

            if (ScenarioSerializedObject != null)
            {
                var scenarioDataProperty = ScenarioSerializedObject.FindProperty(_scenarioDataPropertyName);
                _charactersProperty = scenarioDataProperty.FindPropertyRelative(_charactersPropertyName);
                _itemsProperty = scenarioDataProperty.FindPropertyRelative(_itemsPropertyName);
                _triggersProperty = scenarioDataProperty.FindPropertyRelative(_triggersPropertyName);
                _ikProperty = scenarioDataProperty.FindPropertyRelative(_ikPropertyName);
                _camerasProperty = scenarioDataProperty.FindPropertyRelative(_camerasPropertyName);
            }                    

            switch (objectTypeProperty.intValue)
            {
                case (int)ScenarioObjectType.Character:
                    DrawObjectTypeDependentFields(targetRect, methodRect, argumentsRect, element, _charactersProperty, ScenarioEventProperties.CharacterType);
                    break;
                case (int)ScenarioObjectType.Item:
                    DrawObjectTypeDependentFields(targetRect, methodRect, argumentsRect, element, _itemsProperty, ScenarioEventProperties.ItemType);
                    break;
                case (int)ScenarioObjectType.Trigger:
                    DrawObjectTypeDependentFields(targetRect, methodRect, argumentsRect, element, _triggersProperty, ScenarioEventProperties.TriggerType);
                    break;
                case (int)ScenarioObjectType.IK:
                    DrawObjectTypeDependentFields(targetRect, methodRect, argumentsRect, element, _ikProperty, ScenarioEventProperties.IKType);
                    break;
                case (int)ScenarioObjectType.Camera:
                    DrawObjectTypeDependentFields(targetRect, methodRect, argumentsRect, element, _camerasProperty, ScenarioEventProperties.CameraType);
                    break;
            }

            EditorGUI.EndProperty();
        }

        protected float CalcElementHeight(int index)
        {
            var argumentsCount = _serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative(_argumentsPropertyName).arraySize;
            return _singleLineHeight * 2 + argumentsCount * _singleLineHeight + _inMiddleOffset * 2;
        }

        private void DrawObjectTypeDependentFields(Rect targetRect, Rect methodRect, Rect argumentsRect, SerializedProperty elementProperty, SerializedProperty targetDataProperty, Type targetType)
        {
            bool drawMethodData = DrawTargetField(targetRect, elementProperty, targetDataProperty);
            if (drawMethodData)
            {
                DrawMethodPopup(methodRect, elementProperty, targetType);
                DrawArgumentsList(argumentsRect, elementProperty, targetType);
            }
            else
            {
                elementProperty.FindPropertyRelative(_argumentsPropertyName).arraySize = 0;
                GUI.Box(methodRect, EditorGUIUtility.IconContent("console.warnicon"));
            }
        }

        private bool DrawTargetField(Rect rect, SerializedProperty elementProperty, SerializedProperty dataProperty, GUIContent label = null)
        {
            if(ScenarioSerializedObject == null)
            {
                EditorGUIUtility.labelWidth = rect.width / 2;
                Rect indexFieldRect = new Rect(rect.x, rect.y, rect.width - rect.height, rect.height);
                var targetIndexProperty = elementProperty.FindPropertyRelative(_targetIndexPropertyName);
                EditorGUI.PropertyField(indexFieldRect, targetIndexProperty);
                _serializedObject.ApplyModifiedProperties();
                return true;
            }

            if (dataProperty == null || dataProperty.arraySize == 0)
            {
                GUI.Label(rect, "List is empty");
                return false;
            }
            else
            {
                var targets = new List<string>();
                for (int i = 0; i < dataProperty.arraySize; i++)
                {
                    var value = dataProperty.GetArrayElementAtIndex(i).objectReferenceValue;
                    if (value != null)
                        targets.Add(value.ToString());
                }

                if (targets == null || targets.Count == 0)
                {
                    GUI.Label(rect, "List only contains missing references");
                    return false;
                }
                else
                {
                    GUIContent[] targetsContent = new GUIContent[targets.Count];
                    for (int i = 0; i < targetsContent.Length; i++)
                    {
                        targetsContent[i] = new GUIContent(targets[i]);
                    }

                    Rect objectFieldRect = new Rect(rect.x, rect.y, rect.width - rect.height, rect.height);
                    Rect selectorButtonRect = new Rect(rect.x + objectFieldRect.width, rect.y, rect.height, rect.height);

                    EditorGUI.BeginDisabledGroup(true);
                    var targetIndexProperty = elementProperty.FindPropertyRelative(_targetIndexPropertyName);

                    if (targetIndexProperty.intValue >= dataProperty.arraySize)
                        targetIndexProperty.intValue = 0;
                    GUIContent newLabel = GUIContent.none;
                    if (label != null)
                        newLabel = label;
                    EditorGUI.ObjectField(objectFieldRect, dataProperty.GetArrayElementAtIndex(targetIndexProperty.intValue), newLabel);
                    EditorGUI.EndDisabledGroup();

                    if (GUI.Button(selectorButtonRect, EditorGUIUtility.IconContent("sv_icon_dot0_sml")))
                    {
                        bool useEditorPreview = true;
                        string elementIcon = "";

                        switch (elementProperty.FindPropertyRelative(_objectTypePropertyName).intValue)
                        {
                            case (int)ScenarioObjectType.Character:
                            case (int)ScenarioObjectType.Item:
                                break;
                            case (int)ScenarioObjectType.Trigger:
                                useEditorPreview = false;
                                elementIcon = "OcclusionArea Icon";
                                break;
                            case (int)ScenarioObjectType.IK:
                                useEditorPreview = false;
                                elementIcon = "EdgeCollider2D Icon";
                                break;
                            case (int)ScenarioObjectType.Camera:
                                useEditorPreview = false;
                                elementIcon = "Camera Gizmo";
                                break;
                        }

                        CustomObjectPicker.ShowWindow
                            (dataProperty,
                            (int index) =>
                            {
                                targetIndexProperty.intValue = index;
                                _serializedObject.ApplyModifiedProperties();
                            },
                            useEditorPreview,
                            elementIcon);
                    }

                    return true;
                }
            }
        }

        private void DrawMethodPopup(Rect rect, SerializedProperty elementProperty, Type type)
        {
            var methods = NewMethodsList(type);

            var methodsNames = new string[methods.Length];
            for (int i = 0; i < methods.Length; i++)
                methodsNames[i] = methods[i].Name;

            var methodsDisplayNames = new string[methods.Length];
            for (int i = 0; i < methods.Length; i++)
                methodsDisplayNames[i] = methods[i].DisplayName;

            var methodNameProperty = elementProperty.FindPropertyRelative(_methodNamePropertyName);
            var argumentsTypesProperty = elementProperty.FindPropertyRelative(_argumentsTypesPropertyName);
            var argumentsProperty = elementProperty.FindPropertyRelative(_argumentsPropertyName);

            int index = Mathf.Max(0, Array.IndexOf(methodsNames, methodNameProperty.stringValue));
            index = EditorGUI.Popup(rect, index, methodsDisplayNames);

            methodNameProperty.stringValue = methods[index].Name;
            argumentsTypesProperty.arraySize = methods[index].ArgumentsTypes.Length;
            argumentsProperty.arraySize = methods[index].ArgumentsTypes.Length;

            for (int i = 0; i < argumentsTypesProperty.arraySize; i++)
                argumentsTypesProperty.GetArrayElementAtIndex(i).stringValue = methods[index].ArgumentsTypes[i];

            _serializedObject.ApplyModifiedProperties();
        }

        private void DrawArgumentsList(Rect rect, SerializedProperty elementProperty, Type type)
        {
            _serializedObject.Update();

            var argumentsTypesProperty = elementProperty.FindPropertyRelative(_argumentsTypesPropertyName);
            var methodProperty = elementProperty.FindPropertyRelative(_methodNamePropertyName);

            Type[] types = new Type[argumentsTypesProperty.arraySize];
            for (int i = 0; i < argumentsTypesProperty.arraySize; i++)
                types[i] = Type.GetType(argumentsTypesProperty.GetArrayElementAtIndex(i).stringValue);

            var method = type.GetMethod(methodProperty.stringValue, types);
            var parameters = method.GetParameters();

            for (int i = 0; i < types.Length; i++)
            {
                var argumentProperty = elementProperty.FindPropertyRelative(_argumentsPropertyName).GetArrayElementAtIndex(i);

                Rect argumentRect = new Rect(rect.x, rect.y + i * _singleLineHeight, rect.width, _singleLineHeight);

                var typeName = GetTypeName(types[i]);

                if (ScenarioEventProperties.ValidParameterReferenceTypes.Contains(types[i]) && types[i] != typeof(string))
                {
                    var objectTypeProperty = argumentProperty.FindPropertyRelative(_objectTypePropertyName);

                    switch (typeName)
                    {
                        case ScenarioEventProperties.CharacterTypeName:
                            objectTypeProperty.intValue = (int)ScenarioObjectType.Character;
                            var dataProperty = _charactersProperty;
                            DrawTargetField(argumentRect, argumentProperty, dataProperty, new GUIContent(parameters[i].Name));
                            break;
                        case ScenarioEventProperties.ItemTypeName:
                            objectTypeProperty.intValue = (int)ScenarioObjectType.Item;
                            dataProperty = _itemsProperty;
                            DrawTargetField(argumentRect, argumentProperty, dataProperty, new GUIContent(parameters[i].Name));
                            break;
                        case ScenarioEventProperties.TriggerTypeName:
                            objectTypeProperty.intValue = (int)ScenarioObjectType.Trigger;
                            dataProperty = _triggersProperty;
                            DrawTargetField(argumentRect, argumentProperty, dataProperty, new GUIContent(parameters[i].Name));
                            break;
                        case ScenarioEventProperties.IKTypeName:
                            objectTypeProperty.intValue = (int)ScenarioObjectType.IK;
                            dataProperty = _ikProperty;
                            DrawTargetField(argumentRect, argumentProperty, dataProperty, new GUIContent(parameters[i].Name));
                            break;
                        case ScenarioEventProperties._cameraTypeName:
                            objectTypeProperty.intValue = (int)ScenarioObjectType.Camera;
                            dataProperty = _camerasProperty;
                            DrawTargetField(argumentRect, argumentProperty, dataProperty, new GUIContent(parameters[i].Name));
                            break;
                    }
                }
                else
                {
                    switch (typeName)
                    {
                        case "int":
                            var valueProperty = argumentProperty.FindPropertyRelative(_intValuePropertyName);
                            EditorGUI.PropertyField(argumentRect, valueProperty, new GUIContent(parameters[i].Name));
                            break;
                        case "float":
                            valueProperty = argumentProperty.FindPropertyRelative(_floatValuePropertyName);
                            EditorGUI.PropertyField(argumentRect, valueProperty, new GUIContent(parameters[i].Name));
                            break;
                        case "string":
                            valueProperty = argumentProperty.FindPropertyRelative(_stringValuePropertyName);
                            EditorGUI.PropertyField(argumentRect, valueProperty, new GUIContent(parameters[i].Name));
                            break;
                        case "bool":
                            valueProperty = argumentProperty.FindPropertyRelative(_boolValuePropertyName);
                            EditorGUI.PropertyField(argumentRect, valueProperty, new GUIContent(parameters[i].Name));
                            break;
                        case "Vector2":
                            EditorGUIUtility.wideMode = true;
                            valueProperty = argumentProperty.FindPropertyRelative(_vctor2ValuePropertyName);
                            EditorGUI.PropertyField(argumentRect, valueProperty, new GUIContent(parameters[i].Name));
                            break;
                        case "Vector3":
                            EditorGUIUtility.wideMode = true;
                            valueProperty = argumentProperty.FindPropertyRelative(_vctor3ValuePropertyName);
                            EditorGUI.PropertyField(argumentRect, valueProperty, new GUIContent(parameters[i].Name));
                            break;
                        case "Vector4":
                            EditorGUIUtility.wideMode = true;
                            valueProperty = argumentProperty.FindPropertyRelative(_vctor4ValuePropertyName);
                            EditorGUI.PropertyField(argumentRect, valueProperty, new GUIContent(parameters[i].Name));
                            break;
                    }
                }

                _serializedObject.ApplyModifiedProperties();
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            _serializedProperty = property;
            _serializedObject = property.serializedObject;

            RestoreState(property);

            if (_scenarioEventsList != null && _serializedProperty != null) 
                return _scenarioEventsList.GetHeight();
            else
                return base.GetPropertyHeight(property, label);
        }


        #region ------------[UnityEventDrawer Changed And Added Source Methods]-----------

        private struct ScenarioMethodInfo
        {
            public string Name;
            public string DisplayName;
            public string[] ArgumentsTypes;
        } 

        //first array - raw names of methods, second array - display names, third - argumentTypes
        private ScenarioMethodInfo[] NewMethodsList(Type targetType)
        {
            var methods = CalculateMethodMap(targetType);
            List<ScenarioMethodInfo> methodsInfos = new List<ScenarioMethodInfo>();
            ReorderMethods(methods);
            foreach (var validMethod in methods) {
                var newMethodInfo = new ScenarioMethodInfo();
                newMethodInfo.Name = validMethod.Name;
                newMethodInfo.DisplayName = GetMethodPath(validMethod);
                var newMethodParameters = validMethod.GetParameters();
                newMethodInfo.ArgumentsTypes = new string[newMethodParameters.Length];
                for (int i = 0; i < newMethodParameters.Length; i++)
                    newMethodInfo.ArgumentsTypes[i] = newMethodParameters[i].ParameterType.AssemblyQualifiedName;
                methodsInfos.Add(newMethodInfo);
            }

            return methodsInfos.ToArray();
        }

        private IEnumerable<MethodInfo> CalculateMethodMap(Type targetType)
        {
            var validMethods = new List<MethodInfo>();
            if (targetType == null)
                return validMethods;

            // find the methods on the behaviour that match the signature
            Type componentType = targetType;
            var componentMethods = componentType.GetMethods().Where(x => !x.IsSpecialName).ToList();

            var wantedProperties = componentType.GetProperties().AsEnumerable();
            wantedProperties = wantedProperties.Where(x => x.GetCustomAttributes(typeof(ObsoleteAttribute), true).Length == 0 && x.GetSetMethod() != null);
            componentMethods.AddRange(wantedProperties.Select(x => x.GetSetMethod()));

            foreach (var componentMethod in componentMethods)
            {
                if (IsValidMethod(componentMethod))
                {
                    var validMethod = componentMethod;
                    validMethods.Add(validMethod);
                }
                else
                    continue;
            }
            return validMethods;
        }

        private bool IsValidMethod(MethodInfo method)
        {
            //check for obsolete attribute
            if (method.GetCustomAttributes(typeof(ObsoleteAttribute), true).Length > 0)
                return false;

            //allow only void return type
            if (method.ReturnType != typeof(void))
                return false;

            //check parameters types
            var methodParameters = method.GetParameters();
            foreach (var parameter in methodParameters)
            {
                Type parameterType = parameter.ParameterType;
                if (!parameterType.IsValueType)
                {
                    if (!ScenarioEventProperties.ValidParameterReferenceTypes.Contains(parameterType))
                        return false;
                }
            }

            return true;
        }

        private void ReorderMethods(IEnumerable<MethodInfo> methods)
        {
            // Note: sorting by a bool in OrderBy doesn't seem to work for some reason, so using numbers explicitly.
            methods.OrderBy(e => e.Name.StartsWith("set_") ? 0 : 1).ThenBy(e => e.Name);
        }

        private string GetMethodPath(MethodInfo method)
        {
            var args = new StringBuilder();
            var count = method.GetParameters().Length;
            for (int index = 0; index < count; index++)
            {
                var methodArg = method.GetParameters()[index];
                args.Append(string.Format("{0}", GetTypeName(methodArg.ParameterType)));

                if (index < count - 1)
                    args.Append(", ");
            }

            string path = GetFormattedMethodName(method.Name, args.ToString());

            return path;
        }

        private string GetTypeName(Type t)
        {
            if (t == typeof(int))
                return "int";
            if (t == typeof(float))
                return "float";
            if (t == typeof(string))
                return "string";
            if (t == typeof(bool))
                return "bool";
            return t.Name;
        }

        private string GetFormattedMethodName(string methodName, string args)
        {
                if (methodName.StartsWith("set_"))
                    return string.Format("{0} {1}", methodName.Substring(4), args);
                else
                    return string.Format("{0} ({1})", methodName, args);
        }
        #endregion
    }
}