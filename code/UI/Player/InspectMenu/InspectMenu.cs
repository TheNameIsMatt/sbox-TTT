using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;
using System.Collections.Generic;

namespace TTT.UI;

[UseTemplate]
public partial class InspectMenu : Panel
{
	private readonly Corpse _corpse;
	private InspectEntry _selectedInspectEntry;

	private readonly List<InspectEntry> _inspectionEntries = new();
	private readonly InspectEntry _timeSinceDeathEntry;
	private readonly InspectEntry _deathCauseEntry;
	private readonly InspectEntry _weaponEntry;
	private readonly InspectEntry _headshotEntry;
	private readonly InspectEntry _distanceEntry;

	private Panel InspectContainer { get; set; }
	private Image PlayerAvatar { get; set; }
	private Label RoleName { get; set; }
	private Label PlayerName { get; set; }
	private Panel IconsContainer { get; set; }
	private Button CallDetectiveButton { get; set; }
	private readonly Label _inspectDetailsLabel;

	public InspectMenu( Corpse corpse )
	{
		if ( corpse.DeadPlayer is null )
			return;

		_timeSinceDeathEntry = new InspectEntry( IconsContainer );
		_timeSinceDeathEntry.Enabled( true );
		_timeSinceDeathEntry.SetImage( "/ui/inspectmenu/time.png" );
		_inspectionEntries.Add( _timeSinceDeathEntry );

		_deathCauseEntry = new InspectEntry( IconsContainer );
		_deathCauseEntry.Enabled( false );
		_inspectionEntries.Add( _deathCauseEntry );

		_weaponEntry = new InspectEntry( IconsContainer );
		_weaponEntry.Enabled( false );
		_inspectionEntries.Add( _weaponEntry );

		_headshotEntry = new InspectEntry( IconsContainer );
		_headshotEntry.Enabled( false );
		_inspectionEntries.Add( _headshotEntry );

		_distanceEntry = new InspectEntry( IconsContainer );
		_distanceEntry.Enabled( false );
		_inspectionEntries.Add( _distanceEntry );

		_inspectDetailsLabel = InspectContainer.Add.Label();
		_inspectDetailsLabel.AddClass( "inspect-details-label" );

		_corpse = corpse;
		SetConfirmationData( _corpse.KillerWeapon, _corpse.Perks );
	}

	private void SetConfirmationData( CarriableInfo carriableInfo, PerkInfo[] perks )
	{
		PlayerAvatar.SetTexture( $"avatar:{_corpse.PlayerId}" );
		PlayerName.Text = _corpse.PlayerName;
		RoleName.Text = _corpse.DeadPlayer.Role.Title;
		RoleName.Style.FontColor = _corpse.DeadPlayer.Role.Color;

		_headshotEntry.Enabled( _corpse.WasHeadshot );
		_headshotEntry.SetImage( "/ui/inspectmenu/headshot.png" );
		_headshotEntry.SetImageText( "Headshot" );
		_headshotEntry.SetActiveText( "The fatal wound was a headshot. No time to scream." );

		var (name, imageText, activeText) = GetCauseOfDeathStrings();
		_deathCauseEntry.Enabled( true );
		_deathCauseEntry.SetImage( $"/ui/inspectmenu/{name}.png" );
		_deathCauseEntry.SetImageText( imageText );
		_deathCauseEntry.SetActiveText( activeText );

		_distanceEntry.Enabled( _corpse.KillInfo.Flags != DamageFlags.Generic );
		_distanceEntry.SetImage( "/ui/inspectmenu/distance.png" );
		_distanceEntry.SetImageText( $"{_corpse.Distance:n0}m" );
		_distanceEntry.SetActiveText( $"They were killed from approximately {_corpse.Distance:n0}m away." );

		_weaponEntry.Enabled( carriableInfo is not null );
		if ( _weaponEntry.IsEnabled() )
		{
			_weaponEntry.SetImage( carriableInfo.Icon );
			_weaponEntry.SetImageText( $"{carriableInfo.Title}" );
			_weaponEntry.SetActiveText( $"It appears a {carriableInfo.Title} was used to kill them." );
		}

		if ( !perks.IsNullOrEmpty() )
		{
			foreach ( var perk in perks )
			{
				var perkEntry = new InspectEntry( IconsContainer );
				perkEntry.SetImage( perk.Icon );
				perkEntry.SetImageText( perk.Title );
				perkEntry.SetActiveText( $"They were carrying {perk.Title}." );

				_inspectionEntries.Add( perkEntry );
			}
		}

		foreach ( var entry in _inspectionEntries )
		{
			entry.AddEventListener( "onmouseover", () =>
			 {
				 _selectedInspectEntry = entry;
				 UpdateCurrentInspectDescription();
			 } );

			entry.AddEventListener( "onmouseout", () =>
			 {
				 _selectedInspectEntry = null;
				 UpdateCurrentInspectDescription();
			 } );
		}
	}

