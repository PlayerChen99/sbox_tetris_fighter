using Sandbox;
using Sandbox.Network;
using Sandbox.Services;
using System;

public sealed class TetrisController : Component
{
	[Property] List<GameObject> Shapes = new();
	[Property] GameObject NextShapeContainer { get; set; }

	[Property] GameObject BlockOriginObject { get; set; }

	[Property] GameObject PlayerPrefab { get; set; }

	public Vector3 BlockOrigin => BlockOriginObject.Transform.Position;

	[Sync]
	public GameState CurrentState { get; set; } = GameState.None;

	Random rand = new();
	public static Block[,] BlockArea = new Block[10, 20];
	Shape NextShape;
	Shape CurrentShape;

	public float FallTime = 1;
	public float FreezeTime = 1;


	public List<int> lineToClear = new();
	public List<int> lineToFill = new();
	int fillTo;
	int fillFrom;
	int clearX;
	TimeSince TimeSinceAction;

	[Sync]
	public int Score { get; set; }

	int level;

	int bonus;

	public bool NoPlayer => Network.OwnerConnection == null;

	public enum GameState
	{
		None = 0,
		Falling = 1,
		Clearing = 2,
		Filling = 3,
		Finish = 4,
	}

	[Authority]
	public void SetSeed( int seed )
	{
		rand = new( seed );
	}


	protected override void OnUpdate()
	{
		if ( IsProxy )
		{
			return;
		}
		if ( GameNetworkSystem.IsActive && Network.OwnerConnection == null )
		{
			CurrentState = GameState.None;
		}
		switch ( CurrentState )
		{
			case GameState.None:
				break;
			case GameState.Falling:
				FallingProcess();
				break;
			case GameState.Clearing:
				ClearingProcess();
				break;
			case GameState.Filling:
				FillingProcess();
				break;
			case GameState.Finish:
				break;
		}
	}

	private void FallingProcess()
	{
		if ( CurrentShape == null )
		{
			if ( NextShape == null )
			{
				NextShape = rand.FromList( Shapes ).Clone( NextShapeContainer.Transform.Position ).Components.Get<Shape>();
				NextShape.Controller = this;
			}
			CurrentShape = NextShape;
			CurrentShape.TimeSinceFall = 0;
			CurrentShape.ShapePosition = new( 5, 20 );
			CurrentShape.UpdatePosition();
			CurrentShape.isCurrent = true;
			CurrentShape.GameObject.NetworkSpawn();
			NextShape = rand.FromList( Shapes ).Clone( NextShapeContainer.Transform.Position ).Components.Get<Shape>();
			NextShape.Controller = this;
		}
		if ( CurrentShape.Falling() )
		{

		}
		else if ( CurrentShape.Freeze() )
		{
			CurrentShape = null;
			if ( lineToClear.Count > 0 )
			{
				lineToClear.Sort();
				lineToFill = lineToClear.ToList();
				clearX = 0;
				TimeSinceAction = 0;
				bonus = 0;
				CurrentState = GameState.Clearing;
			}
		}
		else
		{
			CurrentState = GameState.Finish;
			if ( Score > 0 )
			{
				Stats.SetValue( "score", Score );
			}
		}
	}

	private void ClearingProcess()
	{
		while ( TimeSinceAction > 0.05 )
		{
			TimeSinceAction -= 0.05f;
			BlockArea[clearX, lineToClear.First()].GameObject.Destroy();
			BlockArea[clearX, lineToClear.First()] = null;
			clearX++;
			if ( clearX == 10 )
			{
				lineToClear.RemoveAt( 0 );
				clearX = 0;
				bonus++;
				level++;
				Score += bonus;
				FallTime = MathX.Lerp( 1, 0.4f, level / 100 );
				if ( lineToClear.Count == 0 )
				{
					CurrentState = GameState.Filling;
					fillTo = lineToFill.First();
					fillFrom = lineToFill.First() + 1;
					while ( lineToFill.Contains( fillFrom ) )
					{
						fillFrom++;
					}
					TimeSinceAction = 0;
					return;
				}
			}
		}
	}

	private void FillingProcess()
	{
		while ( TimeSinceAction > 0.2 )
		{
			TimeSinceAction -= 0.2f;
			bool top = true;
			for ( int index = 0; index < 10; index++ )
			{
				if ( fillFrom < 20 )
				{
					BlockArea[index, fillTo] = BlockArea[index, fillFrom];
					BlockArea[index, fillFrom] = null;

					if ( BlockArea[index, fillTo].IsValid() )
					{
						top = false;
						BlockArea[index, fillTo].Transform.Position = BlockOrigin + new Vector3( 0, index * 32, fillTo * 32 );
					}
				}
				else
				{
					BlockArea[index, fillTo] = null;
				}
			}
			fillTo++;
			fillFrom++;
			while ( lineToFill.Contains( fillFrom ) )
			{
				fillFrom++;
			}
			if ( top )
			{
				CurrentState = GameState.Falling;
				return;
			}
		}
		for ( int index = 0; index < 10; index++ )
		{
			if ( BlockArea[index, fillFrom].IsValid() )
			{
				BlockArea[index, fillFrom].Transform.Position = BlockArea[index, fillFrom].Transform.Position.WithZ( MathX.Lerp( fillFrom, fillTo, TimeSinceAction * 5 ) * 32 + BlockOrigin.z );
			}
		}
	}

	[Authority(NetPermission.HostOnly)]
	public void StartNewGame()
	{
		bonus = 0;
		Score = 0;
		level = 0;
		FallTime = 1;
		ClearZone();
		CurrentState = GameState.Falling;
	}

	public void ClearZone()
	{
		for ( int index = 0; index < 10; index++ )
		{
			for ( int y = 0; y < 20; y++ )
			{
				BlockArea[index, y]?.GameObject?.Destroy();
				BlockArea[index, y] = null;
				CurrentShape?.GameObject?.Destroy();
				CurrentShape = null;
				NextShape?.GameObject?.Destroy();
				NextShape = null;
			}
		}
	}


	[Authority(NetPermission.HostOnly)]
	public void CreatePlayerController()
	{
		var player = PlayerPrefab.Clone( Transform.Position );
		player.NetworkSpawn();
		player.Components.Get<PlayerController>().Controller = this;
	}
}
