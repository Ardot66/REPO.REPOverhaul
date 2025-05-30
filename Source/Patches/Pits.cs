using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

namespace Ardot.REPO.EnemyOverhaul;

public static class PitOverhaul
{
    public const int MapHurtColliderMeta = 0;

    public static ConfigEntry<bool> 
        OverhaulPlayers,
        OverhaulEnemies,
        OverhaulItems;

    public static void Init()
    {
        OverhaulPlayers = Plugin.BindConfig(
            "Pits",
            "OverhaulPlayers",
            true,
            "If true, pit overhauled mechanics are applied to players",
            () => Plugin.SetPatch(
                OverhaulPlayers.Value,
                AccessTools.Method(typeof(HurtCollider), "PlayerHurt"),
                prefix: new HarmonyMethod(typeof(PitOverhaul), "PlayerHurtPrefix")
            )
        );
        OverhaulEnemies = Plugin.BindConfig(
            "Pits",
            "OverhaulEnemies",
            true,
            "If true, pit overhauled mechanics are applied to enemies",
            () => Plugin.SetPatch(
                OverhaulEnemies.Value,
                AccessTools.Method(typeof(HurtCollider), "EnemyHurt"),
                prefix: new HarmonyMethod(typeof(PitOverhaul), "EnemyHurtPrefix")
            )
        );
        OverhaulItems = Plugin.BindConfig(
            "Pits",
            "OverhaulItems",
            true,
            "If true, pit overhauled mechanics are applied to items",
            () => Plugin.SetPatch(
                OverhaulItems.Value,
                AccessTools.Method(typeof(HurtCollider), "PhysObjectHurt"),
                prefix: new HarmonyMethod(typeof(PitOverhaul), "PhysObjectHurtPrefix")
            )
        );

        Plugin.Harmony.Patch(
            AccessTools.Method(typeof(LevelGenerator), "GenerateDone"),    
            postfix: new HarmonyMethod(typeof(PitOverhaul), "GenerateDonePostfix")
        );
    }

    public static void GenerateDonePostfix(LevelGenerator __instance)
    {
        if(RunManager.instance.levelCurrent == RunManager.instance.levelArena)
            return;

        List<HurtCollider> mapHurtColliders = Utils.GetHurtColliders(__instance.LevelParent.transform);

        for(int x = 0; x < mapHurtColliders.Count; x++)
        {
            HurtCollider hurtCollider = mapHurtColliders[x];

            if(!hurtCollider.name.Contains("Kill Box"))
                continue;

            hurtCollider.SetMetadata(MapHurtColliderMeta, true);
        }    
    }

    public static bool PlayerHurtPrefix(HurtCollider __instance, PlayerAvatar _player)
    {
        if(!__instance.GetMetadata(MapHurtColliderMeta, false))
            return true;

        __instance.onImpactAny.Invoke();
		__instance.onImpactPlayer.Invoke();

        LevelPoint destination = Utils.ChooseLevelPoint(_player.transform.position, 40, 1f);

        Vector3 finalPosition = destination.transform.position + new Vector3(0, 1, 0);
        _player.Spawn(finalPosition, _player.transform.rotation);
        Rigidbody tumbleRB = (Rigidbody)_player.tumble.Get("rb");
        tumbleRB.position = finalPosition;
        tumbleRB.transform.position = finalPosition;

        PlayerController.instance.CollisionController.Set("fallDistance", 0); 
        _player.tumble.TumbleRequest(true, false);
        _player.tumble.TumbleOverrideTime(3f);
        _player.tumble.ImpactHurtSet(float.PositiveInfinity, Random.Range(35, 45));

        return false;
    }

    public static bool EnemyHurtPrefix(HurtCollider __instance, Enemy _enemy)
    {
        if(!__instance.GetMetadata(MapHurtColliderMeta, false))
            return true;

        __instance.onImpactAny.Invoke();
        __instance.onImpactEnemy.Invoke();

        LevelPoint destination = Utils.ChooseLevelPoint(_enemy.transform.position, 30, 1f);
        Vector3 finalPosition = destination.transform.position + new Vector3(0, 1, 0);

        _enemy.EnemyTeleported(finalPosition);
        EnemyHealth health = (EnemyHealth)_enemy.Get("Health");
        if(health != null)
            health.Hurt(Random.Range(100, 145), __instance.transform.forward);
        else if((bool)_enemy.Get("HasStateDespawn"))
        {
            _enemy.Get<EnemyParent, Enemy>("EnemyParent").SpawnedTimerSet(0f);
            _enemy.CurrentState = EnemyState.Despawn;
        }

        EnemyStateStunned stunnedState = (EnemyStateStunned)_enemy.Get("StateStunned");
        if (stunnedState != null)
            stunnedState.Set(5f);

        return false;
    }

    public static bool PhysObjectHurtPrefix(ref bool __result, HurtCollider __instance, PhysGrabObject physGrabObject)
    {
        if(!__instance.GetMetadata(MapHurtColliderMeta, false))
            return true;

        LevelPoint destination = Utils.ChooseLevelPoint(physGrabObject.transform.position, 20, 1f);
        Vector3 finalPosition = destination.transform.position + new Vector3(0, 1, 0);

        physGrabObject.Teleport(finalPosition, physGrabObject.transform.rotation);

        __result = true;
        return false;
    }
}