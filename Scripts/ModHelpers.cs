using System;
using System.Reflection;
using UnityEngine;

public static class ModHelpers{
    // * Non-Publics
    public static object GetNonPublicField(object targetObject, string fieldName){
        FieldInfo fieldInfo = targetObject.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        return fieldInfo.GetValue(targetObject);
    }
    public static void SetNonPublicField(object targetObject, string fieldName, object fieldValue){
        FieldInfo fieldInfo = targetObject.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        fieldInfo.SetValue(targetObject, fieldValue);
    }
    public static void CallNonPublicFunction(object targetObject, string methodName, object[] parameters = null){
        MethodInfo methodInfo = targetObject.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
        methodInfo.Invoke(targetObject, parameters);
    }
    public static MethodInfo GetNonPublicFunction(object targetObject, string methodName){
        return targetObject.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
    }
    public static object CreateNonPublicClassInstance(object targetObject, string className, params object[] args){
        Type classType = targetObject.GetType().GetNestedType(className, BindingFlags.NonPublic);
        ConstructorInfo constructor = classType.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, 
            Array.ConvertAll(args, arg => arg.GetType()), null);
        object classInstance = constructor.Invoke(args);
        return classInstance;
    }
    // * Normalizing
    public static float NormalizeValue(float value, float min, float max){
        return (value - min) / (max - min);
    }
    public static float GetValueFromNormalize(float normalized_value, float min, float max){
        return min + normalized_value * (max - min);
    }
    public static float InvertUnitInterval(float value){
        return 1f - value;
    }
    // * Numbers
    public static bool IsEven(int value){
        if (value % 2 == 0){return true;}
        return false;
    }
    public static bool IsOdd(int value){
        if (value % 2 != 0){return true;}
        return false;
    }
    public static int GetLastNonZeroDigit(float value){
        string strValue = value.ToString("G"); // Convert float to string
        for (int i = strValue.Length - 1; i >= 0; i--){
            if (strValue[i] != '0'){
                return strValue[i] - '0';
            }
        }
        return 0;
    }
    // * Easing
    public static class Easing{
        private const float PI = Mathf.PI; 
        private const float HALFPI = Mathf.PI / 2.0f; 
        static public float CircularEaseOut(float p){
            return Mathf.Sqrt((2 - p) * p);
        }
        static public float SineEaseOut(float p){
            return Mathf.Sin(p * HALFPI);
        }
    }
}
