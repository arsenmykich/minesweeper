// Minesweeper Game JavaScript

const Game = {
    currentSessionId: null,
    currentPlayerId: null,
    gameBoard: null,
    timer: null,
    startTime: null,
    
    // Initialize the game
    init: function() {
        this.setupEventListeners();
        this.loadPlayers();
        this.updateUI();
    },

    // Setup event listeners
    setupEventListeners: function() {
        // Game controls
        document.getElementById('newGameBtn').addEventListener('click', () => this.newGame());
        document.getElementById('hintBtn').addEventListener('click', () => this.getHint());
        document.getElementById('solveBtn').addEventListener('click', () => this.autoSolve());
        
        // Player management
        document.getElementById('newPlayerBtn').addEventListener('click', () => this.showPlayerModal());
        document.getElementById('createPlayerBtn').addEventListener('click', () => this.createPlayer());
        document.getElementById('cancelPlayerBtn').addEventListener('click', () => this.hidePlayerModal());
        document.getElementById('closeSolverBtn').addEventListener('click', () => this.hideSolverModal());
        
        // Player selection
        document.getElementById('playerSelect').addEventListener('change', (e) => {
            this.currentPlayerId = e.target.value ? parseInt(e.target.value) : null;
        });

        // Enter key in player name input
        document.getElementById('playerNameInput').addEventListener('keypress', (e) => {
            if (e.key === 'Enter') {
                this.createPlayer();
            }
        });

        // Close modals when clicking outside
        document.addEventListener('click', (e) => {
            if (e.target.classList.contains('modal')) {
                this.hidePlayerModal();
                this.hideSolverModal();
            }
        });
    },

    // Load players from server
    loadPlayers: async function() {
        try {
            const response = await fetch('/Ranking/GetPlayers');
            const data = await response.json();
            
            if (data.success) {
                const select = document.getElementById('playerSelect');
                
                // Clear existing options except "Anonymous"
                while (select.children.length > 1) {
                    select.removeChild(select.lastChild);
                }
                
                // Add players
                data.players.forEach(player => {
                    const option = document.createElement('option');
                    option.value = player.id;
                    option.textContent = player.name;
                    select.appendChild(option);
                });
            }
        } catch (error) {
            console.error('Error loading players:', error);
        }
    },

    // Show player creation modal
    showPlayerModal: function() {
        document.getElementById('playerModal').style.display = 'flex';
        document.getElementById('playerNameInput').focus();
    },

    // Hide player creation modal
    hidePlayerModal: function() {
        document.getElementById('playerModal').style.display = 'none';
        document.getElementById('playerNameInput').value = '';
    },

    // Create new player
    createPlayer: async function() {
        const name = document.getElementById('playerNameInput').value.trim();
        
        if (!name) {
            alert('Please enter a player name');
            return;
        }

        try {
            const response = await fetch('/Ranking/CreatePlayer', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({ name: name })
            });

            const data = await response.json();
            
            if (data.success) {
                await this.loadPlayers();
                document.getElementById('playerSelect').value = data.player.id;
                this.currentPlayerId = data.player.id;
                this.hidePlayerModal();
                this.showMessage('Player created successfully!', 'success');
            } else {
                alert('Error: ' + data.error);
            }
        } catch (error) {
            console.error('Error creating player:', error);
            alert('Error creating player');
        }
    },

    // Start new game
    newGame: async function() {
        const difficulty = parseInt(document.getElementById('difficultySelect').value);
        
        try {
            const response = await fetch('/Game/NewGame', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    difficulty: difficulty,
                    playerId: this.currentPlayerId
                })
            });

            const data = await response.json();
            
            if (data.success) {
                this.currentSessionId = data.sessionId;
                this.gameBoard = data.gameBoard;
                this.startTime = new Date();
                this.renderBoard();
                this.startTimer();
                this.updateUI();
                this.hideMessage();
            } else {
                alert('Error: ' + data.error);
            }
        } catch (error) {
            console.error('Error starting new game:', error);
            alert('Error starting new game');
        }
    },

    // Generate temporary session ID (fallback)
    generateSessionId: function() {
        return Date.now().toString() + Math.random().toString(36).substr(2, 9);
    },

    // Render game board
    renderBoard: function() {
        if (!this.gameBoard) return;

        const boardElement = document.getElementById('gameBoard');
        boardElement.innerHTML = '';
        
        // Set grid template
        boardElement.style.gridTemplateColumns = `repeat(${this.gameBoard.width}, 1fr)`;
        boardElement.style.gridTemplateRows = `repeat(${this.gameBoard.height}, 1fr)`;

        // Create cells
        for (let y = 0; y < this.gameBoard.height; y++) {
            for (let x = 0; x < this.gameBoard.width; x++) {
                const cell = this.gameBoard.cells[x][y];
                const cellElement = document.createElement('div');
                
                cellElement.className = this.getCellClasses(cell);
                cellElement.textContent = this.getCellDisplay(cell);
                cellElement.dataset.x = x;
                cellElement.dataset.y = y;
                
                // Add event listeners
                cellElement.addEventListener('click', (e) => this.handleCellClick(e, x, y));
                cellElement.addEventListener('contextmenu', (e) => this.handleCellRightClick(e, x, y));
                
                boardElement.appendChild(cellElement);
            }
        }
    },

    // Get CSS classes for cell
    getCellClasses: function(cell) {
        const classes = ['cell'];
        
        switch (cell.state) {
            case 0: // Hidden
                classes.push('hidden');
                break;
            case 1: // Revealed
                classes.push('revealed');
                if (cell.isMine) {
                    classes.push('mine');
                } else if (cell.adjacentMines > 0) {
                    classes.push(`number-${cell.adjacentMines}`);
                }
                break;
            case 2: // Flagged
                classes.push('flagged');
                break;
        }
        
        return classes.join(' ');
    },

    // Get display text for cell
    getCellDisplay: function(cell) {
        switch (cell.state) {
            case 0: // Hidden
                return '';
            case 1: // Revealed
                if (cell.isMine) {
                    return '💣';
                } else if (cell.adjacentMines > 0) {
                    return cell.adjacentMines.toString();
                }
                return '';
            case 2: // Flagged
                return '🚩';
            default:
                return '';
        }
    },

    // Handle cell left click
    handleCellClick: async function(e, x, y) {
        e.preventDefault();
        
        if (!this.currentSessionId || this.gameBoard.status !== 0) return; // 0 = InProgress

        try {
            const response = await fetch('/Game/RevealCell', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    sessionId: this.currentSessionId,
                    x: x,
                    y: y
                })
            });

            const data = await response.json();
            
            if (data.success) {
                this.gameBoard = data.gameBoard;
                this.renderBoard();
                this.updateUI();
                this.checkGameEnd();
            }
        } catch (error) {
            console.error('Error revealing cell:', error);
        }
    },

    // Handle cell right click (flag)
    handleCellRightClick: async function(e, x, y) {
        e.preventDefault();
        
        if (!this.currentSessionId || this.gameBoard.status !== 0) return; // 0 = InProgress

        try {
            const response = await fetch('/Game/ToggleFlag', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    sessionId: this.currentSessionId,
                    x: x,
                    y: y
                })
            });

            const data = await response.json();
            
            if (data.success) {
                this.gameBoard = data.gameBoard;
                this.renderBoard();
                this.updateUI();
            }
        } catch (error) {
            console.error('Error toggling flag:', error);
        }
    },

    // Update UI elements
    updateUI: function() {
        if (!this.gameBoard) {
            document.getElementById('mineCount').textContent = '0';
            document.getElementById('gameStatus').textContent = 'Ready';
            return;
        }

        // Update mine count
        const remainingFlags = this.gameBoard.mineCount - this.gameBoard.flagsUsed;
        document.getElementById('mineCount').textContent = remainingFlags;

        // Update game status
        const statusElement = document.getElementById('gameStatus');
        const gameContainer = document.querySelector('.game-container');
        
        gameContainer.classList.remove('game-won', 'game-lost');
        
        switch (this.gameBoard.status) {
            case 0: // InProgress
                statusElement.textContent = 'Playing';
                break;
            case 1: // Won
                statusElement.textContent = 'Won';
                gameContainer.classList.add('game-won');
                break;
            case 2: // Lost
                statusElement.textContent = 'Lost';
                gameContainer.classList.add('game-lost');
                break;
        }
    },

    // Start game timer
    startTimer: function() {
        if (this.timer) {
            clearInterval(this.timer);
        }

        this.timer = setInterval(() => {
            if (this.startTime && this.gameBoard && this.gameBoard.status === 0) {
                const elapsed = Math.floor((Date.now() - this.startTime.getTime()) / 1000);
                const minutes = Math.floor(elapsed / 60);
                const seconds = elapsed % 60;
                document.getElementById('timer').textContent = `${minutes}:${seconds.toString().padStart(2, '0')}`;
            }
        }, 1000);
    },

    // Stop game timer
    stopTimer: function() {
        if (this.timer) {
            clearInterval(this.timer);
            this.timer = null;
        }
    },

    // Check if game ended
    checkGameEnd: function() {
        if (this.gameBoard.status !== 0) { // Not InProgress
            this.stopTimer();
            
            if (this.gameBoard.status === 1) { // Won
                this.showMessage('Congratulations! You won!', 'success');
                this.saveGameResult();
            } else if (this.gameBoard.status === 2) { // Lost
                this.showMessage('Game Over! You hit a mine.', 'danger');
            }
        }
    },

    // Save game result
    saveGameResult: async function() {
        if (!this.currentPlayerId) return;

        try {
            const response = await fetch('/Game/SaveResult', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    sessionId: this.currentSessionId
                })
            });

            const data = await response.json();
            
            if (data.success && data.result) {
                console.log('Game result saved:', data.result);
            }
        } catch (error) {
            console.error('Error saving game result:', error);
        }
    },

    // Get hint from solver
    getHint: async function() {
        if (!this.currentSessionId || this.gameBoard.status !== 0) return;

        try {
            const response = await fetch('/Game/GetHint', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    sessionId: this.currentSessionId
                })
            });

            const data = await response.json();
            
            if (data.success && data.nextMove) {
                this.highlightHint(data.nextMove);
                this.showMessage(`Hint: ${data.nextMove.reason}`, 'info');
            } else {
                this.showMessage('No hint available', 'info');
            }
        } catch (error) {
            console.error('Error getting hint:', error);
        }
    },

    // Highlight hint cell
    highlightHint: function(move) {
        const cellElement = document.querySelector(`[data-x="${move.x}"][data-y="${move.y}"]`);
        if (cellElement) {
            cellElement.classList.add('hint');
            setTimeout(() => {
                cellElement.classList.remove('hint');
            }, 3000);
        }
    },

    // Auto solve game
    autoSolve: async function() {
        if (!this.currentSessionId || this.gameBoard.status !== 0) return;

        this.showMessage('Solving game...', 'info');

        try {
            const response = await fetch('/Game/SolveGame', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    sessionId: this.currentSessionId
                })
            });

            const data = await response.json();
            
            if (data.success) {
                this.showSolverResults(data.solverResult);
            } else {
                this.showMessage('Error solving game', 'danger');
            }
        } catch (error) {
            console.error('Error auto-solving:', error);
            this.showMessage('Error solving game', 'danger');
        }
    },

    // Show solver results modal
    showSolverResults: function(solverResult) {
        const modal = document.getElementById('solverModal');
        const resultsContainer = document.getElementById('solverResults');
        
        let html = `
            <p><strong>Success:</strong> ${solverResult.success ? 'Yes' : 'No'}</p>
            <p><strong>Solution Time:</strong> ${this.formatTimeSpan(solverResult.solutionTime)}</p>
            <p><strong>Strategy:</strong> ${solverResult.strategy || 'Basic solving'}</p>
            <p><strong>Total Moves:</strong> ${solverResult.moves.length}</p>
        `;

        if (solverResult.moves.length > 0) {
            html += '<h4>Move Details:</h4><div style="max-height: 200px; overflow-y: auto;">';
            solverResult.moves.forEach((move, index) => {
                const moveType = move.type === 0 ? 'Reveal' : 'Flag';
                html += `<p><strong>${index + 1}.</strong> ${moveType} (${move.x}, ${move.y}) - ${move.reason}</p>`;
            });
            html += '</div>';
        }

        resultsContainer.innerHTML = html;
        modal.style.display = 'flex';
    },

    // Hide solver modal
    hideSolverModal: function() {
        document.getElementById('solverModal').style.display = 'none';
    },

    // Format TimeSpan for display
    formatTimeSpan: function(timeSpan) {
        // Assuming timeSpan is in the format "00:00:00.0000000"
        if (typeof timeSpan === 'string') {
            const parts = timeSpan.split(':');
            if (parts.length >= 3) {
                const minutes = parseInt(parts[1]);
                const seconds = Math.floor(parseFloat(parts[2]));
                return `${minutes}:${seconds.toString().padStart(2, '0')}`;
            }
        }
        return timeSpan.toString();
    },

    // Show message
    showMessage: function(message, type = 'info') {
        const container = document.getElementById('messageContainer');
        container.className = `alert alert-${type}`;
        container.textContent = message;
        container.style.display = 'block';
    },

    // Hide message
    hideMessage: function() {
        document.getElementById('messageContainer').style.display = 'none';
    }
};