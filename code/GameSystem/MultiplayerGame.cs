using Sandbox;
using Sandbox.Network;
using System;
using System.Threading.Tasks;

public sealed class MultiplayerGame : Component, Component.INetworkListener
{

	[Property]
	public TetrisController ControllerLeft { get; set; }

	[Property]
	public TetrisController ControllerRight { get; set; }

	[HostSync]
	public GameState CurrentState { get; set; } = GameState.WaitingPlayer;

	public static MultiplayerGame Instance;

	public static Connection LeftConnection => Instance.ControllerLeft.Network.OwnerConnection;
	public static Connection RightConnection => Instance.ControllerRight.Network.OwnerConnection;

	public static bool LeftNoPlayer => Instance.ControllerLeft.NoPlayer;
	public static bool RightNoPlayer => Instance.ControllerRight.NoPlayer;

	public static bool LeftLocal => Instance.ControllerLeft.Network.OwnerConnection == Connection.Local;
	public static bool RightLocal => Instance.ControllerRight.Network.OwnerConnection == Connection.Local;

	public enum GameState
	{
		WaitingPlayer = 0,
		Ready = 1,
		Playing = 2,
		Finish = 3,
	}


	public static TimeUntil TimeUntilGame;

	

	protected override async Task OnLoad()
	{
		if ( Scene.IsEditor )
			return;

		if ( !GameNetworkSystem.IsActive )
		{
			LoadingScreen.Title = "Creating Lobby";
			await Task.DelayRealtimeSeconds( 0.1f );
			GameNetworkSystem.CreateLobby();
		}
	}

	protected override void OnAwake()
	{
		Instance = this;
	}

	protected override void OnUpdate()
	{
		if ( !Networking.IsHost )
		{
			return;
		}
		switch ( CurrentState )
		{
			case GameState.WaitingPlayer:
				if ( !ControllerLeft.NoPlayer && !ControllerRight.NoPlayer )
				{
					SetTimer( 10 );
					CurrentState = GameState.Ready;
				}
				break;
			case GameState.Ready:
				if ( ControllerLeft.NoPlayer || ControllerRight.NoPlayer )
				{
					CurrentState = GameState.WaitingPlayer;
				}
				else if ( TimeUntilGame )
				{
					int seed = Random.Shared.Next();

					ControllerLeft.SetSeed( seed );
					ControllerRight.SetSeed( seed );
					ControllerLeft.StartNewGame();
					ControllerRight.StartNewGame();
					CurrentState = GameState.Playing;
				}
				break;
			case GameState.Playing:
				if ( (ControllerLeft.CurrentState == TetrisController.GameState.Finish || ControllerLeft.NoPlayer) && (ControllerRight.CurrentState == TetrisController.GameState.Finish || ControllerRight.NoPlayer) )
				{
					SetTimer( 20 );
					CurrentState = GameState.Finish;
				}
				break;
			case GameState.Finish:
				if ( ControllerLeft.NoPlayer && ControllerRight.NoPlayer )
				{
					CurrentState |= GameState.WaitingPlayer;
				}
				else if ( TimeUntilGame )
				{
					CurrentState = GameState.WaitingPlayer;
				}
				break;
		}
	}

	[Broadcast(NetPermission.HostOnly)]
	void SetTimer(int time )
	{
		TimeUntilGame = time;
	}

	[Authority]
	public void JoinLeft()
	{
		if ( LeftNoPlayer )
		{
			ControllerLeft.Network.AssignOwnership( Rpc.Caller );
			ControllerLeft.CreatePlayerController();
		}
	}

	[Authority]
	public void JoinRight()
	{
		if ( RightNoPlayer )
		{
			ControllerRight.Network.AssignOwnership( Rpc.Caller );
			ControllerRight.CreatePlayerController();
		}
	}
}
