
using Sandbox.Services;

public sealed class Shape : Component
{
	public TetrisController Controller { get; set; }

	public Vector2Int ShapePosition;

	Block[] blocks;

	public TimeSince TimeSinceFall;

	bool isFreezing = false;

	public bool isCurrent = false;

	protected override void OnAwake()
	{
		blocks = Components.GetAll<Block>( FindMode.InChildren ).ToArray();
	}


	public bool Falling()
	{
		if ( isFreezing )
		{
			if ( MoveShape( Vector2Int.Down ) )
			{
				TimeSinceFall = 0;
				isFreezing = false;
			}
			else if ( TimeSinceFall > Controller.FreezeTime )
			{
				return false;
			}
		}
		else
		{
			while ( TimeSinceFall > Controller.FallTime )
			{
				TimeSinceFall -= Controller.FallTime;
				if ( !MoveShape( Vector2Int.Down ) )
				{
					isFreezing = true;
				}
			}
		}
		UpdatePosition();
		return true;
	}

	public bool Freeze()
	{
		foreach ( Block block in blocks )
		{
			Vector2Int blockPosition = ShapePosition + block.LocalPosition;
			if ( blockPosition.y > 19 )
			{
				return false;
			}
			Vector3 position = Controller.BlockOrigin + new Vector3( 0, blockPosition.x * 32, blockPosition.y * 32 );
			var newBlock = block.GameObject.Clone( position ).Components.Get<Block>();
			newBlock.GameObject.NetworkSpawn();
			TetrisController.BlockArea[blockPosition.x, blockPosition.y] = newBlock;
			if ( CheckLineClear( blockPosition.y ) && !Controller.lineToClear.Contains( blockPosition.y ) )
			{
				Controller.lineToClear.Add( blockPosition.y );
			}

		}
		GameObject.Destroy();
		return true;
	}


	bool CheckPositionValid( Vector2Int delta, int rotate )
	{
		foreach ( Block block in blocks )
		{
			if ( !block.CheckPositionValid( delta, rotate ) )
			{
				return false;
			}
		}
		return true;
	}

	public bool MoveShape( Vector2Int delta )
	{
		if ( CheckPositionValid( ShapePosition + delta, 0 ) )
		{
			ShapePosition += delta;
			return true;
		}
		return false;
	}

	public bool RotateShape( bool clockwise )
	{
		int rotate = clockwise ? 1 : -1;
		if ( CheckPositionValid( ShapePosition, rotate ) )
		{
			RotateShape( rotate );
			return true;
		}
		return false;
	}

	[Broadcast]
	void RotateShape(int rotate)
	{
		foreach ( Block block in blocks )
		{
			block.Rotate( rotate );
		}
	}

	bool CheckLineClear( int line )
	{
		for ( int index = 0; index < 10; index++ )
		{
			if ( !TetrisController.BlockArea[index, line].IsValid() )
			{
				return false;
			}
		}
		return true;
	}

	public void UpdatePosition()
	{
		if ( isFreezing )
		{
			Transform.Position = Controller.BlockOrigin + new Vector3( 0, ShapePosition.x * 32, ShapePosition.y * 32 );
		}
		else
		{
			Transform.Position = Controller.BlockOrigin + new Vector3( 0, ShapePosition.x * 32, (ShapePosition.y + 1 - TimeSinceFall / Controller.FallTime) * 32 );
		}
	}
}
