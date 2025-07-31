using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Minesweeper.Models
{
    public class Player
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public List<GameResult> GameResults { get; set; } = new List<GameResult>();
    }

    public class GameResult
    {
        public int Id { get; set; }
        
        public int PlayerId { get; set; }
        public Player Player { get; set; } = null!;
        
        public Difficulty Difficulty { get; set; }
        
        public bool IsWon { get; set; }
        
        public TimeSpan CompletionTime { get; set; }
        
        public DateTime PlayedAt { get; set; } = DateTime.Now;
        
        public int Width { get; set; }
        public int Height { get; set; }
        public int MineCount { get; set; }
        
        // For solver results
        public bool IsSolverGame { get; set; } = false;
        public string? SolverStrategy { get; set; }
    }

    public class GameSession
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        public int? PlayerId { get; set; }
        public Player? Player { get; set; }
        
        public Difficulty Difficulty { get; set; }
        
        public string GameBoardJson { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? LastUpdated { get; set; }
        
        public bool IsCompleted { get; set; } = false;
    }
}