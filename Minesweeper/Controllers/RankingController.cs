using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Minesweeper.Data;
using Minesweeper.Models;
using Minesweeper.Services;

namespace Minesweeper.Controllers
{
    public class RankingController : Controller
    {
        private readonly IGameService _gameService;
        private readonly MinesweeperContext _context;

        public RankingController(IGameService gameService, MinesweeperContext context)
        {
            _gameService = gameService;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var model = new RankingViewModel
            {
                BeginnerLeaderboard = await _gameService.GetLeaderboardAsync(Difficulty.Beginner),
                IntermediateLeaderboard = await _gameService.GetLeaderboardAsync(Difficulty.Intermediate),
                ExpertLeaderboard = await _gameService.GetLeaderboardAsync(Difficulty.Expert),
                SolverLeaderboard = await _gameService.GetLeaderboardAsync(null, true)
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> GetLeaderboard(Difficulty? difficulty = null, bool solverOnly = false)
        {
            try
            {
                var leaderboard = await _gameService.GetLeaderboardAsync(difficulty, solverOnly);
                return Json(new { success = true, leaderboard });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreatePlayer([FromBody] CreatePlayerRequest request)
        {
            try
            {
                var existingPlayer = await _context.Players
                    .FirstOrDefaultAsync(p => p.Name == request.Name);

                if (existingPlayer != null)
                    return Json(new { success = false, error = "Player name already exists" });

                var player = new Player
                {
                    Name = request.Name,
                    CreatedAt = DateTime.Now
                };

                _context.Players.Add(player);
                await _context.SaveChangesAsync();

                return Json(new { success = true, player });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPlayers()
        {
            try
            {
                var players = await _context.Players
                    .OrderBy(p => p.Name)
                    .ToListAsync();

                return Json(new { success = true, players });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPlayerStats(int playerId)
        {
            try
            {
                var player = await _context.Players
                    .Include(p => p.GameResults)
                    .FirstOrDefaultAsync(p => p.Id == playerId);

                if (player == null)
                    return Json(new { success = false, error = "Player not found" });

                var stats = new PlayerStatsViewModel
                {
                    Player = player,
                    TotalGames = player.GameResults.Count,
                    WonGames = player.GameResults.Count(gr => gr.IsWon),
                    BestTimes = new Dictionary<Difficulty, TimeSpan?>
                    {
                        { Difficulty.Beginner, player.GameResults.Where(gr => gr.Difficulty == Difficulty.Beginner && gr.IsWon).MinBy(gr => gr.CompletionTime)?.CompletionTime },
                        { Difficulty.Intermediate, player.GameResults.Where(gr => gr.Difficulty == Difficulty.Intermediate && gr.IsWon).MinBy(gr => gr.CompletionTime)?.CompletionTime },
                        { Difficulty.Expert, player.GameResults.Where(gr => gr.Difficulty == Difficulty.Expert && gr.IsWon).MinBy(gr => gr.CompletionTime)?.CompletionTime }
                    }
                };

                return Json(new { success = true, stats });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }
    }

    public class RankingViewModel
    {
        public List<GameResult> BeginnerLeaderboard { get; set; } = new List<GameResult>();
        public List<GameResult> IntermediateLeaderboard { get; set; } = new List<GameResult>();
        public List<GameResult> ExpertLeaderboard { get; set; } = new List<GameResult>();
        public List<GameResult> SolverLeaderboard { get; set; } = new List<GameResult>();
    }

    public class PlayerStatsViewModel
    {
        public Player Player { get; set; } = null!;
        public int TotalGames { get; set; }
        public int WonGames { get; set; }
        public Dictionary<Difficulty, TimeSpan?> BestTimes { get; set; } = new Dictionary<Difficulty, TimeSpan?>();
    }

    public class CreatePlayerRequest
    {
        public string Name { get; set; } = string.Empty;
    }
}