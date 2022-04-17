using Sandbox;

namespace TTT;

[Library( "ttt_grenade_smoke", Title = "Smoke Grenade" )]
public class SmokeGrenade : Grenade
{
	protected override void OnExplode()
	{
		base.OnExplode();

		Particles.Create( RawStrings.SmokeParticle, Position );
		Sound.FromWorld( RawStrings.SmokeExplodeSound, Position );
	}
}