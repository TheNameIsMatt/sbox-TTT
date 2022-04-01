﻿using Sandbox;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace TTT;

public enum FireMode
{
	Automatic = 0,
	Semi = 1,
	Burst = 2
}

[Library( "weapon" ), AutoGenerate]
public partial class WeaponInfo : CarriableInfo
{
	[Property, Category( "Sounds" )]
	public string FireSound { get; set; } = "";

	[Property, Category( "Sounds" )]
	public string DryFireSound { get; set; } = "";

	[Property( "ammotype", "The ammo type. Set this to None if you want the ammo for your weapon to be limited (you can't pick up or drop ammo for it)." ), Category( "Important" )]
	public AmmoType AmmoType { get; set; } = AmmoType.None;

	[Property, Category( "Important" )]
	public FireMode FireMode { get; set; } = FireMode.Automatic;

	[Property( "bulletsperfire", "The amount of bullets that come out in one shot." ), Category( "Stats" )]
	public int BulletsPerFire { get; set; } = 1;

	[Property, Category( "Stats" )]
	public int ClipSize { get; set; } = 30;

	[Property, Category( "Stats" )]
	public float Damage { get; set; } = 20;

	[Property, Category( "Stats" )]
	public float DamageFallOffStart { get; set; } = 0f;

	[Property, Category( "Stats" )]
	public float DamageFallOffEnd { get; set; } = 0f;

	[Property, Category( "Stats" )]
	public float HeadshotMultiplier { get; set; } = 1f;

	[Property, Category( "Stats" )]
	public float Spread { get; set; } = 0f;

	[Property, Category( "Stats" )]
	public float PrimaryRate { get; set; } = 0f;

	[Property, Category( "Stats" )]
	public float SecondaryRate { get; set; } = 0f;

	[Property( "reserveammo", "The amount of ammo this weapon spawns with if the ammo type is set to None." ), Category( "Stats" )]
	public int ReserveAmmo { get; set; } = 0;

	[Property, Category( "Stats" )]
	public float ReloadTime { get; set; } = 2f;

	[Property, Category( "Stats" )]
	public float VerticalRecoil { get; set; } = 0f;

	[Property, Category( "Stats" )]
	public float HorizontalRecoilRange { get; set; } = 0f;

	[Property, Category( "Stats" )]
	public float RecoilRecoveryScale { get; set; } = 0f;

	[Property, Category( "VFX" ), ResourceType( "vpcf" )]
	public string EjectParticle { get; set; } = "";

	[Property, Category( "VFX" ), ResourceType( "vpcf" )]
	public string MuzzleFlashParticle { get; set; } = "";

	[Property, Category( "VFX" ), ResourceType( "vpcf" )]
	public string TracerParticle { get; set; } = "";
}

public abstract partial class Weapon : Carriable
{
	[Net, Predicted]
	public int AmmoClip { get; protected set; }

	[Net, Predicted]
	public int ReserveAmmo { get; protected set; }

	[Net, Local, Predicted]
	public bool IsReloading { get; protected set; }

	[Net, Local, Predicted]
	public TimeSince TimeSincePrimaryAttack { get; protected set; }

	[Net, Local, Predicted]
	public TimeSince TimeSinceSecondaryAttack { get; protected set; }

	[Net, Local, Predicted]
	public TimeSince TimeSinceReload { get; protected set; }

	public override string SlotText => $"{AmmoClip} + {ReserveAmmo + Owner?.AmmoCount( Info.AmmoType )}";
	public Vector3 RecoilOnShot => new( Rand.Float( -Info.HorizontalRecoilRange, Info.HorizontalRecoilRange ), Info.VerticalRecoil, 0 );
	public Vector3 CurrentRecoilAmount { get; private set; } = Vector3.Zero;

	public new WeaponInfo Info => base.Info as WeaponInfo;

	public bool UnlimitedAmmo { get; set; }

	public override void Spawn()
	{
		base.Spawn();

		AmmoClip = Info.ClipSize;

		if ( Info.AmmoType == AmmoType.None )
			ReserveAmmo = Info.ReserveAmmo;
	}

	public override void ActiveStart( Entity entity )
	{
		base.ActiveStart( entity );

		IsReloading = false;
		TimeSinceReload = 0;
	}

