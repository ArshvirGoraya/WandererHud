using System;
using System.Reflection;
using UnityEngine;
public static class ModHelpers{
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
    public static object CreateNonPublicClassInstance(object targetObject, string className, params object[] args){
        Type classType = targetObject.GetType().GetNestedType(className, BindingFlags.NonPublic);
        ConstructorInfo constructor = classType.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, 
            Array.ConvertAll(args, arg => arg.GetType()), null);
        object classInstance = constructor.Invoke(args);
        return classInstance;
    }
    // 
    public static float NormalizeValue(float value, float min, float max){
        return (value - min) / (max - min);
    }
    public static float GetValueFromNormalize(float normalized_value, float min, float max){
        return min + normalized_value * (max - min);
    }
    // 
}
