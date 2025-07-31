using Microsoft.EntityFrameworkCore;
using Minesweeper.Data;
using Minesweeper.Models;
using System.Text.Json;

namespace Minesweeper.Services
{
    public interface IGameService
    {
        Task<(string sessionId, GameBoard gameBoard)> CreateNewGameAsync(Difficulty difficulty, int? playerId = null);
        Task<GameBoard?> GetGameAsync(string sessionId);
        Task<GameBoard> RevealCellAsync(string sessionId, int x, int y);
        Task<GameBoard> ToggleFlagAsync(string sessionId, int x, int y);
        Task<GameResult?> SaveGameResultAsync(string sessionId);
        Task<List<GameResult>> GetLeaderboardAsync(Difficulty? difficulty = null, bool solverOnly = false);
    }

    public class GameService : IGameService
    {
        private readonly MinesweeperContext _context;

        public GameService(MinesweeperContext context)
        {
            _context = context;
        }

        public async Task<(string sessionId, GameBoard gameBoard)> CreateNewGameAsync(Difficulty difficulty, int? playerId = null)
        {
            var settings = DifficultySettings.GetSettings(difficulty);
            var gameBoard = new GameBoard(settings.width, settings.height, settings.mines);

            var session = new GameSession
            {
                Id = Guid.NewGuid().ToString(),
                PlayerId = playerId,
                Difficulty = difficulty,
                GameBoardJson = JsonSerializer.Serialize(gameBoard),
                CreatedAt = DateTime.Now
            };

            _context.GameSessions.Add(session);
            await _context.SaveChangesAsync();

            return (session.Id, gameBoard);
        }

        public async Task<GameBoard?> GetGameAsync(string sessionId)
        {
            var session = await _context.GameSessions.FindAsync(sessionId);
            if (session == null) return null;

            return JsonSerializer.Deserialize<GameBoard>(session.GameBoardJson);
        }

        public async Task<GameBoard> RevealCellAsync(string sessionId, int x, int y)
        {
            var session = await _context.GameSessions.FindAsync(sessionId);
            if (session == null) throw new ArgumentException("Session not found");

            var gameBoard = JsonSerializer.Deserialize<GameBoard>(session.GameBoardJson)!;
            gameBoard.RevealCell(x, y);

            session.GameBoardJson = JsonSerializer.Serialize(gameBoard);
            session.LastUpdated = DateTime.Now;

            if (gameBoard.Status != GameStatus.InProgress)
            {
                session.IsCompleted = true;
            }

            await _context.SaveChangesAsync();
            return gameBoard;
        }

        public async Task<GameBoard> ToggleFlagAsync(string sessionId, int x, int y)
        {
            var session = await _context.GameSessions.FindAsync(sessionId);
            if (session == null) throw new ArgumentException("Session not found");

            var gameBoard = JsonSerializer.Deserialize<GameBoard>(session.GameBoardJson)!;
            gameBoard.ToggleFlag(x, y);

            session.GameBoardJson = JsonSerializer.Serialize(gameBoard);
            session.LastUpdated = DateTime.Now;

            await _context.SaveChangesAsync();
            return gameBoard;
        }

        public async Task<GameResult?> SaveGameResultAsync(string sessionId)
        {
            var session = await _context.GameSessions
                .Include(s => s.Player)
                .FirstOrDefaultAsync(s => s.Id == sessionId);

            if (session == null || !session.IsCompleted) return null;

            var gameBoard = JsonSerializer.Deserialize<GameBoard>(session.GameBoardJson)!;

            if (gameBoard.Status != GameStatus.Won) return null; // Only save wins

            var gameResult = new GameResult
            {
                PlayerId = session.PlayerId ?? 0,
                Difficulty = session.Difficulty,
                IsWon = true,
                CompletionTime = gameBoard.GetElapsedTime(),
                PlayedAt = DateTime.Now,
                Width = gameBoard.Width,
                Height = gameBoard.Height,
                MineCount = gameBoard.MineCount
            };

            _context.GameResults.Add(gameResult);
            await _context.SaveChangesAsync();

            return gameResult;
        }

        public async Task<List<GameResult>> GetLeaderboardAsync(Difficulty? difficulty = null, bool solverOnly = false)
        {
            var query = _context.GameResults
                .Include(gr => gr.Player)
                .Where(gr => gr.IsWon);

            if (difficulty.HasValue)
            {
                query = query.Where(gr => gr.Difficulty == difficulty.Value);
            }

            if (solverOnly)
            {
                query = query.Where(gr => gr.IsSolverGame);
            }
            else
            {
                query = query.Where(gr => !gr.IsSolverGame);
            }

            return await query
                .OrderBy(gr => gr.CompletionTime)
                .Take(10)
                .ToListAsync();
        }


    }
}