	public override void Simulate( Client client )
	{
		if ( TimeSinceDeployed < Info.DeployTime )
			return;

		if ( CanReload() )
		{
			Reload();
			return;
		}

		if ( !IsReloading )
		{
			if ( CanPrimaryAttack() )
			{
				using ( LagCompensation() )
				{
					TimeSincePrimaryAttack = 0;
					AttackPrimary();
				}
			}

			if ( CanSecondaryAttack() )
			{
				using ( LagCompensation() )
				{
					TimeSinceSecondaryAttack = 0;
					AttackSecondary();
				}
			}

			if ( Input.Down( InputButton.Run ) && Input.Pressed( InputButton.Drop ) )
				DropAmmo();
		}
		else if ( TimeSinceReload > Info.ReloadTime )
			OnReloadFinish();
	}

	public override void BuildInput( InputBuilder input )
	{
		base.BuildInput( input );

		float oldPitch = input.ViewAngles.pitch;
		float oldYaw = input.ViewAngles.yaw;
		input.ViewAngles.pitch -= CurrentRecoilAmount.y * Time.Delta;
		input.ViewAngles.yaw -= CurrentRecoilAmount.x * Time.Delta;
		CurrentRecoilAmount -= CurrentRecoilAmount.WithY( (oldPitch - input.ViewAngles.pitch) * Info.RecoilRecoveryScale * 1f ).WithX( (oldYaw - input.ViewAngles.yaw) * Info.RecoilRecoveryScale * 1f );
	}

	protected virtual bool CanPrimaryAttack()
	{
		if ( Info.FireMode == FireMode.Semi && !Input.Pressed( InputButton.Attack1 ) )
			return false;
		else if ( Info.FireMode != FireMode.Semi && !Input.Down( InputButton.Attack1 ) )
			return false;

		float rate = Info.PrimaryRate;
		if ( rate <= 0 )
			return true;

		return TimeSincePrimaryAttack > (1 / rate);
	}

	protected virtual bool CanSecondaryAttack()
	{
		if ( !Input.Pressed( InputButton.Attack2 ) )
			return false;

		float rate = Info.SecondaryRate;
		if ( rate <= 0 )
			return true;

		return TimeSinceSecondaryAttack > (1 / rate);
	}

	protected void AttackPrimary()
	{
		if ( AmmoClip == 0 )
		{
			DryFireEffects();
			PlaySound( Info.DryFireSound );
			return;
		}

		TimeSincePrimaryAttack = 0;
		TimeSinceSecondaryAttack = 0;
		AmmoClip -= 1;

		Owner.SetAnimParameter( "b_attack", true );
		ShootEffects();
		PlaySound( Info.FireSound );

		ShootBullet( Info.Spread, 1.5f, Info.Damage, 3.0f, Info.BulletsPerFire );
	}

	protected void AttackSecondary() { }

	protected virtual bool CanReload()
	{
		if ( IsReloading )
			return false;

		if ( AmmoClip >= Info.ClipSize || (!UnlimitedAmmo && Owner.AmmoCount( Info.AmmoType ) == 0 && ReserveAmmo == 0) )
			return false;

		if ( !Owner.IsValid() || !Input.Pressed( InputButton.Reload ) )
			return false;

		return true;
	}

	protected virtual void Reload()
	{
		if ( IsReloading )
			return;

		TimeSinceReload = 0;
		IsReloading = true;

		Owner.SetAnimParameter( "b_reload", true );
		ReloadEffects();
	}

	protected virtual void OnReloadFinish()
	{
		IsReloading = false;
		AmmoClip += TakeAmmo( Info.ClipSize - AmmoClip );
	}

	[ClientRpc]
	protected virtual void ShootEffects()
	{
		if ( !string.IsNullOrEmpty( Info.MuzzleFlashParticle ) )
			Particles.Create( Info.MuzzleFlashParticle, EffectEntity, "muzzle" );

		if ( IsLocalPawn )
			_ = new Sandbox.ScreenShake.Perlin( 1f, 0.2f, 0.8f );

		ViewModelEntity?.SetAnimParameter( "fire", true );
		CrosshairPanel?.CreateEvent( "fire" );

		CurrentRecoilAmount += RecoilOnShot;
	}

	[ClientRpc]
	protected virtual void DryFireEffects()
	{
		ViewModelEntity?.SetAnimParameter( "dryfire", true );
	}

