using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
        private MouseState _previousMouseState;
        private SpriteFont _DefaultFont;

        private Texture2D blankTexture;
        private Texture2D chessPieceSpritesheet;

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
            4, 2, 3, 5, 6, 3, 2, 4, // Rooks, Knights, Bishops, Queen, King
            1, 1, 1, 1, 1, 1, 1, 1, // Pawns
            0, 0, 0, 0, 0, 0, 0, 0, // Empty tiles
            0, 0, 0, 0, 0, 0, 0, 0, // Empty tiles
            0, 0, 0, 0, 0, 0, 0, 0, // Empty tiles
            0, 0, 0, 0, 0, 0, 0, 0, // Empty tiles
            1, 1, 1, 1, 1, 1, 1, 1, // Pawns
            4, 2, 3, 5, 6, 3, 2, 4, // Rooks, Knights, Bishops, Queen, King
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

            int resetbuttonWidth = 300;
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
                    ChessTeam sourceArchiveTeam = chessBoard[selectedX, selectedY]._chessTeam;
                    ChessPieceType sourceArchiveType = chessBoard[selectedX, selectedY]._chessPieceType;
                    ChessTeam targetArchiveTeam = chessBoard[hoveredX, hoveredY]._chessTeam;
                    ChessPieceType targetArchiveType = chessBoard[hoveredX, selectedY]._chessPieceType;
                    ref ChessTile sourceTile = ref chessBoard[selectedX, selectedY];
                    ref ChessTile targetTile = ref chessBoard[hoveredX, hoveredY];
                    targetTile._chessPieceType = sourceTile._chessPieceType;
                    targetTile._chessTeam = sourceTile._chessTeam;
                    sourceTile._chessPieceType = ChessPieceType.None;
                    sourceTile._chessTeam = ChessTeam.none;

                    if (targetTile.CheckPromotion(hoveredX))
                    {
                        targetTile._chessPieceType = ChessPieceType.Queen;
                    }

                    HashSet<(int, int)> CurrentTurnsTerritory = ChessTile.GetTerritory(chessBoard, chessboardSize, currentTurn);

                    ChessTile.IsKingInCheck(chessBoard, chessboardSize, ChessTeam.white, out bool IsOpposingKingInCheck, in CurrentTurnsTerritory);

                    if (IsOpposingKingInCheck)
                    {
                        gameState = isWhiteTurn ? GameState.BlackInCheck : GameState.WhiteInCheck;
                    }
                    else
                    {
                        gameState = GameState.Playing;
                    }

                    bool InCheckmate = true;
                    for (int pieceCheckingX = 0; pieceCheckingX < chessboardSize; pieceCheckingX++)
                    {
                        for (int pieceCheckingY = 0; pieceCheckingY < chessboardSize; pieceCheckingY++)
                        {
                            var tile = chessBoard[pieceCheckingX, pieceCheckingY];

                            if (tile._chessTeam != currentTurn)
                                continue;

                            //for (int i = 0; i < PossibleMoves.Count; i++)
                            foreach (var index in tile.GetMovePositions(chessBoard, (pieceCheckingX, pieceCheckingY)))
                            {
                                ChessTile[,] chessBoardArchive = new ChessTile[chessboardSize, chessboardSize];
                                for (int x = 0; x < chessboardSize; x++)
                                {
                                    for (int y = 0; y < chessboardSize; y++)
                                    {
                                        chessBoardArchive[x, y] = new ChessTile(chessBoard[x, y]._chessPieceType, chessBoard[x, y]._chessTeam);
                                    }
                                }
                                ref ChessTile sourceTile1 = ref chessBoardArchive[selectedX, selectedY];
                                ref ChessTile targetTile1 = ref chessBoardArchive[index.Item1, index.Item2];
                                targetTile1._chessPieceType = sourceTile1._chessPieceType;
                                targetTile1._chessTeam = sourceTile1._chessTeam;
                                sourceTile1._chessPieceType = ChessPieceType.None;
                                sourceTile1._chessTeam = ChessTeam.none;

                                HashSet<(int, int)> CurrentTurnsTerritorySim = ChessTile.GetTerritory(chessBoard, chessboardSize, currentTurn);

                                ChessTile.IsKingInCheck(chessBoard, chessboardSize, ChessTeam.white, out bool IsOpposingKingInCheckSim, in CurrentTurnsTerritorySim);
                                if (!(IsOpposingKingInCheckSim))
                                {
                                    InCheckmate = false;
                                    break;
                                }
                            }
                        }
                    }
                    if (InCheckmate)
                    {
                        if (isBlackTurn)
                        {
                            gameState = GameState.WhiteInCheckmate;
                        }
                        else
                        {
                            gameState = GameState.BlackInCheckmate;
                        }
                    }

                    isWhiteTurn = !isWhiteTurn;

                }

                selectedX = hoveredX;
                selectedY = hoveredY;

                if (chessBoard[selectedX, selectedY]._chessPieceType != ChessPieceType.None)
                {
                    vacantPositions = chessBoard[selectedX, selectedY].GetMovePositions(chessBoard, (selectedX, selectedY));

                    for (int i = 0; i < vacantPositions.Count; i++)
                    {
                        var index = vacantPositions[i];
                        ChessTile[,] chessBoardArchive = new ChessTile[chessboardSize, chessboardSize];
                        for (int x = 0; x < chessboardSize; x++)
                        {
                            for (int y = 0; y < chessboardSize; y++)
                            {
                                chessBoardArchive[x, y] = new ChessTile(chessBoard[x, y]._chessPieceType, chessBoard[x, y]._chessTeam);
                            }
                        }
                        ref ChessTile sourceTile = ref chessBoardArchive[selectedX, selectedY];
                        ref ChessTile targetTile = ref chessBoardArchive[index.Item1, index.Item2];
                        targetTile._chessPieceType = sourceTile._chessPieceType;
                        targetTile._chessTeam = sourceTile._chessTeam;
                        sourceTile._chessPieceType = ChessPieceType.None;
                        sourceTile._chessTeam = ChessTeam.none;

                        HashSet<(int, int)> WhiteTerritory1 = ChessTile.GetTerritory(chessBoardArchive, chessboardSize, ChessTeam.white);
                        HashSet<(int, int)> BlackTerritory1 = ChessTile.GetTerritory(chessBoardArchive, chessboardSize, ChessTeam.black);

                        ChessTile.IsKingInCheck(chessBoardArchive, chessboardSize, ChessTeam.white, out bool IsBlackInCheck1, in WhiteTerritory1);
                        ChessTile.IsKingInCheck(chessBoardArchive, chessboardSize, ChessTeam.black, out bool IsWhiteInCheck1, in BlackTerritory1);
                        if ((isWhiteTurn && IsWhiteInCheck1) || (isBlackTurn && IsBlackInCheck1))
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
            if ((selectedX != -1 && selectedY != -1) &&
                (
                isWhiteTurn && chessBoard[selectedX, selectedY]._chessTeam != ChessTeam.white ||
                isBlackTurn && chessBoard[selectedX, selectedY]._chessTeam != ChessTeam.black
                )
                )
            {
                selectedX = -1;
                selectedY = -1;
                vacantPositions.Clear();
            }

            
            // TODO: Add your update logic here
            _previousMouseState = _mouseState;
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            _spriteBatch.Begin();
            for (int x = 0; x < chessboardSize; x++)
            {
                for (int y = 0; y < chessboardSize; y++)
                {
                    Color color =
                        vacantPositions.Contains((x, y)) ? Color.Red :
                        (selectedX == x && selectedY == y) ? Color.Blue :
                        boardColors[(x + y) % 2];
                    Rectangle rectangle = new Rectangle(
                        tileDrawSize * x,
                        tileDrawSize * y,
                        tileDrawSize,
                        tileDrawSize
                    );
                    Rectangle sourceRectangle = new Rectangle(
                        ((int)chessBoard[x, y]._chessPieceType - 1) * 210,
                        (int)chessBoard[x, y]._chessTeam * 215,
                        210,
                        215
                    );
                    _spriteBatch.Draw(
                        blankTexture,
                        rectangle,
                        color
                        );
                    if (chessBoard[x, y]._chessPieceType != ChessPieceType.None)
                        _spriteBatch.Draw(
                            chessPieceSpritesheet,
                            rectangle,
                            sourceRectangle,
                            Color.White
                            );
                }
            }
            var drawPosition = new Vector2(10, _resetButtonTextPosition.Y);
            string drawString = drawString = isWhiteTurn ? "White's Turn" : "Black's Turn";
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
