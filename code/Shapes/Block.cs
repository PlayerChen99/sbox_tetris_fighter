using System;

public sealed class Block : Component
{

	public Vector2Int LocalPosition;
	protected override void OnAwake()
	{
		LocalPosition.x = (int)Math.Round( Transform.LocalPosition.y / 32 );
		LocalPosition.y = (int)Math.Round( Transform.LocalPosition.z / 32 );
	}

	public void Rotate( int rotate )
	{
		Vector2Int newPosition;
		if ( rotate == 1 )
		{
			newPosition.x = -LocalPosition.y;
			newPosition.y = LocalPosition.x;
		}
		else if ( rotate == -1 )
		{
			newPosition.x = LocalPosition.y;
			newPosition.y = -LocalPosition.x;
		}
		else
		{
			return;
		}
		LocalPosition.x = newPosition.x;
		LocalPosition.y = newPosition.y;
		Transform.LocalPosition = new( 0, LocalPosition.x * 32, LocalPosition.y * 32 );
	}

	public bool CheckPositionValid( Vector2Int delta, int rotate )
	{
		Vector2Int newPosition;
		if ( rotate == 0 )
		{
			newPosition = LocalPosition + delta;
		}
		else if ( rotate == 1 )
		{
			newPosition.x = delta.x - LocalPosition.y;
			newPosition.y = delta.y + LocalPosition.x;
		}
		else if ( rotate == -1 )
		{
			newPosition.x = delta.x + LocalPosition.y;
			newPosition.y = delta.y - LocalPosition.x;
		}
		else
		{
			return false;
		}
		if ( newPosition.x < 0 || newPosition.x > 9 || newPosition.y < 0 )
		{
			return false;
		}
		else if ( newPosition.y > 19 )
		{
			return true;
		}
		else if ( TetrisController.BlockArea[newPosition.x, newPosition.y].IsValid() )
		{
			return false;
		}
		return true;
	}

	//public static implicit operator bool( Block block )
	//{
	//	return block is not null && block.IsValid;
	//}
}