	[ClientRpc]
	protected virtual void ReloadEffects()
	{
		ViewModelEntity?.SetAnimParameter( "reload", true );
	}

	protected virtual void ShootBullet( float spread, float force, float damage, float bulletSize, int bulletCount )
	{
		// Seed rand using the tick, so bullet cones match on client and server
		Rand.SetSeed( Time.Tick );

		while ( bulletCount-- > 0 )
		{
			var forward = Owner.EyeRotation.Forward;
			forward += (Vector3.Random + Vector3.Random + Vector3.Random + Vector3.Random) * spread * 0.25f;
			forward = forward.Normal;

			foreach ( var trace in TraceBullet( Owner.EyePosition, Owner.EyePosition + forward * 20000f, bulletSize ) )
			{
				trace.Surface.DoBulletImpact( trace );

				var fullEndPosition = trace.EndPosition + trace.Direction * bulletSize;

				if ( !string.IsNullOrEmpty( Info.TracerParticle ) && trace.Distance > 200 )
				{
					var tracer = Particles.Create( Info.TracerParticle );
					tracer?.SetPosition( 0, trace.StartPosition );
					tracer?.SetPosition( 1, trace.EndPosition );
				}

				if ( !IsServer )
					continue;

				if ( !trace.Entity.IsValid() )
					continue;

				using ( Prediction.Off() )
				{
					var damageInfo = DamageInfo.FromBullet( trace.EndPosition, forward * 100f * force, damage )
						.UsingTraceResult( trace )
						.WithAttacker( Owner )
						.WithWeapon( this );

					if ( trace.Entity is Player player )
						player.LastDistanceToAttacker = Owner.Position.Distance( player.Position ).SourceUnitsToMeters();

					OnHit( trace.Entity );
					trace.Entity.TakeDamage( damageInfo );
				}
			}
		}
	}

	protected virtual void OnHit( Entity entity ) { }

	/// <summary>
	/// Does a trace from start to end, does bullet impact effects. Coded as an IEnumerable so you can return multiple
	/// hits, like if you're going through layers or ricocet'ing or something.
	/// </summary>
	protected IEnumerable<TraceResult> TraceBullet( Vector3 start, Vector3 end, float radius = 2.0f )
	{
		bool InWater = Map.Physics.IsPointWater( start );

		var trace = Trace.Ray( start, end )
				.UseHitboxes()
				.HitLayer( CollisionLayer.Water, !InWater )
				.HitLayer( CollisionLayer.Debris )
				.Ignore( Owner )
				.Ignore( this )
				.Size( radius )
				.Run();

		yield return trace;

		//
		// Another trace, bullet going through thin material, penetrating water surface?
		//
	}

	protected void DropAmmo()
	{
		if ( Info.AmmoType == AmmoType.None || AmmoClip <= 0 )
			return;

		if ( IsServer )
		{
			var ammoCrate = Ammo.Create( Info.AmmoType, AmmoClip );
			ammoCrate.Position = Owner.EyePosition + Owner.EyeRotation.Forward * 40;
			ammoCrate.Rotation = Owner.EyeRotation;
			ammoCrate.PhysicsGroup.Velocity = Owner.Velocity + Owner.EyeRotation.Forward * Player.DropVelocity;
			ammoCrate.Dropper = Owner;
		}

		AmmoClip = 0;
	}

	protected int TakeAmmo( int ammo )
	{
		if ( UnlimitedAmmo )
			return ammo;

		int available = Math.Min( Info.AmmoType == AmmoType.None ? ReserveAmmo : Owner.AmmoCount( Info.AmmoType ), ammo );

		if ( Info.AmmoType == AmmoType.None )
			ReserveAmmo -= available;
		else
			Owner.TakeAmmo( Info.AmmoType, available );

		return available;
	}

	public static float GetDamageFalloff( float distance, float damage, float start, float end )
	{
		if ( end > 0f )
		{
			if ( start > 0f )
			{
				if ( distance < start )
				{
					return damage;
				}

				var falloffRange = end - start;
				var difference = (distance - start);

				return Math.Max( damage - (damage / falloffRange) * difference, 0f );
			}

			return Math.Max( damage - (damage / end) * distance, 0f );
		}

		return damage;
	}
}