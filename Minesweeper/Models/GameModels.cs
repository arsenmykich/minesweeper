using System.ComponentModel.DataAnnotations;

namespace Minesweeper.Models
{
    public enum CellState
    {
        Hidden,
        Revealed,
        Flagged
    }

    public enum GameStatus
    {
        InProgress,
        Won,
        Lost
    }

    public enum Difficulty
    {
        Beginner,   // 9x9 10 mines
        Intermediate, // 16x16 40 mines  
        Expert      // 30x16 99 mines
    }

    public class Cell
    {
        public int X { get; set; }
        public int Y { get; set; }
        public bool IsMine { get; set; }
        public CellState State { get; set; } = CellState.Hidden;
        public int AdjacentMines { get; set; }

        public string GetDisplayValue()
        {
            if (State == CellState.Flagged)
                return "🚩";

            if (State == CellState.Hidden)
                return "";

            if (IsMine)
                return "💣";

            return AdjacentMines > 0 ? AdjacentMines.ToString() : "";
        }

        public string GetCssClass()
        {
            var classes = new List<string> { "cell" };

            if (State == CellState.Hidden)
                classes.Add("hidden");
            else if (State == CellState.Revealed)
            {
                classes.Add("revealed");
                if (IsMine)
                    classes.Add("mine");
                else if (AdjacentMines > 0)
                    classes.Add($"number-{AdjacentMines}");
            }
            else if (State == CellState.Flagged)
                classes.Add("flagged");

            return string.Join(" ", classes);
        }
    }

    public class GameBoard
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public int MineCount { get; set; }
        public Cell[][] Cells { get; set; } = null!;
        public GameStatus Status { get; set; } = GameStatus.InProgress;
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int FlagsUsed { get; set; }
        public bool FirstMove { get; set; } = true;

        public GameBoard(int width, int height, int mineCount)
        {
            Width = width;
            Height = height;
            MineCount = mineCount;
            Cells = new Cell[width][];
            for (int i = 0; i < width; i++)
            {
                Cells[i] = new Cell[height];
            }
            StartTime = DateTime.Now;

            InitializeCells();
        }

        public GameBoard() { } // For serialization

        private void InitializeCells()
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    Cells[x][y] = new Cell { X = x, Y = y };
                }
            }
        }

        public void PlaceMines(int firstClickX, int firstClickY)
        {
            var random = new Random();
            var minesPlaced = 0;

            while (minesPlaced < MineCount)
            {
                int x = random.Next(Width);
                int y = random.Next(Height);

                // Don't place mine on first click or if already has mine
                if ((x == firstClickX && y == firstClickY) || Cells[x][y].IsMine)
                    continue;

                Cells[x][y].IsMine = true;
                minesPlaced++;
            }

            CalculateAdjacentMines();
        }

        private void CalculateAdjacentMines()
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (!Cells[x][y].IsMine)
                    {
                        Cells[x][y].AdjacentMines = CountAdjacentMines(x, y);
                    }
                }
            }
        }

        private int CountAdjacentMines(int x, int y)
        {
            int count = 0;
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;

                    int nx = x + dx;
                    int ny = y + dy;

                    if (IsValidCell(nx, ny) && Cells[nx][ny].IsMine)
                        count++;
                }
            }
            return count;
        }

        public void RevealCell(int x, int y)
        {
            if (!IsValidCell(x, y) || Cells[x][y].State != CellState.Hidden)
                return;

            if (FirstMove)
            {
                PlaceMines(x, y);
                FirstMove = false;
            }

            Cells[x][y].State = CellState.Revealed;

            if (Cells[x][y].IsMine)
            {
                Status = GameStatus.Lost;
                EndTime = DateTime.Now;
                RevealAllMines();
                return;
            }

            // Auto-reveal adjacent cells if no adjacent mines
            if (Cells[x][y].AdjacentMines == 0)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        RevealCell(x + dx, y + dy);
                    }
                }
            }

            CheckWinCondition();
        }

        public void ToggleFlag(int x, int y)
        {
            if (!IsValidCell(x, y) || Cells[x][y].State == CellState.Revealed)
                return;

            if (Cells[x][y].State == CellState.Hidden)
            {
                if (FlagsUsed < MineCount)
                {
                    Cells[x][y].State = CellState.Flagged;
                    FlagsUsed++;
                }
            }
            else if (Cells[x][y].State == CellState.Flagged)
            {
                Cells[x][y].State = CellState.Hidden;
                FlagsUsed--;
            }
        }

        private void RevealAllMines()
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (Cells[x][y].IsMine)
                        Cells[x][y].State = CellState.Revealed;
                }
            }
        }

        private void CheckWinCondition()
        {
            int revealedCount = 0;
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (Cells[x][y].State == CellState.Revealed && !Cells[x][y].IsMine)
                        revealedCount++;
                }
            }

            if (revealedCount == (Width * Height - MineCount))
            {
                Status = GameStatus.Won;
                EndTime = DateTime.Now;
            }
        }

        private bool IsValidCell(int x, int y)
        {
            return x >= 0 && x < Width && y >= 0 && y < Height;
        }

        public int GetRemainingFlags()
        {
            return MineCount - FlagsUsed;
        }

        public TimeSpan GetElapsedTime()
        {
            return (EndTime ?? DateTime.Now) - StartTime;
        }
    }

    public static class DifficultySettings
    {
        public static (int width, int height, int mines) GetSettings(Difficulty difficulty)
        {
            return difficulty switch
            {
                Difficulty.Beginner => (9, 9, 10),
                Difficulty.Intermediate => (16, 16, 40),
                Difficulty.Expert => (30, 16, 99),
                _ => (9, 9, 10)
            };
        }
    }
}