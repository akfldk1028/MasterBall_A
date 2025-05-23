// using System;
// using System.Collections;
// using System.Collections.Generic;
// using Unity.Assets.Scripts.Data;
// using Unity.Assets.Scripts.Objects;
// using UnityEngine;


// public abstract class SkillBase : InitBase
// {
// 	public Creature Owner { get; protected set; }
// 	public ClientCreature ClientCreature { get; protected set; }
// 	public float RemainCoolTime { get; set; }

// 	public SkillData SkillData { get; private set; }

// 	public override bool Init()
// 	{
// 		if (base.Init() == false)
// 			return false;

// 		return true;
// 	}

// 	public virtual void SetInfo(Creature owner, int skillTemplateID, ClientCreature clientCreature)
// 	{
// 		Owner = owner;
// 		SkillData = DataLoader.instance.SkillDic[skillTemplateID];
// 		ClientCreature = clientCreature;						
//         // Owner.
// 		// // Register AnimEvent
// 		if (ClientCreature.SkeletonAnim != null && ClientCreature.SkeletonAnim.AnimationState != null)
// 		{
// 			ClientCreature.SkeletonAnim.AnimationState.Event -= OnOwnerAnimEventHandler;
// 			ClientCreature.SkeletonAnim.AnimationState.Event += OnOwnerAnimEventHandler;
// 		}
// 	}

// 	private void OnDisable()
// 	{
// 		// if (Managers.Game == null)
// 		// 	return;
//         //CLIENT로가야하는데 아아
// 		if (Owner.IsValid() == false)
// 			return;
// 		if (ClientCreature.SkeletonAnim == null)
// 			return;
// 		if (ClientCreature.SkeletonAnim.AnimationState == null)
// 			return;

// 		ClientCreature.SkeletonAnim.AnimationState.Event -= OnOwnerAnimEventHandler;
// 	}

// 	public virtual void DoSkill()
// 	{
// 		// 준비된 스킬에서 해제
// 		if (Owner.Skills != null)
// 			Owner.Skills.ActiveSkills.Remove(this);

// 		float timeScale = 1.0f;

// 		if (Owner.Skills.DefaultSkill == this)
// 			ClientCreature.PlayAnimation(0, SkillData.AnimName, false).TimeScale = timeScale;
// 		else
// 			ClientCreature.PlayAnimation(0, SkillData.AnimName, false).TimeScale = 1;

// 		StartCoroutine(CoCountdownCooldown());
// 	}

// 	private IEnumerator CoCountdownCooldown()
// 	{
// 		RemainCoolTime = SkillData.CoolTime;
// 		yield return new WaitForSeconds(SkillData.CoolTime);
// 		RemainCoolTime = 0;

// 		// 준비된 스킬에 추가
// 		if (Owner.Skills != null)
// 			Owner.Skills.ActiveSkills.Add(this);
// 	}

// 	public virtual void CancelSkill()
// 	{

// 	}



// 	private void OnOwnerAnimEventHandler(TrackEntry trackEntry, Event e)
// 	{
// 		// 다른스킬의 애니메이션 이벤트도 받기 때문에 자기꺼만 써야함
// 		if (trackEntry.Animation.Name == SkillData.AnimName)
// 			OnAttackEvent();
// 	}

// 	protected abstract void OnAttackEvent();

// 	protected virtual void GenerateProjectile(Creature owner, Vector3 spawnPos)
// 	{
// 	// 	Projectile projectile = Managers.Object.Spawn<Projectile>(spawnPos, SkillData.ProjectileId);

// 	// 	LayerMask excludeMask = 0;
// 	// 	excludeMask.AddLayer(Define.ELayer.Default);
// 	// 	excludeMask.AddLayer(Define.ELayer.Projectile);
// 	// 	excludeMask.AddLayer(Define.ELayer.Env);
// 	// 	excludeMask.AddLayer(Define.ELayer.Obstacle);

// 	// 	switch (owner.ObjectType)
// 	// 	{
// 	// 		case Define.EObjectType.Hero:
// 	// 			excludeMask.AddLayer(Define.ELayer.Hero);
// 	// 			break;
// 	// 		case Define.EObjectType.Monster:
// 	// 			excludeMask.AddLayer(Define.ELayer.Monster);
// 	// 			break;
// 	// 	}

// 	// 	projectile.SetSpawnInfo(Owner, this, excludeMask);
// 	}
	
// 	public virtual void GenerateAoE(Vector3 spawnPos)
// 	{
// 		// AoEBase aoe = null;
// 		// int id = SkillData.AoEId;
// 		// string className = Managers.Data.AoEDic[id].ClassName;

// 		// Type componentType = Type.GetType(className);

// 		// if (componentType == null)
// 		// {
// 		// 	Debug.LogError("AoE Type not found: " + className);
// 		// 	return;
// 		// }

// 		// GameObject go = Managers.Object.SpawnGameObject(spawnPos, "AoE");
// 		// go.name = Managers.Data.AoEDic[id].ClassName;
// 		// aoe = go.AddComponent(componentType) as AoEBase;
// 		// aoe.SetInfo(SkillData.AoEId, Owner, this);
// 	}
// }
