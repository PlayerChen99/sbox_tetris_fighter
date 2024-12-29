using Sandbox;

public sealed class SinglePlayerGame : Component
{

	[Property]
	public TetrisController ControllerSingle { get; set; }

	public static SinglePlayerGame Instance;

	protected override void OnStart()
	{
		Instance = this;
		ControllerSingle.CreatePlayerController();
		StartNewGame();
	}

	public void StartNewGame()
	{
		ControllerSingle.StartNewGame();
	}

}
