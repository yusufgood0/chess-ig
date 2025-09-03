using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace chess_ig
{
    public enum ChessPieceType
    {
        None = 0,
        Pawn = 1,
        Knight = 2,
        Bishop = 3,
        Rook = 4,
        King = 5,
        Queen = 6,
    }
    public enum ChessTeam
    {
        none = 2,
        white = 0,
        black = 1
    }
    internal class ChessTile
    {

        public ChessPieceType ChessPieceType { get; set; }
        public ChessTeam ChessTeam { get; set; }
        public bool HasMoved = false;

        private IChessPieceInterface[] chessBehaviours = new IChessPieceInterface[]
        {
            new Pawn(),
            new Knight(),
            new Bishop(),
            new Rook(),
            new King(),
            new Queen(),
        };
        public ChessTile(ChessPieceType chessPieceType, ChessTeam typeOfChessTile, bool hasMoved = false)
        {
            ChessPieceType = chessPieceType;
            ChessTeam = typeOfChessTile;
            HasMoved = hasMoved;
        }
        public List<(int, int)> GetMovePositions(ChessTile[,] chessBoard, (int, int) position)
        {

            return chessBehaviours[(int)ChessPieceType - 1].GetMovePositions(chessBoard, position);
        }

        public static bool IsInBounds((int, int) position, int boardSizeX, int boardSizeY)
        {
            return position.Item1 >= 0 && position.Item1 < boardSizeX && position.Item2 >= 0 && position.Item2 < boardSizeY;
        }
        public static bool IsTileOpen(ChessTile currentTile, ChessTile newTile)
        {
            return newTile.ChessPieceType == ChessPieceType.None || newTile.ChessTeam != currentTile.ChessTeam;
        }
        public static bool IsMoveValid((int, int) position, int boardSizeX, int boardSizeY, ChessTile currentTile, ChessTile newTile)
        {
            return IsInBounds(position, boardSizeX, boardSizeY) && IsTileOpen(currentTile, newTile);
        }
        public bool CheckPromotion(int xPos)
        {
            if (ChessPieceType == ChessPieceType.Pawn && (ChessTeam == ChessTeam.white && xPos == 7 || ChessTeam == ChessTeam.black && xPos == 0))
            {
                return true;
            }
            return false;
        }
        public static void DirectionCheck(ref List<(int, int)> returnValues, ChessTile[,] chessBoard, (int, int) position, int xDirection, int yDirection, int boardSizeX, int boardSizeY)
        {
            int x = position.Item1;
            int y = position.Item2;
            while (true)
            {
                x += xDirection;
                y += yDirection;
                if (!ChessTile.IsInBounds((x, y), boardSizeX, boardSizeY))
                {
                    break;
                }
                ref ChessTile newTile = ref chessBoard[x, y];
                if (ChessTile.IsTileOpen(chessBoard[position.Item1, position.Item2], newTile))
                {
                    returnValues.Add((x, y));
                }
                if (newTile.ChessTeam != ChessTeam.none)
                {
                    break; // Stop if we hit a piece
                }
            }
        }
        public static HashSet<(int, int)> GetTerritory(ChessTile[,] chessBoard, int chessboardSize, ChessTeam checkingTeam)
        {
            HashSet<(int, int)> tilesInDanger = new HashSet<(int, int)>();
            for (int x = 0; x < chessboardSize; x++)
            {
                for (int y = 0; y < chessboardSize; y++)
                {
                    if (chessBoard[x, y].ChessTeam != checkingTeam)
                    {
                        continue; // Only check white pieces if checkingTeam is white
                    }
                    foreach (var index in chessBoard[x, y].GetMovePositions(chessBoard, (x, y)))
                    {
                        tilesInDanger.Add(index);
                    }
                }
            }
            return tilesInDanger;
        }
        public static void IsKingInCheck(ChessTile[,] chessBoard, ChessTeam whichKing, out bool inCheck, in HashSet<(int, int)> tilesInDanger)
        {
            inCheck = false;
            foreach (var index in tilesInDanger)
            {
                ref ChessTile tile = ref chessBoard[index.Item1, index.Item2];
                if (tile.ChessPieceType == ChessPieceType.King && tile.ChessTeam == whichKing)
                {
                    inCheck = true;
                    break;
                }
            }
        }
        public void TransferTo(ref ChessTile targetTile)
        {
            targetTile.ChessPieceType = ChessPieceType;
            targetTile.ChessTeam = ChessTeam;
            targetTile.HasMoved = true;

            ChessPieceType = ChessPieceType.None;
            ChessTeam = ChessTeam.none;
        }
        public static ChessTile[,] SimulateMove(in ChessTile[,] chessBoard, int chessboardSize, (int, int) startPos, (int, int) endPos)
        {
            ChessTile[,] chessBoardArchive = new ChessTile[chessboardSize, chessboardSize];

            for (int x = 0; x < chessboardSize; x++)
            {
                for (int y = 0; y < chessboardSize; y++)
                {
                    chessBoardArchive[x, y] = new ChessTile(chessBoard[x, y].ChessPieceType, chessBoard[x, y].ChessTeam, chessBoard[x, y].HasMoved);
                }
            }

            chessBoardArchive[startPos.Item1, startPos.Item2].TransferTo(ref chessBoardArchive[endPos.Item1, endPos.Item2]);

            return chessBoardArchive;
        }
    }
    interface IChessPieceInterface
    {
        List<(int, int)> GetMovePositions(ChessTile[,] chessBoard, (int, int) position)
        {
            return new List<(int, int)>();
        }
    }
    internal class Pawn : IChessPieceInterface
    {
        public List<(int, int)> GetMovePositions(ChessTile[,] chessBoard, (int, int) position)
        {
            List<(int, int)> returnValues = new List<(int, int)>();
            ref ChessTile currentTile = ref chessBoard[position.Item1, position.Item2];

            int boardSizeX = chessBoard.GetLength(0);
            int boardSizeY = chessBoard.GetLength(1);
            int xChecking;
            int yChecking;
            bool isWhite = currentTile.ChessTeam == ChessTeam.white;
            ChessTeam oppositeTeam = isWhite ? ChessTeam.black : ChessTeam.white;
            yChecking = position.Item2;

            xChecking = position.Item1 + (isWhite ? 1 : -1);
            if (ChessTile.IsInBounds((xChecking, yChecking), boardSizeX, boardSizeY) && chessBoard[xChecking, position.Item2].ChessTeam == ChessTeam.none)
            {
                returnValues.Add((xChecking, yChecking));
                xChecking = position.Item1 + (isWhite ? 2 : -2);

                if (ChessTile.IsInBounds((xChecking, yChecking), boardSizeX, boardSizeY) && !currentTile.HasMoved && chessBoard[xChecking, position.Item2].ChessTeam == ChessTeam.none)
                {
                    returnValues.Add((xChecking, yChecking));
                }
            }

            xChecking = position.Item1 + (isWhite ? 1 : -1);
            yChecking = position.Item2 + 1;
            if (ChessTile.IsInBounds((xChecking, yChecking), boardSizeX, boardSizeY) && chessBoard[xChecking, yChecking].ChessTeam == oppositeTeam)
            {
                returnValues.Add((xChecking, yChecking));
            }

            yChecking = position.Item2 - 1;
            if (ChessTile.IsInBounds((xChecking, yChecking), boardSizeX, boardSizeY) && chessBoard[xChecking, yChecking].ChessTeam == oppositeTeam)
            {
                returnValues.Add((xChecking, yChecking));
            }

            return returnValues;
        }
    }
    internal class Knight : IChessPieceInterface
    {
        (int, int)[] point = new (int, int)[8]
        {
            (2, 1), (1, 2), (-1, 2), (-2, 1),
            (-2, -1), (-1, -2), (1, -2), (2, -1)
        };
        public List<(int, int)> GetMovePositions(ChessTile[,] chessBoard, (int, int) position)
        {
            // A knight can move in an "L" shape: two squares in one direction and then one square perpendicular, or one square in one direction and then two squares perpendicular.
            List<(int, int)> returnValues = new List<(int, int)>();

            int boardSizeX = chessBoard.GetLength(0);
            int boardSizeY = chessBoard.GetLength(1);

            ref ChessTile currentTile = ref chessBoard[position.Item1, position.Item2];

            foreach (var direction in point)
            {
                int x = position.Item1 + direction.Item1;
                int y = position.Item2 + direction.Item2;

                if (ChessTile.IsInBounds((x, y), boardSizeX, boardSizeY))
                {
                    ref ChessTile newTile = ref chessBoard[x, y];
                    if (ChessTile.IsTileOpen(currentTile, newTile))
                    {
                        returnValues.Add((x, y));
                    }
                }
            }
            return returnValues;
        }
    }
    internal class Bishop : IChessPieceInterface
    {
        int[] ints = new int[] { 1, -1 };

        public List<(int, int)> GetMovePositions(ChessTile[,] chessBoard, (int, int) position)
        {
            // A knight can move in an "L" shape: two squares in one direction and then one square perpendicular, or one square in one direction and then two squares perpendicular.
            List<(int, int)> returnValues = new List<(int, int)>();

            int boardSizeX = chessBoard.GetLength(0);
            int boardSizeY = chessBoard.GetLength(1);

            foreach (int xDirection in ints)
            {
                foreach (int yDirection in ints)
                {
                    ChessTile.DirectionCheck(ref returnValues, chessBoard, position, xDirection, yDirection, boardSizeX, boardSizeY);
                }
            }

            return returnValues;
        }
    }
    internal class Rook : IChessPieceInterface
    {
        int[] ints = new int[] { 1, -1 };

        public List<(int, int)> GetMovePositions(ChessTile[,] chessBoard, (int, int) position)
        {
            // A knight can move in an "L" shape: two squares in one direction and then one square perpendicular, or one square in one direction and then two squares perpendicular.
            List<(int, int)> returnValues = new List<(int, int)>();

            int boardSizeX = chessBoard.GetLength(0);
            int boardSizeY = chessBoard.GetLength(1);

            foreach (int xDirection in ints)
            {
                ChessTile.DirectionCheck(ref returnValues, chessBoard, position, xDirection, 0, boardSizeX, boardSizeY);
            }
            foreach (int yDirection in ints)
            {
                ChessTile.DirectionCheck(ref returnValues, chessBoard, position, 0, yDirection, boardSizeX, boardSizeY);
            }
            return returnValues;
        }
    }
    internal class Queen : IChessPieceInterface
    {
        int[] ints = new int[] { 1, -1 };
        public List<(int, int)> GetMovePositions(ChessTile[,] chessBoard, (int, int) position)
        {
            int boardSizeX = chessBoard.GetLength(0);
            int boardSizeY = chessBoard.GetLength(1);
            // A knight can move in an "L" shape: two squares in one direction and then one square perpendicular, or one square in one direction and then two squares perpendicular.
            List<(int, int)> returnValues = new List<(int, int)>();
            foreach (int xDirection in ints)
            {
                ChessTile.DirectionCheck(ref returnValues, chessBoard, position, xDirection, 0, boardSizeX, boardSizeY);
            }
            foreach (int yDirection in ints)
            {
                ChessTile.DirectionCheck(ref returnValues, chessBoard, position, 0, yDirection, boardSizeX, boardSizeY);
            }
            foreach (int xDirection in ints)
            {
                foreach (int yDirection in ints)
                {
                    ChessTile.DirectionCheck(ref returnValues, chessBoard, position, xDirection, yDirection, boardSizeX, boardSizeY);
                }
            }
            return returnValues;
        }
    }
    internal class King : IChessPieceInterface
    {
        (int x, int y)[] rookStartPos = new (int x, int y)[4]
        {
            (0,0), (0,7), // White Rooks
            (7,0), (7,7)  // Black Rooks
        };
        public List<(int, int)> GetMovePositions(ChessTile[,] chessBoard, (int, int) position)
        {
            // A knight can move in an "L" shape: two squares in one direction and then one square perpendicular, or one square in one direction and then two squares perpendicular.
            List<(int, int)> returnValues = new List<(int, int)>();
            ref var currentTile = ref chessBoard[position.Item1, position.Item2];

            int boardSizeX = chessBoard.GetLength(0);
            int boardSizeY = chessBoard.GetLength(1);

            int clampMinX = Math.Max(position.Item1 - 1, 0);
            int clampMinY = Math.Max(position.Item2 - 1, 0);

            int clampMaxX = Math.Min(position.Item1 + 1, boardSizeX - 1);
            int clampMaxY = Math.Min(position.Item2 + 1, boardSizeY - 1);


            if (!currentTile.HasMoved)
            {
                // Check for castling
                List<(int x, int y)> viableRooks = new List<(int x, int y)>(2);
                int CheckIndexX = position.Item1;
                foreach (var (x, y) in rookStartPos)
                {
                    ref ChessTile chessTile = ref chessBoard[x, y];
                    if (chessTile.ChessTeam == currentTile.ChessTeam &&
                        !chessTile.HasMoved
                        )
                    {
                        int CheckIndexStartY = Math.Min(y, position.Item2);
                        int CheckIndexEndY = Math.Max(y, position.Item2);
                        bool pathClear = true;
                        for (int i = CheckIndexStartY + 1; i < CheckIndexEndY; i++)
                        {
                            if (chessBoard[x, i].ChessPieceType != ChessPieceType.None)
                            {
                                pathClear = false; // There is a piece in the way
                            }
                        }
                        if (!pathClear) continue;
                        viableRooks.Add((x, y));
                    }
                }
                foreach ((int x, int y) rookIndex in viableRooks)
                {
                    int difference = Math.Clamp(rookIndex.y - position.Item2, -2, 2);
                    int indexX = position.Item1;
                    int indexY = position.Item2 + difference;
                    ref ChessTile newTile = ref chessBoard[indexX, indexY];
                    if (ChessTile.IsTileOpen(currentTile, newTile))
                    {
                        returnValues.Add((indexX, indexY));
                    }
                }
                
            }


            for (int x = clampMinX; x <= clampMaxX; x++)
            {
                for (int y = clampMinY; y <= clampMaxY; y++)
                {
                    if (x == position.Item1 && y == position.Item2)
                    {
                        continue; // Skip the current tile
                    }
                    ref ChessTile newTile = ref chessBoard[x, y];
                    if (ChessTile.IsTileOpen(currentTile, newTile))
                    {
                        returnValues.Add((x, y));
                    }
                }
            }

            return returnValues;
        }
    }

}
