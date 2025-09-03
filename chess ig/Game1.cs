using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security.AccessControl;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace chess_ig
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private MouseState _mouseState;
        private KeyboardState _keyboardState;
        private MouseState _previousMouseState;
        private KeyboardState _previousKeyboardState;
        private SpriteFont _DefaultFont;

        private Texture2D blankTexture;
        private Texture2D chessPieceSpritesheet;

        List<(bool castle, ((ChessPieceType ChessPieceType, ChessTeam ChessTeam, bool hasMoved) TileInfo, (int, int) Index) source, ((ChessPieceType ChessPieceType, ChessTeam ChessTeam, bool hasMoved) TileInfo, (int, int) Index) destination)> redoLog = new();
        List<(bool castle, ((ChessPieceType ChessPieceType, ChessTeam ChessTeam, bool hasMoved) TileInfo, (int, int) Index) source, ((ChessPieceType ChessPieceType, ChessTeam ChessTeam, bool hasMoved) TileInfo, (int, int) Index) destination)> undoLog = new();

        private Color[] boardColors = new Color[] { Color.White, Color.Black };
        private int tileDrawSize = 80;

        Rectangle _resetButton;
        Vector2 _resetButtonTextPosition;
        string _resetButtonText = "Reset Game";

        private int selectedX = -1;
        private int selectedY = -1;
        private int hoveredX;
        private int hoveredY;
        private List<(int, int)> vacantPositions = new List<(int, int)>();

        private bool isWhiteTurn = true;
        private bool isBlackTurn => !isWhiteTurn;
        private ChessTeam currentTurn => isWhiteTurn ? ChessTeam.white : ChessTeam.black;
        private ChessTeam opponentTurn => isWhiteTurn ? ChessTeam.black : ChessTeam.white;

        GameState gameState = GameState.Playing;
        enum GameState
        {
            Playing,
            BlackInCheck,
            WhiteInCheck,
            BlackInCheckmate,
            WhiteInCheckmate,
            Stalemate
        }

        private ChessTile[,] chessBoard;
        private int chessboardSize = 8;
        private int[] chessboardInfo = new int[]{
            4, 2, 3, 6, 5, 3, 2, 4, // Rooks, Knights, Bishops, Queen, King
            1, 1, 1, 1, 1, 1, 1, 1, // Pawns
            0, 0, 0, 0, 0, 0, 0, 0, // Empty tiles
            0, 0, 0, 0, 0, 0, 0, 0, // Empty tiles
            0, 0, 0, 0, 0, 0, 0, 0, // Empty tiles
            0, 0, 0, 0, 0, 0, 0, 0, // Empty tiles
            1, 1, 1, 1, 1, 1, 1, 1, // Pawns
            4, 2, 3, 6, 5, 3, 2, 4, // Rooks, Knights, Bishops, Queen, King
            };
        private int[] teamInfo = new int[]{
            0, 0, 0, 0, 0, 0, 0, 0, // Rooks, Knights, Bishops, Queen, King
            0, 0, 0, 0, 0, 0, 0, 0, // Pawns
            2, 2, 2, 2, 2, 2, 2, 2, // Empty tiles
            2, 2, 2, 2, 2, 2, 2, 2, // Empty tiles
            2, 2, 2, 2, 2, 2, 2, 2, // Empty tiles
            2, 2, 2, 2, 2, 2, 2, 2, // Empty tiles
            1, 1, 1, 1, 1, 1, 1, 1, // Pawns
            1, 1, 1, 1, 1, 1, 1, 1, // Rooks, Knights, Bishops, Queen, King
            };
        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }
        private void ResetBoard(ref ChessTile[,] chessBoard)
        {
            for (int x = 0; x < chessboardSize; x++)
            {
                for (int y = 0; y < chessboardSize; y++)
                {
                    int index = x * chessboardSize + y;
                    ChessPieceType pieceType = (ChessPieceType)chessboardInfo[index];
                    ChessTeam tileType = (ChessTeam)teamInfo[index];
                    chessBoard[x, y] = new ChessTile(pieceType, tileType);
                }
            }
        }
        protected override void Initialize()
        {
            chessBoard = new ChessTile[chessboardSize, chessboardSize];
            ResetBoard(ref chessBoard);

            _graphics.PreferredBackBufferWidth = 640;
            _graphics.PreferredBackBufferHeight = 720;
            _graphics.ApplyChanges();

            int resetbuttonWidth = tileDrawSize * 4;
            _resetButton = new Rectangle(
                tileDrawSize * chessboardSize - resetbuttonWidth,
                tileDrawSize * chessboardSize,
                resetbuttonWidth,
                tileDrawSize
            );



            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            blankTexture = new Texture2D(GraphicsDevice, 1, 1);
            blankTexture.SetData(new[] { Color.White });

            chessPieceSpritesheet = Content.Load<Texture2D>("chessPieces");
            _DefaultFont = Content.Load<SpriteFont>("DefaultFont");

            // Should in initialize, but here since it needs the content loaded
            Vector2 resetButtonTextSize = _DefaultFont.MeasureString(_resetButtonText);
            int PaddingWidth = (_resetButton.Width - (int)resetButtonTextSize.X) / 2;
            int PaddingHeight = (_resetButton.Height - (int)resetButtonTextSize.Y) / 2;

            _resetButtonTextPosition = new Vector2(
                _resetButton.X + PaddingWidth,
                _resetButton.Y + PaddingHeight
            );
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();
            _mouseState = Mouse.GetState();
            _keyboardState = Keyboard.GetState();

            int mouseX = _mouseState.X / tileDrawSize;
            int mouseY = _mouseState.Y / tileDrawSize;
            if (mouseX >= 0 && mouseX < chessboardSize && mouseY >= 0 && mouseY < chessboardSize)
            {
                hoveredX = mouseX;
                hoveredY = mouseY;
            }

            if (_previousMouseState.LeftButton == ButtonState.Released && _mouseState.LeftButton == ButtonState.Pressed)
            {
                if (_resetButton.Contains(_mouseState.Position))
                {
                    selectedX = -1;
                    selectedY = -1;
                    vacantPositions.Clear();
                    isWhiteTurn = true;
                    gameState = GameState.Playing;
                    ResetBoard(ref chessBoard);
                    return;
                }
                if (vacantPositions.Contains((hoveredX, hoveredY)))
                {
                    ref ChessTile sourceTile = ref chessBoard[selectedX, selectedY];
                    ref ChessTile targetTile = ref chessBoard[hoveredX, hoveredY];
                    undoLog.Add((
                        false,
                        ((sourceTile.ChessPieceType, sourceTile.ChessTeam, sourceTile.HasMoved), (selectedX, selectedY)),
                        ((targetTile.ChessPieceType, targetTile.ChessTeam, targetTile.HasMoved), (hoveredX, hoveredY))
                        ));
                    redoLog.Clear();
                    sourceTile.TransferTo(ref targetTile);

                    int moveDistanceY = hoveredY - selectedY;
                    if (targetTile.ChessPieceType == ChessPieceType.King && Math.Abs(moveDistanceY) > 1)
                    {

                        Debug.WriteLine("Castling move detected");

                        if (moveDistanceY > 0)
                        {
                            ref ChessTile rookTile = ref chessBoard[hoveredX, 7];
                            ref ChessTile DestinationTile = ref chessBoard[hoveredX, 5];
                            undoLog.Add((
                                true,
                                ((rookTile.ChessPieceType, rookTile.ChessTeam, sourceTile.HasMoved), (hoveredX, 7)),
                                ((DestinationTile.ChessPieceType, DestinationTile.ChessTeam, targetTile.HasMoved), (hoveredX, 5))
                                ));
                            chessBoard[hoveredX, 7].TransferTo(ref chessBoard[hoveredX, 5]);
                        }
                        else if (moveDistanceY < 0)
                        {
                            ref ChessTile rookTile = ref chessBoard[hoveredX, 0];
                            ref ChessTile DestinationTile = ref chessBoard[hoveredX, 3];
                            undoLog.Add((
                                true,
                                ((rookTile.ChessPieceType, rookTile.ChessTeam, sourceTile.HasMoved), (hoveredX, 0)),
                                ((DestinationTile.ChessPieceType, DestinationTile.ChessTeam, targetTile.HasMoved), (hoveredX, 3))
                                ));
                            chessBoard[hoveredX, 0].TransferTo(ref chessBoard[hoveredX, 3]);
                        }
                    }

                    if (targetTile.CheckPromotion(hoveredX))
                    {
                        targetTile.ChessPieceType = ChessPieceType.Queen;
                    }

                    HashSet<(int, int)> CurrentTurnsTerritory = ChessTile.GetTerritory(chessBoard, chessboardSize, currentTurn);

                    ChessTile.IsKingInCheck(chessBoard, opponentTurn, out bool IsOpposingKingInCheck, in CurrentTurnsTerritory);

                    

                    bool IsNoPossibleMoves = true;
                    for (int pieceCheckingX = 0; pieceCheckingX < chessboardSize; pieceCheckingX++)
                    {
                        for (int pieceCheckingY = 0; pieceCheckingY < chessboardSize; pieceCheckingY++)
                        {
                            var tile = chessBoard[pieceCheckingX, pieceCheckingY];

                            if (tile.ChessTeam != opponentTurn)
                                continue;

                            //for (int i = 0; i < PossibleMoves.Count; i++)
                            foreach (var index in tile.GetMovePositions(chessBoard, (pieceCheckingX, pieceCheckingY)))
                            {
                                //ChessTile[,] chessBoardArchive = ChessTile.SimulateMove(chessBoard, chessboardSize, (pieceCheckingX, pieceCheckingY), (index.Item1, index.Item2));
                                ChessTile[,] chessBoardArchive = new ChessTile[chessboardSize, chessboardSize];
                                for (int x = 0; x < chessboardSize; x++)
                                {
                                    for (int y = 0; y < chessboardSize; y++)
                                    {
                                        chessBoardArchive[x, y] = new ChessTile(chessBoard[x, y].ChessPieceType, chessBoard[x, y].ChessTeam);
                                    }
                                }
                                chessBoardArchive[pieceCheckingX, pieceCheckingY].TransferTo(ref chessBoardArchive[index.Item1, index.Item2]);

                                HashSet<(int, int)> CurrentTurnsTerritorySim = ChessTile.GetTerritory(chessBoardArchive, chessboardSize, currentTurn);

                                ChessTile.IsKingInCheck(chessBoardArchive, opponentTurn, out bool IsOpposingKingInCheckSim, in CurrentTurnsTerritorySim);
                                if (!(IsOpposingKingInCheckSim))
                                {
                                    IsNoPossibleMoves = false;
                                    break;
                                }
                            }
                        }
                    }
                    
                    if (IsNoPossibleMoves)
                    {
                        if (IsOpposingKingInCheck)
                        {
                            gameState = isWhiteTurn ? GameState.BlackInCheckmate : GameState.WhiteInCheckmate;
                        }
                        else
                        {
                            gameState = GameState.Stalemate;
                        }
                    }
                    else if (IsOpposingKingInCheck)
                    {
                        gameState = isWhiteTurn ? GameState.BlackInCheck : GameState.WhiteInCheck;
                    }
                    else
                    {
                        gameState = GameState.Playing;
                    }

                        isWhiteTurn = !isWhiteTurn;

                }

                selectedX = hoveredX;
                selectedY = hoveredY;

                if (chessBoard[selectedX, selectedY].ChessPieceType != ChessPieceType.None && chessBoard[selectedX, selectedY].ChessTeam == currentTurn)
                {
                    vacantPositions = chessBoard[selectedX, selectedY].GetMovePositions(chessBoard, (selectedX, selectedY));

                    ChessTile[,] chessBoardArchive = new ChessTile[chessboardSize, chessboardSize];

                    for (int i = 0; i < vacantPositions.Count; i++)
                    {
                        var index = vacantPositions[i];
                        for (int x = 0; x < chessboardSize; x++)
                        {
                            for (int y = 0; y < chessboardSize; y++)
                            {
                                chessBoardArchive[x, y] = new ChessTile(chessBoard[x, y].ChessPieceType, chessBoard[x, y].ChessTeam);
                            }
                        }

                        chessBoardArchive[selectedX, selectedY].TransferTo(ref chessBoardArchive[index.Item1, index.Item2]);

                        HashSet<(int, int)> opponentTerritory = ChessTile.GetTerritory(chessBoardArchive, chessboardSize, opponentTurn);
                        ChessTile.IsKingInCheck(chessBoardArchive, currentTurn, out bool yourKingInCheck, in opponentTerritory);

                        if (yourKingInCheck)
                        {
                            vacantPositions.RemoveAt(i);
                            i--;
                        }
                    }
                }
                else
                {
                    vacantPositions.Clear();
                }
            }

            if (_mouseState.RightButton == ButtonState.Pressed && _previousMouseState.RightButton == ButtonState.Released)
            {
                selectedX = -1;
                selectedY = -1;
                vacantPositions.Clear();
            }

            if (_keyboardState.IsKeyDown(Keys.Left) && _previousKeyboardState.IsKeyUp(Keys.Left) && undoLog.Count > 0)
            {
                vacantPositions.Clear();

                int repeat = undoLog[^1].castle ? 2 : 1;

                for (int i = 0; i < repeat; i++)
                {
                    int index = undoLog.Count - 1;
                    var moveToUndo = undoLog[index];

                    undoLog.RemoveAt(index);

                    var sourceIndex = moveToUndo.source.Index;
                    ref var sourceTile = ref chessBoard[sourceIndex.Item1, sourceIndex.Item2];
                    sourceTile.ChessTeam = moveToUndo.source.TileInfo.ChessTeam;
                    sourceTile.ChessPieceType = moveToUndo.source.TileInfo.ChessPieceType;
                    sourceTile.HasMoved = moveToUndo.source.TileInfo.hasMoved;

                    var destinationIndex = moveToUndo.destination.Index;
                    ref var destinationTile = ref chessBoard[destinationIndex.Item1, destinationIndex.Item2];
                    destinationTile.ChessTeam = moveToUndo.destination.TileInfo.ChessTeam;
                    destinationTile.ChessPieceType = moveToUndo.destination.TileInfo.ChessPieceType;
                    destinationTile.HasMoved = moveToUndo.destination.TileInfo.hasMoved;

                    moveToUndo.castle = false;
                    if (i == 1) moveToUndo.castle = true;
                    redoLog.Add(moveToUndo);
                    Debug.WriteLine($"Undoing move to {destinationIndex} from {sourceIndex}");
                }
                isWhiteTurn = !isWhiteTurn;
            }
            if (_keyboardState.IsKeyDown(Keys.Right) && _previousKeyboardState.IsKeyUp(Keys.Right) && redoLog.Count > 0)
            {
                vacantPositions.Clear();

                int repeat = redoLog[^1].castle ? 2 : 1;

                for (int i = 0; i < repeat; i++)
                {
                    int index = redoLog.Count - 1;
                    var moveToUndo = redoLog[index];

                    redoLog.RemoveAt(index);

                    var sourceIndex = moveToUndo.source.Index;
                    ref var sourceTile = ref chessBoard[sourceIndex.Item1, sourceIndex.Item2];
                    sourceTile.HasMoved = moveToUndo.source.TileInfo.hasMoved;


                    var destinationIndex = moveToUndo.destination.Index;
                    ref var destinationTile = ref chessBoard[destinationIndex.Item1, destinationIndex.Item2];
                    destinationTile.HasMoved = moveToUndo.destination.TileInfo.hasMoved;

                    sourceTile.TransferTo(ref destinationTile);

                    moveToUndo.castle = false;
                    if (i == 1) moveToUndo.castle = true;
                    undoLog.Add(moveToUndo);
                    Debug.WriteLine($"Undoing move to {destinationIndex} from {sourceIndex}");
                }
                isWhiteTurn = !isWhiteTurn;
            }
            _previousMouseState = _mouseState;
            _previousKeyboardState = _keyboardState;
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CadetBlue * 0.7f);

            _spriteBatch.Begin();
            for (int x = 0; x < chessboardSize; x++)
            {
                for (int y = 0; y < chessboardSize; y++)
                {
                    bool isOccupied = chessBoard[x, y].ChessPieceType != ChessPieceType.None;
                    Color color =
                        vacantPositions.Contains((x, y)) ? (isOccupied ? Color.Red : Color.LightSkyBlue) :
                        (selectedX == x && selectedY == y) ? Color.DeepSkyBlue :
                        boardColors[(x + y) % 2];
                    Rectangle rectangle = new Rectangle(
                        tileDrawSize * x,
                        tileDrawSize * y,
                        tileDrawSize,
                        tileDrawSize
                    );
                    Rectangle sourceRectangle = new Rectangle(
                        ((int)chessBoard[x, y].ChessPieceType - 1) * 210,
                        (int)chessBoard[x, y].ChessTeam * 215,
                        210,
                        215
                    );
                    _spriteBatch.Draw(
                        blankTexture,
                        rectangle,
                        color
                        );
                    if (isOccupied)
                        _spriteBatch.Draw(
                            chessPieceSpritesheet,
                            rectangle,
                            sourceRectangle,
                            Color.White
                            );
                }
            }
            var drawPosition = new Vector2(_resetButtonTextPosition.X - tileDrawSize * 4, _resetButtonTextPosition.Y);
            string drawString = isWhiteTurn ? "White's Turn" : "Black's Turn";
            switch (gameState)
            {
                case GameState.BlackInCheck:
                    drawString = "Black in Check";
                    break;
                case GameState.WhiteInCheck:
                    drawString = "White in Check";
                    break;
                case GameState.BlackInCheckmate:
                    drawString = "White Wins!";
                    break;
                case GameState.WhiteInCheckmate:
                    drawString = "Black Wins!";
                    break;
                case GameState.Stalemate:
                    drawString = "Stalemate";
                    break;
            }
            _spriteBatch.DrawString(_DefaultFont, drawString, drawPosition, Color.White);

            _spriteBatch.Draw(blankTexture, _resetButton, Color.Red);
            _spriteBatch.DrawString(_DefaultFont, _resetButtonText, _resetButtonTextPosition, Color.White);

            _spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
