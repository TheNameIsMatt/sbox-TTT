using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace TTT;

public class Perks : IEnumerable<Perk>
{
	public Player Owner { get; init; }

	public Perk this[int i] => _list[i];

	private readonly List<Perk> _list = new();
	public int Count => _list.Count;

	public Perks( Player player ) => Owner = player;

	public void Add( Perk perk )
	{
		_list.Add( perk );
		Owner.Components.Add( perk );
	}

	public void Remove( Perk perk )
	{
		_list.Remove( perk );
		Owner.Components.Remove( perk );
	}

	public bool Has( Type t )
	{
		return _list.Any( x => x.GetType() == t );
	}

	public bool Contains( Perk perk )
	{
		return _list.Contains( perk );
	}

	public Perk Get( int i )
	{
		return _list[i];
	}

	public T Find<T>() where T : Perk
	{
		foreach ( var perk in _list )
		{
			if ( perk is not T t || t.Equals( default( T ) ) )
				continue;

			return t;
		}
		return default;
	}

	public void Clear()
	{
		foreach ( var perk in _list.ToArray() )
		{
			Remove( perk );
		}
	}

	public IEnumerator<Perk> GetEnumerator() => _list.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}