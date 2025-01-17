using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox;
using Sandbox.UI;

namespace TTT.UI;

public partial class RoleMenu : Panel
{
	private enum Tab
	{
		Shop,
		DNA,
		Radio
	}

	// Tab => Condition that allows access to the tab.
	private readonly Dictionary<Tab, Func<bool>> _access = new()
	{
		{Tab.Shop, () => (Game.LocalPawn as Player).Role.ShopItems.Any()},
		{Tab.DNA, () => (Game.LocalPawn as Player).Inventory.Find<DNAScanner>() is not null},
		{Tab.Radio, () => (Game.LocalPawn as Player).Components.Get<RadioComponent>() is not null}
	};
	private Tab _currentTab;

	// Determines if we have access to any tabs before we show the role menu.
	// Also ensures that _currentTab is selecting a tab we have access to.
	private bool HasTabAccess()
	{
		if ( _access[_currentTab].Invoke() )
			return true;

		foreach ( var tabEntry in _access )
		{
			if ( tabEntry.Value.Invoke() )
			{
				_currentTab = tabEntry.Key;
				return true;
			}
		}

		return false;
	}

	public override void Tick() => SetClass( "fade-in", Input.Down( InputButton.View ) && HasTabAccess() );

	protected override int BuildHash()
	{
		return HashCode.Combine( (Game.LocalPawn as Player).Role, _access.HashCombine( a => a.Value.Invoke().GetHashCode() ), _currentTab );
	}
}
