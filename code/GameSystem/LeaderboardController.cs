using Sandbox.Services;
using System.Threading.Tasks;

namespace Sandbox
{
	public static class LeaderboardController
	{
		public static Leaderboards.Board board = Leaderboards.Get( "high_score" );

		public static int Highscore { get; set; } = 0;

		public static async Task RefreshLeaderboard()
		{
			board.MaxEntries = 10;
			await board.Refresh();
			foreach ( var entry in board.Entries )
			{
				if ( entry.Me )
				{
					Highscore = (int)entry.Value;
				}
			}
			Log.Info( "Leaderboard refreshed" );
		}
	}

}
