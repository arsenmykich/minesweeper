namespace Minesweeper.Models
{
    public class GameBoardViewModel
    {
        public string SessionId { get; set; } = string.Empty;
        public GameBoard GameBoard { get; set; } = null!;
    }

    public class GameResponse
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public string? SessionId { get; set; }
        public GameBoard? GameBoard { get; set; }
    }
}