	private void UpdateCurrentInspectDescription()
	{
		_inspectDetailsLabel.SetClass( "fade-in", _selectedInspectEntry is not null );

		if ( _selectedInspectEntry is null )
			return;

		_inspectDetailsLabel.Text = _selectedInspectEntry.ActiveText;
	}

	private (string name, string imageText, string activeText) GetCauseOfDeathStrings()
	{
		return _corpse.KillInfo.Flags switch
		{
			DamageFlags.Generic => ("Unknown", "Unknown", "The cause of death is unknown."),
			DamageFlags.Crush => ("Crushed", "Crushed", "This corpse was crushed to death."),
			DamageFlags.Bullet => ("Bullet", "Bullet", "This corpse was shot to death."),
			DamageFlags.Buckshot => ("Bullet", "Bullet", "This corpse was shot to death."),
			DamageFlags.Slash => ("Slash", "Slashed", "This corpse was cut to death."),
			DamageFlags.Burn => ("Burn", "Burned", "This corpse has burn marks all over."),
			DamageFlags.Vehicle => ("Vehicle", "Vehicle", "This corpse was hit by a vehicle."),
			DamageFlags.Fall => ("Fall", "Fell", "This corpse fell from a high height."),
			DamageFlags.Blast => ("Explode", "Explosion", "An explosion eviscerated this corpse."),
			DamageFlags.PhysicsImpact => ("Prop", "Prop", "A wild flying prop caused this death."),
			DamageFlags.Drown => ("Drown", "Drown", "This player drowned to death."),
			_ => ("Unknown", "Unknown", "The cause of death is unknown.")
		};
	}

	public override void Tick()
	{
		CallDetectiveButton.SetClass( "inactive", _corpse.HasCalledDetective || !Local.Pawn.IsAlive() );

		string timeSinceDeath = (Time.Now - _corpse.KilledTime).TimerString();
		_timeSinceDeathEntry.SetImageText( $"{timeSinceDeath}" );
		_timeSinceDeathEntry.SetActiveText( $"They died roughly {timeSinceDeath} ago." );

		if ( _selectedInspectEntry is not null && _selectedInspectEntry == _timeSinceDeathEntry )
			UpdateCurrentInspectDescription();
	}

	// Called from UI panel
	public void CallDetective()
	{
		if ( _corpse.HasCalledDetective )
			return;

		CallDetectives( _corpse.NetworkIdent );
		_corpse.HasCalledDetective = true;
	}

	[ServerCmd]
	private static void CallDetectives( int ident )
	{
		var ent = Entity.FindByIndex( ident );
		if ( !ent.IsValid() || ent is not Corpse corpse )
			return;

		ChatBox.AddInfo( To.Everyone, $"{ConsoleSystem.Caller.Name} called a Detective to the body of {corpse.PlayerName}" );
		SendDetectiveMarker( To.Multiple( Utils.GetAliveClientsWithRole( new DetectiveRole() ) ), corpse.Position );
	}

	[ClientRpc]
	public static void SendDetectiveMarker( Vector3 corpseLocation )
	{
		var activeDetectiveMarkers = WorldPoints.Instance.FindPoints<DetectiveMarker>();
		foreach ( var marker in activeDetectiveMarkers )
		{
			if ( marker.CorpseLocation == corpseLocation )
				return;
		}

		WorldPoints.Instance.AddChild( new DetectiveMarker( corpseLocation ) );
	}
}