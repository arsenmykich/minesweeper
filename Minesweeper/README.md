# Minesweeper Game - ASP.NET Core

A complete implementation of the Minesweeper game with web interface, automatic solving algorithm, and player rating system.

## Features

### Part 1: Minesweeper Game
- ✅ Classic Minesweeper game with three difficulty levels:
  - Beginner (9x9, 10 mines)
  - Intermediate (16x16, 40 mines)  
  - Expert (30x16, 99 mines)
- ✅ Full functionality matching Windows version:
  - Left click to reveal cells
  - Right click to place flags
  - Automatic revealing of empty areas
  - Timer and mine counter
  - Game states (playing, won, lost)

### Part 2: Solving Algorithm
- ✅ Intelligent puzzle-solving algorithm with multiple strategies:
  - Basic number analysis
  - Pattern recognition (e.g., 1-2-1 pattern)
  - Constraint solving
  - Probability-based guessing as last resort
- ✅ Hint function to get next move
- ✅ Complete automatic game solving

### Part 3: Database and Ratings
- ✅ Entity Framework with SQLite database
- ✅ Player system with unique names
- ✅ Game result saving with completion time
- ✅ Difficulty-based leaderboards
- ✅ Separate AI solver ratings
- ✅ Web interface for viewing rankings

## Screenshots

### Game Interface
[Screenshot of the main game interface showing the minesweeper board, controls, and game information]

### Leaderboard
[Screenshot of the leaderboard page showing different difficulty tabs and player rankings]

### AI Solver Results
[Screenshot showing the solver modal with move-by-move analysis]

## Requirements

### Development Environment
- .NET 9.0 SDK
- Visual Studio 2022 / VS Code / Rider
- Any modern web browser

### Runtime Requirements
- .NET 9.0 Runtime
- SQLite (included with Entity Framework)
- 50MB disk space
- 512MB RAM minimum

## Installation and Setup

### Prerequisites
1. Install .NET 9.0 SDK from [Microsoft's official site](https://dotnet.microsoft.com/download)
2. Ensure you have a code editor (VS Code recommended)

### Getting Started
1. Clone the repository or extract the archive
2. Navigate to the project directory:
   ```bash
   cd Minesweeper
   ```
3. Restore dependencies:
   ```bash
   dotnet restore
   ```
4. Run the application:
   ```bash
   dotnet run
   ```
5. Open your browser and navigate to: `https://localhost:5001` or `http://localhost:5000`

### Database Setup
The SQLite database (`minesweeper.db`) is automatically created on first run in the project root directory.

## Usage

### Playing the Game
1. Select a player or play anonymously
2. Choose difficulty level
3. Click "New Game" to start
4. Use left click to reveal cells
5. Use right click to place flags
6. Use "Get Hint" for assistance
7. Use "Auto Solve" for automatic completion

### Creating a Player
1. Click "New Player"
2. Enter a unique player name
3. Click "Create"

### Viewing Leaderboards
1. Navigate to "Leaderboard" section
2. Select the difficulty tab
3. View best completion times

## Architecture

### Backend (C# / ASP.NET Core)
- **Controllers**: HTTP request handling
  - `GameController`: Game management
  - `RankingController`: Ratings and players
- **Services**: Business logic
  - `GameService`: Game logic and sessions
  - `MinesweeperSolver`: Solving algorithm
- **Models**: Data models
  - `GameModels`: Game entities
  - `DatabaseModels`: Database models
- **Data**: Entity Framework context

### Frontend (HTML/CSS/JavaScript)
- **Responsive web interface** with adaptive design
- **Interactive JavaScript** for game logic
- **AJAX API calls** for server communication
- **Modern CSS** with Material Design styling

### Database (SQLite + Entity Framework)
- **Players**: Player information
- **GameResults**: Completed game results
- **GameSessions**: Active game sessions

## Solving Algorithm

The algorithm uses several strategies in priority order:

1. **Basic Analysis**: Check cells where flag count equals the number
2. **Patterns**: Recognize known patterns like 1-2-1
3. **Constraints**: Analyze overlapping constraints between adjacent cells
4. **Probabilities**: Choose cell with lowest mine probability

The algorithm works only with visible information, just like a real player.

## Technical Features

- **Async/Await** for all database operations
- **Entity Framework Core** with migrations
- **Dependency Injection** for services
- **JSON API** for AJAX requests
- **Responsive CSS Grid** for game board
- **Local Storage** for settings persistence
- **Error Handling** with user-friendly messages

## File Structure

```
Minesweeper/
├── Controllers/          # MVC Controllers
├── Data/                 # Entity Framework Context
├── Models/               # Data Models
├── Services/             # Business Logic
├── Views/                # Razor Views
│   ├── Game/            # Game Pages
│   ├── Ranking/         # Ranking Pages
│   └── Shared/          # Shared Layouts
├── wwwroot/             # Static Files
│   ├── css/            # Stylesheets
│   └── js/             # JavaScript
├── Program.cs           # Entry Point
└── README.md           # Documentation
```

## Additional Features

- Cell reveal animations
- Hint highlighting
- Adaptive design for mobile devices
- Incomplete game saving
- Player statistics
- Multiple theme options (via CSS)

## Development

### Building the Project
```bash
dotnet build
```

### Running Tests
```bash
dotnet test
```

### Database Migrations
```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## License

This project is open source and available under the MIT License.

---

**Author**: Implemented on ASP.NET Core 9.0 with Entity Framework Core and modern web interface.