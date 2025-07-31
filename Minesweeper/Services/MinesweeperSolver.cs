using Minesweeper.Models;

namespace Minesweeper.Services
{
    public interface IMinesweeperSolver
    {
        Task<SolverResult> SolveAsync(GameBoard gameBoard);
        Task<SolverMove?> GetNextMoveAsync(GameBoard gameBoard);
    }

    public class SolverResult
    {
        public bool Success { get; set; }
        public List<SolverMove> Moves { get; set; } = new List<SolverMove>();
        public string Strategy { get; set; } = string.Empty;
        public TimeSpan SolutionTime { get; set; }
    }

    public class SolverMove
    {
        public int X { get; set; }
        public int Y { get; set; }
        public SolverMoveType Type { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public enum SolverMoveType
    {
        Reveal,
        Flag
    }

    public class MinesweeperSolver : IMinesweeperSolver
    {
        public async Task<SolverResult> SolveAsync(GameBoard gameBoard)
        {
            var startTime = DateTime.Now;
            var result = new SolverResult();
            var strategies = new List<string>();

            // Create a copy of the game board to work with
            var workingBoard = CloneGameBoard(gameBoard);

            // If it's the first move, click the center
            if (workingBoard.FirstMove)
            {
                int centerX = workingBoard.Width / 2;
                int centerY = workingBoard.Height / 2;
                
                workingBoard.RevealCell(centerX, centerY);
                result.Moves.Add(new SolverMove 
                { 
                    X = centerX, 
                    Y = centerY, 
                    Type = SolverMoveType.Reveal, 
                    Reason = "First move - center click" 
                });
            }

            while (workingBoard.Status == GameStatus.InProgress)
            {
                var move = await GetNextMoveAsync(workingBoard);
                if (move == null)
                {
                    // Try probability-based guessing as last resort
                    move = GetProbabilityBasedMove(workingBoard);
                    if (move == null)
                        break;
                    strategies.Add("Probability-based guessing");
                }

                result.Moves.Add(move);

                if (move.Type == SolverMoveType.Reveal)
                {
                    workingBoard.RevealCell(move.X, move.Y);
                    if (workingBoard.Status == GameStatus.Lost)
                        break;
                }
                else
                {
                    workingBoard.ToggleFlag(move.X, move.Y);
                }

                // Prevent infinite loops
                if (result.Moves.Count > 1000)
                    break;
            }

            result.Success = workingBoard.Status == GameStatus.Won;
            result.Strategy = string.Join(", ", strategies.Distinct());
            result.SolutionTime = DateTime.Now - startTime;

            return result;
        }

        public Task<SolverMove?> GetNextMoveAsync(GameBoard gameBoard)
        {
            // Strategy 1: Basic number analysis
            var basicMove = GetBasicMove(gameBoard);
            if (basicMove != null) return Task.FromResult<SolverMove?>(basicMove);

            // Strategy 2: Pattern recognition
            var patternMove = GetPatternMove(gameBoard);
            if (patternMove != null) return Task.FromResult<SolverMove?>(patternMove);

            // Strategy 3: Advanced constraint solving
            var constraintMove = GetConstraintMove(gameBoard);
            if (constraintMove != null) return Task.FromResult<SolverMove?>(constraintMove);

            return Task.FromResult<SolverMove?>(null);
        }

        private SolverMove? GetBasicMove(GameBoard gameBoard)
        {
            for (int x = 0; x < gameBoard.Width; x++)
            {
                for (int y = 0; y < gameBoard.Height; y++)
                {
                    var cell = gameBoard.Cells[x][y];
                    if (cell.State != CellState.Revealed || cell.AdjacentMines == 0)
                        continue;

                    var adjacentCells = GetAdjacentCells(gameBoard, x, y);
                    var hiddenCells = adjacentCells.Where(c => c.State == CellState.Hidden).ToList();
                    var flaggedCells = adjacentCells.Where(c => c.State == CellState.Flagged).ToList();

                    // If number of flags equals the number, reveal all hidden cells
                    if (flaggedCells.Count == cell.AdjacentMines && hiddenCells.Any())
                    {
                        var cellToReveal = hiddenCells.First();
                        return new SolverMove
                        {
                            X = cellToReveal.X,
                            Y = cellToReveal.Y,
                            Type = SolverMoveType.Reveal,
                            Reason = $"All mines found around ({x},{y})"
                        };
                    }

                    // If hidden + flagged equals the number, flag all hidden cells
                    if (hiddenCells.Count + flaggedCells.Count == cell.AdjacentMines && hiddenCells.Any())
                    {
                        var cellToFlag = hiddenCells.First();
                        return new SolverMove
                        {
                            X = cellToFlag.X,
                            Y = cellToFlag.Y,
                            Type = SolverMoveType.Flag,
                            Reason = $"Must be mine around ({x},{y})"
                        };
                    }
                }
            }

            return null;
        }

        private SolverMove? GetPatternMove(GameBoard gameBoard)
        {
            // 1-2-1 pattern recognition
            for (int x = 1; x < gameBoard.Width - 1; x++)
            {
                for (int y = 0; y < gameBoard.Height; y++)
                {
                    if (IsPattern121Horizontal(gameBoard, x, y))
                    {
                        var move = SolvePattern121Horizontal(gameBoard, x, y);
                        if (move != null) return move;
                    }
                }
            }

            for (int x = 0; x < gameBoard.Width; x++)
            {
                for (int y = 1; y < gameBoard.Height - 1; y++)
                {
                    if (IsPattern121Vertical(gameBoard, x, y))
                    {
                        var move = SolvePattern121Vertical(gameBoard, x, y);
                        if (move != null) return move;
                    }
                }
            }

            return null;
        }

        private bool IsPattern121Horizontal(GameBoard gameBoard, int x, int y)
        {
            if (y >= gameBoard.Height - 1) return false;

            var cells = new[]
            {
                gameBoard.Cells[x-1][y],
                gameBoard.Cells[x][y],
                gameBoard.Cells[x+1][y]
            };

            return cells.All(c => c.State == CellState.Revealed) &&
                   cells[0].AdjacentMines == 1 &&
                   cells[1].AdjacentMines == 2 &&
                   cells[2].AdjacentMines == 1;
        }

        private bool IsPattern121Vertical(GameBoard gameBoard, int x, int y)
        {
            if (x >= gameBoard.Width - 1) return false;

            var cells = new[]
            {
                gameBoard.Cells[x][y-1],
                gameBoard.Cells[x][y],
                gameBoard.Cells[x][y+1]
            };

            return cells.All(c => c.State == CellState.Revealed) &&
                   cells[0].AdjacentMines == 1 &&
                   cells[1].AdjacentMines == 2 &&
                   cells[2].AdjacentMines == 1;
        }

        private SolverMove? SolvePattern121Horizontal(GameBoard gameBoard, int x, int y)
        {
            // Check top and bottom rows for safe moves
            if (y > 0)
            {
                var topCell = gameBoard.Cells[x][y - 1];
                if (topCell.State == CellState.Hidden)
                {
                    return new SolverMove
                    {
                        X = x,
                        Y = y - 1,
                        Type = SolverMoveType.Reveal,
                        Reason = "1-2-1 pattern: safe cell"
                    };
                }
            }

            if (y < gameBoard.Height - 1)
            {
                var bottomCell = gameBoard.Cells[x][y + 1];
                if (bottomCell.State == CellState.Hidden)
                {
                    return new SolverMove
                    {
                        X = x,
                        Y = y + 1,
                        Type = SolverMoveType.Reveal,
                        Reason = "1-2-1 pattern: safe cell"
                    };
                }
            }

            return null;
        }

        private SolverMove? SolvePattern121Vertical(GameBoard gameBoard, int x, int y)
        {
            // Check left and right columns for safe moves
            if (x > 0)
            {
                var leftCell = gameBoard.Cells[x - 1][y];
                if (leftCell.State == CellState.Hidden)
                {
                    return new SolverMove
                    {
                        X = x - 1,
                        Y = y,
                        Type = SolverMoveType.Reveal,
                        Reason = "1-2-1 pattern: safe cell"
                    };
                }
            }

            if (x < gameBoard.Width - 1)
            {
                var rightCell = gameBoard.Cells[x + 1][y];
                if (rightCell.State == CellState.Hidden)
                {
                    return new SolverMove
                    {
                        X = x + 1,
                        Y = y,
                        Type = SolverMoveType.Reveal,
                        Reason = "1-2-1 pattern: safe cell"
                    };
                }
            }

            return null;
        }

        private SolverMove? GetConstraintMove(GameBoard gameBoard)
        {
            // This is a simplified constraint solver
            // In a full implementation, this would use more advanced CSP techniques
            
            var revealedCells = new List<Cell>();
            for (int x = 0; x < gameBoard.Width; x++)
            {
                for (int y = 0; y < gameBoard.Height; y++)
                {
                    if (gameBoard.Cells[x][y].State == CellState.Revealed && 
                        gameBoard.Cells[x][y].AdjacentMines > 0)
                    {
                        revealedCells.Add(gameBoard.Cells[x][y]);
                    }
                }
            }

            // Try to find overlapping constraints
            foreach (var cell1 in revealedCells)
            {
                foreach (var cell2 in revealedCells)
                {
                    if (cell1 == cell2) continue;

                    var move = AnalyzeOverlappingConstraints(gameBoard, cell1, cell2);
                    if (move != null) return move;
                }
            }

            return null;
        }

        private SolverMove? AnalyzeOverlappingConstraints(GameBoard gameBoard, Cell cell1, Cell cell2)
        {
            var adj1 = GetAdjacentCells(gameBoard, cell1.X, cell1.Y);
            var adj2 = GetAdjacentCells(gameBoard, cell2.X, cell2.Y);

            var overlap = adj1.Intersect(adj2).Where(c => c.State == CellState.Hidden).ToList();
            if (!overlap.Any()) return null;

            var unique1 = adj1.Except(adj2).Where(c => c.State == CellState.Hidden).ToList();
            var unique2 = adj2.Except(adj1).Where(c => c.State == CellState.Hidden).ToList();

            var flags1 = adj1.Count(c => c.State == CellState.Flagged);
            var flags2 = adj2.Count(c => c.State == CellState.Flagged);

            var remaining1 = cell1.AdjacentMines - flags1;
            var remaining2 = cell2.AdjacentMines - flags2;

            // If difference in remaining mines equals difference in unique cells
            if (remaining1 - remaining2 == unique1.Count && unique1.Any())
            {
                return new SolverMove
                {
                    X = unique1[0].X,
                    Y = unique1[0].Y,
                    Type = SolverMoveType.Flag,
                    Reason = "Constraint analysis: must be mine"
                };
            }

            if (remaining2 - remaining1 == unique2.Count && unique2.Any())
            {
                return new SolverMove
                {
                    X = unique2[0].X,
                    Y = unique2[0].Y,
                    Type = SolverMoveType.Flag,
                    Reason = "Constraint analysis: must be mine"
                };
            }

            return null;
        }

        private SolverMove? GetProbabilityBasedMove(GameBoard gameBoard)
        {
            var hiddenCells = new List<Cell>();
            for (int x = 0; x < gameBoard.Width; x++)
            {
                for (int y = 0; y < gameBoard.Height; y++)
                {
                    if (gameBoard.Cells[x][y].State == CellState.Hidden)
                    {
                        hiddenCells.Add(gameBoard.Cells[x][y]);
                    }
                }
            }

            if (!hiddenCells.Any()) return null;

            // Simple probability: choose the cell with lowest mine probability
            var cellProbabilities = hiddenCells.Select(cell => new
            {
                Cell = cell,
                Probability = CalculateMineProbability(gameBoard, cell.X, cell.Y)
            }).OrderBy(cp => cp.Probability).ToList();

            var safestCell = cellProbabilities.First().Cell;
            return new SolverMove
            {
                X = safestCell.X,
                Y = safestCell.Y,
                Type = SolverMoveType.Reveal,
                Reason = "Probability-based guess"
            };
        }

        private double CalculateMineProbability(GameBoard gameBoard, int x, int y)
        {
            var adjacentRevealed = GetAdjacentCells(gameBoard, x, y)
                .Where(c => c.State == CellState.Revealed && c.AdjacentMines > 0)
                .ToList();

            if (!adjacentRevealed.Any())
            {
                // Global probability
                int totalCells = gameBoard.Width * gameBoard.Height;
                int revealedCells = 0;
                int mines = gameBoard.MineCount;

                for (int i = 0; i < gameBoard.Width; i++)
                {
                    for (int j = 0; j < gameBoard.Height; j++)
                    {
                        if (gameBoard.Cells[i][j].State == CellState.Revealed)
                            revealedCells++;
                    }
                }

                return (double)mines / (totalCells - revealedCells);
            }

            // Local probability based on adjacent revealed cells
            double totalProbability = 0;
            int constraintCount = 0;

            foreach (var revealedCell in adjacentRevealed)
            {
                var adjacent = GetAdjacentCells(gameBoard, revealedCell.X, revealedCell.Y);
                var hidden = adjacent.Where(c => c.State == CellState.Hidden).ToList();
                var flagged = adjacent.Count(c => c.State == CellState.Flagged);

                if (hidden.Any())
                {
                    var remainingMines = revealedCell.AdjacentMines - flagged;
                    var probability = (double)remainingMines / hidden.Count;
                    totalProbability += probability;
                    constraintCount++;
                }
            }

            return constraintCount > 0 ? totalProbability / constraintCount : 0.5;
        }

        private List<Cell> GetAdjacentCells(GameBoard gameBoard, int x, int y)
        {
            var adjacent = new List<Cell>();

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;

                    int nx = x + dx;
                    int ny = y + dy;

                    if (nx >= 0 && nx < gameBoard.Width && ny >= 0 && ny < gameBoard.Height)
                    {
                        adjacent.Add(gameBoard.Cells[nx][ny]);
                    }
                }
            }

            return adjacent;
        }

        private GameBoard CloneGameBoard(GameBoard original)
        {
            var clone = new GameBoard(original.Width, original.Height, original.MineCount)
            {
                Status = original.Status,
                StartTime = original.StartTime,
                EndTime = original.EndTime,
                FlagsUsed = original.FlagsUsed,
                FirstMove = original.FirstMove
            };

            for (int x = 0; x < original.Width; x++)
            {
                for (int y = 0; y < original.Height; y++)
                {
                    var originalCell = original.Cells[x][y];
                    clone.Cells[x][y] = new Cell
                    {
                        X = originalCell.X,
                        Y = originalCell.Y,
                        IsMine = originalCell.IsMine,
                        State = originalCell.State,
                        AdjacentMines = originalCell.AdjacentMines
                    };
                }
            }

            return clone;
        }
    }
}