using Microsoft.AspNetCore.Mvc;
using Minesweeper.Models;
using Minesweeper.Services;
using System.Text.Json;

namespace Minesweeper.Controllers
{
    public class GameController : Controller
    {
        private readonly IGameService _gameService;
        private readonly IMinesweeperSolver _solver;

        public GameController(IGameService gameService, IMinesweeperSolver solver)
        {
            _gameService = gameService;
            _solver = solver;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> NewGame([FromBody] NewGameRequest request)
        {
            try
            {
                var (sessionId, gameBoard) = await _gameService.CreateNewGameAsync(request.Difficulty, request.PlayerId);
                return Json(new { success = true, sessionId, gameBoard });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RevealCell([FromBody] CellActionRequest request)
        {
            try
            {
                var gameBoard = await _gameService.RevealCellAsync(request.SessionId, request.X, request.Y);
                return Json(new { success = true, gameBoard });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ToggleFlag([FromBody] CellActionRequest request)
        {
            try
            {
                var gameBoard = await _gameService.ToggleFlagAsync(request.SessionId, request.X, request.Y);
                return Json(new { success = true, gameBoard });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetGame(string sessionId)
        {
            try
            {
                var gameBoard = await _gameService.GetGameAsync(sessionId);
                if (gameBoard == null)
                    return Json(new { success = false, error = "Game not found" });

                return Json(new { success = true, gameBoard });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveResult([FromBody] SaveResultRequest request)
        {
            try
            {
                var result = await _gameService.SaveGameResultAsync(request.SessionId);
                return Json(new { success = true, result });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SolveGame([FromBody] SolveGameRequest request)
        {
            try
            {
                var gameBoard = await _gameService.GetGameAsync(request.SessionId);
                if (gameBoard == null)
                    return Json(new { success = false, error = "Game not found" });

                var solverResult = await _solver.SolveAsync(gameBoard);
                return Json(new { success = true, solverResult });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetHint([FromBody] SolveGameRequest request)
        {
            try
            {
                var gameBoard = await _gameService.GetGameAsync(request.SessionId);
                if (gameBoard == null)
                    return Json(new { success = false, error = "Game not found" });

                var nextMove = await _solver.GetNextMoveAsync(gameBoard);
                return Json(new { success = true, nextMove });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }
    }

    public class NewGameRequest
    {
        public Difficulty Difficulty { get; set; }
        public int? PlayerId { get; set; }
    }

    public class CellActionRequest
    {
        public string SessionId { get; set; } = string.Empty;
        public int X { get; set; }
        public int Y { get; set; }
    }

    public class SaveResultRequest
    {
        public string SessionId { get; set; } = string.Empty;
    }

    public class SolveGameRequest
    {
        public string SessionId { get; set; } = string.Empty;
    }
}