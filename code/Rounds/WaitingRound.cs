using Sandbox;

namespace TTT;

public class WaitingRound : BaseRound
{
	public override string RoundName => "Waiting";

	public override void OnSecond()
	{
		if ( Host.IsServer && Utils.HasMinimumPlayers() )
			Game.Current.ForceRoundChange( new PreRound() );
	}

	public override void OnPlayerJoin( Player player )
	{
		base.OnPlayerJoin( player );

		player.Respawn();
	}

	public override void OnPlayerKilled( Player player )
	{
		base.OnPlayerKilled( player );

		StartRespawnTimer( player );
	}

	protected override void OnStart()
	{
		if ( !Host.IsServer )
			return;

		foreach ( Client client in Client.All )
		{
			var player = client.Pawn as Player;
			player.Respawn();
		}
	}
}
