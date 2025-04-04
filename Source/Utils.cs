using System.Collections.Generic;
using System;
using HarmonyLib;
using UnityEngine;
using System.Reflection;

namespace Ardot.REPO.REPOverhaul;

public static class Utils
{
    private record struct FieldKey(Type Type, string Field);

    private static Dictionary<FieldKey, FieldInfo> Fields = new ();
    private static Dictionary<MonoBehaviour, List<object>> Metadata = new ();

    public static object Get<O>(this O obj, string field)
    {
        return GetField<O>(field).GetValue(obj);
    }

    public static T Get<T, O>(this O obj, string field)
    {
        return (T)Get(obj, field);
    }

    public static void Set<T>(this T obj, string field, object value)
    {
        GetField<T>(field).SetValue(obj, value);
    }

    public static FieldInfo GetField<T>(string field)
    {
        FieldKey fieldKey = new (typeof(T), field);

        if(!Fields.TryGetValue(fieldKey, out FieldInfo fieldInfo))
        {
            fieldInfo = AccessTools.Field(typeof(T), field);
            Fields.Add(fieldKey, fieldInfo);
        }

        return fieldInfo;
    }

    public static void SetMetadata(this MonoBehaviour component, int index, object obj)
    {
        if(!Metadata.TryGetValue(component, out List<object> metadata))
        {
            metadata = new List<object> ();
            Metadata.Add(component, metadata);
        }

        while(metadata.Count <= index)
            metadata.Add(default);

        metadata[index] = obj;
    }

    public static T GetMetadata<T>(this MonoBehaviour component, int index, T defaultValue = default)
    {
        if(!Metadata.TryGetValue(component, out List<object> metadata) || metadata.Count <= index || metadata[index] == default)
            return defaultValue;

        return (T)metadata[index];
    }

    public static void CleanMetadata()
    {
        foreach(MonoBehaviour component in Metadata.Keys)
            if(component == null)
                Metadata.Remove(component);
    }

    public static Dictionary<string, GameObject> GetEnemies()
    {
        List<EnemySetup> enemyList = new ();
        enemyList.AddRange(EnemyDirector.instance.enemiesDifficulty1);
        enemyList.AddRange(EnemyDirector.instance.enemiesDifficulty2);
        enemyList.AddRange(EnemyDirector.instance.enemiesDifficulty3);

        Dictionary<string, GameObject> enemyObjects = new ();

        for(int x = 0; x < enemyList.Count; x++)
        {
            List<GameObject> spawnObjects = enemyList[x].spawnObjects;
            for (int y = 0; y < spawnObjects.Count; y++)
            {
                GameObject enemyObject = spawnObjects[y];

                if(enemyObject.GetComponent<EnemyParent>() is not EnemyParent enemyParent)
                    continue;

                string enemyName = enemyParent.enemyName.ToLower().Replace(' ', '-');

                if(enemyObjects.ContainsKey(enemyName))
                    continue;

                enemyObjects.Add(enemyName, enemyObject);
            }
        }

        return enemyObjects;
    }

    public static void ForObjectsInTree(GameObject root, Action<GameObject, int> action)
    {
        Stack<ValueTuple<Transform, int>> branches = new ();
        branches.Push((root.GetComponent<Transform>(), 0));
        
        while(branches.Count > 0)
        {
            ValueTuple<Transform, int> branch = branches.Pop();
            Transform transform = branch.Item1;
            int depth = branch.Item2;

            for(int x = 0; x < transform.childCount; x++)
                branches.Push((transform.GetChild(x), depth + 1));

            action(transform.gameObject, depth);
        }
    }

    public static List<HurtCollider> GetHurtColliders(GameObject root)
    {
        List<HurtCollider> hurtColliders = new ();
        
        ForObjectsInTree(root, (GameObject branch, int depth) => {
            if(branch.GetComponent<HurtCollider>() is HurtCollider hurtCollider)
                hurtColliders.Add(hurtCollider);
        });

        return hurtColliders;
    }

    public static Value Value(float min, float max)
    {
        Value value = ScriptableObject.CreateInstance<Value>();
        value.valueMin = min;
        value.valueMax = max;
        return value;
    }
}
